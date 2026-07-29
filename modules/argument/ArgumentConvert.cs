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
        try
        {
            return long.Parse(tk, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (OverflowException)
        {
            throw new CosmosArgumentException(ErrorCode.ArgumentOverflow,
                $"Number out of range: '{tk}' exceeds Int64 bounds");
        }
    }

    public static double ToFloat(string tk)
    {
        var type = ArgumentTypeJudge.Judge(tk);
        if (type != ArgumentType.Float)
            throw new CosmosArgumentException(ErrorCode.ArgumentTypeMismatch,
                $"Type mismatch: expected Float, got {type}");
        try
        {
            var result = double.Parse(tk, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture);
            if (double.IsInfinity(result))
                throw new CosmosArgumentException(ErrorCode.ArgumentOverflow,
                    $"Float out of range: '{tk}' exceeds Double bounds");
            return result;
        }
        catch (OverflowException)
        {
            throw new CosmosArgumentException(ErrorCode.ArgumentOverflow,
                $"Float out of range: '{tk}' exceeds Double bounds");
        }
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