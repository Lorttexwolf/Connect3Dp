using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;

namespace Connect3Dp.Validation.Tests;

public enum RiskTier { ReadOnly, NonDestructive, Destructive }

public abstract class ValidationTest
{
	public abstract string Name { get; }
	public abstract string Description { get; }
	public abstract RiskTier Tier { get; }
	public abstract Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct);
}
