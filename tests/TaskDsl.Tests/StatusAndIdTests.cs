using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TaskDsl.Tests;

public class StatusAndIdTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Theory]
    [InlineData("O")]
    [InlineData("o")]
    [InlineData("X")]
    [InlineData("x")]
    [InlineData("O!")]
    public Task Accepts_Valid_Status(string status)
        => Given(() => status)
            .When(s => Parser.ParseLine($"{s} [a] -- t", TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t => Assert.Equal("a", t.Id))
            .AssertPassed();

    [Theory]
    [InlineData("Z")]
    [InlineData("OO")]
    [InlineData("")]
    public Task Rejects_Invalid_Status(string status)
        => Given(() => status)
            .When(s => new Action(() => Parser.ParseLine($"{s} [a] -- t", TestUtil.ChicagoTz, TestUtil.FixedNowUtc)))
            .Then(a => Assert.ThrowsAny<Exception>(a))
            .AssertPassed();

    [Theory]
    [InlineData("[a]")]
    [InlineData("[A1_-]")]
    [InlineData("[abc-123]")]
    public Task Accepts_Valid_Id(string id)
        => Given(() => id)
            .When(i => Parser.ParseLine($"O {i} -- t", TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t => Assert.Equal(id.Trim('[', ']'), t.Id))
            .AssertPassed();

    [Theory]
    [InlineData("a")]
    [InlineData("[bad id]")]
    [InlineData("[bad/id]")]
    [InlineData("[]")]
    public Task Rejects_Invalid_Id(string id)
        => Given(() => id)
            .When(i => new Action(() => Parser.ParseLine($"O {i} -- t", TestUtil.ChicagoTz, TestUtil.FixedNowUtc)))
            .Then(a => Assert.ThrowsAny<Exception>(a))
            .AssertPassed();

    [Fact]
    public Task Title_Is_Optional()
        => Given(() => "O [a]")
            .When(s => Parser.ParseLine(s, TestUtil.ChicagoTz, TestUtil.FixedNowUtc))
            .Then(t => Assert.Equal(string.Empty, t.Title))
            .AssertPassed();
}