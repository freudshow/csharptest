// ExprCalc.cs
// dotnet run or csc ExprCalc.cs

using System;
using System.Collections.Generic;
using System.Globalization;

internal class EvalProgram
{
    private enum TokenType
    {
        NUM, HASH, IDENT,
        PLUS, MINUS, MUL, DIV,
        LP, RP,
        NOT, ANDAND, OROR,
        GT, GTE, LT, LTE, EQ,
        AMP, PIPE, CARET, TILDE,
        LSHIFT, RSHIFT,
        ASSIGN,
        EOF, INVALID
    }

    private class Token
    {
        public TokenType Type;
        public string Text;
        public double Num;

        public Token(TokenType t, string text = null)
        {
            Type = t;
            Text = text;
        }

        public override string ToString() => Text ?? Type.ToString();
    }

    private class Tokenizer
    {
        private readonly string s; private int i, n;

        public Tokenizer(string input)
        {
            s = input ?? "";
            i = 0;
            n = s.Length;
        }

        private void Skip()
        {
            while (i < n && char.IsWhiteSpace(s[i]))
                i++;
        }

        public List<Token> Tokenize()
        {
            var list = new List<Token>();
            while (true)
            {
                Skip();
                if (i >= n) { list.Add(new Token(TokenType.EOF)); break; }
                char c = s[i];
                if (c == '&' && i + 1 < n && s[i + 1] == '&') { list.Add(new Token(TokenType.ANDAND, "&&")); i += 2; continue; }
                if (c == '|' && i + 1 < n && s[i + 1] == '|') { list.Add(new Token(TokenType.OROR, "||")); i += 2; continue; }
                if (c == '<' && i + 1 < n && s[i + 1] == '<') { list.Add(new Token(TokenType.LSHIFT, "<<")); i += 2; continue; }
                if (c == '>' && i + 1 < n && s[i + 1] == '>') { list.Add(new Token(TokenType.RSHIFT, ">>")); i += 2; continue; }
                if (c == '>' && i + 1 < n && s[i + 1] == '=') { list.Add(new Token(TokenType.GTE, ">=")); i += 2; continue; }
                if (c == '<' && i + 1 < n && s[i + 1] == '=') { list.Add(new Token(TokenType.LTE, "<=")); i += 2; continue; }
                if (c == '=' && i + 1 < n && s[i + 1] == '=') { list.Add(new Token(TokenType.EQ, "==")); i += 2; continue; }
                if (char.IsDigit(c) || c == '.')
                {
                    int start = i;
                    while (i < n && (char.IsDigit(s[i]) || s[i] == '.')) i++;
                    string sub = s.Substring(start, i - start);
                    if (!double.TryParse(sub, NumberStyles.Float, CultureInfo.InvariantCulture, out double val)) throw new Exception("Invalid number: " + sub);
                    var tk = new Token(TokenType.NUM, sub); tk.Num = val; list.Add(tk); continue;
                }
                if (c == '#')
                {
                    i++;
                    int start = i;
                    while (i < n && char.IsDigit(s[i])) i++;
                    if (start == i) throw new Exception("Invalid # marker");
                    string id = s.Substring(start, i - start);
                    list.Add(new Token(TokenType.HASH, id));
                    continue;
                }
                if (char.IsLetter(c))
                {
                    int start = i;
                    while (i < n && char.IsLetter(s[i])) i++;
                    string id = s.Substring(start, i - start);
                    list.Add(new Token(TokenType.IDENT, id));
                    continue;
                }
                switch (c)
                {
                    case '+': list.Add(new Token(TokenType.PLUS, "+")); i++; break;
                    case '-': list.Add(new Token(TokenType.MINUS, "-")); i++; break;
                    case '*': list.Add(new Token(TokenType.MUL, "*")); i++; break;
                    case '/': list.Add(new Token(TokenType.DIV, "/")); i++; break;
                    case '(': list.Add(new Token(TokenType.LP, "(")); i++; break;
                    case ')': list.Add(new Token(TokenType.RP, ")")); i++; break;
                    case '!': list.Add(new Token(TokenType.NOT, "!")); i++; break;
                    case '>': list.Add(new Token(TokenType.GT, ">")); i++; break;
                    case '<': list.Add(new Token(TokenType.LT, "<")); i++; break;
                    case '&': list.Add(new Token(TokenType.AMP, "&")); i++; break;
                    case '|': list.Add(new Token(TokenType.PIPE, "|")); i++; break;
                    case '^': list.Add(new Token(TokenType.CARET, "^")); i++; break;
                    case '~': list.Add(new Token(TokenType.TILDE, "~")); i++; break;
                    case '=': list.Add(new Token(TokenType.ASSIGN, "=")); i++; break;
                    default: throw new Exception($"Invalid character: {c}");
                }
            }
            return list;
        }
    }

