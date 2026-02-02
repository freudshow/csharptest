/******************************************************************************************************************
 * grammar eval.g4;
 * ----------------------------------------------------------------------------------------------------------------
 * # Top-level
 * prog : stmt EOF ;
 * stmt : assignStmt | expr ;
 * assignStmt : HASH ASSIGN expr ;
 * ----------------------------------------------------------------------------------------------------------------
 * # Expression entry
 * expr : assignExpr ;
 *
 * # Priority 1 (lowest): assignment, right-associative
 * assignExpr
 *     : logicalOr ( ASSIGN assignExpr )?   # priority 1, right-assoc
 *     ;
 *
 * # Priority 2: || (left-assoc)
 * logicalOr
 *     : logicalAnd ( OROR logicalAnd )*
 *     ;
 *
 * # Priority 3: && (left-assoc)
 * logicalAnd
 *     : bitOr ( ANDAND bitOr )*
 *     ;
 *
 * # Priority 4: bitwise OR '|' (left-assoc)
 * bitOr
 *     : bitXor ( PIPE bitXor )*
 *     ;
 *
 * # Priority 5: bitwise XOR '^' (left-assoc)
 * bitXor
 *     : bitAnd ( CARET bitAnd )*
 *     ;
 *
 * # Priority 6: bitwise AND '&' (left-assoc)
 * bitAnd
 *     : equality ( AMP equality )*
 *     ;
 *
 * # Priority 7: equality '==' '!=' (left-assoc)
 * equality
 *     : relational ( (EQ | NEQ) relational )*
 *     ;
 *
 * # Priority 8: relational < <= > >= (left-assoc)
 * relational
 *     : shift ( (LT | LTE | GT | GTE) shift )*
 *     ;
 *
 * # Priority 9: shifts << >> (left-assoc)
 * shift
 *     : add ( (LSHIFT | RSHIFT) add )*
 *     ;
 *
 * # Priority 10: addition/subtraction + - (left-assoc)
 * add
 *     : mul ( (PLUS | MINUS) mul )*
 *     ;
 *
 * # Priority 11: multiply/divide * / (left-assoc)
 * mul
 *     : unary ( (MULT | DIV) unary )*
 *     ;
 *
 * # Priority 12: unary: ~, !, - (right-assoc)
 * unary
 *     : ( TILDE | NOT | MINUS ) unary
 *     | power
 *     ;
 *
 * # Priority 13: sin, cos function calls (tighter than unary)
 * # handled as primary forms below
 *
 * # Priority 14 (highest): exp function call (tightest)
 * # handled as primary form below
 *
 * # power/primary level (functions and atoms)
 * power
 *     : expFunc                 # ExpFunction
 *     | sinCosFunc              # SinCosFunction
 *     | primary                 # AtomPrimary
 *     ;
 *
 * # function productions
 * expFunc
 *     : EXP LP expr RP          # 'exp(expr)' ¡ª priority 14 (highest)
 *     ;
 *
 * sinCosFunc
 *     : ( SIN | COS ) LP expr RP  # 'sin(expr)' or 'cos(expr)' ¡ª priority 13
 *     ;
 *
 * # primary atoms
 * primary
 *     : NUMBER
 *     | HASH                     # realtime marker '#123'
 *     | LP expr RP
 *     ;
 *
 * # Lexer tokens (representative)
 * PLUS    : '+' ;
 * MINUS   : '-' ;
 * MULT    : '*' ;
 * DIV     : '/' ;
 * NOT     : '!' ;
 * ANDAND  : '&&' ;
 * OROR    : '||' ;
 * GT      : '>' ;
 * GTE     : '>=' ;
 * LT      : '<' ;
 * LTE     : '<=' ;
 * EQ      : '==' ;
 * NEQ     : '!=' ;
 * AMP     : '&' ;
 * PIPE    : '|' ;
 * CARET   : '^' ;
 * TILDE   : '~' ;
 * LSHIFT  : '<<' ;
 * RSHIFT  : '>>' ;
 * LP      : '(' ;
 * RP      : ')' ;
 * ASSIGN  : '=' ;
 *
 * # functions and identifiers
 * SIN     : 'sin' ;
 * COS     : 'cos' ;
 * EXP     : 'exp' ;
 * NUMBER  : [0-9]+ ('.' [0-9]*)? | '.' [0-9]+ ;
 * HASH    : '#' [0-9]+ ;
 * IDENT   : [a-zA-Z]+ ;
 *
 * # whitespace & error
 * WS      : [ \t\r\n]+ -> skip ;
 * ERROR_CHAR : . -> channel(HIDDEN) ;
 ****************************************************************************************************************/

