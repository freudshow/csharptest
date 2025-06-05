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
            var input = "(#909 == 32768) || (#909 == 4864) || (#909 == 5376) || (#909 == 5120) || (#909 == 4608) || (#909 == 5632) || (#909 == 21760)";
            var inputStream = new AntlrInputStream(input);
            var lexer = new ExprLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new ExprParser(tokenStream);

            var tree = parser.prog();
            Console.WriteLine(tree.ToStringTree(parser));

            Console.ReadLine();
        }
    }
}