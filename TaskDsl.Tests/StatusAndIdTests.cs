namespace TaskDsl.Tests;

public class StatusAndIdTests
{
    [Theory]
    [InlineData("O")]
    [InlineData("o")]
    [InlineData("X")]
    [InlineData("x")]
    [InlineData("O!")]
    public void Accepts_Valid_Status(string status)
    {
        var t = Parser.ParseLine($"{status} [a] -- t", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        Assert.Equal("a", t.Id);
    }

    [Theory]
    [InlineData("Z")]
    [InlineData("OO")]
    [InlineData("")]
    public void Rejects_Invalid_Status(string status)
    {
        Assert.ThrowsAny<Exception>(() =>
            Parser.ParseLine($"{status} [a] -- t", TestUtil.ChicagoTz, TestUtil.FixedNowUtc));
    }

    [Theory]
    [InlineData("[a]")]
    [InlineData("[A1_-]")]
    [InlineData("[abc-123]")]
    public void Accepts_Valid_Id(string id)
    {
        var t = Parser.ParseLine($"O {id} -- t", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        Assert.Equal(id.Trim('[', ']'), t.Id);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("[bad id]")]
    [InlineData("[bad/id]")]
    [InlineData("[]")]
    public void Rejects_Invalid_Id(string id)
    {
        Assert.ThrowsAny<Exception>(() =>
            Parser.ParseLine($"O {id} -- t", TestUtil.ChicagoTz, TestUtil.FixedNowUtc));
    }

    [Fact]
    public void Title_Is_Optional()
    {
        var t = Parser.ParseLine("O [a]", TestUtil.ChicagoTz, TestUtil.FixedNowUtc);
        Assert.Equal("", t.Title);
    }
}