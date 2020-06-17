namespace SelectParser
{
    public enum SelectToken
    {
        None,
        Identifier,
        // literals
        StringLiteral,
        NumberLiteral,
        BooleanLiteral,
        // keywords
        Select,
        As,
        From,
        Where,
        Order,
        By,
        Limit,
        Offset,
        Asc,
        Desc,
        Is,
        Missing,
        // operators
        Dot,
        Comma,
        LeftBracket,
        RightBracket,
        Star,

        Not,
        Negate,

        And,
        Or,

        Lesser,
        Greater,
        LesserOrEqual,
        GreaterOrEqual,
        Equal,
        NotEqual,

        Add,
        Divide,
        Modulo,

        Between,
        In,
        Like,
        Escape,
    }
}