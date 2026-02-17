namespace Lib3Dp.State
{
	public readonly struct MaterialColor : IEquatable<MaterialColor>
	{
		public string? Name { get; }
		public byte R { get; }
		public byte G { get; }
		public byte B { get; }

		// https://www.hunterlab.com/blog/what-is-cielab-color-space/
		// https://www.youtube.com/watch?v=YzkOjL9JUJU

		private readonly double CIELabL; // 0 = black, 100 = white
		private readonly double CIELabA; // green (-) to red (+)
		private readonly double CIELabB; // blue (-) to yellow (+)

		public MaterialColor(string? name, string hexColor)
		{
			Name = name;

			if (string.IsNullOrWhiteSpace(hexColor)) throw new ArgumentException("Filament color may not be null or empty", nameof(hexColor));

			ReadOnlySpan<char> hex = hexColor.AsSpan();

			if (hex[0] == '#') hex = hex[1..];

			if (hex.Length == 8)
			{
				hex = hex.Slice(0, 6);
			}

			if (hex.Length != 6) throw new ArgumentException($"Filament color must be in Hex Color format: {hex}", nameof(hexColor));

			if (!byte.TryParse(hex.Slice(0, 2), System.Globalization.NumberStyles.HexNumber, null, out byte r) ||
				!byte.TryParse(hex.Slice(2, 2), System.Globalization.NumberStyles.HexNumber, null, out byte g) ||
				!byte.TryParse(hex.Slice(4, 2), System.Globalization.NumberStyles.HexNumber, null, out byte b))
			{
				throw new ArgumentException("Filament color must be in Hex Color format", nameof(hexColor));
			}

			R = r;
			G = g;
			B = b;

			(CIELabL, CIELabA, CIELabB) = ToLab(r, g, b);
		}

		public string Hex => $"{R:X2}{G:X2}{B:X2}";

		/// <summary>
		/// Returns true if colors are perceptually similar using Delta E (CIE76).
		/// </summary>
		public bool IsSimilarTo(MaterialColor other, out double deltaE, double threshold = 40)
		{
			deltaE = DeltaE(this, other);

			return deltaE <= threshold;
		}

		public static double DeltaE(MaterialColor a, MaterialColor b)
		{
			double dL = b.CIELabL - a.CIELabL;
			double dA = b.CIELabA - a.CIELabA;
			double dB = b.CIELabB - a.CIELabB;
			double deltaE = Math.Sqrt(dL * dL + dA * dA + dB * dB);

			return deltaE;
		}

		public bool Equals(MaterialColor other)
		{
			return R == other.R && G == other.G && B == other.B;
		}

		private static (double L, double A, double B) ToLab(byte r, byte g, byte b)
		{
			static double Pivot(double v) => v > 0.04045 ? Math.Pow((v + 0.055) / 1.055, 2.4) : v / 12.92;

			const double inv255 = 1.0 / 255.0;

			double rL = Pivot(r * inv255);
			double gL = Pivot(g * inv255);
			double bL = Pivot(b * inv255);

			double x = (rL * 0.4124564 + gL * 0.3575761 + bL * 0.1804375) / 0.95047;
			double y = rL * 0.2126729 + gL * 0.7151522 + bL * 0.0721750;
			double z = (rL * 0.0193339 + gL * 0.1191920 + bL * 0.9503041) / 1.08883;

			static double F(double t) => t > 0.008856 ? Math.Cbrt(t) : 7.787 * t + 16.0 / 116.0;

			x = F(x);
			y = F(y);
			z = F(z);

			return (116.0 * y - 16.0, 500.0 * (x - y), 200.0 * (y - z));
		}
	}
}
