using System;
using System.Collections.Generic;
using System.Globalization;

namespace ArithmeticEvaluator
{
    // 词法符号类型
    public enum ArithmeticTokenType
    {
        NUMBER,
        PLUS,
        MINUS,
        MULTIPLY,
        DIVIDE,
        LPAREN,
        RPAREN,
        EOF
    }

    // 词法符号
    public class ArithmeticToken
    {
        public ArithmeticTokenType Type { get; }
        public string Value { get; }

        public ArithmeticToken(ArithmeticTokenType type, string value = null)
        {
            Type = type;
            Value = value;
        }

        public override string ToString() => Value is null ? Type.ToString() : $"{Type}({Value})";
    }

    // 将输入分词
    public class Tokenizer
    {
        private readonly string input;
        private int pos = 0;

        public Tokenizer(string input)
        {
            this.input = input ?? string.Empty;
        }

        public List<ArithmeticToken> Tokenize()
        {
            List<ArithmeticToken> tokens = new List<ArithmeticToken>();
            while (pos < input.Length)
            {
                char c = input[pos];
                if (char.IsWhiteSpace(c))
                {
                    pos++;
                    continue;
                }
                if (char.IsDigit(c) || c == '.')
                {
                    string number = ParseNumber();
                    tokens.Add(new ArithmeticToken(ArithmeticTokenType.NUMBER, number));
                }
                else if (c == '+')
                {
                    tokens.Add(new ArithmeticToken(ArithmeticTokenType.PLUS));
                    pos++;
                }
                else if (c == '-')
                {
                    tokens.Add(new ArithmeticToken(ArithmeticTokenType.MINUS));
                    pos++;
                }
                else if (c == '*')
                {
                    tokens.Add(new ArithmeticToken(ArithmeticTokenType.MULTIPLY));
                    pos++;
                }
                else if (c == '/')
                {
                    tokens.Add(new ArithmeticToken(ArithmeticTokenType.DIVIDE));
                    pos++;
                }
                else if (c == '(')
                {
                    tokens.Add(new ArithmeticToken(ArithmeticTokenType.LPAREN));
                    pos++;
                }
                else if (c == ')')
                {
                    tokens.Add(new ArithmeticToken(ArithmeticTokenType.RPAREN));
                    pos++;
                }
                else
                {
                    throw new Exception("Invalid character: " + ShowChar(c));
                }
            }
            tokens.Add(new ArithmeticToken(ArithmeticTokenType.EOF));
            return tokens;
        }

        // 把不可见或非 ASCII 字符以 \uXXXX 形式显示，便于定位
        private static string ShowChar(char c)
        {
            if (c < 32 || c > 126) return $"\\u{(int)c:X4}";
            return c.ToString();
        }

