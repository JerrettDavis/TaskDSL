namespace TaskDsl.Tests;

public class ErrorCoverageTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_Line_Throws(string line)
    {
        Assert.Throws<ArgumentException>(() =>
            Parser.ParseLine(line, TestUtil.ChicagoTz, TestUtil.FixedNowUtc));
    }

    [Theory]
    [InlineData("O")]
    [InlineData("O    ")]
    [InlineData("O -- title only")]
    public void Missing_Id_Throws(string line)
    {
        Assert.Throws<FormatException>(() =>
            Parser.ParseLine(line, TestUtil.ChicagoTz, TestUtil.FixedNowUtc));
    }

    [Fact]
    public void Dependency_Id_Must_Be_Simple()
    {
        Assert.ThrowsAny<Exception>(() =>
            Parser.ParseLine("O [t] +[bad id] -- x", TestUtil.ChicagoTz, TestUtil.FixedNowUtc));
    }
}