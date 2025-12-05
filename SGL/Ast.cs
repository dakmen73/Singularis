// Core AST
public abstract record Node;
public record Stylesheet(IReadOnlyList<Node> Statements) : Node;

public abstract record Statement : Node;
public record Rule(Selector Selector, IReadOnlyList<Statement> Body) : Statement;
public record Declaration(string Name, Value Value) : Statement;

// At-rules
public abstract record AtRule : Statement;
public record IfRule(Value Condition, IReadOnlyList<Statement> ThenBranch, IReadOnlyList<Statement>? ElseBranch) : AtRule;
public record ForeachRule(string Variable, Value Sequence, IReadOnlyList<Statement> Body) : AtRule;
public record ExecRule(Value Command, Value? WorkingDirectory, Value? Timeout) : AtRule;
public record MixinDecl(string Name, IReadOnlyList<string> Parameters, IReadOnlyList<Statement> Body) : AtRule;
public record MixinInclude(string Name, IReadOnlyList<Value> Arguments) : AtRule;
public record RootRule(IReadOnlyList<Statement> Body) : AtRule;

// Selectors
public abstract record Selector;
public record PathSelector(IReadOnlyList<PathSegment> Segments) : Selector;
public record FileSelector(string Pattern) : Selector;

public abstract record PathSegment;
public record LiteralSegment(string Text) : PathSegment;
public record InterpolatedSegment(Value Expression) : PathSegment;

// Values
public abstract record Value;
public record StringValue(string Text, IReadOnlyList<Interpolation>? Interpolations = null) : Value;
public record NumberValue(double Value) : Value;
public record BoolValue(bool Value) : Value;
public record NullValue : Value;
public record ArrayValue(IReadOnlyList<Value> Items) : Value;
public record ObjectValue(IReadOnlyDictionary<string, Value> Properties) : Value;
public record VarFunction(string VariableName) : Value;

public record Interpolation(int Start, int Length, Value Expression);