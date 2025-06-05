using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace ExprAntlr
{
    internal class ExprAntlr
    {
        private static void Main(string[] args)
        {
            var input = "sin(1+2)*3";
            var inputStream = new AntlrInputStream(input);
            var lexer = new ExprLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new ExprParser(tokenStream);

            var tree = parser.prog();
            Console.WriteLine(tree.ToStringTree(parser));
        }
    }
}