using TinyBDD;

namespace TaskDsl.Tests;

public static class TinyBddExtensions
{
    public static ThenChain<T> Then<T>(this ScenarioChain<T> chain, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(chain);
        ArgumentNullException.ThrowIfNull(action);
        return chain.Then(t =>
        {
            action(t);
            return new ValueTask();
        });
    }

    public static ThenChain<T> Then<T>(this ScenarioChain<T> chain, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(chain);
        ArgumentNullException.ThrowIfNull(predicate);
        return chain.Then(t => new ValueTask<bool>(predicate(t)));
    }

    public static ThenChain<T> And<T>(this ThenChain<T> chain, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return chain.And(t =>
        {
            action(t);
            return new ValueTask();
        });
    }

    public static ThenChain<T> And<T>(this ThenChain<T> chain, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return chain.And(t => new ValueTask<bool>(predicate(t)));
    }
}