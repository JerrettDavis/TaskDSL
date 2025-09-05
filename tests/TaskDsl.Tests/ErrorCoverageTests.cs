using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace TaskDsl.Tests;

public class ErrorCoverageTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public Task Empty_Line_Throws(string line)
        => Given(() => line)
            .When(l => new Action(() => Parser.ParseLine(l, TestUtil.ChicagoTz, TestUtil.FixedNowUtc)))
            .Then(a => Assert.Throws<ArgumentException>(a))
            .AssertPassed();

    [Theory]
    [InlineData("O")]
    [InlineData("O    ")]
    [InlineData("O -- title only")]
    public Task Missing_Id_Throws(string line)
        => Given(() => line)
            .When(l => new Action(() => Parser.ParseLine(l, TestUtil.ChicagoTz, TestUtil.FixedNowUtc)))
            .Then(a => Assert.Throws<FormatException>(a))
            .AssertPassed();

    [Fact]
    public Task Dependency_Id_Must_Be_Simple()
        => Given(() => "O [t] +[bad id] -- x")
            .When(l => new Action(() => Parser.ParseLine(l, TestUtil.ChicagoTz, TestUtil.FixedNowUtc)))
            .Then(a => Assert.ThrowsAny<Exception>(a))
            .AssertPassed();
}