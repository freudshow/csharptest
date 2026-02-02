using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Rendering;
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
        private int _disconnectedSignaled = 0;

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
            // reset disconnected signal in case helper is reused
            System.Threading.Interlocked.Exchange(ref _disconnectedSignaled, 0);
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
            // Ensure we only signal disconnection once
            if (System.Threading.Interlocked.Exchange(ref _disconnectedSignaled, 1) == 1)
                return;

            try
            {
                // stop background monitor to avoid repeated attempts
                StopMonitor();

                // attempt to close underlying client gracefully if still connected
                try { if (_client.IsConnected) _client.Disconnect(); } catch { }

                // Raise event after we've attempted to quiesce
                Disconnected?.Invoke(ex);
            }
            catch { }
        }

        public void Dispose()
        {
            StopMonitor();
            try { _client.Dispose(); } catch { }
        }

        private static async Task<int> Main(string[] args)
        {
            try
            {
                Console.Write("Host: ");
                var host = Console.ReadLine()?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(host))
                {
                    Console.WriteLine("Host required");
                    return 1;
                }

                Console.Write("Port (default 22): ");
                var portStr = Console.ReadLine();
                var port = 22;
                if (!string.IsNullOrEmpty(portStr) && !int.TryParse(portStr, out port))
                {
                    Console.WriteLine("Invalid port");
                    return 1;
                }

                Console.Write("Username: ");
                var user = Console.ReadLine()?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(user))
                {
                    Console.WriteLine("Username required");
                    return 1;
                }

                Console.Write("Auth method ([p]assword/[k]ey, default p): ");
                var method = Console.ReadLine()?.Trim().ToLowerInvariant();
                SshHelper? helper = null;

                TimeSpan? keepAliveInterval = TimeSpan.FromSeconds(10);
                TimeSpan? keepAliveCommandTimeout = TimeSpan.FromSeconds(5);

                if (method == "k")
                {
                    Console.Write("Private key path: ");
                    var keyPath = Console.ReadLine()?.Trim() ?? string.Empty;
                    if (string.IsNullOrEmpty(keyPath))
                    {
                        Console.WriteLine("Key path required");
                        return 1;
                    }

                    Console.Write("Key passphrase (enter for none): ");
                    var pass = ReadPassword();
                    PrivateKeyFile keyFile;
                    try
                    {
                        if (string.IsNullOrEmpty(pass))
                            keyFile = new PrivateKeyFile(keyPath);
                        else
                            keyFile = new PrivateKeyFile(keyPath, pass);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to load key: {ex.Message}");
                        return 1;
                    }

                    helper = new SshHelper(host, port, user, keyFile, keepAliveInterval, keepAliveCommandTimeout);
                }
                else
                {
                    Console.Write("Password: ");
                    var pwd = ReadPassword();
                    helper = new SshHelper(host, port, user, pwd, keepAliveInterval, keepAliveCommandTimeout);
                }

                var disconnected = new TaskCompletionSource<Exception?>();
                helper.Disconnected += (ex) =>
                {
                    Console.WriteLine($"[{DateTime.Now}] Disconnected: {ex?.Message ?? "closed"}");
                    disconnected.TrySetResult(ex);
                };

                // connect
                Console.WriteLine("Connecting...");
                await helper.ConnectAsync();
                Console.WriteLine("Connected.");

                // interactive REPL loop
                while (helper.IsConnected)
                {
                    Console.Write("> ");
                    var line = Console.ReadLine();
                    if (line == null) break;
                    line = line.Trim();
                    if (string.Equals(line, "exit", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                    if (line.Length == 0) continue;

                    try
                    {
                        var output = await helper.RunCommandAsync(line, TimeSpan.FromSeconds(30));
                        if (!string.IsNullOrEmpty(output))
                            Console.WriteLine(output);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Command error: {ex.Message}");
                        // if disconnected, break out
                        if (!helper.IsConnected) break;
                    }
                }

                // disconnect and cleanup
                helper.Disconnect();
                helper.Dispose();

                // wait for disconnect
                await disconnected.Task;

                Console.WriteLine("Disconnected. Press any key to exit.");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }

            return 0;
        }

        private static string ReadPassword()
        {
            var pwd = string.Empty;
            ConsoleKeyInfo key;
            while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd = pwd[0..^1];
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    pwd += key.KeyChar;
                    Console.Write('*');
                }
            }
            Console.WriteLine();
            return pwd;
        }
    }
}