using System;
using System.Collections.Generic;

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

public class ArithmeticToken
{
    public ArithmeticTokenType Type { get; }
    public string Value { get; }

    public ArithmeticToken(ArithmeticTokenType type, string value = null)
    {
        Type = type;
        Value = value;
    }
}

public class Tokenizer
{
    private string input;
    private int pos = 0;

    public Tokenizer(string input)
    {
        this.input = input;
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
                throw new Exception("Invalid character: " + c);
            }
        }
        tokens.Add(new ArithmeticToken(ArithmeticTokenType.EOF));
        return tokens;
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
                hasDot = true;
                pos++;
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
        // Validate number format (e.g., not just ".")
        if (number == "." || number.StartsWith(".") && !char.IsDigit(number[1]) || number.EndsWith("."))
        {
            throw new Exception("Invalid number format: " + number);
        }
        return number;
    }
}

public class ArithmeticParser
{
    private List<ArithmeticToken> tokens;
    private int pos = 0;

    public ArithmeticParser(List<ArithmeticToken> tokens)
    {
        this.tokens = tokens;
    }

    public bool Parse()
    {
        try
        {
            ParseExpression();
            return pos == tokens.Count - 1 && tokens[pos].Type == ArithmeticTokenType.EOF;
        }
        catch
        {
            return false;
        }
    }

    private void ParseExpression()
    {
        ParseTerm();
        while (tokens[pos].Type == ArithmeticTokenType.PLUS || tokens[pos].Type == ArithmeticTokenType.MINUS)
        {
            Consume(tokens[pos].Type);
            ParseTerm();
        }
    }

    private void ParseTerm()
    {
        ParseFactor();
        while (tokens[pos].Type == ArithmeticTokenType.MULTIPLY || tokens[pos].Type == ArithmeticTokenType.DIVIDE)
        {
            Consume(tokens[pos].Type);
            ParseFactor();
        }
    }

    private void ParseFactor()
    {
        if (tokens[pos].Type == ArithmeticTokenType.MINUS)
        {
            Consume(ArithmeticTokenType.MINUS);
            ParseFactor();
        }
        else if (tokens[pos].Type == ArithmeticTokenType.NUMBER)
        {
            Consume(ArithmeticTokenType.NUMBER);
        }
        else if (tokens[pos].Type == ArithmeticTokenType.LPAREN)
        {
            Consume(ArithmeticTokenType.LPAREN);
            ParseExpression();
            Consume(ArithmeticTokenType.RPAREN);
        }
        else
        {
            throw new Exception("Unexpected token: " + tokens[pos].Type);
        }
    }

    private void Consume(ArithmeticTokenType expected)
    {
        if (tokens[pos].Type == expected)
        {
            pos++;
        }
        else
        {
            throw new Exception("Expected " + expected + ", got " + tokens[pos].Type);
        }
    }
}

internal class ArithmeticApps
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
            bool isValid = parser.Parse();
            Console.WriteLine("Is valid: " + isValid);
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}