using Lib3Dp.State;
using Lib3Dp.Constants;

namespace Lib3Dp.Connectors.BambuLab.Constants
{
	public class BBLConstants
	{
		public const ushort NotLoadedID = 255;
		public const ushort ExternalTrayID = 254;
		public const string ExternalTrayMMID = "External";
		public const int ExternalTraySlotNumber = 0;
		public readonly static SpoolLocation ExternalTrayLocation = new(ExternalTrayMMID, ExternalTraySlotNumber);

		public const string ModelX1C = "X1C";
		public const string ModelX1E = "X1E";
		public const string ModelP1P = "P1P";
		public const string ModelP1S = "P1S";
		public const string ModelP2S = "P2S";
		public const string ModelA1 = "A1";
		public const string ModelA1Mini = "A1 Mini";
		public const string ModelH2D = "H2D";

		public static bool HasMQTTDevicesObject(string modelName)
		{
			return modelName switch
			{
				ModelX1C => false,
				ModelX1E => false,
				ModelP1P => false,
				ModelP1S => false,
				ModelA1 => false,
				ModelA1Mini => false,
				ModelH2D => false,
				_ => throw new Exception($"Unknown BBL model of {modelName}")
			};
		}

		public static string GetModelFromSerialNumber(string serialNumber)
		{
			return serialNumber[..3] switch
			{
				"00M" => ModelX1C,
				"03W" => ModelX1E,
				"01S" => ModelP1P,
				"01P" => ModelP1S,
				"22E" => ModelP2S,
				"039" => ModelA1,
				"030" => ModelA1Mini,
				"094" => ModelH2D,
				_ => throw new Exception($"Unknown model of {serialNumber} ({serialNumber[..3]})! Is the serial number correct?")
			};
		}

		public static HeatingConstraints? GetHeatingConstraintsFromElementName(string elementName, string modelName)
		{
			return modelName switch
			{
				ModelX1C => GetHeatingConstraintsFromElementNameModelX1C(elementName),
				ModelX1E => GetHeatingConstraintsFromElementNameModelX1E(elementName),
				ModelP1P => GetHeatingConstraintsFromElementNameModelP1Series(elementName),
				ModelP1S => GetHeatingConstraintsFromElementNameModelP1Series(elementName),
				ModelP2S => GetHeatingConstraintsFromElementNameModelP2S(elementName),
				ModelA1 => GetHeatingConstraintsFromElementNameModelA1Series(elementName),
				ModelA1Mini => GetHeatingConstraintsFromElementNameModelA1Series(elementName),
				ModelH2D => GetHeatingConstraintsFromElementNameModelH2Series(elementName),
				_ => null
			};
		}
		private static HeatingConstraints? GetHeatingConstraintsFromElementNameModelX1C(string elementName)
		{
			return elementName switch
			{
				HeatingElementNames.Bed => new HeatingConstraints(20, 110),
				HeatingElementNames.Nozzle => new HeatingConstraints(20, 300),
				_ => null
			};
		}
		private static HeatingConstraints? GetHeatingConstraintsFromElementNameModelX1E(string elementName)
		{
			return elementName switch
			{
				HeatingElementNames.Bed => new HeatingConstraints(20, 120),
				HeatingElementNames.Nozzle => new HeatingConstraints(20, 320),
				HeatingElementNames.Chamber => new HeatingConstraints(40, 60),
				_ => null
			};
		}
		private static HeatingConstraints? GetHeatingConstraintsFromElementNameModelP1Series(string elementName)
		{
			return elementName switch
			{
				HeatingElementNames.Bed => new HeatingConstraints(20, 100),
				HeatingElementNames.Nozzle => new HeatingConstraints(20, 300),
				_ => null
			};
		}
		private static HeatingConstraints? GetHeatingConstraintsFromElementNameModelA1Series(string elementName)
		{
			return elementName switch
			{
				HeatingElementNames.Bed => new HeatingConstraints(20, 100),
				HeatingElementNames.Nozzle => new HeatingConstraints(20, 300),
				_ => null
			};
		}
		private static HeatingConstraints? GetHeatingConstraintsFromElementNameModelP2S(string elementName)
		{
			return elementName switch
			{
				HeatingElementNames.Bed => new HeatingConstraints(20, 110),
				HeatingElementNames.Nozzle => new HeatingConstraints(20, 300),
				_ => null
			};
		}
		private static HeatingConstraints? GetHeatingConstraintsFromElementNameModelH2Series(string elementName)
		{
			return elementName switch
			{
				HeatingElementNames.Bed => new HeatingConstraints(20, 120),
				HeatingElementNames.Nozzle => new HeatingConstraints(20, 350),
				HeatingElementNames.Chamber => new HeatingConstraints(40, 65),
				_ => null
			};
		}


