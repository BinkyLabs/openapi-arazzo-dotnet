using resultsComparer.Models;

namespace resultsComparer.Policies;

internal sealed class ZeroPointOnePercentDifferenceMemoryUsagePolicy : PercentageMemoryUsagePolicy
{
    public static ZeroPointOnePercentDifferenceMemoryUsagePolicy Instance { get; } = new();

    protected override string TypeName => nameof(ZeroPointOnePercentDifferenceMemoryUsagePolicy);

    public ZeroPointOnePercentDifferenceMemoryUsagePolicy()
        : base(0.1f)
    {
    }
}

internal sealed class ZeroPointTwoPercentDifferenceMemoryUsagePolicy : PercentageMemoryUsagePolicy
{
    public static ZeroPointTwoPercentDifferenceMemoryUsagePolicy Instance { get; } = new();

    protected override string TypeName => nameof(ZeroPointTwoPercentDifferenceMemoryUsagePolicy);

    public ZeroPointTwoPercentDifferenceMemoryUsagePolicy()
        : base(0.2f)
    {
    }
}

internal sealed class OnePercentDifferenceMemoryUsagePolicy : PercentageMemoryUsagePolicy
{
    public static OnePercentDifferenceMemoryUsagePolicy Instance { get; } = new();

    protected override string TypeName => nameof(OnePercentDifferenceMemoryUsagePolicy);

    public OnePercentDifferenceMemoryUsagePolicy()
        : base(1)
    {
    }
}

internal sealed class TwoPercentDifferenceMemoryUsagePolicy : PercentageMemoryUsagePolicy
{
    public static TwoPercentDifferenceMemoryUsagePolicy Instance { get; } = new();

    protected override string TypeName => nameof(TwoPercentDifferenceMemoryUsagePolicy);

    public TwoPercentDifferenceMemoryUsagePolicy()
        : base(2)
    {
    }
}

internal sealed class FivePercentDifferenceMemoryUsagePolicy : PercentageMemoryUsagePolicy
{
    public static FivePercentDifferenceMemoryUsagePolicy Instance { get; } = new();

    protected override string TypeName => nameof(FivePercentDifferenceMemoryUsagePolicy);

    public FivePercentDifferenceMemoryUsagePolicy()
        : base(5)
    {
    }
}

internal abstract class PercentageMemoryUsagePolicy(float tolerancePercentagePoints) : BaseBenchmarkComparisonPolicy
{
    private float TolerancePercentagePoints { get; } = Math.Abs(tolerancePercentagePoints);

    public override bool Equals(BenchmarkMemory? x, BenchmarkMemory? y)
    {
        if (x?.AllocatedBytes == y?.AllocatedBytes)
        {
            return true;
        }

        if (x?.AllocatedBytes is null || y?.AllocatedBytes is null || x.AllocatedBytes == 0 || y.AllocatedBytes == 0)
        {
            return false;
        }

        var forwardRatio = GetPercentageDifference(x, y);
        var backwardRatio = GetPercentageDifference(y, x);
        return forwardRatio <= TolerancePercentagePoints && backwardRatio <= TolerancePercentagePoints;
    }

    public override string GetErrorMessage(BenchmarkMemory? x, BenchmarkMemory? y)
    {
        if (x?.AllocatedBytes is null || y?.AllocatedBytes is null)
        {
            return "One of the benchmarks is null.";
        }

        return $"Allocated bytes differ: {x.AllocatedBytes} != {y.AllocatedBytes}, Ratio: {GetRatio(x, y)}, Allowed: {TolerancePercentagePoints}%";
    }

    private static double GetPercentageDifference(BenchmarkMemory x, BenchmarkMemory y)
    {
        return Math.Truncate(Math.Abs(GetRatio(x, y)) * 10000) / 100;
    }

    private static double GetRatio(BenchmarkMemory x, BenchmarkMemory y)
    {
        var xAllocatedBytes = x.AllocatedBytes;
        var yAllocatedBytes = y.AllocatedBytes;

        if (xAllocatedBytes is null || yAllocatedBytes is null)
        {
            throw new ArgumentException("AllocatedBytes cannot be null.");
        }

        return (double)(yAllocatedBytes.Value - xAllocatedBytes.Value) / xAllocatedBytes.Value;
    }
}