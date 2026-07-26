using cosmos_error;

namespace argument;

public class ArgumentTypeJudge
{
    public static ArgumentType Judge(string tk)
    {
        // Null or empty input is invalid
        if (string.IsNullOrEmpty(tk))
            throw new CosmosArgumentException(ErrorCode.ArgumentNull,
                "Invalid argument: null or empty token");

        // String: content wrapped in single or double quotes
        if ((tk.StartsWith('\'') && tk.EndsWith('\''))
            || (tk.StartsWith('"') && tk.EndsWith('"')))
            return ArgumentType.String;

        // Int (Number): pure integer like 1, -42, 0 — must check BEFORE Float
        if (long.TryParse(tk, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out _))
            return ArgumentType.Number;

        // Float: matches patterns like 1.0, .5, 1f, 1d, 1.0f, 1.0d
        if (float.TryParse(tk, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out _))
            return ArgumentType.Float;

        // Unrecognized token format
        throw new CosmosArgumentException(ErrorCode.ArgumentFormatInvalid,
            $"Invalid argument: unrecognized token format '{tk}'");
    }
}