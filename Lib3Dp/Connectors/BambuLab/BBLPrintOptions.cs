namespace Lib3Dp.Connectors.BambuLab
{
	public sealed record BBLPrintOptions(
			
		int PlateIndex,
		string FileName,
		string MetadataId,
		int ProjectFilamentCount,

		bool BedLeveling,
		bool FlowCalibration,
		bool VibrationCalibration,
		bool LayerInspect,
		bool Timelapse,

		Dictionary<int, AMSSlot>? AMSMapping = null

	);

	public readonly record struct AMSSlot(int AMSId, int SlotId);

	//		{
	//  "print": {
	//    "ams_mapping": [
	//	  3,
	//	  -1,
	//	  2
	//	],
	//    "ams_mapping2": [
	//      {
	//        "ams_id": 0,
	//        "slot_id": 3

	//	  },
	//      {
	//        "ams_id": 255,
	//        "slot_id": 255
	//      },
	//      {
	//	"ams_id": 0,
	//        "slot_id": 2

	//	  }
	//    ],
	//    "auto_bed_leveling": 2,
	//    "bed_leveling": true,
	//    "command": "project_file",
	//    "err_code": 0,
	//    "extrude_cali_flag": 2,
	//    "extrude_cali_manual_mode": 1,
	//    "flow_cali": true,
	//    "high_tmpr_auto_bed_leveling": 2,
	//    "job_id": "0",
	//    "job_type": 1,
	//    "md5": "9CE49DD0F91EA7840A83ABA7662D8E35",
	//    "no_cache": false,
	//    "nozzle_offset_cali": 2,
	//    "param": "Metadata/plate_1.gcode",
	//    "plate_idx": 1,
	//    "reason": "SUCCESS",
	//    "result": "SUCCESS",
	//    "sequence_id": "4010",
	//    "skip_objects": [],
	//    "subtask_name": "Untitled.gcode.3mf",
	//    "timeStamp": 1770255091159,
	//    "timelapse": false,
	//    "url": "ftp://Untitled.gcode.3mf",
	//    "url_enc": "ftp://Untitled.gcode.3mf",
	//    "use_ams": true
	//  }
	//}
}
