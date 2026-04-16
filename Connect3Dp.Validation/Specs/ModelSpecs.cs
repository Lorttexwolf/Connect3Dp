using Lib3Dp.Constants;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Specs;

public static class ModelSpecs
{
	// Common capability sets for BambuLab
	private static readonly MachineCapabilities BBLBase =
		MachineCapabilities.StartLocalJob |
		MachineCapabilities.LocalJobs |
		MachineCapabilities.Control |
		MachineCapabilities.Lighting |
		MachineCapabilities.PrintHistory |
		MachineCapabilities.Print_Options_BedLevel;

	private static readonly MachineCapabilities BBLWithFlowCali =
		BBLBase | MachineCapabilities.Print_Options_FlowCalibration;

	private static readonly MachineCapabilities BBLWithFlowAndInspect =
		BBLWithFlowCali | MachineCapabilities.Print_Options_InspectFirstLayer;

	public static readonly Dictionary<string, ModelSpec> All = BuildAll();

	private static Dictionary<string, ModelSpec> BuildAll()
	{
		// ── BambuLab standalone specs ──────────────────────────

		var x1c = new ModelSpec(
			Brand: PrinterBrand.BambuLab,
			ModelName: "X1C",
			ExpectedCapabilities: BBLWithFlowAndInspect,
			ExplicitlyAbsentCapabilities: MachineCapabilities.AirDuct,
			ExpectedHeatingConstraints: new()
			{
				[HeatingElementNames.Bed] = new(20, 110),
				[HeatingElementNames.Nozzle] = new(20, 300),
			},
			RequiresSDOrUSB: true,
			ValidationPrintFileName: "Connect3Dp_Validation"
		);

		var x1e = new ModelSpec(
			Brand: PrinterBrand.BambuLab,
			ModelName: "X1E",
			ExpectedCapabilities: BBLWithFlowAndInspect,
			ExplicitlyAbsentCapabilities: MachineCapabilities.AirDuct,
			ExpectedHeatingConstraints: new()
			{
				[HeatingElementNames.Bed] = new(20, 120),
				[HeatingElementNames.Nozzle] = new(20, 320),
				[HeatingElementNames.Chamber] = new(40, 60),
			},
			RequiresSDOrUSB: true,
			ValidationPrintFileName: "Connect3Dp_Validation"
		);

		var p1s = new ModelSpec(
			Brand: PrinterBrand.BambuLab,
			ModelName: "P1S",
			ExpectedCapabilities: BBLBase,
			ExplicitlyAbsentCapabilities:
				MachineCapabilities.AirDuct |
				MachineCapabilities.Print_Options_InspectFirstLayer |
				MachineCapabilities.Print_Options_FlowCalibration,
			ExpectedHeatingConstraints: new()
			{
				[HeatingElementNames.Bed] = new(20, 100),
				[HeatingElementNames.Nozzle] = new(20, 300),
			},
			RequiresSDOrUSB: true,
			ValidationPrintFileName: "Connect3Dp_Validation"
		);

		var p2s = new ModelSpec(
			Brand: PrinterBrand.BambuLab,
			ModelName: "P2S",
			ExpectedCapabilities: BBLWithFlowCali | MachineCapabilities.AirDuct,
			ExplicitlyAbsentCapabilities: MachineCapabilities.Print_Options_InspectFirstLayer,
			ExpectedHeatingConstraints: new()
			{
				[HeatingElementNames.Bed] = new(20, 110),
				[HeatingElementNames.Nozzle] = new(20, 300),
			},
			RequiresSDOrUSB: true,
			ValidationPrintFileName: "Connect3Dp_Validation"
		);

		var a1 = new ModelSpec(
			Brand: PrinterBrand.BambuLab,
			ModelName: "A1",
			ExpectedCapabilities: BBLWithFlowCali,
			ExplicitlyAbsentCapabilities:
				MachineCapabilities.AirDuct |
				MachineCapabilities.Print_Options_InspectFirstLayer,
			ExpectedHeatingConstraints: new()
			{
				[HeatingElementNames.Bed] = new(20, 100),
				[HeatingElementNames.Nozzle] = new(20, 300),
			},
			RequiresSDOrUSB: true,
			ValidationPrintFileName: "Connect3Dp_Validation"
		);

		var a1Mini = new ModelSpec(
			Brand: PrinterBrand.BambuLab,
			ModelName: "A1 Mini",
			ExpectedCapabilities: BBLWithFlowCali,
			ExplicitlyAbsentCapabilities:
				MachineCapabilities.AirDuct |
				MachineCapabilities.Print_Options_InspectFirstLayer,
			ExpectedHeatingConstraints: new()
			{
				[HeatingElementNames.Bed] = new(20, 100),
				[HeatingElementNames.Nozzle] = new(20, 300),
			},
			RequiresSDOrUSB: true,
			ValidationPrintFileName: "Connect3Dp_Validation"
		);

		// ── ELEGOO ────────────────────────────────────────────

		var centauriCarbon = new ModelSpec(
			Brand: PrinterBrand.ELEGOO,
			ModelName: "Centauri Carbon",
			ExpectedCapabilities:
				MachineCapabilities.StartLocalJob |
				MachineCapabilities.Control |
				MachineCapabilities.Lighting,
			ExplicitlyAbsentCapabilities:
				MachineCapabilities.AirDuct |
				MachineCapabilities.PrintHistory |
				MachineCapabilities.Print_Options_InspectFirstLayer |
				MachineCapabilities.Print_Options_FlowCalibration |
				MachineCapabilities.Print_Options_BedLevel,
			ExpectedHeatingConstraints: new()
			{
				[HeatingElementNames.Bed] = new(0, 110),
				[HeatingElementNames.Nozzle] = new(0, 300),
			},
			RequiresSDOrUSB: true,
            ValidationPrintFileName: "Connect3Dp_Validation"
        );

		// ── Creality ──────────────────────────────────────────

		var k1c = new ModelSpec(
			Brand: PrinterBrand.Creality,
			ModelName: "K1C",
			ExpectedCapabilities:
				MachineCapabilities.StartLocalJob |
				MachineCapabilities.Control |
				MachineCapabilities.Lighting,
			ExplicitlyAbsentCapabilities:
				MachineCapabilities.AirDuct |
				MachineCapabilities.Print_Options_InspectFirstLayer |
				MachineCapabilities.Print_Options_FlowCalibration |
				MachineCapabilities.Print_Options_BedLevel,
			ExpectedHeatingConstraints: new()
			{
				[HeatingElementNames.Bed] = new(0, 100),
				[HeatingElementNames.Nozzle] = new(0, 300),
				[HeatingElementNames.Chamber] = new(0, 60),
			},
			RequiresSDOrUSB: true,
            ValidationPrintFileName: "Connect3Dp_Validation"
        );

		// ── Build dictionary ──────────────────────────────────
		// AMS units are detected dynamically after connecting.

		return new Dictionary<string, ModelSpec>
		{
			["X1C"] = x1c,
			["X1E"] = x1e,
			["P1S"] = p1s,
			["P2S"] = p2s,
			["A1"] = a1,
			["A1 Mini"] = a1Mini,
			["Centauri Carbon"] = centauriCarbon,
			["K1C"] = k1c,
		};
	}

	public static IEnumerable<string> GetModelsForBrand(PrinterBrand brand) =>
		All.Where(kv => kv.Value.Brand == brand).Select(kv => kv.Key);
}