using System;
using System.Collections.Generic;
using System.Globalization;

internal class EvaluateProgram
{
    private enum TokenType
    {
        NUM, HASH, IDENT,
        PLUS, MINUS, MUL, DIV,
        LP, RP,
        NOT, NEQ, ANDAND, OROR,
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
        public int Pos; // start index in input
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
                if (c == '!' && i + 1 < n && s[i + 1] == '=') { list.Add(new Token(TokenType.NEQ, "!=")); i += 2; continue; }
                if (c == '=' && i + 1 < n && s[i + 1] == '=') { list.Add(new Token(TokenType.EQ, "==")); i += 2; continue; }
                if (char.IsDigit(c) || c == '.')
                {
                    int start = i;
                    while (i < n && (char.IsDigit(s[i]) || s[i] == '.')) i++;
                    string sub = s.Substring(start, i - start);
                    if (!double.TryParse(sub, NumberStyles.Float, CultureInfo.InvariantCulture, out double val)) throw new Exception("Invalid number: " + sub);
                    var tk = new Token(TokenType.NUM, sub); tk.Num = val; tk.Pos = start; list.Add(tk); continue;
                }

                if (c == '#')
                {
                    i++;
                    int start = i;
                    while (i < n && char.IsDigit(s[i])) i++;
                    if (start == i) throw new Exception("Invalid # marker");
                    string id = s.Substring(start, i - start);
                    var th = new Token(TokenType.HASH, id); th.Pos = start - 1; list.Add(th);
                    continue;
                }