		public static bool IsSDCardOrUSBRequired(string machineModel)
		{
			return machineModel switch
			{
				ModelP1P => true,
				ModelP1S => true,
				ModelA1 => true,
				ModelA1Mini => true,
				_ => false
			};
		}

		public const string ModelAMS = "AMS";
		public const string ModelAMSLite = "AMS Lite";
		public const string ModelAMSHT = "AMS HT";
		public const string ModelAMS2Pro = "AMS 2 Pro";

		/// <param name="AMSInfo">From JSON print.ams.ams[i].info</param>
		public static string GetAMSModelFromSN(string amsSN)
		{
			return amsSN[..3] switch
			{
				"006" => ModelAMS,
				"03C" => ModelAMSLite,
				"19C" => ModelAMS2Pro,
				"19F" => ModelAMSHT,
				_ => throw new Exception($"Unknown AMS model of SN {amsSN}")
			};
		}

		public static HeatingConstraints? GetAMSHeatingConstraintsFromModel(string AMSModel)
		{
			return AMSModel switch
			{
				ModelAMS2Pro => new HeatingConstraints(45, 65),
				ModelAMSHT => new HeatingConstraints(45, 85),
				ModelAMS => null,
				ModelAMSLite => null,
				_ => throw new Exception($"Unknown AMS model of {AMSModel}")
			};
		}

		public static int GetAMSCapacityFromModel(string AMSModel)
		{
			return AMSModel switch
			{
				ModelAMS => 4,
				ModelAMS2Pro => 4,
				ModelAMSLite => 4,
				ModelAMSHT => 1,
				_ => throw new Exception($"Unknown AMS model of {AMSModel}")
			};
		}

		public static MaterialUnitCapabilities GetAMSFeaturesFromModel(string AMSModel)
		{
			return AMSModel switch
			{
				ModelAMS => MaterialUnitCapabilities.AutomaticFeeding | MaterialUnitCapabilities.Humidity | MaterialUnitCapabilities.Temperature,
				ModelAMSHT => MaterialUnitCapabilities.AutomaticFeeding | MaterialUnitCapabilities.Humidity | MaterialUnitCapabilities.Temperature | MaterialUnitCapabilities.Heating | MaterialUnitCapabilities.AutomaticFeeding | MaterialUnitCapabilities.Humidity | MaterialUnitCapabilities.Heating_CanSpin,
				ModelAMS2Pro => MaterialUnitCapabilities.AutomaticFeeding | MaterialUnitCapabilities.Humidity | MaterialUnitCapabilities.Temperature | MaterialUnitCapabilities.Heating | MaterialUnitCapabilities.AutomaticFeeding | MaterialUnitCapabilities.Humidity | MaterialUnitCapabilities.Heating_CanSpin,
				_ => throw new Exception($"Unknown AMS model of {AMSModel}")
			};
		}

		public static class MQTT
		{
			public static string ReportTopic(string serialNumber) => $"device/{serialNumber}/report";
			public static string RequestTopic(string serialNumber) => $"device/{serialNumber}/request";

			public const string SECURITY_FAILED_ERROR_MSG = "mqtt message verify failed";
		}

		public static class PrintStages
		{
			//public static readonly int[] PRINTING = [0, -1, 255];
			public const int HOMING_TOOLHEAD_STAGE = 13;
			public const int CHANGING_FILAMENT = 4;
			public const int COOLING_CHAMBER = 29;
			public const int IDENTIFYING_BUILD_PLATE = 11;
		}

		public static class ModelFeatures
		{
			public static readonly string[] WithInspectFirstLayer = [ModelX1C, ModelX1E];

			public static readonly string[] WithFlowRateCali = [ModelX1C, ModelX1E, ModelH2D, ModelA1, ModelA1Mini, ModelP2S];

			public static readonly string[] WithClimateControl = [ModelP2S];

			public static readonly string[] WithRTSPSCamera = [ModelX1C, ModelX1E, ModelH2D, ModelP2S];

			public static readonly string[] With30FPMCamera = [ModelP1P, ModelP1S, ModelA1, ModelA1Mini];
		}
	}
}
