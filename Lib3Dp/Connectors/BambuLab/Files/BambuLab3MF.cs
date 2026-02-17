using Lib3Dp.State;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Lib3Dp.Connectors.BambuLab.Files
{
	public sealed class BambuLab3MF
	{
		public int PlateIndex { get; private set; }
		public double PredictionSeconds { get; private set; }
		public TimeSpan PredictedTime => TimeSpan.FromSeconds(PredictionSeconds);
		public double WeightGrams { get; private set; }

		public IReadOnlyList<FilamentInfo> Filaments => _filaments;
		public IReadOnlyList<LayerFilamentRange> LayerFilaments => _layerFilaments;

		public double TotalFilamentMeters { get; private set; }
		public double TotalFilamentGrams { get; private set; }

		public byte[]? ThumbnailSmall { get; private set; }

		private readonly List<FilamentInfo> _filaments = new();
		private readonly List<LayerFilamentRange> _layerFilaments = new();

		private BambuLab3MF() { }

		public static BambuLab3MF Load(Stream threeMfStream)
		{
			var result = new BambuLab3MF();

			using var archive = new ZipArchive(threeMfStream, ZipArchiveMode.Read, leaveOpen: false);

			var sliceEntry = archive.GetEntry("Metadata/slice_info.config")
				?? throw new InvalidDataException("Not a sliced 3MF (slice_info.config missing)");

			XDocument doc;
			using (var s = sliceEntry.Open())
			{
				doc = XDocument.Load(s);
			}

			var plate = doc.Root?
				.Elements("plate")
				.FirstOrDefault()
				?? throw new InvalidDataException("Plate node missing");


			var meta = plate
				.Elements("metadata")
				.ToDictionary(
					x => (string)x.Attribute("key")!,
					x => (string)x.Attribute("value")!
				);

			result.PlateIndex = GetInt(meta, "index");
			result.PredictionSeconds = GetDouble(meta, "prediction");
			result.WeightGrams = GetDouble(meta, "weight");

			foreach (var f in plate.Elements("filament"))
			{
				var filament = new FilamentInfo
				{
					Id = GetIntAttr(f, "id"),
					Filament = new Material()
					{
						FProfileIDX = (string)f.Attribute("tray_info_idx")!,
						Name = (string)f.Attribute("type")!,
						Color = new MaterialColor(null, (string)f.Attribute("color")!)
					},
					UsedMeters = GetDoubleAttr(f, "used_m"),
					UsedGrams = GetDoubleAttr(f, "used_g"),
					UsedForObject = GetBoolAttr(f, "used_for_object"),
					UsedForSupport = GetBoolAttr(f, "used_for_support"),
					NozzleDiameter = GetDoubleAttr(f, "nozzle_diameter")
				};

				result._filaments.Add(filament);
			}

			result.TotalFilamentMeters = result._filaments.Sum(f => f.UsedMeters);
			result.TotalFilamentGrams = result._filaments.Sum(f => f.UsedGrams);

			var layerLists = plate.Element("layer_filament_lists");

			if (layerLists != null)
			{
				foreach (var l in layerLists.Elements("layer_filament_list"))
				{
					var rangeText = (string?)l.Attribute("layer_ranges");
					var filamentList = (string?)l.Attribute("filament_list");

					if (string.IsNullOrWhiteSpace(rangeText) || string.IsNullOrWhiteSpace(filamentList)) continue;

					var parts = rangeText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

					if (parts.Length == 2)
					{
						result._layerFilaments.Add(new LayerFilamentRange
						{
							FilamentIDs = filamentList!.Split(' ').Select(i => int.Parse(i)).ToArray(),
							StartLayer = int.Parse(parts[0]),
							EndLayer = int.Parse(parts[1])
						});
					}
				}
			}

			//var imagePath = $"Metadata/plate_{result.PlateIndex}_small.png";
			//var imageEntry = archive.GetEntry(imagePath);

			//if (imageEntry != null)
			//{
			//	using var imgStream = imageEntry.Open();
			//	using var ms = new MemoryStream();
			//	imgStream.CopyTo(ms);
			//	result.PlateThumbnailSmall = ms.ToArray();
			//}

			return result;
		}

		public sealed class FilamentInfo
		{
			public int Id { get; set; }
			public required Material Filament { get; set; }
			public double UsedMeters { get; set; }
			public double UsedGrams { get; set; }
			public bool UsedForObject { get; set; }
			public bool UsedForSupport { get; set; }
			public double NozzleDiameter { get; set; }
		}

		public sealed class LayerFilamentRange
		{
			public required int[] FilamentIDs { get; set; }
			public int StartLayer { get; set; }
			public int EndLayer { get; set; }
		}

		static int GetInt(Dictionary<string, string> map, string key) => map.TryGetValue(key, out var v) && int.TryParse(v, out var r) ? r : 0;

		static double GetDouble(Dictionary<string, string> map, string key) => map.TryGetValue(key, out var v) && double.TryParse(v, out var r) ? r : 0;

		static int GetIntAttr(XElement e, string name) => int.TryParse((string)e.Attribute(name)!, out var v) ? v : 0;

		static double GetDoubleAttr(XElement e, string name) => double.TryParse((string)e.Attribute(name)!, out var v) ? v : 0;

		static bool GetBoolAttr(XElement e, string name) => bool.TryParse((string)e.Attribute(name)!, out var v) && v;

	}

}
