using Tython.Enum;
using Tython.Model;

namespace Tython
{
    public class Optimizer(Statement[] statements)
    {
        readonly Statement[] statements = statements;

        public Statement[] Execute()
        {
            List<Statement> result = [];

            foreach (var statement in statements)
            {
                Expression expression = EvaluateExpression(statement.Expression);
                result.Add(new(statement.Token, expression, statement.Type));
            }

            return [.. result];
        }

        Expression EvaluateExpression(Expression expression)
        {
            switch (expression.Type)
            {
                case ExpressionType.Literal:
                    return expression;
                case ExpressionType.Variable:
                    return expression;
                case ExpressionType.Grouping:
                    return EvaluateExpression(expression.Primary);
                case ExpressionType.Unary:
                    {
                        Expression primary = EvaluateExpression(expression.Primary);

                        if (primary.Type != ExpressionType.Literal) return primary;

                        object value = primary.Token.Value;
                        bool isDouble = value is double;
                        bool isLong = value is long;
                        bool isBool = value is bool;

                        switch (expression.Token.Type)
                        {
                            case TokenType.Minus:
                                {
                                    if (isLong) value = -(long)value;
                                    else if (isDouble) value = -(double)value;
                                    else throw new Exception($"Operator - is not defined for {value}");
                                    break;
                                }
                            case TokenType.Not:
                                {
                                    if (isBool) value = !(bool)value;
                                    else throw new Exception($"Operator not is not defined for {value}");
                                    break;
                                }
                            default:
                                throw new Exception($"Operator {expression.Token.Value} is not unary");
                        }

                        Token token;
                        if (isDouble)
                            token = new(value, primary.Token.Line, TokenType.Real);
                        else if (isLong)
                            token = new(value, primary.Token.Line, TokenType.Int);
                        else
                            token = new((bool)value ? TokenType.True : TokenType.False, primary.Token.Line);

                        return new(token, ExpressionType.Literal);
                    }
                case ExpressionType.Binary:
                    {
                        Expression primary = EvaluateExpression(expression.Primary);
                        Expression secondary = EvaluateExpression(expression.Secondary);

                        if (primary.Type != ExpressionType.Literal || secondary.Type != ExpressionType.Literal)
                            return new(expression.Token, primary, secondary, ExpressionType.Binary);

                        object primaryValue = primary.Token.Value;
                        object secondaryValue = secondary.Token.Value;
                        object value;

                        bool isPrimaryDouble = primaryValue is double;
                        bool isPrimaryLong = primaryValue is long;
                        bool isPrimaryString = primaryValue is string;

                        bool isSecondaryDouble = secondaryValue is double;
                        bool isSecondaryLong = secondaryValue is long;
                        bool isSecondaryString = secondaryValue is string;

                        switch (expression.Token.Type)
                        {
                            case TokenType.Minus:
                                {
                                    if (isPrimaryLong && isSecondaryLong) value = (long)primaryValue - (long)secondaryValue;
                                    else if (isPrimaryDouble && isSecondaryDouble) value = (double)primaryValue - (double)secondaryValue;
                                    else if (isPrimaryLong && isSecondaryDouble) value = (long)primaryValue - (double)secondaryValue;
                                    else if (isPrimaryDouble && isSecondaryLong) value = (double)primaryValue - (long)secondaryValue;
                                    else throw new Exception($"Operator - is not defined for {primaryValue}, {secondaryValue}");
                                    break;
                                }
                            case TokenType.Plus:
                                {
                                    if (isPrimaryString && isSecondaryString) value = (string)primaryValue + (string)secondaryValue;
                                    else if (isPrimaryLong && isSecondaryLong) value = (long)primaryValue + (long)secondaryValue;
                                    else if (isPrimaryDouble && isSecondaryDouble) value = (double)primaryValue + (double)secondaryValue;
                                    else if (isPrimaryLong && isSecondaryDouble) value = (long)primaryValue + (double)secondaryValue;
                                    else if (isPrimaryDouble && isSecondaryLong) value = (double)primaryValue + (long)secondaryValue;
                                    else throw new Exception($"Operator + is not defined for {primary}, {secondary}");
                                    break;
                                }
                            case TokenType.Slash:
                                {
                                    if ((isSecondaryDouble && (double)secondaryValue == 0)
                                        || (isSecondaryLong && (long)secondaryValue == 0)) throw new Exception("Division by zero");

                                    if (isPrimaryLong && isSecondaryLong) value = (long)primaryValue / (long)secondaryValue;
                                    else if (isPrimaryDouble && isSecondaryDouble) value = (double)primaryValue / (double)secondaryValue;
                                    else if (isPrimaryLong && isSecondaryDouble) value = (long)primaryValue / (double)secondaryValue;
                                    else if (isPrimaryDouble && isSecondaryLong) value = (double)primaryValue / (long)secondaryValue;
                                    else throw new Exception($"Operator / is not defined for {primary}, {secondary}");
                                    break;
                                }
                            case TokenType.Star:
                                if (isPrimaryLong && isSecondaryLong) value = (long)primaryValue * (long)secondaryValue;
                                else if (isPrimaryDouble && isSecondaryDouble) value = (double)primaryValue * (double)secondaryValue;
                                else if (isPrimaryLong && isSecondaryDouble) value = (long)primaryValue * (double)secondaryValue;
                                else if (isPrimaryDouble && isSecondaryLong) value = (double)primaryValue * (long)secondaryValue;
                                else throw new Exception($"Operator * is not defined for {primary}, {secondary}");
                                break;
                            case TokenType.Greater:
                                {
                                    if (isPrimaryLong && isSecondaryLong) value = (long)primaryValue > (long)secondaryValue;
                                    else if (isPrimaryDouble && isSecondaryDouble) value = (double)primaryValue > (double)secondaryValue;
                                    else if (isPrimaryLong && isSecondaryDouble) value = (long)primaryValue > (double)secondaryValue;
                                    else if (isPrimaryDouble && isSecondaryLong) value = (double)primaryValue > (long)secondaryValue;
                                    else throw new Exception($"Operator > is not defined for {primary}, {secondary}");
                                    break;
                                }
                            case TokenType.GreaterEqual:
                                {
                                    if (isPrimaryLong && isSecondaryLong) value = (long)primaryValue >= (long)secondaryValue;
                                    else if (isPrimaryDouble && isSecondaryDouble) value = (double)primaryValue >= (double)secondaryValue;
                                    else if (isPrimaryLong && isSecondaryDouble) value = (long)primaryValue >= (double)secondaryValue;
                                    else if (isPrimaryDouble && isSecondaryLong) value = (double)primaryValue >= (long)secondaryValue;
                                    else throw new Exception($"Operator >= is not defined for {primary}, {secondary}");
                                    break;
                                }
                            case TokenType.Less:
                                {
                                    if (isPrimaryLong && isSecondaryLong) value = (long)primaryValue < (long)secondaryValue;
                                    else if (isPrimaryDouble && isSecondaryDouble) value = (double)primaryValue < (double)secondaryValue;
                                    else if (isPrimaryLong && isSecondaryDouble) value = (long)primaryValue < (double)secondaryValue;
                                    else if (isPrimaryDouble && isSecondaryLong) value = (double)primaryValue < (long)secondaryValue;
                                    else throw new Exception($"Operator < is not defined for {primary}, {secondary}");
                                    break;
                                }
                            case TokenType.LessEqual:
                                {
                                    if (isPrimaryLong && isSecondaryLong) value = (long)primaryValue <= (long)secondaryValue;
                                    else if (isPrimaryDouble && isSecondaryDouble) value = (double)primaryValue <= (double)secondaryValue;
                                    else if (isPrimaryLong && isSecondaryDouble) value = (long)primaryValue <= (double)secondaryValue;
                                    else if (isPrimaryDouble && isSecondaryLong) value = (double)primaryValue <= (long)secondaryValue;
                                    else throw new Exception($"Operator <= is not defined for {primary}, {secondary}");
                                    break;
                                }
                            case TokenType.Equal:
                                {
                                    value = primaryValue.Equals(secondaryValue);
                                    break;
                                }
                            case TokenType.NotEqual:
                                {
                                    value = !primaryValue.Equals(secondaryValue);
                                    break;
                                }
                            default:
                                throw new Exception($"Operator {expression.Token.Value} is not binary");
                        }

                        Token token;
                        if (value is double)
                            token = new(value, primary.Token.Line, TokenType.Real);
                        else if (value is long)
                            token = new(value, primary.Token.Line, TokenType.Int);
                        else if (value is string)
                            token = new(value, primary.Token.Line, TokenType.String);
                        else
                            token = new((bool)value ? TokenType.True : TokenType.False, primary.Token.Line);

                        return new(token, ExpressionType.Literal);
                    }
            }

            return expression;
        }
    }
}