                if (char.IsLetter(c))
                {
                    int start = i;
                    while (i < n && char.IsLetter(s[i])) i++;
                    string id = s.Substring(start, i - start);
                    var ti = new Token(TokenType.IDENT, id); ti.Pos = start; list.Add(ti);
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
        private bool lastAssignment;
        private int lastAssignmentId;

        // AST node types
        private abstract class ExprNode
        {
            public int Pos;

            public abstract double Eval(Dictionary<int, double> rt);

            public abstract void Print(String indent, bool last);
        }

        private class NumberNode : ExprNode
        {
            public double Value;

            public NumberNode(double v, int pos)
            { Value = v; Pos = pos; }

            public override double Eval(Dictionary<int, double> rt) => Value;

            public override void Print(string indent, bool last)
            {
                Console.Write(indent);
                Console.Write(last ? "©¸©¤ " : "©À©¤ ");
                Console.WriteLine(Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        private class HashNode : ExprNode
        {
            public int Id;

            public HashNode(int id, int pos)
            { Id = id; Pos = pos; }

            public override double Eval(Dictionary<int, double> rt) => rt.TryGetValue(Id, out double v) ? v : 0.0;

            public override void Print(string indent, bool last)
            {
                Console.Write(indent);
                Console.Write(last ? "©¸©¤ " : "©À©¤ ");
                Console.WriteLine("#" + Id);
            }
        }

        private enum UnaryOp
        { Negate, Not, BitNot }

        private class UnaryNode : ExprNode
        {
            public UnaryOp Op; public ExprNode Operand;

            public UnaryNode(UnaryOp op, ExprNode operand, int pos)
            { Op = op; Operand = operand; Pos = pos; }

            public override double Eval(Dictionary<int, double> rt)
            {
                var v = Operand.Eval(rt);
                return Op switch
                {
                    UnaryOp.Negate => -v,
                    UnaryOp.Not => v != 0.0 ? 0.0 : 1.0,
                    UnaryOp.BitNot => (double)(~((long)v)),
                    _ => throw new InvalidOperationException()
                };
            }

            public override void Print(string indent, bool last)
            {
                Console.Write(indent); Console.Write(last ? "©¸©¤ " : "©À©¤ "); Console.WriteLine("Unary(" + Op + ")");
                Operand.Print(indent + (last ? "   " : "©¦  "), true);
            }
        }

        private enum BinaryOp
        { Add, Sub, Mul, Div, LShift, RShift, GT, GTE, LT, LTE, EQ, NEQ, BitAnd, BitXor, BitOr, AndAnd, OrOr }

        private class BinaryNode : ExprNode
        {
            public BinaryOp Op; public ExprNode Left; public ExprNode Right;

            public BinaryNode(ExprNode l, BinaryOp op, ExprNode r, int pos)
            { Left = l; Op = op; Right = r; Pos = pos; }

            public override double Eval(Dictionary<int, double> rt)
            {
                switch (Op)
                {
                    case BinaryOp.Add: return Left.Eval(rt) + Right.Eval(rt);
                    case BinaryOp.Sub: return Left.Eval(rt) - Right.Eval(rt);
                    case BinaryOp.Mul: return Left.Eval(rt) * Right.Eval(rt);
                    case BinaryOp.Div: { double rv = Right.Eval(rt); if (rv == 0) throw new DivideByZeroException(); return Left.Eval(rt) / rv; }
                    case BinaryOp.LShift: return (double)(((long)Left.Eval(rt)) << (int)Right.Eval(rt));
                    case BinaryOp.RShift: return (double)(((long)Left.Eval(rt)) >> (int)Right.Eval(rt));
                    case BinaryOp.GT: return Left.Eval(rt) > Right.Eval(rt) ? 1.0 : 0.0;
                    case BinaryOp.GTE: return Left.Eval(rt) >= Right.Eval(rt) ? 1.0 : 0.0;
                    case BinaryOp.LT: return Left.Eval(rt) < Right.Eval(rt) ? 1.0 : 0.0;
                    case BinaryOp.LTE: return Left.Eval(rt) <= Right.Eval(rt) ? 1.0 : 0.0;
                    case BinaryOp.EQ: return Left.Eval(rt) == Right.Eval(rt) ? 1.0 : 0.0;
                    case BinaryOp.NEQ: return Left.Eval(rt) != Right.Eval(rt) ? 1.0 : 0.0;
                    case BinaryOp.BitAnd: return (double)(((long)Left.Eval(rt)) & ((long)Right.Eval(rt)));
                    case BinaryOp.BitXor: return (double)(((long)Left.Eval(rt)) ^ ((long)Right.Eval(rt)));
                    case BinaryOp.BitOr: return (double)(((long)Left.Eval(rt)) | ((long)Right.Eval(rt)));
                    case BinaryOp.AndAnd: { double lv = Left.Eval(rt); if (lv == 0.0) return 0.0; double rv = Right.Eval(rt); return rv != 0.0 ? 1.0 : 0.0; }
                    case BinaryOp.OrOr: { double lv = Left.Eval(rt); if (lv != 0.0) return 1.0; double rv = Right.Eval(rt); return rv != 0.0 ? 1.0 : 0.0; }
                    default: throw new InvalidOperationException();
                }
            }

            public override void Print(string indent, bool last)
            {
                Console.Write(indent); Console.Write(last ? "©¸©¤ " : "©À©¤ "); Console.WriteLine("Binary(" + Op + ")");
                Left.Print(indent + (last ? "   " : "©¦  "), false);
                Right.Print(indent + (last ? "   " : "©¦  "), true);
            }
        }

        private class FuncNode : ExprNode
        {
            public string Name; public ExprNode Arg;

            public FuncNode(string name, ExprNode arg, int pos)
            { Name = name; Arg = arg; Pos = pos; }

            public override double Eval(Dictionary<int, double> rt)
            {
                double a = Arg.Eval(rt);
                return Name switch
                {
                    "sin" => Math.Sin(a * Math.PI / 180),
                    "cos" => Math.Cos(a * Math.PI / 180),
                    "exp" => Math.Exp(a),
                    _ => throw new Exception("Unknown function: " + Name)
                };
            }

            public override void Print(string indent, bool last)
            {
                Console.Write(indent); Console.Write(last ? "©¸©¤ " : "©À©¤ "); Console.WriteLine("Func(" + Name + ")");
                Arg.Print(indent + (last ? "   " : "©¦  "), true);
            }
        }

        private class AssignNode : ExprNode
        {
            public int Id; public ExprNode Rhs;

            public AssignNode(int id, ExprNode rhs, int pos)
            { Id = id; Rhs = rhs; Pos = pos; }

            public override double Eval(Dictionary<int, double> rt)
            {
                double v = Rhs.Eval(rt);
                rt[Id] = v;
                return v;
            }

            public override void Print(string indent, bool last)
            {
                Console.Write(indent); Console.Write(last ? "©¸©¤ " : "©À©¤ "); Console.WriteLine("Assign(#" + Id + ")");
                Rhs.Print(indent + (last ? "   " : "©¦  "), true);
            }
        }

        public Parser(List<Token> tokens, Dictionary<int, double> rtmap = null)
        { toks = tokens; pos = 0; rt = rtmap ?? new Dictionary<int, double>(); }

        private Token Peek() => pos < toks.Count ? toks[pos] : new Token(TokenType.EOF);

        private Token Next() => pos < toks.Count ? toks[pos++] : new Token(TokenType.EOF);

        private bool Match(TokenType t)
        { if (Peek().Type == t) { Next(); return true; } return false; }

        // top-level: parse statement (assignment or expression)
        public (bool isAssignment, int id, double value) ParseStatement()
        {
            lastAssignment = false;
            lastAssignmentId = 0;
            // build AST then evaluate
            ExprNode ast = ParseExpressionNode();
            double v = ast.Eval(rt);
            Console.WriteLine("AST:");
            ast.Print("", true);
            if (Peek().Type != TokenType.EOF) throw new Exception("Unexpected token");
            return (lastAssignment, lastAssignmentId, v);
        }

        // precedence: assignment (right-assoc) -> logical OR -> AND -> bitwise OR -> XOR -> AND -> equality -> relational -> shift -> add -> mul -> unary -> primary
        private ExprNode ParseExpressionNode() => ParseAssignNode();

        // ParseExpression kept for compatibility
        public double ParseExpression() => ParseExpressionNode().Eval(rt);

        // Priority 1: assignment, right-associative; left-value must be HASH
        private ExprNode ParseAssignNode()
        {
            if (Peek().Type == TokenType.HASH && pos + 1 < toks.Count && toks[pos + 1].Type == TokenType.ASSIGN)
            {
                var h = Next(); // consume HASH
                int id = int.Parse(h.Text);
                int assignPos = Peek().Pos;
                Next(); // consume ASSIGN
                ExprNode rhs = ParseAssignNode(); // right-assoc
                lastAssignment = true; lastAssignmentId = id;
                return new AssignNode(id, rhs, assignPos);
            }
            return ParseLogicalOrNode();
        }

        private double ParseLogicalOr()
        {
            throw new NotImplementedException();
        }

        private ExprNode ParseLogicalOrNode()
        {
            ExprNode left = ParseLogicalAndNode();
            while (Match(TokenType.OROR))
            {
                ExprNode right = ParseLogicalAndNode();
                left = new BinaryNode(left, BinaryOp.OrOr, right, left.Pos);
            }
            return left;
        }

        private double ParseLogicalAnd()
        {
            throw new NotImplementedException();
        }

        private ExprNode ParseLogicalAndNode()
        {
            ExprNode left = ParseBitwiseOrNode();
            while (Match(TokenType.ANDAND))
            {
                ExprNode right = ParseBitwiseOrNode();
                left = new BinaryNode(left, BinaryOp.AndAnd, right, left.Pos);
            }
            return left;
        }

        private double ParseBitwiseOr()
        {
            throw new NotImplementedException();
        }

        private ExprNode ParseBitwiseOrNode()
        {
            ExprNode left = ParseBitwiseXorNode();
            while (Match(TokenType.PIPE)) { ExprNode r = ParseBitwiseXorNode(); left = new BinaryNode(left, BinaryOp.BitOr, r, left.Pos); }
            return left;
        }

        private double ParseBitwiseXor()
        {
            throw new NotImplementedException();
        }

        private ExprNode ParseBitwiseXorNode()
        {
            ExprNode left = ParseBitwiseAndNode();
            while (Match(TokenType.CARET)) { ExprNode r = ParseBitwiseAndNode(); left = new BinaryNode(left, BinaryOp.BitXor, r, left.Pos); }
            return left;
        }

        private double ParseBitwiseAnd()
        {
            throw new NotImplementedException();
        }

        private ExprNode ParseBitwiseAndNode()
        {
            ExprNode left = ParseEqualityNode();
            while (Match(TokenType.AMP)) { ExprNode r = ParseEqualityNode(); left = new BinaryNode(left, BinaryOp.BitAnd, r, left.Pos); }
            return left;
        }

        private double ParseEquality()
        {
            throw new NotImplementedException();
        }

        private ExprNode ParseEqualityNode()
        {
            ExprNode left = ParseRelationalNode();
            while (true)
            {
                if (Match(TokenType.EQ)) { ExprNode r = ParseRelationalNode(); left = new BinaryNode(left, BinaryOp.EQ, r, left.Pos); }
                else if (Match(TokenType.NEQ)) { ExprNode r = ParseRelationalNode(); left = new BinaryNode(left, BinaryOp.NEQ, r, left.Pos); }
                else break;
            }
            return left;
        }

        private double ParseRelational()
        {
            throw new NotImplementedException();
        }

        private ExprNode ParseRelationalNode()
        {
            ExprNode left = ParseShiftNode();
            while (true)
            {
                if (Match(TokenType.GT)) { ExprNode r = ParseShiftNode(); left = new BinaryNode(left, BinaryOp.GT, r, left.Pos); }
                else if (Match(TokenType.GTE)) { ExprNode r = ParseShiftNode(); left = new BinaryNode(left, BinaryOp.GTE, r, left.Pos); }
                else if (Match(TokenType.LT)) { ExprNode r = ParseShiftNode(); left = new BinaryNode(left, BinaryOp.LT, r, left.Pos); }
                else if (Match(TokenType.LTE)) { ExprNode r = ParseShiftNode(); left = new BinaryNode(left, BinaryOp.LTE, r, left.Pos); }
                else break;
            }
            return left;
        }

        private double ParseShift()
        {
            throw new NotImplementedException();
        }

        private ExprNode ParseShiftNode()
        {
            ExprNode left = ParseAddNode();
            while (true)
            {
                if (Match(TokenType.LSHIFT)) { ExprNode r = ParseAddNode(); left = new BinaryNode(left, BinaryOp.LShift, r, left.Pos); }
                else if (Match(TokenType.RSHIFT)) { ExprNode r = ParseAddNode(); left = new BinaryNode(left, BinaryOp.RShift, r, left.Pos); }
                else break;
            }
            return left;
        }

        private ExprNode ParseAddNode()
        {
            ExprNode left = ParseMultiplyNode();
            while (true)
            {
                if (Match(TokenType.PLUS)) { ExprNode r = ParseMultiplyNode(); left = new BinaryNode(left, BinaryOp.Add, r, left.Pos); }
                else if (Match(TokenType.MINUS)) { ExprNode r = ParseMultiplyNode(); left = new BinaryNode(left, BinaryOp.Sub, r, left.Pos); }
                else break;
            }
            return left;
        }

        private ExprNode ParseMultiplyNode()
        {
            ExprNode left = ParseUnaryNode();
            while (true)
            {
                if (Match(TokenType.MUL)) { ExprNode r = ParseUnaryNode(); left = new BinaryNode(left, BinaryOp.Mul, r, left.Pos); }
                else if (Match(TokenType.DIV)) { ExprNode r = ParseUnaryNode(); left = new BinaryNode(left, BinaryOp.Div, r, left.Pos); }
                else break;
            }
            return left;
        }

        private ExprNode ParseUnaryNode()
        {
            if (Match(TokenType.NOT)) { ExprNode op = ParseUnaryNode(); return new UnaryNode(UnaryOp.Not, op, op.Pos); }
            if (Match(TokenType.TILDE)) { ExprNode op = ParseUnaryNode(); return new UnaryNode(UnaryOp.BitNot, op, op.Pos); }
            if (Match(TokenType.MINUS)) { ExprNode op = ParseUnaryNode(); return new UnaryNode(UnaryOp.Negate, op, op.Pos); }
            return ParsePowerNode();
        }

        private ExprNode ParsePowerNode()
        {
            var t = Peek();
            if (t.Type == TokenType.IDENT && t.Text == "exp")
            {
                Next(); // consume 'exp'
                if (!Match(TokenType.LP)) throw new Exception($"Expected ( after exp at position {t.Pos}");
                ExprNode arg = ParseAssignNode();
                if (!Match(TokenType.RP)) throw new Exception($"Expected ) after exp at position {t.Pos}");
                return new FuncNode("exp", arg, t.Pos);
            }

            if (t.Type == TokenType.IDENT && (t.Text == "sin" || t.Text == "cos"))
            {
                Next(); // consume 'sin' or 'cos'
                if (!Match(TokenType.LP)) throw new Exception($"Expected ( after {t.Text} at position {t.Pos}");
                ExprNode arg = ParseAssignNode();
                if (!Match(TokenType.RP)) throw new Exception($"Expected ) after {t.Text} at position {t.Pos}");
                return new FuncNode(t.Text, arg, t.Pos);
            }

            return ParsePrimaryNode();
        }

        private ExprNode ParsePrimaryNode()
        {
            var t = Peek();
            if (Match(TokenType.NUM)) return new NumberNode(t.Num, t.Pos);
            if (Match(TokenType.HASH))
            {
                int id = int.Parse(t.Text);
                return new HashNode(id, t.Pos);
            }
            if (Match(TokenType.IDENT))
            {
                throw new Exception($"Unexpected identifier '{t.Text}' at position {t.Pos}");
            }
            if (Match(TokenType.LP))
            {
                ExprNode v = ParseAssignNode();
                if (!Match(TokenType.RP)) throw new Exception($"Expected ) at position {Peek().Pos}");
                return v;
            }

            throw new Exception($"Unexpected token: {t} at position {t.Pos}");
        }
    }

    private static void EvalMain()
    {
        var realDataBaseMap = new Dictionary<int, double>();
        Console.WriteLine("Enter expression (empty to quit):");
        while (true)
        {
            string? line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) break;
            try
            {
                var parser = new Parser((new Tokenizer(line)).Tokenize(), realDataBaseMap);
                var statement = parser.ParseStatement();
                if (statement.isAssignment)
                {
                    Console.WriteLine($"Assigned #{statement.id} = {statement.value.ToString(CultureInfo.InvariantCulture)}");
                }
                else
                {
                    Console.WriteLine("Result: " + statement.value.ToString(CultureInfo.InvariantCulture));
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