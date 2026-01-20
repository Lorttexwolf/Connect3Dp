using PartialSourceGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connect3Dp
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Name">ex, PLA, PETG, PPS-CF, PA-GF</param>
    /// <param name="FProfileIDX">Filament_id of the JSON filament profiles on Orca Slicer / BambuLab Studio.</param>
    [Partial]
    public struct Material(string name, MaterialColor color, string? fProfileIDX) : IEquatable<Material>
    {
        public string Name { get; set; } = name;
        public MaterialColor Color { get; set; } = color;
        public string? FProfileIDX { get; set; } = fProfileIDX;

        public override readonly bool Equals(object? obj)
        {
            return obj is Material material && Equals(material);
        }

        public readonly bool Equals(Material other)
        {
            return Name == other.Name &&
                   EqualityComparer<MaterialColor>.Default.Equals(Color, other.Color) &&
                   FProfileIDX == other.FProfileIDX;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Name, Color, FProfileIDX);
        }

        public static bool operator ==(Material left, Material right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Material left, Material right)
        {
            return !(left == right);
        }
    }
}