    private class Parser
    {
        private List<Token> toks; private int pos;
        private Dictionary<int, double> rt;

        public Parser(List<Token> tokens, Dictionary<int, double> rtmap = null)
        { toks = tokens; pos = 0; rt = rtmap ?? new Dictionary<int, double>(); }

        private Token Peek() => pos < toks.Count ? toks[pos] : new Token(TokenType.EOF);

        private Token Next() => pos < toks.Count ? toks[pos++] : new Token(TokenType.EOF);

        private bool Match(TokenType t)
        { if (Peek().Type == t) { Next(); return true; } return false; }

        // top-level: if starts with HASH and next ASSIGN => assignment statement
        public (bool isAssignment, int id, double value) ParseStatement()
        {
            if (Peek().Type == TokenType.HASH && pos + 1 < toks.Count && toks[pos + 1].Type == TokenType.ASSIGN)
            {
                var h = Next(); Next(); // consume HASH and ASSIGN
                double val = ParseExpression();
                if (Peek().Type != TokenType.EOF) throw new Exception("Unexpected token after assignment");
                int id = int.Parse(h.Text);
                rt[id] = val;
                return (true, id, val);
            }
            else
            {
                double v = ParseExpression();
                if (Peek().Type != TokenType.EOF) throw new Exception("Unexpected token");
                return (false, 0, v);
            }
        }

        // precedence: logical OR -> AND -> bitwise OR -> XOR -> AND -> equality -> relational -> shift -> add -> mul -> unary -> primary
        public double ParseExpression() => ParseLogicalOr();

        private double ParseLogicalOr()
        {
            double left = ParseLogicalAnd();
            while (Match(TokenType.OROR))
            {
                if (left != 0.0) { ParseLogicalAnd(); left = 1.0; }
                else { double r = ParseLogicalAnd(); left = r != 0.0 ? 1.0 : 0.0; }
            }
            return left;
        }

        private double ParseLogicalAnd()
        {
            double left = ParseBitwiseOr();
            while (Match(TokenType.ANDAND))
            {
                if (left == 0.0) { ParseBitwiseOr(); left = 0.0; }
                else { double r = ParseBitwiseOr(); left = r != 0.0 ? 1.0 : 0.0; }
            }
            return left;
        }

        private double ParseBitwiseOr()
        {
            double left = ParseBitwiseXor();
            while (Match(TokenType.PIPE)) { double r = ParseBitwiseXor(); left = (double)((long)left | (long)r); }
            return left;
        }

        private double ParseBitwiseXor()
        {
            double left = ParseBitwiseAnd();
            while (Match(TokenType.CARET)) { double r = ParseBitwiseAnd(); left = (double)((long)left ^ (long)r); }
            return left;
        }

        private double ParseBitwiseAnd()
        {
            double left = ParseEquality();
            while (Match(TokenType.AMP)) { double r = ParseEquality(); left = (double)((long)left & (long)r); }
            return left;
        }

        private double ParseEquality()
        {
            double left = ParseRelational();
            while (Match(TokenType.EQ)) { double r = ParseRelational(); left = left == r ? 1.0 : 0.0; }
            return left;
        }

