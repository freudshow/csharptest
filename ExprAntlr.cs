using Antlr4.Runtime;

namespace ExprAntlr
{
    internal class ErrorListener : BaseErrorListener
    {
        private readonly List<string> _errors;

        public ErrorListener(List<string> errors) => _errors = errors;

        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            _errors.Add($"line {line}:{charPositionInLine} {msg}");
        }
    }

    internal class ExprAntlr
    {
        private static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    Console.WriteLine("Enter an expression (or type 'exit' to quit) >");
                    var input = Console.ReadLine();
                    if (input == "exit")
                    {
                        break;
                    }

                    var inputStream = new AntlrInputStream(input);
                    var lexer = new ExprLexer(inputStream);
                    var tokenStream = new CommonTokenStream(lexer);
                    var parser = new ExprParser(tokenStream);

                    // 自定义错误监听器
                    var errors = new List<string>();
                    parser.RemoveErrorListeners();
                    parser.AddErrorListener(new ErrorListener(errors));

                    var tree = parser.prog();

                    //errors.Count == 0, 表示在解析过程中没有收集到任何语法错误
                    //tokenStream.LA(1) == TokenConstants.EOF, 表示输入流中没有更多的字符
                    if (errors.Count == 0 && tokenStream.LA(1) == TokenConstants.EOF)
                    {
                        Console.WriteLine("合法表达式");
                    }
                    else
                    {
                        Console.WriteLine("非法表达式: " + string.Join("; ", errors));
                    }

                    //Console.WriteLine(tree.ToStringTree(parser));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"异常: {ex.Message}");
                }
            }
        }
    }
}