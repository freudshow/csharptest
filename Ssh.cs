using System;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;

namespace ConsoleApp1
{
    /// <summary>
    /// Simple SSH helper based on SSH.NET (Renci.SshNet).
    /// Provides connect, execute command, disconnect and background disconnect detection.
    /// </summary>
    public class SshHelper : IDisposable
    {
        private readonly SshClient _client;
        private readonly TimeSpan _keepAliveInterval;
        private readonly TimeSpan _keepAliveCommandTimeout;
        private CancellationTokenSource? _cts;
        private Task? _monitorTask;

        /// <summary>
        /// Raised when connection is detected lost (or explicit disconnect).
        /// The Exception parameter is non-null when the disconnect was caused by an error.
        /// </summary>
        public event Action<Exception?>? Disconnected;

        public bool IsConnected => _client?.IsConnected ?? false;

        /// <summary>
        /// Create SSH helper using password authentication.
        /// </summary>
        public SshHelper(string host, int port, string username, string password,
            TimeSpan? keepAliveInterval = null, TimeSpan? keepAliveCommandTimeout = null)
        {
            _client = new SshClient(host, port, username, password);
            _keepAliveInterval = keepAliveInterval ?? TimeSpan.FromSeconds(10);
            _keepAliveCommandTimeout = keepAliveCommandTimeout ?? TimeSpan.FromSeconds(5);
        }

        /// <summary>
        /// Create SSH helper using private key authentication.
        /// </summary>
        public SshHelper(string host, int port, string username, PrivateKeyFile keyFile,
            TimeSpan? keepAliveInterval = null, TimeSpan? keepAliveCommandTimeout = null)
        {
            var keyAuth = new PrivateKeyAuthenticationMethod(username, keyFile);
            // build a ConnectionInfo for the client
            var conn = new ConnectionInfo(host, port, username, keyAuth);
            _client = new SshClient(conn);
            _keepAliveInterval = keepAliveInterval ?? TimeSpan.FromSeconds(10);
            _keepAliveCommandTimeout = keepAliveCommandTimeout ?? TimeSpan.FromSeconds(5);
        }

        /// <summary>
        /// Connect to the remote host (synchronous). Starts background monitor that detects dead connections.
        /// </summary>
        public void Connect()
        {
            _client.Connect();
            StartMonitor();
        }

        /// <summary>
        /// Async connect wrapper.
        /// </summary>
        public Task ConnectAsync(CancellationToken cancellation = default)
        {
            return Task.Run(() =>
            {
                cancellation.ThrowIfCancellationRequested();
                Connect();
            }, cancellation);
        }

        /// <summary>
        /// Executes a command on the remote host and returns stdout. Throws on timeout or connection error.
        /// </summary>
        public async Task<string> RunCommandAsync(string command, TimeSpan? timeout = null)
        {
            if (!_client.IsConnected)
                throw new InvalidOperationException("SSH client is not connected");

            var cmd = _client.CreateCommand(command);
            var cmdTimeout = timeout ?? TimeSpan.FromSeconds(30);
            cmd.CommandTimeout = cmdTimeout;

            try
            {
                // Execute in Task.Run so we can observe timeouts/cancellation
                var execTask = Task.Run(() => cmd.Execute());

                var completed = await Task.WhenAny(execTask, Task.Delay(cmdTimeout));
                if (completed != execTask)
                {
                    // timeout
                    try { cmd.CancelAsync(); } catch { }
                    throw new TimeoutException($"Command '{command}' timed out after {cmdTimeout}");
                }

                // check for errors
                if (!string.IsNullOrEmpty(cmd.Error))
                {
                    // still return stdout but include stderr via exception
                    throw new InvalidOperationException($"Command produced error: {cmd.Error}");
                }

                return cmd.Result ?? string.Empty;
            }
            catch (Exception ex)
            {
                // If command failed due to connection problem, raise Disconnected
                if (!_client.IsConnected)
                {
                    OnDisconnected(ex);
                }
                throw;
            }
        }

        /// <summary>
        /// Disconnect gracefully and stop monitor.
        /// </summary>
        public void Disconnect()
        {
            StopMonitor();
            try
            {
                if (_client.IsConnected)
                    _client.Disconnect();
            }
            finally
            {
                OnDisconnected(null);
            }
        }

        private void StartMonitor()
        {
            StopMonitor();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _monitorTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(_keepAliveInterval, token).ConfigureAwait(false);
                        if (token.IsCancellationRequested) break;

                        if (!_client.IsConnected)
                        {
                            OnDisconnected(null);
                            break;
                        }

                        // perform lightweight keep-alive by executing a short command with short timeout
                        try
                        {
                            var keep = _client.CreateCommand("echo 1");
                            keep.CommandTimeout = _keepAliveCommandTimeout;
                            var t = Task.Run(() => keep.Execute());
                            var completed = await Task.WhenAny(t, Task.Delay(_keepAliveCommandTimeout, token)).ConfigureAwait(false);
                            if (completed != t)
                            {
                                // timeout -> treat as connection problem
                                OnDisconnected(new TimeoutException("Keepalive command timed out"));
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            // an exception here likely means the TCP connection died silently or channel failure
                            OnDisconnected(ex);
                            break;
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        OnDisconnected(ex);
                        break;
                    }
                }
            }, token);
        }

        private void StopMonitor()
        {
            try
            {
                if (_cts != null && !_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                }
                _monitorTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch { }
            finally
            {
                _monitorTask = null;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void OnDisconnected(Exception? ex)
        {
            try
            {
                Disconnected?.Invoke(ex);
            }
            catch { }
        }

        public void Dispose()
        {
            StopMonitor();
            try { _client.Dispose(); } catch { }
        }
    }
}