        private double ParseRelational()
        {
            double left = ParseShift();
            while (true)
            {
                if (Match(TokenType.GT)) { double r = ParseShift(); left = left > r ? 1.0 : 0.0; }
                else if (Match(TokenType.GTE)) { double r = ParseShift(); left = left >= r ? 1.0 : 0.0; }
                else if (Match(TokenType.LT)) { double r = ParseShift(); left = left < r ? 1.0 : 0.0; }
                else if (Match(TokenType.LTE)) { double r = ParseShift(); left = left <= r ? 1.0 : 0.0; }
                else break;
            }
            return left;
        }

        private double ParseShift()
        {
            double left = ParseAdd();
            while (true)
            {
                if (Match(TokenType.LSHIFT)) { double r = ParseAdd(); left = (double)((long)left << (int)r); }
                else if (Match(TokenType.RSHIFT)) { double r = ParseAdd(); left = (double)((long)left >> (int)r); }
                else break;
            }
            return left;
        }

        private double ParseAdd()
        {
            double left = ParseMul();
            while (true)
            {
                if (Match(TokenType.PLUS)) { double r = ParseMul(); left += r; }
                else if (Match(TokenType.MINUS)) { double r = ParseMul(); left -= r; }
                else break;
            }
            return left;
        }

        private double ParseMul()
        {
            double left = ParseUnary();
            while (true)
            {
                if (Match(TokenType.MUL)) { double r = ParseUnary(); left *= r; }
                else if (Match(TokenType.DIV)) { double r = ParseUnary(); if (r == 0.0) throw new Exception("Division by zero"); left /= r; }
                else break;
            }
            return left;
        }

        private double ParseUnary()
        {
            if (Match(TokenType.NOT)) { double v = ParseUnary(); return v != 0.0 ? 0.0 : 1.0; }
            if (Match(TokenType.TILDE)) { double v = ParseUnary(); return (double)(~((long)v)); }
            if (Match(TokenType.MINUS)) { double v = ParseUnary(); return -v; }
            return ParsePrimary();
        }

        private double ParsePrimary()
        {
            var t = Peek();
            if (Match(TokenType.NUM)) return t.Num;
            if (Match(TokenType.HASH))
            {
                int id = int.Parse(t.Text);
                return rt.TryGetValue(id, out double vv) ? vv : 0.0;
            }
            if (Match(TokenType.IDENT))
            {
                string name = t.Text;
                if (!Match(TokenType.LP)) throw new Exception("Expected ( after function");
                double arg = ParseExpression();
                if (!Match(TokenType.RP)) throw new Exception("Expected ) after function");
                return name switch
                {
                    "sin" => Math.Sin(arg),
                    "cos" => Math.Cos(arg),
                    "exp" => Math.Exp(arg),
                    _ => throw new Exception("Unknown function: " + name)
                };
            }
            if (Match(TokenType.LP))
            {
                double v = ParseExpression();
                if (!Match(TokenType.RP)) throw new Exception("Expected )");
                return v;
            }
            throw new Exception("Unexpected token: " + t);
        }
    }

    private static void Main()
    {
        var rtmap = new Dictionary<int, double>();
        Console.WriteLine("Enter expression (empty to quit):");
        while (true)
        {
            string line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) break;
            try
            {
                var tokenizer = new Tokenizer(line);
                var toks = tokenizer.Tokenize();
                var parser = new Parser(toks, rtmap);
                var stmt = parser.ParseStatement();
                if (stmt.isAssignment)
                {
                    Console.WriteLine($"Assigned #{stmt.id} = {stmt.value.ToString(CultureInfo.InvariantCulture)}");
                }
                else
                {
                    Console.WriteLine("Result: " + stmt.value.ToString(CultureInfo.InvariantCulture));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            Console.WriteLine("Enter expression (empty to quit):");
        }
    }
}