        private string ParseNumber()
        {
            int start = pos;
            bool hasDot = false;
            while (pos < input.Length)
            {
                char c = input[pos];
                if (char.IsDigit(c))
                {
                    pos++;
                }
                else if (c == '.' && !hasDot)
                {
                    // ensure '.' is followed by a digit (but allow leading '.' as in .5)
                    if (pos + 1 < input.Length && char.IsDigit(input[pos + 1]))
                    {
                        hasDot = true;
                        pos++;
                    }
                    else
                    {
                        // single '.' not part of a valid number
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            if (start == pos)
            {
                throw new Exception("Expected number");
            }
            string number = input.Substring(start, pos - start);
            // Validate number more strictly using TryParse
            if (!double.TryParse(number, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _))
            {
                throw new Exception("Invalid number format: " + number);
            }
            return number;
        }
    }

    // AST 节点定义
    public abstract class ExprNode
    {
        public abstract double Evaluate();
    }

    public sealed class NumberNode : ExprNode
    {
        public double Value { get; }

        public NumberNode(double value) => Value = value;

        public override double Evaluate() => Value;
    }

    public enum UnaryOp
    { Negate }

    public sealed class UnaryNode : ExprNode
    {
        public UnaryOp Op { get; }
        public ExprNode Operand { get; }

        public UnaryNode(UnaryOp op, ExprNode operand)
        { Op = op; Operand = operand; }

        public override double Evaluate()
        {
            return Op switch
            {
                UnaryOp.Negate => -Operand.Evaluate(),
                _ => throw new InvalidOperationException("Unknown unary operator")
            };
        }
    }

    public enum BinaryOp
    { Add, Subtract, Multiply, Divide }

    public sealed class BinaryNode : ExprNode
    {
        public ExprNode Left { get; }
        public ExprNode Right { get; }
        public BinaryOp Op { get; }

        public BinaryNode(ExprNode left, BinaryOp op, ExprNode right)
        { Left = left; Op = op; Right = right; }

        public override double Evaluate()
        {
            double l = Left.Evaluate();
            double r = Right.Evaluate();
            return Op switch
            {
                BinaryOp.Add => l + r,
                BinaryOp.Subtract => l - r,
                BinaryOp.Multiply => l * r,
                BinaryOp.Divide => r == 0 ? throw new DivideByZeroException() : l / r,
                _ => throw new InvalidOperationException("Unknown binary operator")
            };
        }
    }

    // 解析器：生成 AST（遵守运算符优先级）
    public class ArithmeticParser
    {
        private readonly List<ArithmeticToken> tokens;
        private int pos = 0;

        public ArithmeticParser(List<ArithmeticToken> tokens)
        {
            this.tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        }

        // Parse 返回根节点（若语法错误抛出异常）
        public ExprNode Parse()
        {
            ExprNode root = ParseExpression();
            if (Current.Type != ArithmeticTokenType.EOF)
                throw new Exception($"Unexpected token: {Current}");
            return root;
        }

        // expression := term (('+'|'-') term)*
        private ExprNode ParseExpression()
        {
            ExprNode left = ParseTerm();
            while (Current.Type == ArithmeticTokenType.PLUS || Current.Type == ArithmeticTokenType.MINUS)
            {
                var op = Current.Type;
                Consume(op);
                ExprNode right = ParseTerm();
                left = new BinaryNode(left, op == ArithmeticTokenType.PLUS ? BinaryOp.Add : BinaryOp.Subtract, right);
            }
            return left;
        }

        // term := factor (('*'|'/') factor)*
        private ExprNode ParseTerm()
        {
            ExprNode left = ParseFactor();
            while (Current.Type == ArithmeticTokenType.MULTIPLY || Current.Type == ArithmeticTokenType.DIVIDE)
            {
                var op = Current.Type;
                Consume(op);
                ExprNode right = ParseFactor();
                left = new BinaryNode(left, op == ArithmeticTokenType.MULTIPLY ? BinaryOp.Multiply : BinaryOp.Divide, right);
            }
            return left;
        }

        // factor := '-' factor | NUMBER | '(' expression ')'
        private ExprNode ParseFactor()
        {
            if (Current.Type == ArithmeticTokenType.MINUS)
            {
                Consume(ArithmeticTokenType.MINUS);
                ExprNode operand = ParseFactor();
                return new UnaryNode(UnaryOp.Negate, operand);
            }
            else if (Current.Type == ArithmeticTokenType.NUMBER)
            {
                string text = Current.Value;
                Consume(ArithmeticTokenType.NUMBER);
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                    throw new Exception("Invalid number: " + text);
                return new NumberNode(value);
            }
            else if (Current.Type == ArithmeticTokenType.LPAREN)
            {
                Consume(ArithmeticTokenType.LPAREN);
                ExprNode node = ParseExpression();
                Consume(ArithmeticTokenType.RPAREN);
                return node;
            }
            else
            {
                throw new Exception("Unexpected token: " + Current);
            }
        }

        private ArithmeticToken Current => pos < tokens.Count ? tokens[pos] : tokens[^1];

        private void Consume(ArithmeticTokenType expected)
        {
            if (Current.Type == expected)
            {
                pos++;
                return;
            }
            throw new Exception($"Expected {expected}, got {Current}");
        }
    }

    // AST 打印工具（格式化输出）
    public static class AstPrinter
    {
        public static void Print(ExprNode node)
        {
            PrintNode(node, "", true);
        }

        private static void PrintNode(ExprNode node, string indent, bool last)
        {
            Console.Write(indent);
            Console.Write(last ? "└─ " : "├─ ");
            switch (node)
            {
                case NumberNode n:
                    Console.WriteLine(n.Value.ToString(CultureInfo.InvariantCulture));
                    break;

                case UnaryNode u:
                    Console.WriteLine($"Unary({u.Op})");
                    PrintNode(u.Operand, indent + (last ? "   " : "│  "), true);
                    break;

                case BinaryNode b:
                    Console.WriteLine($"Binary({b.Op})");
                    PrintNode(b.Left, indent + (last ? "   " : "│  "), false);
                    PrintNode(b.Right, indent + (last ? "   " : "│  "), true);
                    break;

                default:
                    Console.WriteLine(node.GetType().Name);
                    break;
            }
        }
    }

    // 控制台应用入口：读入、词法、解析、打印语法树并求值
    internal static class ArithmeticApps
    {
        private static void ArithmeticMain(string[] args)
        {
            Console.WriteLine("Enter an arithmetic expression (e.g., 2 + 3 * (4 - 1)):");
            string input = Console.ReadLine();
            try
            {
                Tokenizer tokenizer = new Tokenizer(input);
                List<ArithmeticToken> tokens = tokenizer.Tokenize();

                ArithmeticParser parser = new ArithmeticParser(tokens);
                ExprNode root = parser.Parse();

                Console.WriteLine("Parsed AST:");
                AstPrinter.Print(root);

                double result = root.Evaluate();
                Console.WriteLine("Result: " + result.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            // pause for console
            Console.ReadLine();
        }
    }
}