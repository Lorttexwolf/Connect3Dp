using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lib3Dp.Connectors.BambuLab
{
	/// <summary>
	/// Versioning format Bmabu Lab utilizes for firmware updates.
	/// </summary>
	[JsonConverter(typeof(BBLFirmwareVersionJsonConverter))]
	public readonly struct BBLFirmwareVersion(int a, int b, int c, int d) : IComparable<BBLFirmwareVersion>
	{
		public int A { get; } = a;
		public int B { get; } = b;
		public int C { get; } = c;
		public int D { get; } = d;

		public int CompareTo(BBLFirmwareVersion other)
		{
			if (A != other.A) return A.CompareTo(other.A);
			if (B != other.B) return B.CompareTo(other.B);
			if (C != other.C) return C.CompareTo(other.C);
			return D.CompareTo(other.D);
		}

		public bool IsZero()
		{
			return A == 0 && B == 0 && C == 0 && D == 0;
		}

		public static BBLFirmwareVersion Parse(string str)
		{
			if (string.IsNullOrWhiteSpace(str))
				throw new ArgumentException("Version string cannot be null or empty.");

			var segs = str.Trim().Split('.');
			if (segs.Length != 4)
				throw new FormatException("Version string must have 4 segments.");

			return new BBLFirmwareVersion(
				int.Parse(segs[0]),
				int.Parse(segs[1]),
				int.Parse(segs[2]),
				int.Parse(segs[3])
			);
		}

		public override string ToString() => $"{A:D2}.{B:D2}.{C:D2}.{D:D2}";

		public override bool Equals(object? obj) => obj is BBLFirmwareVersion other && this.A == other.A && this.B == other.B && this.C == other.C && this.D == other.D;

		public override int GetHashCode() => HashCode.Combine(A, B, C, D);

		public static bool operator >(BBLFirmwareVersion left, BBLFirmwareVersion right) => left.CompareTo(right) > 0;
		public static bool operator <(BBLFirmwareVersion left, BBLFirmwareVersion right) => left.CompareTo(right) < 0;

		public static bool operator ==(BBLFirmwareVersion left, BBLFirmwareVersion right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(BBLFirmwareVersion left, BBLFirmwareVersion right)
		{
			return !(left == right);
		}

		public static bool operator <=(BBLFirmwareVersion left, BBLFirmwareVersion right)
		{
			return left.CompareTo(right) <= 0;
		}

		public static bool operator >=(BBLFirmwareVersion left, BBLFirmwareVersion right)
		{
			return left.CompareTo(right) >= 0;
		}

		public static readonly BBLFirmwareVersion X1CUnsupportedSecurityChangesVersion = BBLFirmwareVersion.Parse("01.08.05.00");
	}

	public sealed class BBLFirmwareVersionJsonConverter : JsonConverter<BBLFirmwareVersion>
	{
		public override BBLFirmwareVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.String) throw new JsonException("Expected firmware version as string.");

			var str = reader.GetString() ?? throw new JsonException("Firmware version string was null.");
			return BBLFirmwareVersion.Parse(str);
		}

		public override void Write(Utf8JsonWriter writer, BBLFirmwareVersion value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToString());
		}
	}
}
