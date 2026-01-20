namespace Connect3Dp
{
    public struct MaterialColor
    {
        public string? Name;
        public int R;
        public int G; 
        public int B;

        public MaterialColor(string? name, string hexColor)
        {
            this.Name = name;

            if (string.IsNullOrWhiteSpace(hexColor))
            {
                throw new ArgumentException($"Filament color may not be null or empty");
            }
            if (hexColor.StartsWith("#"))
            {
                hexColor = hexColor[1..];
            }

            try
            {
                this.R = Convert.ToInt32(hexColor.Substring(0, 2), 16);
                this.G = Convert.ToInt32(hexColor.Substring(2, 2), 16);
                this.B = Convert.ToInt32(hexColor.Substring(4, 2), 16);
            }
            catch (FormatException)
            {
                throw new ArgumentException("Filament color must be in Hex Color");
            }
        }

        public readonly string Hex => $"{R:X2}{G:X2}{B:X2}";

        public readonly bool EqualsInRange(MaterialColor other, int range = 40)
        {
            return Math.Abs(this.R - other.R) < range
                && Math.Abs(this.G - other.G) < range
                && Math.Abs(this.B - other.B) < range;
        }
    }
}
