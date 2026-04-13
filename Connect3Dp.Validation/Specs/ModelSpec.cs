using Lib3Dp.State;

namespace Connect3Dp.Validation.Specs;

public enum PrinterBrand { BambuLab, ELEGOO, Creality }

public record ModelSpec(
	PrinterBrand Brand,
	string ModelName,
	MachineCapabilities ExpectedCapabilities,
	MachineCapabilities ExplicitlyAbsentCapabilities,
	Dictionary<string, HeatingConstraints>? ExpectedHeatingConstraints,
	bool RequiresSDOrUSB,
	int ExpectedNozzleCount = 1,
	double DefaultNozzleDiameter = 0.4,
	string? ValidationPrintFileName = null,
	string? ExpectedAMSModel = null
);
