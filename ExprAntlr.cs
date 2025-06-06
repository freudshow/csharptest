using Antlr4.Runtime;
using Antlr4.Runtime.Tree; // 添加此命名空间以解决 IParseTree 未找到的问题

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
                        TreePrinter.Print(tree, parser);
                    }
                    else
                    {
                        Console.WriteLine("非法表达式: " + string.Join("; ", errors));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"异常: {ex.Message}");
                }
            }
        }

        public class TreePrinter
        {
            private static string GetNodeText(IParseTree node, Parser parser)
            {
                if (node is TerminalNodeImpl terminal)
                {
                    return terminal.GetText();
                }
                else
                {
                    var ruleContext = node as ParserRuleContext;
                    var ruleName = parser.RuleNames[ruleContext.RuleIndex];
                    return ruleName;
                }
            }

            public static void Print(IParseTree tree, Parser parser)
            {
                PrintTree(tree, parser, "", true);
            }

            private static void PrintTree(IParseTree node, Parser parser, string indent, bool last)
            {
                // 打印当前节点
                Console.Write(indent);
                Console.Write(last ? "└─ " : "├─ ");
                Console.WriteLine(GetNodeText(node, parser));

                // 为子节点准备缩进
                indent += last ? "   " : "│  ";

                // 递归打印子节点
                for (int i = 0; i < node.ChildCount; i++)
                {
                    var child = node.GetChild(i);
                    PrintTree(child, parser, indent, i == node.ChildCount - 1);
                }
            }
        }
    }
}