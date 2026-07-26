using cosmos_error;

namespace argument;

public class ArgumentConvert
{
    public static long ToNumber(string tk)
    {
        var type = ArgumentTypeJudge.Judge(tk);
        if (type != ArgumentType.Number)
            throw new CosmosArgumentException(ErrorCode.ArgumentTypeMismatch,
                $"Type mismatch: expected Number, got {type}");
        return long.Parse(tk, System.Globalization.CultureInfo.InvariantCulture);
    }

    public static double ToFloat(string tk)
    {
        var type = ArgumentTypeJudge.Judge(tk);
        if (type != ArgumentType.Float)
            throw new CosmosArgumentException(ErrorCode.ArgumentTypeMismatch,
                $"Type mismatch: expected Float, got {type}");
        return double.Parse(tk, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture);
    }

    public static string ToString_(string tk)
    {
        var type = ArgumentTypeJudge.Judge(tk);
        if (type != ArgumentType.String)
            throw new CosmosArgumentException(ErrorCode.ArgumentTypeMismatch,
                $"Type mismatch: expected String, got {type}");
        // Strip surrounding quotes (single or double)
        return tk[1..^1];
    }


}