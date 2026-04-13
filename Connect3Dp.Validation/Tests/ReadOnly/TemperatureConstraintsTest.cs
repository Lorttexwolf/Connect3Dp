using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.Connectors.BambuLab;
using Lib3Dp.Connectors.BambuLab.Constants;

namespace Connect3Dp.Validation.Tests.ReadOnly;

public class TemperatureConstraintsTest : ValidationTest
{
	public override string Name => "Temperature Constraints";
	public override string Description => "Verify heating constraints match model spec";
	public override RiskTier Tier => RiskTier.ReadOnly;

	public override Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		if (spec.ExpectedHeatingConstraints == null || spec.ExpectedHeatingConstraints.Count == 0)
			return Task.FromResult(TestResult.Skip("No heating constraints in spec"));

		if (spec.Brand != PrinterBrand.BambuLab)
			return Task.FromResult(TestResult.Skip("Temperature constraint validation only implemented for BambuLab"));

		var bbl = connection as BBLMachineConnection;
		if (bbl == null)
			return Task.FromResult(TestResult.Skip("Not a BambuLab connection"));

		var model = bbl.Model;
		var failures = new List<string>();

		foreach (var (elementName, expectedConstraints) in spec.ExpectedHeatingConstraints)
		{
			var actual = BBLConstants.GetHeatingConstraintsFromElementName(elementName, model);
			if (!actual.HasValue)
			{
				failures.Add($"{elementName}: no constraints returned for this model");
				continue;
			}
			if (actual.Value.MinTempC != expectedConstraints.MinTempC || actual.Value.MaxTempC != expectedConstraints.MaxTempC)
			{
				failures.Add($"{elementName}: expected {expectedConstraints}, got {actual.Value}");
			}
		}

		if (failures.Count > 0)
			return Task.FromResult(TestResult.Fail(
				$"{failures.Count} constraint mismatches",
				string.Join("; ", failures)));

		return Task.FromResult(TestResult.Pass($"All {spec.ExpectedHeatingConstraints.Count} heating constraints match"));
	}
}
