using argument;
using cosmos_error;

namespace tests;

/// <summary>
/// Unit tests for the argument module: ArgumentTypeJudge and ArgumentConvert.
/// </summary>
public class ArgumentTests
{
    // ── ArgumentTypeJudge ──────────────────────────────────────────

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("-42")]
    [InlineData("999999")]
    public void Judge_Integers_ReturnsNumber(string tk)
    {
        Assert.Equal(ArgumentType.Number, ArgumentTypeJudge.Judge(tk));
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("3.14")]
    [InlineData("-2.5")]
    [InlineData(".5")]
    public void Judge_Floats_ReturnsFloat(string tk)
    {
        Assert.Equal(ArgumentType.Float, ArgumentTypeJudge.Judge(tk));
    }

    [Theory]
    [InlineData("\"hello\"")]
    [InlineData("'world'")]
    [InlineData("\"\"")]
    [InlineData("''")]
    public void Judge_QuotedStrings_ReturnsString(string tk)
    {
        Assert.Equal(ArgumentType.String, ArgumentTypeJudge.Judge(tk));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Judge_NullOrEmpty_ThrowsArgumentNull(string? tk)
    {
        var ex = Assert.Throws<CosmosArgumentException>(() => ArgumentTypeJudge.Judge(tk!));
        Assert.Equal(ErrorCode.ArgumentNull, ex.ErrorCode);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12abc")]
    public void Judge_UnrecognizedFormat_ThrowsArgumentFormatInvalid(string tk)
    {
        var ex = Assert.Throws<CosmosArgumentException>(() => ArgumentTypeJudge.Judge(tk));
        Assert.Equal(ErrorCode.ArgumentFormatInvalid, ex.ErrorCode);
    }

    // ── ArgumentConvert.ToNumber ───────────────────────────────────

    [Theory]
    [InlineData("0", 0L)]
    [InlineData("42", 42L)]
    [InlineData("-100", -100L)]
    public void ToNumber_ValidInteger_ReturnsLong(string tk, long expected)
    {
        Assert.Equal(expected, ArgumentConvert.ToNumber(tk));
    }

    [Fact]
    public void ToNumber_FloatInput_ThrowsTypeMismatch()
    {
        var ex = Assert.Throws<CosmosArgumentException>(() => ArgumentConvert.ToNumber("3.14"));
        Assert.Equal(ErrorCode.ArgumentTypeMismatch, ex.ErrorCode);
    }

    [Fact]
    public void ToNumber_StringInput_ThrowsTypeMismatch()
    {
        var ex = Assert.Throws<CosmosArgumentException>(() => ArgumentConvert.ToNumber("\"hello\""));
        Assert.Equal(ErrorCode.ArgumentTypeMismatch, ex.ErrorCode);
    }

    // ── ArgumentConvert.ToFloat ────────────────────────────────────

    [Theory]
    [InlineData("1.0", 1.0)]
    [InlineData("3.14", 3.14)]
    [InlineData("-2.5", -2.5)]
    public void ToFloat_ValidFloat_ReturnsDouble(string tk, double expected)
    {
        Assert.Equal(expected, ArgumentConvert.ToFloat(tk), 1e-10);
    }

    [Fact]
    public void ToFloat_IntegerInput_ThrowsTypeMismatch()
    {
        var ex = Assert.Throws<CosmosArgumentException>(() => ArgumentConvert.ToFloat("42"));
        Assert.Equal(ErrorCode.ArgumentTypeMismatch, ex.ErrorCode);
    }

    [Fact]
    public void ToFloat_StringInput_ThrowsTypeMismatch()
    {
        var ex = Assert.Throws<CosmosArgumentException>(() => ArgumentConvert.ToFloat("\"hello\""));
        Assert.Equal(ErrorCode.ArgumentTypeMismatch, ex.ErrorCode);
    }

    // ── ArgumentConvert.ToString_ ──────────────────────────────────

    [Theory]
    [InlineData("\"hello\"", "hello")]
    [InlineData("'world'", "world")]
    [InlineData("\"\"", "")]
    public void ToString__ValidString_ReturnsStrippedContent(string tk, string expected)
    {
        Assert.Equal(expected, ArgumentConvert.ToString_(tk));
    }

    [Fact]
    public void ToString__IntegerInput_ThrowsTypeMismatch()
    {
        var ex = Assert.Throws<CosmosArgumentException>(() => ArgumentConvert.ToString_("42"));
        Assert.Equal(ErrorCode.ArgumentTypeMismatch, ex.ErrorCode);
    }

    [Fact]
    public void ToString__FloatInput_ThrowsTypeMismatch()
    {
        var ex = Assert.Throws<CosmosArgumentException>(() => ArgumentConvert.ToString_("3.14"));
        Assert.Equal(ErrorCode.ArgumentTypeMismatch, ex.ErrorCode);
    }

    // ── Overflow scenarios ─────────────────────────────────────────

    [Fact]
    public void ToFloat_Overflow_ThrowsArgumentOverflow()
    {
        // Exceeds Double.MaxValue, parsed as PositiveInfinity
        var ex = Assert.Throws<CosmosArgumentException>(() =>
            ArgumentConvert.ToFloat("1.0e999999"));
        Assert.Equal(ErrorCode.ArgumentOverflow, ex.ErrorCode);
    }
}
