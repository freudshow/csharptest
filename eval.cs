using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ConsoleApp1
{
    // Ported from eval.c (subset faithfully adapted)
    public enum TokenType
    {
        T_NUM,
        T_REALDB,
        T_REALDB_LINK_DEV_REG,
        T_IF,
        T_ELSE,
        T_WHILE,
        T_FOR,
        T_RETURN,
        T_VAR,
        T_IDENT,
        T_PLUS,
        T_MINUS,
        T_MUL,
        T_DIV,
        T_LP,
        T_RP,
        T_NOT,
        T_NEQ,
        T_ANDAND,
        T_OROR,
        T_GT,
        T_GTE,
        T_LT,
        T_LTE,
        T_EQ,
        T_AMP,
        T_PIPE,
        T_CARET,
        T_TILDE,
        T_LSHIFT,
        T_RSHIFT,
        T_ASSIGN,
        T_COMMA,
        T_SEMI,
        T_LBRACE,
        T_RBRACE,
        T_EOF,
        T_INVALID
    }

    public class ReturnException : System.Exception
    {
        public double Value;

        public ReturnException(double v)
        {
            Value = v;
        }
    }

    public class EvalException : System.Exception
    {
        public EvalException(string msg) : base(msg)
        {
        }
    }

    public class RealDbLinkDevReg
    {
        public int LinkNo;
        public int DevNo;
        public int RegNo;
    }

    public class Token
    {
        public TokenType Type;
        public string? Text;
        public double Num;
        public int Pos;
        public RealDbLinkDevReg? RealDbRef;
    }

    public class TokenList
    {
        public List<Token> Arr = new List<Token>();
        public int Idx = 0;

        public Token Peek() => Idx < Arr.Count ? Arr[Idx] : new Token { Type = TokenType.T_EOF };

        public Token Next() => Idx < Arr.Count ? Arr[Idx++] : new Token { Type = TokenType.T_EOF };

        public void Push(Token t) => Arr.Add(t);
    }

    public enum NodeType
    {
        N_NUMBER,
        N_REAL_DATABASE,
        N_REALDB_LINK_DEV_REG,
        N_UNARY,
        N_BINARY,
        N_FUNC,
        N_ASSIGN,
        N_IF,
        N_WHILE,
        N_FOR,
        N_RETURN,
        N_BLOCK,
        N_VAR,
        N_VDECL
    }

    public enum UnaryOp { U_NEG, U_NOT, U_BITNOT }

    public enum BinaryOp { B_ADD, B_SUB, B_MUL, B_DIV, B_LSHIFT, B_RSHIFT, B_GT, B_GTE, B_LT, B_LTE, B_EQ, B_NEQ, B_BITAND, B_BITXOR, B_BITOR, B_ANDAND, B_OROR }

    public class ASTNode
    {
        public NodeType Type;
        public int Pos;
        public double Number;
        public int RealDataBaseId;
        public RealDbLinkDevReg? LinkDevReg;
        public UnaryOp UnaryOp;
        public ASTNode? Child;
        public BinaryOp BinaryOp;
        public ASTNode? Left;
        public ASTNode? Right;
        public string? FuncName;
        public Delegate? FuncPtr;
        public ASTNode[]? Args;
        public int Argc;
        public int AssignId;
        public ASTNode? Rhs;
        public string? VarName;
    }

    public class ArithmeticEvaluator
    {
        public ASTNode Ast;
        private List<Dictionary<string, double>> Scopes = new List<Dictionary<string, double>>();

        public ArithmeticEvaluator(ASTNode ast)
        {
            Ast = ast;
            Scopes.Add(new Dictionary<string, double>());
        }

        public double Eval()
        {
            try
            {
                return EvalNode(Ast);
            }
            catch (ReturnException rex)
            {
                return rex.Value;
            }
        }

        private static double Pi() => 3.14159265358979323846;

        static double E() => 2.71828182845904523536;

        private static double Fac(double a)
        {
            if (a < 0.0) return double.NaN;
            if (a > uint.MaxValue) return double.PositiveInfinity;
            uint ua = (uint)a;
            ulong result = 1;
            for (ulong i = 1; i <= ua; i++)
            {
                if (i > ulong.MaxValue / result) return double.PositiveInfinity;
                result *= i;
            }
            return (double)result;
        }

        private static double Ncr(double n, double r)
        {
            if (n < 0 || r < 0 || n < r) return double.NaN;
            if (n > uint.MaxValue || r > uint.MaxValue) return double.PositiveInfinity;
            ulong un = (uint)n, ur = (uint)r;
            if (ur > un / 2) ur = un - ur;
            ulong res = 1;
            for (ulong i = 1; i <= ur; i++)
            {
                if (res > ulong.MaxValue / (un - ur + i)) return double.PositiveInfinity;
                res = res * (un - ur + i) / i;
            }
            return (double)res;
        }

        private static double Npr(double n, double r) => Ncr(n, r) * Fac(r);

        static double Max(double a, double b) => a > b ? a : b;

        private static double Min(double a, double b) => a < b ? a : b;

        private static readonly (string name, Delegate func, int arity)[] Builtins = new (string, Delegate, int)[] {
            ("abs", new Func<double,double>(Math.Abs),1),
            ("acos", new Func<double,double>(Math.Acos),1),
            ("asin", new Func<double,double>(Math.Asin),1),
            ("atan", new Func<double,double>(Math.Atan),1),
            ("atan2", new Func<double,double,double>(Math.Atan2),2),
            ("ceil", new Func<double,double>(Math.Ceiling),1),
            ("cos", new Func<double,double>(Math.Cos),1),
            ("cosh", new Func<double,double>(Math.Cosh),1),
            ("e", new Func<double>(E),0),
            ("exp", new Func<double,double>(Math.Exp),1),
            ("fac", new Func<double,double>(Fac),1),
            ("floor", new Func<double,double>(Math.Floor),1),
            ("ln", new Func<double,double>(Math.Log),1),
            ("log", new Func<double,double>(Math.Log),1),
            ("log10", new Func<double,double>(Math.Log10),1),
            ("ncr", new Func<double,double,double>(Ncr),2),
            ("npr", new Func<double,double,double>(Npr),2),
            ("pi", new Func<double>(Pi),0),
            ("pow", new Func<double,double,double>(Math.Pow),2),
            ("sin", new Func<double,double>(Math.Sin),1),
            ("sinh", new Func<double,double>(Math.Sinh),1),
            ("sqrt", new Func<double,double>(Math.Sqrt),1),
            ("tan", new Func<double,double>(Math.Tan),1),
            ("tanh", new Func<double,double>(Math.Tanh),1),
            (null!, null!, 0)
        };

        private static readonly (string name, Delegate func, int arity)[] Custom = new (string, Delegate, int)[] {
            ("max", new Func<double,double,double>(Max),2),
            ("min", new Func<double,double,double>(Min),2)
        };

        private static (string name, Delegate func, int arity)? FindBuiltin(string name)
        {
            int lo = 0, hi = Builtins.Length - 2;
            while (hi >= lo)
            {
                int i = lo + ((hi - lo) / 2);
                int c = string.Compare(name, Builtins[i].name, StringComparison.Ordinal);
                if (c == 0) return Builtins[i];
                if (c > 0) lo = i + 1; else hi = i - 1;
            }
            return null;
        }

        private static (string name, Delegate func, int arity)? FindCustom(string name)
        {
            foreach (var t in Custom) if (t.name == name) return t;
            return null;
        }

        private static int FastGetRtdbNo(int linkNo, int devNo, int regNo)
        {
            Console.WriteLine($"linkNo={linkNo}, devNo={devNo}, regNo={regNo}");
            return 132;
        }

        private static double GetValueByRealNo(int realNo)
        {
            Console.WriteLine($"realNo={realNo}");
            return 42.0;
        }

        static void SetValueByRealNo(int realNo, float value)
        {
            Console.WriteLine($"Set realNo={realNo} to value={value}");
        }

        private double EvalNode(ASTNode n)
        {
            if (n == null)
            {
                return double.NaN;
            }

            switch (n.Type)
            {
                case NodeType.N_BLOCK:
                    {
                        Scopes.Add(new Dictionary<string, double>());

                        double last = 0.0;

                        if (n.Args != null)
                        {
                            foreach (var s in n.Args)
                            {
                                last = EvalNode(s);
                            }
                        }

                        Scopes.RemoveAt(Scopes.Count - 1);

                        return last;
                    }

                case NodeType.N_VAR:
                    {
                        string name = n.VarName!;

                        for (int i = Scopes.Count - 1; i >= 0; --i)
                        {
                            if (Scopes[i].TryGetValue(name, out var val))
                            {
                                return val;
                            }
                        }

                        return double.NaN;
                    }

                case NodeType.N_VDECL:
                    {
                        string name = n.VarName!;

                        double val = 0.0;

                        if (n.Child != null)
                        {
                            val = EvalNode(n.Child);
                        }

                        // create in current scope only; error on duplicate declaration in same scope
                        var cur = Scopes[Scopes.Count - 1];

                        if (cur.ContainsKey(name))
                        {
                            throw new EvalException($"Duplicate declaration of '{name}' in same scope");
                        }

                        cur[name] = val;

                        return val;
                    }

                case NodeType.N_NUMBER:
                    {
                        return n.Number;
                    }

                case NodeType.N_REAL_DATABASE:
                    {
                        return GetValueByRealNo(n.RealDataBaseId);
                    }

                case NodeType.N_REALDB_LINK_DEV_REG:
                    {
                        int rtdbNo = FastGetRtdbNo(n.LinkDevReg!.LinkNo, n.LinkDevReg.DevNo, n.LinkDevReg.RegNo);

                        if (rtdbNo < 0)
                        {
                            return double.NaN;
                        }

                        return GetValueByRealNo(rtdbNo);
                    }

                case NodeType.N_UNARY:
                    {
                        double v = EvalNode(n.Child!);

                        if (n.UnaryOp == UnaryOp.U_NEG)
                        {
                            return -v;
                        }

                        if (n.UnaryOp == UnaryOp.U_NOT)
                        {
                            return v != 0.0 ? 0.0 : 1.0;
                        }

                        return (double)(~((long)v));
                    }

                case NodeType.N_BINARY:
                    {
                        switch (n.BinaryOp)
                        {
                            case BinaryOp.B_ADD:
                                {
                                    return EvalNode(n.Left!) + EvalNode(n.Right!);
                                }

                            case BinaryOp.B_SUB:
                                {
                                    return EvalNode(n.Left!) - EvalNode(n.Right!);
                                }

                            case BinaryOp.B_MUL:
                                {
                                    return EvalNode(n.Left!) * EvalNode(n.Right!);
                                }

                            case BinaryOp.B_DIV:
                                {
                                    double r = EvalNode(n.Right!);

                                    if (r == 0)
                                    {
                                        return double.NaN;
                                    }

                                    return EvalNode(n.Left!) / r;
                                }

                            case BinaryOp.B_LSHIFT:
                                {
                                    return (double)(((long)EvalNode(n.Left!)) << (int)EvalNode(n.Right!));
                                }

                            case BinaryOp.B_RSHIFT:
                                {
                                    return (double)(((long)EvalNode(n.Left!)) >> (int)EvalNode(n.Right!));
                                }

                            case BinaryOp.B_GT:
                                {
                                    return EvalNode(n.Left!) > EvalNode(n.Right!) ? 1.0 : 0.0;
                                }

                            case BinaryOp.B_GTE:
                                {
                                    return EvalNode(n.Left!) >= EvalNode(n.Right!) ? 1.0 : 0.0;
                                }

                            case BinaryOp.B_LT:
                                {
                                    return EvalNode(n.Left!) < EvalNode(n.Right!) ? 1.0 : 0.0;
                                }

                            case BinaryOp.B_LTE:
                                {
                                    return EvalNode(n.Left!) <= EvalNode(n.Right!) ? 1.0 : 0.0;
                                }

                            case BinaryOp.B_EQ:
                                {
                                    return EvalNode(n.Left!) == EvalNode(n.Right!) ? 1.0 : 0.0;
                                }

                            case BinaryOp.B_NEQ:
                                {
                                    return EvalNode(n.Left!) != EvalNode(n.Right!) ? 1.0 : 0.0;
                                }

                            case BinaryOp.B_BITAND:
                                {
                                    return (double)(((long)EvalNode(n.Left!)) & ((long)EvalNode(n.Right!)));
                                }

                            case BinaryOp.B_BITXOR:
                                {
                                    return (double)(((long)EvalNode(n.Left!)) ^ ((long)EvalNode(n.Right!)));
                                }

                            case BinaryOp.B_BITOR:
                                {
                                    return (double)(((long)EvalNode(n.Left!)) | ((long)EvalNode(n.Right!)));
                                }

                            case BinaryOp.B_ANDAND:
                                {
                                    double lv = EvalNode(n.Left!);

                                    if (lv == 0.0)
                                    {
                                        return 0.0;
                                    }

                                    double rv = EvalNode(n.Right!);

                                    return (rv == 0.0) ? 0.0 : 1.0;
                                }

                            case BinaryOp.B_OROR:
                                {
                                    double lv = EvalNode(n.Left!);

                                    if (lv != 0.0)
                                    {
                                        return 1.0;
                                    }

                                    double rv = EvalNode(n.Right!);

                                    return (rv == 0.0) ? 0.0 : 1.0;
                                }
                        }

                        break;
                    }

                case NodeType.N_FUNC:
                    {
                        int argc = n.Argc;

                        double[] args = new double[argc];

                        for (int i = 0; i < argc; ++i)
                        {
                            args[i] = EvalNode(n.Args![i]);
                        }

                        // if a compiled delegate is present, try to invoke it
                        if (n.FuncPtr != null)
                        {
                            try
                            {
                                if (argc == 0 && n.FuncPtr is Func<double> f0)
                                {
                                    return f0();
                                }

                                if (argc == 1 && n.FuncPtr is Func<double, double> f1)
                                {
                                    // degree/radian handling for trig functions
                                    if (n.FuncPtr == (Delegate)(Func<double, double>)Math.Sin || n.FuncPtr == (Delegate)(Func<double, double>)Math.Cos || n.FuncPtr == (Delegate)(Func<double, double>)Math.Tan)
                                    {
                                        return f1(args[0] * Pi() / 180.0);
                                    }

                                    if (n.FuncPtr == (Delegate)(Func<double, double>)Math.Asin || n.FuncPtr == (Delegate)(Func<double, double>)Math.Acos || n.FuncPtr == (Delegate)(Func<double, double>)Math.Atan)
                                    {
                                        return f1(args[0]) * 180.0 / Pi();
                                    }

                                    return f1(args[0]);
                                }

                                if (argc == 2 && n.FuncPtr is Func<double, double, double> f2)
                                {
                                    return f2(args[0], args[1]);
                                }

                                // fallback to DynamicInvoke for other delegate shapes
                                object?[] boxed = args.Select(d => (object)d).ToArray();
                                var res = n.FuncPtr.DynamicInvoke(boxed);

                                return Convert.ToDouble(res);
                            }
                            catch
                            {
                                return double.NaN;
                            }
                        }

                        // try user-registered functions by name
                        if (!string.IsNullOrEmpty(n.FuncName) && global::ConsoleApp1.Eval.TryGetUserFunction(n.FuncName!, out var ufun))
                        {
                            try
                            {
                                return ufun(args);
                            }
                            catch
                            {
                                return double.NaN;
                            }
                        }

                        return double.NaN;
                    }

                case NodeType.N_IF:
                    {
                        double cv = EvalNode(n.Child!);

                        if (cv != 0.0)
                        {
                            return EvalNode(n.Left!);
                        }

                        if (n.Right != null)
                        {
                            return EvalNode(n.Right);
                        }

                        return 0.0;
                    }

                case NodeType.N_WHILE:
                    {
                        double last = 0.0;

                        while (EvalNode(n.Child!) != 0.0)
                        {
                            last = EvalNode(n.Left!);
                        }

                        return last;
                    }

                case NodeType.N_FOR:
                    {
                        double last = 0.0;

                        ASTNode? init = (n.Args != null && n.Args.Length > 0) ? n.Args[0] : null;
                        ASTNode? cond = (n.Args != null && n.Args.Length > 1) ? n.Args[1] : null;
                        ASTNode? iter = (n.Args != null && n.Args.Length > 2) ? n.Args[2] : null;

                        if (init != null)
                        {
                            EvalNode(init);
                        }

                        while (cond == null || EvalNode(cond) != 0.0)
                        {
                            last = EvalNode(n.Left!);

                            if (iter != null)
                            {
                                EvalNode(iter);
                            }
                        }

                        return last;
                    }

                case NodeType.N_RETURN:
                    {
                        double v = n.Child != null ? EvalNode(n.Child) : 0.0;

                        throw new ReturnException(v);
                    }

                case NodeType.N_ASSIGN:
                    {
                        double v = EvalNode(n.Rhs!);

                        if (!string.IsNullOrEmpty(n.VarName))
                        {
                            // assign to existing nearest scope or create in top scope
                            for (int i = Scopes.Count - 1; i >= 0; --i)
                            {
                                if (Scopes[i].ContainsKey(n.VarName))
                                {
                                    Scopes[i][n.VarName!] = v;
                                    return v;
                                }
                            }

                            Scopes[Scopes.Count - 1][n.VarName!] = v;

                            return v;
                        }

                        if (n.AssignId >= 0)
                        {
                            SetValueByRealNo(n.AssignId, (float)v);
                        }
                        else if (n.LinkDevReg != null)
                        {
                            int rtdbNo = FastGetRtdbNo(n.LinkDevReg!.LinkNo, n.LinkDevReg.DevNo, n.LinkDevReg.RegNo);

                            if (rtdbNo < 0)
                            {
                                return double.NaN;
                            }

                            SetValueByRealNo(rtdbNo, (float)v);
                        }

                        return v;
                    }
            }
            return double.NaN;
        }
    }

    public static class Eval
    {
        // user-registered functions (variadic)
        private static readonly Dictionary<string, Func<double[], double>> UserFunctions = new Dictionary<string, Func<double[], double>>();

        public static void RegisterFunction(string name, Func<double[], double> func)
        {
            UserFunctions[name] = func;
        }

        internal static bool TryGetUserFunction(string name, out Func<double[], double>? func)
        {
            return UserFunctions.TryGetValue(name, out func);
        }

        // Example usages and simple tests
        public static void RunExamples()
        {
            RegisterFunction("sum", a => a.Sum());
            var e1 = GetNewEvaluator("sum(1,2,3)"); Console.WriteLine(e1?.Eval());
            var e2 = GetNewEvaluator("{ x = 1; x = x + 2; x }"); Console.WriteLine(e2?.Eval());
            var e3 = GetNewEvaluator("{ i = 0; for (i = 0; i < 3; i = i + 1) { } i }"); Console.WriteLine(e3?.Eval());
            var e4 = GetNewEvaluator("{ var x = 1; x = x + 2; x }"); Console.WriteLine(e4?.Eval());
        }

        // duplicate of builtin/custom function table used by the parser
        private static readonly (string name, Delegate func, int arity)[] Builtins = new (string, Delegate, int)[] {
            ("abs", new Func<double,double>(Math.Abs),1),
            ("acos", new Func<double,double>(Math.Acos),1),
            ("asin", new Func<double,double>(Math.Asin),1),
            ("atan", new Func<double,double>(Math.Atan),1),
            ("atan2", new Func<double,double,double>(Math.Atan2),2),
            ("ceil", new Func<double,double>(Math.Ceiling),1),
            ("cos", new Func<double,double>(Math.Cos),1),
            ("cosh", new Func<double,double>(Math.Cosh),1),
            ("e", new Func<double>(() => 2.71828182845904523536),0),
            ("exp", new Func<double,double>(Math.Exp),1),
            ("fac", new Func<double,double>((d) => { if (d<0) return double.NaN; double r=1; for(int i=1;i<= (int)d;i++) r*=i; return r; }),1),
            ("floor", new Func<double,double>(Math.Floor),1),
            ("ln", new Func<double,double>(Math.Log),1),
            ("log", new Func<double,double>(Math.Log),1),
            ("log10", new Func<double,double>(Math.Log10),1),
            ("ncr", new Func<double,double,double>((a,b)=> { return 0.0; }),2),
            ("npr", new Func<double,double,double>((a,b)=> { return 0.0; }),2),
            ("pi", new Func<double>(() => 3.14159265358979323846),0),
            ("pow", new Func<double,double,double>(Math.Pow),2),
            ("sin", new Func<double,double>(Math.Sin),1),
            ("sinh", new Func<double,double>(Math.Sinh),1),
            ("sqrt", new Func<double,double>(Math.Sqrt),1),
            ("tan", new Func<double,double>(Math.Tan),1),
            ("tanh", new Func<double,double>(Math.Tanh),1),
            (null!, null!, 0)
        };

        private static readonly (string name, Delegate func, int arity)[] Custom = new (string, Delegate, int)[] {
            ("max", new Func<double,double,double>((a,b)=> a>b?a:b),2),
            ("min", new Func<double,double,double>((a,b)=> a<b?a:b),2)
        };

        static (string name, Delegate func, int arity)? FindBuiltin(string name)
        {
            int lo = 0, hi = Builtins.Length - 2;
            while (hi >= lo)
            {
                int i = lo + ((hi - lo) / 2);
                int c = string.Compare(name, Builtins[i].name, StringComparison.Ordinal);
                if (c == 0) return Builtins[i];
                if (c > 0) lo = i + 1; else hi = i - 1;
            }
            return null;
        }

        private static (string name, Delegate func, int arity)? FindCustom(string name)
        {
            foreach (var t in Custom) if (t.name == name) return t;
            return null;
        }

        // Tokenize, parse and return ArithmeticEvaluator
        public static ArithmeticEvaluator? GetNewEvaluator(string input)
        {
            var toks = new TokenList();
            Tokenize(input, toks);
            // check invalid tokens
            var invalid = toks.Arr.FirstOrDefault(t => t.Type == TokenType.T_INVALID);
            if (invalid != null) return null;
            toks.Idx = 0;
            var ast = ParseStatement(toks);
            if (ast == null) return null;
            if (toks.Peek().Type != TokenType.T_EOF) return null;
            return new ArithmeticEvaluator(ast);
        }

        private static void Tokenize(string s, TokenList outList)
        {
            int i = 0;
            int n = s.Length;
            while (true)
            {
                while (i < n && char.IsWhiteSpace(s[i]))
                {
                    i++;
                }

                if (i >= n)
                {
                    outList.Push(new Token { Type = TokenType.T_EOF, Pos = i });
                    break;
                }

                char c = s[i];

                if (c == '&' && i + 1 < n && s[i + 1] == '&')
                {
                    outList.Push(new Token { Type = TokenType.T_ANDAND, Text = "&&", Pos = i });
                    i += 2;
                    continue;
                }

                if (c == '|' && i + 1 < n && s[i + 1] == '|')
                {
                    outList.Push(new Token { Type = TokenType.T_OROR, Text = "||", Pos = i });
                    i += 2;
                    continue;
                }

                if (c == '<' && i + 1 < n && s[i + 1] == '<')
                {
                    outList.Push(new Token { Type = TokenType.T_LSHIFT, Text = "<<", Pos = i });
                    i += 2;
                    continue;
                }

                if (c == '>' && i + 1 < n && s[i + 1] == '>')
                {
                    outList.Push(new Token { Type = TokenType.T_RSHIFT, Text = ">>", Pos = i });
                    i += 2;
                    continue;
                }

                if (c == '>' && i + 1 < n && s[i + 1] == '=')
                {
                    outList.Push(new Token { Type = TokenType.T_GTE, Text = ">=", Pos = i });
                    i += 2;
                    continue;
                }

                if (c == '<' && i + 1 < n && s[i + 1] == '=')
                {
                    outList.Push(new Token { Type = TokenType.T_LTE, Text = "<=", Pos = i });
                    i += 2;
                    continue;
                }

                if (c == '!' && i + 1 < n && s[i + 1] == '=')
                {
                    outList.Push(new Token { Type = TokenType.T_NEQ, Text = "!=", Pos = i });
                    i += 2;
                    continue;
                }

                if (c == '=' && i + 1 < n && s[i + 1] == '=')
                {
                    outList.Push(new Token { Type = TokenType.T_EQ, Text = "==", Pos = i });
                    i += 2;
                    continue;
                }
                if (char.IsDigit(c))
                {
                    int start = i; while (i < n && char.IsDigit(s[i])) i++;
                    if (i < n && s[i] == '.')
                    {
                        int dot = i;
                        if (i + 1 < n && char.IsDigit(s[i + 1])) { i++; while (i < n && char.IsDigit(s[i])) i++; }
                        else i = dot;
                    }
                    var txt = s.Substring(start, i - start);
                    double v = double.Parse(txt, CultureInfo.InvariantCulture);
                    outList.Push(new Token { Type = TokenType.T_NUM, Text = txt, Num = v, Pos = start });
                    continue;
                }
                if (c == '#')
                {
                    int hashPos = i; i++;
                    if (i < n && s[i] == '(')
                    {
                        i++;
                        int[] vals = new int[3]; int vi = 0; bool ok = true;
                        for (vi = 0; vi < 3 && ok; vi++)
                        {
                            while (i < n && char.IsWhiteSpace(s[i])) i++;
                            int numStart = i;
                            if (i < n && s[i] == '-') i++;
                            while (i < n && char.IsDigit(s[i])) i++;
                            if (i == numStart || (i == numStart + 1 && s[numStart] == '-')) { ok = false; break; }
                            vals[vi] = int.Parse(s.Substring(numStart, i - numStart), CultureInfo.InvariantCulture);
                            while (i < n && char.IsWhiteSpace(s[i])) i++;
                            if (vi < 2)
                            {
                                if (i < n && s[i] == ',') i++; else { ok = false; break; }
                            }
                            else { if (i < n && s[i] == ')') i++; else { ok = false; break; } }
                        }
                        if (!ok) { outList.Push(new Token { Type = TokenType.T_INVALID, Pos = hashPos }); break; }
                        outList.Push(new Token { Type = TokenType.T_REALDB_LINK_DEV_REG, Pos = hashPos, RealDbRef = new RealDbLinkDevReg { LinkNo = vals[0], DevNo = vals[1], RegNo = vals[2] } });
                        continue;
                    }
                    int start = i; while (i < n && char.IsDigit(s[i])) i++;
                    if (start == i) { outList.Push(new Token { Type = TokenType.T_INVALID, Pos = start - 1 }); break; }
                    var txt = s.Substring(start, i - start);
                    outList.Push(new Token { Type = TokenType.T_REALDB, Text = txt, Pos = start - 1 });
                    continue;
                }
                if (char.IsLetter(c) || c == '_')
                {
                    int start = i; i++; while (i < n && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++; var txt = s.Substring(start, i - start);
                    // keywords
                    if (txt == "if") { outList.Push(new Token { Type = TokenType.T_IF, Text = txt, Pos = start }); continue; }
                    if (txt == "else") { outList.Push(new Token { Type = TokenType.T_ELSE, Text = txt, Pos = start }); continue; }
                    if (txt == "while") { outList.Push(new Token { Type = TokenType.T_WHILE, Text = txt, Pos = start }); continue; }
                    if (txt == "for") { outList.Push(new Token { Type = TokenType.T_FOR, Text = txt, Pos = start }); continue; }
                    if (txt == "return") { outList.Push(new Token { Type = TokenType.T_RETURN, Text = txt, Pos = start }); continue; }
                    if (txt == "var") { outList.Push(new Token { Type = TokenType.T_VAR, Text = txt, Pos = start }); continue; }
                    outList.Push(new Token { Type = TokenType.T_IDENT, Text = txt, Pos = start }); continue;
                }
                switch (c)
                {
                    case '+': outList.Push(new Token { Type = TokenType.T_PLUS, Text = "+", Pos = i }); i++; break;
                    case '-': outList.Push(new Token { Type = TokenType.T_MINUS, Text = "-", Pos = i }); i++; break;
                    case '*': outList.Push(new Token { Type = TokenType.T_MUL, Text = "*", Pos = i }); i++; break;
                    case '/': outList.Push(new Token { Type = TokenType.T_DIV, Text = "/", Pos = i }); i++; break;
                    case '(': outList.Push(new Token { Type = TokenType.T_LP, Text = "(", Pos = i }); i++; break;
                    case ')': outList.Push(new Token { Type = TokenType.T_RP, Text = ")", Pos = i }); i++; break;
                    case '!': outList.Push(new Token { Type = TokenType.T_NOT, Text = "!", Pos = i }); i++; break;
                    case '>': outList.Push(new Token { Type = TokenType.T_GT, Text = ">", Pos = i }); i++; break;
                    case '<': outList.Push(new Token { Type = TokenType.T_LT, Text = "<", Pos = i }); i++; break;
                    case '&': outList.Push(new Token { Type = TokenType.T_AMP, Text = "&", Pos = i }); i++; break;
                    case '|': outList.Push(new Token { Type = TokenType.T_PIPE, Text = "|", Pos = i }); i++; break;
                    case '^': outList.Push(new Token { Type = TokenType.T_CARET, Text = "^", Pos = i }); i++; break;
                    case '~': outList.Push(new Token { Type = TokenType.T_TILDE, Text = "~", Pos = i }); i++; break;
                    case '=': outList.Push(new Token { Type = TokenType.T_ASSIGN, Text = "=", Pos = i }); i++; break;
                    case ',': outList.Push(new Token { Type = TokenType.T_COMMA, Text = ",", Pos = i }); i++; break;
                    case ';': outList.Push(new Token { Type = TokenType.T_SEMI, Text = ";", Pos = i }); i++; break;
                    case '{': outList.Push(new Token { Type = TokenType.T_LBRACE, Text = "{", Pos = i }); i++; break;
                    case '}': outList.Push(new Token { Type = TokenType.T_RBRACE, Text = "}", Pos = i }); i++; break;
                    default: outList.Push(new Token { Type = TokenType.T_INVALID, Pos = i }); i++; break;
                }
            }
        }

        private static bool Match(TokenList t, TokenType ty)
        {
            if (t.Peek().Type == ty)
            {
                t.Next();
                return true;
            }

            return false;
        }

        static ASTNode? ParseAssign(TokenList toks)
        {
            var cur = toks.Peek();
            if (cur.Type == TokenType.T_IDENT && toks.Arr.Count > toks.Idx + 1 && toks.Arr[toks.Idx + 1].Type == TokenType.T_ASSIGN)
            {
                var h = toks.Next();
                var a = toks.Next();
                var rhs = ParseAssign(toks);

                if (rhs == null)
                {
                    return null;
                }

                return new ASTNode
                {
                    Type = NodeType.N_ASSIGN,
                    Pos = a.Pos,
                    VarName = h.Text,
                    Rhs = rhs
                };
            }

            if (cur.Type == TokenType.T_REALDB && toks.Arr.Count > toks.Idx + 1 && toks.Arr[toks.Idx + 1].Type == TokenType.T_ASSIGN)
            {
                var h = toks.Next();
                var a = toks.Next();
                var rhs = ParseAssign(toks);

                if (rhs == null)
                {
                    return null;
                }

                int id = int.Parse(h.Text!);

                return new ASTNode
                {
                    Type = NodeType.N_ASSIGN,
                    Pos = a.Pos,
                    AssignId = id,
                    Rhs = rhs
                };
            }

            if (cur.Type == TokenType.T_REALDB_LINK_DEV_REG && toks.Arr.Count > toks.Idx + 1 && toks.Arr[toks.Idx + 1].Type == TokenType.T_ASSIGN)
            {
                var h = toks.Next();
                var a = toks.Next();
                var rhs = ParseAssign(toks);

                if (rhs == null)
                {
                    return null;
                }

                return new ASTNode
                {
                    Type = NodeType.N_ASSIGN,
                    Pos = a.Pos,
                    AssignId = -1,
                    LinkDevReg = h.RealDbRef,
                    Rhs = rhs
                };
            }
            return ParseLogicalOr(toks);
        }

        private static ASTNode? ParseLogicalOr(TokenList toks)
        {
            var left = ParseLogicalAnd(toks); if (left == null) return null;
            while (Match(toks, TokenType.T_OROR)) { var right = ParseLogicalAnd(toks); if (right == null) { return null; } left = new ASTNode { Type = NodeType.N_BINARY, BinaryOp = BinaryOp.B_OROR, Left = left, Right = right, Pos = left.Pos }; }
            return left;
        }

        static ASTNode? ParseLogicalAnd(TokenList toks)
        {
            var left = ParseBitOr(toks); if (left == null) return null;
            while (Match(toks, TokenType.T_ANDAND)) { var right = ParseBitOr(toks); if (right == null) return null; left = new ASTNode { Type = NodeType.N_BINARY, BinaryOp = BinaryOp.B_ANDAND, Left = left, Right = right, Pos = left.Pos }; }
            return left;
        }

        private static ASTNode? ParseBitOr(TokenList toks)
        {
            var left = ParseBitXor(toks); if (left == null) return null;
            while (Match(toks, TokenType.T_PIPE)) { var r = ParseBitXor(toks); if (r == null) return null; left = new ASTNode { Type = NodeType.N_BINARY, BinaryOp = BinaryOp.B_BITOR, Left = left, Right = r, Pos = left.Pos }; }
            return left;
        }

        static ASTNode? ParseBitXor(TokenList toks)
        {
            var left = ParseBitAnd(toks); if (left == null) return null;
            while (Match(toks, TokenType.T_CARET)) { var r = ParseBitAnd(toks); if (r == null) return null; left = new ASTNode { Type = NodeType.N_BINARY, BinaryOp = BinaryOp.B_BITXOR, Left = left, Right = r, Pos = left.Pos }; }
            return left;
        }

        private static ASTNode? ParseBitAnd(TokenList toks)
        {
            var left = ParseEquality(toks); if (left == null) return null;
            while (Match(toks, TokenType.T_AMP)) { var r = ParseEquality(toks); if (r == null) return null; left = new ASTNode { Type = NodeType.N_BINARY, BinaryOp = BinaryOp.B_BITAND, Left = left, Right = r, Pos = left.Pos }; }
            return left;
        }

        static ASTNode? ParseEquality(TokenList toks)
        {
            var left = ParseRelational(toks); if (left == null) return null;
            while (true)
            {
                if (Match(toks, TokenType.T_EQ)) { var r = ParseRelational(toks); if (r == null) return null; left = new ASTNode { Type = NodeType.N_BINARY, BinaryOp = BinaryOp.B_EQ, Left = left, Right = r, Pos = left.Pos }; }
                else if (Match(toks, TokenType.T_NEQ)) { var r = ParseRelational(toks); if (r == null) return null; left = new ASTNode { Type = NodeType.N_BINARY, BinaryOp = BinaryOp.B_NEQ, Left = left, Right = r, Pos = left.Pos }; }
                else break;
            }
            return left;
        }

        private static ASTNode? ParseRelational(TokenList toks)
        {
            var left = ParseShift(toks); if (left == null) return null;
            while (true)
            {
                if (Match(toks, TokenType.T_GT)) { var r = ParseShift(toks); if (r == null) return null; left = new ASTNode { Type = NodeType.N_BINARY, BinaryOp = BinaryOp.B_GT, Left = left, Right = r, Pos = left.Pos }; }
                else if (Match(toks, TokenType.T_GTE)) { var r = ParseShift(toks); if (r == null) return null; left = new ASTNode { Type = NodeType.N_BINARY, BinaryOp = BinaryOp.B_GTE, Left = left, Right = r, Pos = left.Pos }; }
                else if (Match(toks, TokenType.T_LT)) { var r = ParseShift(toks); if (r == null) return null; left = new ASTNode { Type = NodeType.N_BINARY, BinaryOp = BinaryOp.B_LT, Left = left, Right = r, Pos = left.Pos }; }
                else if (Match(toks, TokenType.T_LTE)) { var r = ParseShift(toks); if (r == null) return null; left = new ASTNode { Type = NodeType.N_BINARY, BinaryOp = BinaryOp.B_LTE, Left = left, Right = r, Pos = left.Pos }; }
                else break;
            }
            return left;
        }

        private static ASTNode? ParseShift(TokenList toks)
        {
            var left = ParseAdd(toks); if (left == null) return null;
            while (true)
            {
                if (Match(toks, TokenType.T_LSHIFT)) { var r = ParseAdd(toks); if (r == null) return null; left = new ASTNode { Type = NodeType.N_BINARY, BinaryOp = BinaryOp.B_LSHIFT, Left = left, Right = r, Pos = left.Pos }; }
                else if (Match(toks, TokenType.T_RSHIFT)) { var r = ParseAdd(toks); if (r == null) return null; left = new ASTNode { Type = NodeType.N_BINARY, BinaryOp = BinaryOp.B_RSHIFT, Left = left, Right = r, Pos = left.Pos }; }
                else break;
            }
            return left;
        }

        private static ASTNode? ParseAdd(TokenList toks)
        {
            var left = ParseMultiply(toks); if (left == null) return null;
            while (true)
            {
                if (Match(toks, TokenType.T_PLUS)) { var r = ParseMultiply(toks); if (r == null) return null; left = new ASTNode { Type = NodeType.N_BINARY, BinaryOp = BinaryOp.B_ADD, Left = left, Right = r, Pos = left.Pos }; }
                else if (Match(toks, TokenType.T_MINUS)) { var r = ParseMultiply(toks); if (r == null) return null; left = new ASTNode { Type = NodeType.N_BINARY, BinaryOp = BinaryOp.B_SUB, Left = left, Right = r, Pos = left.Pos }; }
                else break;
            }
            return left;
        }

        private static ASTNode? ParseMultiply(TokenList toks)
        {
            var left = ParseUnary(toks); if (left == null) return null;
            while (true)
            {
                if (Match(toks, TokenType.T_MUL)) { var r = ParseUnary(toks); if (r == null) return null; left = new ASTNode { Type = NodeType.N_BINARY, BinaryOp = BinaryOp.B_MUL, Left = left, Right = r, Pos = left.Pos }; }
                else if (Match(toks, TokenType.T_DIV)) { var r = ParseUnary(toks); if (r == null) return null; left = new ASTNode { Type = NodeType.N_BINARY, BinaryOp = BinaryOp.B_DIV, Left = left, Right = r, Pos = left.Pos }; }
                else break;
            }
            return left;
        }

        private static ASTNode? ParseUnary(TokenList toks)
        {
            if (Match(toks, TokenType.T_NOT)) { var op = ParseUnary(toks); if (op == null) return null; return new ASTNode { Type = NodeType.N_UNARY, UnaryOp = UnaryOp.U_NOT, Child = op, Pos = op.Pos }; }
            if (Match(toks, TokenType.T_TILDE)) { var op = ParseUnary(toks); if (op == null) return null; return new ASTNode { Type = NodeType.N_UNARY, UnaryOp = UnaryOp.U_BITNOT, Child = op, Pos = op.Pos }; }
            if (Match(toks, TokenType.T_MINUS)) { var op = ParseUnary(toks); if (op == null) return null; return new ASTNode { Type = NodeType.N_UNARY, UnaryOp = UnaryOp.U_NEG, Child = op, Pos = op.Pos }; }
            return ParsePower(toks);
        }

        private static ASTNode? ParsePower(TokenList toks)
        {
            var cur = toks.Peek();
            if (cur.Type == TokenType.T_IDENT && cur.Text != null)
            {
                // allow any identifier followed by '(' to be a function call (variadic)
                // lookup builtin/custom functions if present and enforce arity for builtins/customs
                if (toks.Arr.Count > toks.Idx + 1 && toks.Arr[toks.Idx + 1].Type == TokenType.T_LP)
                {
                    toks.Next();

                    if (!Match(toks, TokenType.T_LP))
                    {
                        return null;
                    }

                    var args = new List<ASTNode>();

                    if (!Match(toks, TokenType.T_RP))
                    {
                        while (true)
                        {
                            var a = ParseAssign(toks);

                            if (a == null)
                            {
                                return null;
                            }

                            args.Add(a);

                            if (Match(toks, TokenType.T_RP))
                            {
                                break;
                            }

                            if (!Match(toks, TokenType.T_COMMA))
                            {
                                return null;
                            }
                        }
                    }

                    var f = FindBuiltin(cur.Text) ?? FindCustom(cur.Text);

                    if (f != null && f.Value.arity >= 0 && f.Value.arity != args.Count)
                    {
                        return null;
                    }

                    return new ASTNode
                    {
                        Type = NodeType.N_FUNC,
                        Pos = cur.Pos,
                        FuncName = cur.Text,
                        FuncPtr = f?.func,
                        Args = args.ToArray(),
                        Argc = args.Count
                    };
                }
            }
            return ParsePrimary(toks);
        }

        private static ASTNode? ParsePrimary(TokenList toks)
        {
            var t = toks.Peek();

            if (t.Type == TokenType.T_NUM)
            {
                var tk = toks.Next();

                return new ASTNode
                {
                    Type = NodeType.N_NUMBER,
                    Pos = tk.Pos,
                    Number = tk.Num
                };
            }

            if (t.Type == TokenType.T_REALDB)
            {
                var tk = toks.Next();
                int id = int.Parse(tk.Text!);

                return new ASTNode
                {
                    Type = NodeType.N_REAL_DATABASE,
                    Pos = tk.Pos,
                    RealDataBaseId = id
                };
            }

            if (t.Type == TokenType.T_REALDB_LINK_DEV_REG)
            {
                var tk = toks.Next();

                return new ASTNode
                {
                    Type = NodeType.N_REALDB_LINK_DEV_REG,
                    Pos = tk.Pos,
                    LinkDevReg = tk.RealDbRef
                };
            }

            if (t.Type == TokenType.T_IDENT)
            {
                var tk = toks.Next();

                return new ASTNode
                {
                    Type = NodeType.N_VAR,
                    Pos = tk.Pos,
                    VarName = tk.Text
                };
            }

            if (t.Type == TokenType.T_IF)
            {
                var tk = toks.Next();

                if (!Match(toks, TokenType.T_LP))
                {
                    return null;
                }

                var cond = ParseAssign(toks);

                if (cond == null)
                {
                    return null;
                }

                if (!Match(toks, TokenType.T_RP))
                {
                    return null;
                }

                var thenNode = ParseAssign(toks);

                if (thenNode == null)
                {
                    return null;
                }

                ASTNode? elseNode = null;

                if (Match(toks, TokenType.T_ELSE))
                {
                    elseNode = ParseAssign(toks);

                    if (elseNode == null)
                    {
                        return null;
                    }
                }

                return new ASTNode
                {
                    Type = NodeType.N_IF,
                    Pos = tk.Pos,
                    Child = cond,
                    Left = thenNode,
                    Right = elseNode
                };
            }

            if (t.Type == TokenType.T_WHILE)
            {
                var tk = toks.Next();

                if (!Match(toks, TokenType.T_LP))
                {
                    return null;
                }

                var cond = ParseAssign(toks);

                if (cond == null)
                {
                    return null;
                }

                if (!Match(toks, TokenType.T_RP))
                {
                    return null;
                }

                var body = ParseAssign(toks);

                if (body == null)
                {
                    return null;
                }

                return new ASTNode
                {
                    Type = NodeType.N_WHILE,
                    Pos = tk.Pos,
                    Child = cond,
                    Left = body
                };
            }

            if (t.Type == TokenType.T_FOR)
            {
                var tk = toks.Next();

                if (!Match(toks, TokenType.T_LP))
                {
                    return null;
                }

                ASTNode? init = null;
                ASTNode? cond = null;
                ASTNode? iter = null;

                if (!Match(toks, TokenType.T_SEMI))
                {
                    init = ParseAssign(toks);

                    if (init == null)
                    {
                        return null;
                    }

                    if (!Match(toks, TokenType.T_SEMI))
                    {
                        return null;
                    }
                }

                if (!Match(toks, TokenType.T_SEMI))
                {
                    cond = ParseAssign(toks);

                    if (cond == null)
                    {
                        return null;
                    }

                    if (!Match(toks, TokenType.T_SEMI))
                    {
                        return null;
                    }
                }

                if (!Match(toks, TokenType.T_RP))
                {
                    iter = ParseAssign(toks);

                    if (iter == null)
                    {
                        return null;
                    }

                    if (!Match(toks, TokenType.T_RP))
                    {
                        return null;
                    }
                }

                var body = ParseAssign(toks);

                if (body == null)
                {
                    return null;
                }

                return new ASTNode
                {
                    Type = NodeType.N_FOR,
                    Pos = tk.Pos,
                    Args = new ASTNode[] { init, cond, iter },
                    Argc = 3,
                    Left = body
                };
            }

            if (t.Type == TokenType.T_RETURN)
            {
                var tk = toks.Next();
                ASTNode? expr = null;

                var pk = toks.Peek().Type;

                if (pk != TokenType.T_RP && pk != TokenType.T_EOF && pk != TokenType.T_SEMI)
                {
                    expr = ParseAssign(toks);

                    if (expr == null)
                    {
                        return null;
                    }
                }

                // optionally consume semicolon
                Match(toks, TokenType.T_SEMI);

                return new ASTNode { Type = NodeType.N_RETURN, Pos = tk.Pos, Child = expr };
            }

            if (Match(toks, TokenType.T_LP))
            {
                var v = ParseAssign(toks);

                if (v == null)
                {
                    return null;
                }

                if (!Match(toks, TokenType.T_RP))
                {
                    return null;
                }

                return v;
            }

            return null;
        }

        private static ASTNode? ParseStatement(TokenList toks)
        {
            if (Match(toks, TokenType.T_LBRACE))
            {
                var stmts = new List<ASTNode>();

                while (!Match(toks, TokenType.T_RBRACE))
                {
                    var s = ParseStatement(toks);

                    if (s == null)
                    {
                        return null;
                    }

                    stmts.Add(s);
                }

                return new ASTNode
                {
                    Type = NodeType.N_BLOCK,
                    Args = stmts.ToArray(),
                    Argc = stmts.Count
                };
            }
            if (Match(toks, TokenType.T_VAR))
            {
                var id = toks.Peek();
                if (id.Type != TokenType.T_IDENT) return null;
                var tk = toks.Next();
                ASTNode? init = null;
                if (Match(toks, TokenType.T_ASSIGN))
                {
                    init = ParseAssign(toks); if (init == null) return null;
                }
                Match(toks, TokenType.T_SEMI);

                return new ASTNode
                {
                    Type = NodeType.N_VDECL,
                    Pos = tk.Pos,
                    VarName = tk.Text,
                    Child = init
                };
            }
            var node = ParseAssign(toks);
            if (node == null) return null;
            // optional semicolon as statement terminator
            Match(toks, TokenType.T_SEMI);
            return node;
        }
    }
}