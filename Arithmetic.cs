using System;
using System.Collections.Generic;

public enum TokenType
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

public class Token
{
    public TokenType Type { get; }
    public string Value { get; }

    public Token(TokenType type, string value = null)
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

    public List<Token> Tokenize()
    {
        List<Token> tokens = new List<Token>();
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
                tokens.Add(new Token(TokenType.NUMBER, number));
            }
            else if (c == '+')
            {
                tokens.Add(new Token(TokenType.PLUS));
                pos++;
            }
            else if (c == '-')
            {
                tokens.Add(new Token(TokenType.MINUS));
                pos++;
            }
            else if (c == '*')
            {
                tokens.Add(new Token(TokenType.MULTIPLY));
                pos++;
            }
            else if (c == '/')
            {
                tokens.Add(new Token(TokenType.DIVIDE));
                pos++;
            }
            else if (c == '(')
            {
                tokens.Add(new Token(TokenType.LPAREN));
                pos++;
            }
            else if (c == ')')
            {
                tokens.Add(new Token(TokenType.RPAREN));
                pos++;
            }
            else
            {
                throw new Exception("Invalid character: " + c);
            }
        }
        tokens.Add(new Token(TokenType.EOF));
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

public class Parser
{
    private List<Token> tokens;
    private int pos = 0;

    public Parser(List<Token> tokens)
    {
        this.tokens = tokens;
    }

    public bool Parse()
    {
        try
        {
            ParseExpression();
            return pos == tokens.Count - 1 && tokens[pos].Type == TokenType.EOF;
        }
        catch
        {
            return false;
        }
    }

    private void ParseExpression()
    {
        ParseTerm();
        while (tokens[pos].Type == TokenType.PLUS || tokens[pos].Type == TokenType.MINUS)
        {
            Consume(tokens[pos].Type);
            ParseTerm();
        }
    }

    private void ParseTerm()
    {
        ParseFactor();
        while (tokens[pos].Type == TokenType.MULTIPLY || tokens[pos].Type == TokenType.DIVIDE)
        {
            Consume(tokens[pos].Type);
            ParseFactor();
        }
    }

    private void ParseFactor()
    {
        if (tokens[pos].Type == TokenType.MINUS)
        {
            Consume(TokenType.MINUS);
            ParseFactor();
        }
        else if (tokens[pos].Type == TokenType.NUMBER)
        {
            Consume(TokenType.NUMBER);
        }
        else if (tokens[pos].Type == TokenType.LPAREN)
        {
            Consume(TokenType.LPAREN);
            ParseExpression();
            Consume(TokenType.RPAREN);
        }
        else
        {
            throw new Exception("Unexpected token: " + tokens[pos].Type);
        }
    }

    private void Consume(TokenType expected)
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

internal class Arithmetic
{
    private static void ArithmeticMain(string[] args)
    {
        Console.WriteLine("Enter an arithmetic expression (e.g., 2 + 3 * (4 - 1)):");
        string input = Console.ReadLine();
        try
        {
            Tokenizer tokenizer = new Tokenizer(input);
            List<Token> tokens = tokenizer.Tokenize();
            Parser parser = new Parser(tokens);
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