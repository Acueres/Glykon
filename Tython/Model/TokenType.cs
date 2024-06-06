namespace Tython.Model
{
    public enum TokenType : byte
    {
        Null,
        //literals
        Identifier, Int, Real, True, False, String, None,

        //symbols
        BracketLeft, BracketRight, //[]
        ParenthesisLeft, ParenthesisRight, //()
        BraceLeft, BraceRight, //{}

        Comma, Dot, Minus, Plus, Semicolon, Colon, Slash, SlashDouble, Star, StarDouble, Assignment, //, . - + ; : / // * ** =

        NotEqual, Equal, //!= ==
        Greater, GreaterEqual, //> >=
        Less, LessEqual, //< <=

        //keywords
        Class, Struct, Interface, Enum, Def, Let, Print,
        And, Not, Or,
        If, Else, Elif, For, While, Return, Break, Continue,

        EOF
    }
}
