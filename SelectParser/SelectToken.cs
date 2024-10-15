namespace SelectParser;

public enum SelectToken
{
    Error = -2,
    Eof = -1,
    Unknown = 0,

    Identifier = 1,

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
    Null,

    // functions
    Avg,
    Count,
    Max,
    Min,
    Sum,

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
    Concat,

    Between,
    In,
    Like,
    Escape,
}