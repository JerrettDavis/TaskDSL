namespace TaskDsl.Tests;

public class AttributeParsingTests
{
    [Fact]
    public void Parses_Assignees_Tags_Deps()
    {
        var line = "O [t] ^jd ^sam -work -bgis +[a] +[b] -- Title";
        var t = Parser.ParseLine(line, TestUtil.ChicagoTz, TestUtil.FixedNowUtc);

        Assert.True(t.Assignees.SetEquals(["jd", "sam"]));
        Assert.True(t.Tags.SetEquals(["work", "bgis"]));
        Assert.Equal(new[] { "a", "b" }, t.Dependencies.ToArray());
        Assert.Equal("Title", t.Title);
    }

    [Fact]
    public void Parses_Estimate_Priority_Context_Meta()
    {
        var line = "O [t] =45m p:3 @home @office meta:source=ops meta:ticket=BG-12 -- Do it";
        var t = Parser.ParseLine(line, TestUtil.ChicagoTz, TestUtil.FixedNowUtc);

        Assert.Equal(TimeSpan.FromMinutes(45), t.Estimate);
        Assert.True(t.Contexts.SetEquals(["home", "office"]));
        Assert.Equal("ops", t.Meta["source"]);
        Assert.Equal("BG-12", t.Meta["ticket"]);
    }

    [Theory]
    [InlineData("meta:x")]
    [InlineData("meta:=y")]
    [InlineData("meta:x=")]
    public void Bad_Meta_Throws(string meta)
    {
        Assert.ThrowsAny<Exception>(() =>
            Parser.ParseLine($"O [t] {meta} -- x", TestUtil.ChicagoTz, TestUtil.FixedNowUtc));
    }

    [Fact]
    public void Unknown_Token_Throws()
    {
        Assert.ThrowsAny<Exception>(() =>
            Parser.ParseLine("O [t] %weird -- x", TestUtil.ChicagoTz, TestUtil.FixedNowUtc));
    }
}