using Connect3Dp.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connect3Dp.Connectors.BambuLab.Constants
{
    public class BBLConstants
    {
        public const ushort ExternalTrayID = 254;
        public const string ExternalTrayMUID = "External";
        public const int ExternalTraySlotNumber = 0;
        public readonly static MaterialLocation ExternalTrayLocation = new(ExternalTrayMUID, ExternalTraySlotNumber);

        public const string ModelX1C = "X1C";
        public const string ModelX1 = "X1";
        public const string ModelX1E = "X1E";
        public const string ModelP1P = "P1P";
        public const string ModelP1S = "P1S";
        public const string ModelP2S = "P2S";
        public const string ModelA1 = "A1";
        public const string ModelA1Mini = "A1 Mini";
        public const string ModelH2D = "H2D";

        public static string GetModelFromSerialNumber(string serialNumber)
        {
            return serialNumber[..3] switch
            {
                "00W" => ModelX1,
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

        public static MaterialUnitFeatures GetAMSFeaturesFromModel(string AMSModel)
        {
            return AMSModel switch
            {
                ModelAMS => MaterialUnitFeatures.AutomaticFeeding | MaterialUnitFeatures.Humidity | MaterialUnitFeatures.Temperature,
                ModelAMSHT => MaterialUnitFeatures.AutomaticFeeding | MaterialUnitFeatures.Humidity | MaterialUnitFeatures.Temperature | MaterialUnitFeatures.Heating | MaterialUnitFeatures.AutomaticFeeding | MaterialUnitFeatures.Humidity | MaterialUnitFeatures.Heating_CanSpin,
                ModelAMS2Pro => MaterialUnitFeatures.AutomaticFeeding | MaterialUnitFeatures.Humidity | MaterialUnitFeatures.Temperature | MaterialUnitFeatures.Heating | MaterialUnitFeatures.AutomaticFeeding | MaterialUnitFeatures.Humidity | MaterialUnitFeatures.Heating_CanSpin,
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

            public static readonly string[] WithRTSPSCamera = [ModelX1C, ModelX1E, ModelH2D, ModelP2S, ModelX1];

            public static readonly string[] With30FPMCamera = [ModelP1P, ModelP1S, ModelA1, ModelA1Mini];
        }
    }
}
