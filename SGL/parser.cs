using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

public static class CssLikeParser
{
    // Basic tokens
    private static readonly Parser<char, char> LBrace = Char('{');
    private static readonly Parser<char, char> RBrace = Char('}');
    private static readonly Parser<char, char> LParen = Char('(');
    private static readonly Parser<char, char> RParen = Char(')');
    private static readonly Parser<char, char> LBracket = Char('[');
    private static readonly Parser<char, char> RBracket = Char(']');
    private static readonly Parser<char, char> Semicolon = Char(';');
    private static readonly Parser<char, char> Colon = Char(':');
    private static readonly Parser<char, char> Comma = Char(',');
    private static readonly Parser<char, char> Dot = Char('.');
    private static readonly Parser<char, char> Slash = Char('/');
    private static readonly Parser<char, char> Quote = Char('"');
    
    // Skip whitespace
    private static Parser<char, T> Tok<T>(Parser<char, T> parser) 
        => parser.Between(SkipWhitespaces);

    private static Parser<char, string> Comment 
        => Try(String("/*")).Then(AnyCharExcept('*').ManyString())
            .Before(Try(String("*/"))).Between(SkipWhitespaces);

    // Identifiers
    private static readonly Parser<char, string> Identifier =
        Tok(Letter.Or(Char('_')).Then(LetterOrDigit.Or(Char('_')).ManyString(), (h, t) => h + t));

    // String literals with interpolation
    private static readonly Parser<char, char> StringChar = 
        AnyCharExcept('"', '\\', '$', '\r', '\n').Or(EscapedChar);
    
    private static readonly Parser<char, char> EscapedChar =
        Char('\\').Then(OneOf('"', '\\', '/', 'b', 'f', 'n', 'r', 't'));
    
    private static readonly Parser<char, Value> Interpolation =
        String("${").Then(Rec(() => Expression!)).Before(Char('}'));
    
    private static readonly Parser<char, (string Text, bool IsInterpolation)> StringPart =
        OneOf(
            Interpolation.Select(x => (Text: "", IsInterpolation: true)),
            StringChar.AtLeastOnceString().Select(s => (s, false))
        );
    
    private static readonly Parser<char, StringValue> StringLiteral =
        Tok(Quote)
            .Then(StringPart.Many())
            .Before(Quote)
            .Select(parts => {
                var builder = new StringBuilder();
                var interpolations = new List<Interpolation>();
                
                foreach (var (text, isInterp) in parts)
                {
                    if (isInterp)
                    {
                        var start = builder.Length;
                        builder.Append('\0'); // placeholder
                        interpolations.Add(new Interpolation(start, 1, null!)); // filled later
                    }
                    else
                    {
                        builder.Append(text);
                    }
                }
                
                return new StringValue(builder.ToString(), interpolations);
            });

    // Values
    private static readonly Parser<char, Value> StringValueParser = 
        StringLiteral.Select<Value>(s => s);
    
    private static readonly Parser<char, Value> NumberValueParser =
        Tok(Real).Select(d => (Value)new NumberValue(d));
    
    private static readonly Parser<char, Value> BoolValueParser =
        Try(String("true")).Then(Return<Value>(new BoolValue(true)))
            .Or(Try(String("false")).Then(Return<Value>(new BoolValue(false))));
    
    private static readonly Parser<char, Value> NullValueParser =
        String("null").Then(Return<Value>(new NullValue()));
    
    private static readonly Parser<char, Value> ArrayValueParser =
        ValueList.Between(LBracket, RBracket)
            .Select(items => (Value)new ArrayValue(items));
    
    private static readonly Parser<char, Value> ObjectValueParser =
        KeyValuePairs.Between(LBrace, RBrace)
            .Select(props => (Value)new ObjectValue(props.ToDictionary()));

    // Expressions
    private static readonly Parser<char, Value>? Expression = null;
    
    private static readonly Parser<char, Value> PrimaryExpression =
        OneOf(
            StringValueParser,
            NumberValueParser,
            BoolValueParser,
            NullValueParser,
            ArrayValueParser,
            ObjectValueParser,
            VarFunctionParser,
            LParen.Then(Rec(() => Expression!)).Before(RParen)
        );

    // Initialize recursive parser
    static CssLikeParser()
    {
        Expression = BuildExpressionParser();
    }
    
    private static Parser<char, Value> BuildExpressionParser()
    {
        // Build precedence climbing parser here
        // Implementation details for operator precedence
        return PrimaryExpression; // Simplified
    }

    public static Stylesheet Parse(string input)
    {
        var parser = StylesheetParser;
        var result = parser.Parse(input);
        if (!result.Success)
            throw new FormatException($"Parse error: {result.Error}");
        return result.Value;
    }
}