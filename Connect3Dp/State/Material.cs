using Connect3Dp.SourceGeneration.UpdateGen;
using PartialSourceGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connect3Dp.State
{
    [GenerateUpdate]
    public partial class Material : IEquatable<Material?>
    {
        /// <summary>
        /// PLA, PETG, PPS-CF, PA-GF
        /// </summary>
        public required string Name { get; set; }

        public required MaterialColor Color { get; set; }

        /// <summary>
        /// Filament_id of the JSON filament profiles on Orca Slicer / BambuLab Studio.
        /// </summary>
        public string? FProfileIDX { get; set; }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Material);
        }

        public bool Equals(Material? other)
        {
            return other is not null &&
                   Name == other.Name &&
                   EqualityComparer<MaterialColor>.Default.Equals(Color, other.Color);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Color);
        }

        public static bool operator ==(Material? left, Material? right)
        {
            return EqualityComparer<Material>.Default.Equals(left, right);
        }

        public static bool operator !=(Material? left, Material? right)
        {
            return !(left == right);
        }
    }
}
