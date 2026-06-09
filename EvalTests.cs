using System;

namespace ConsoleApp1
{
    public static class EvalTests
    {
        private static void AssertApprox(string expr, double expected, double eps = 1e-9)
        {
            var ev = Eval.GetNewEvaluator(expr);
            if (ev == null) { Console.WriteLine($"Failed to parse: {expr}"); return; }
            double r = ev.Eval();
            if (double.IsNaN(expected))
            {
                Console.WriteLine($"{expr} => {r} (expected NaN)");
                return;
            }
            if (Math.Abs(r - expected) > eps)
            {
                Console.WriteLine($"Test FAILED: {expr} => {r} (expected {expected})");
            }
            else
            {
                Console.WriteLine($"Test OK: {expr} => {r}");
            }
        }

        public static void Run()
        {
            Console.WriteLine("--- Eval tests ---");
            AssertApprox("1+2*3", 7);
            AssertApprox("(1+2)*3", 9);
            AssertApprox("2+3==5", 1);
            AssertApprox("2+2==5", 0);
            AssertApprox("10/2", 5);
            AssertApprox("5-10", -5);
            AssertApprox("~1", ~1);
            AssertApprox("2<<3", 16);
            AssertApprox("8>>2", 2);
            AssertApprox("1 && 0", 0);
            AssertApprox("1 || 0", 1);
            AssertApprox("sin(30)", Math.Sin(30.0 * Math.PI / 180.0));
            AssertApprox("pi()", 3.14159265358979323846);
            AssertApprox("#42", 42.0); // GetValueByRealNo returns 42.0 and prints
            // assignment test: #1 = 5 should return 5
            AssertApprox("#1 = 5", 5);
            AssertApprox("#(2,4,8) = #5+1", 43.0);
            Console.WriteLine("--- Eval tests done ---");

            // interactive mode
            Console.WriteLine("input expression, with .exit to quit: ");
            while (true)
            {
                Console.Write("> ");
                var input = Console.ReadLine();
                if (input == ".exit") break;
                var ev = Eval.GetNewEvaluator(input);
                if (ev == null) { Console.WriteLine($"Failed to parse: {input}"); continue; }
                double r = ev.Eval();
                Console.WriteLine($"Result: {r}");
            }
        }
    }
}