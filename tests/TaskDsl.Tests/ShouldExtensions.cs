namespace TaskDsl.Tests;

public static class ShouldExtensions
{
    
    public static ValueTask ShouldBe(bool condition)
    {
        Assert.True(condition);
        return default;
    }

    public static ValueTask ShouldEqual<T>(IEnumerable<T> received, IEnumerable<T> expected)
    {
        // Order: expected then received to match xUnit signature
        Assert.Equal(expected, received);
        return default;
    }
}