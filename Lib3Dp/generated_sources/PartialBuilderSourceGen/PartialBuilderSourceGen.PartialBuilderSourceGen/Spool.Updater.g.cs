#nullable enable
namespace Lib3Dp.State;

public struct SpoolUpdate
{
    public int Number { get; private set; }
    public bool NumberIsSet { get; private set; }
    public MaterialUpdate? Material { get; private set; }
    public bool MaterialIsSet { get; private set; }
    public int? GramsMaximum { get; private set; }
    public bool GramsMaximumIsSet { get; private set; }
    public int? GramsRemaining { get; private set; }
    public bool GramsRemainingIsSet { get; private set; }

public SpoolUpdate SetNumber(int value)
{
	NumberIsSet = true;
	Number = value;
	return this;
}

public SpoolUpdate RemoveNumber()
{
	NumberIsSet = true;
	Number = default;
	return this;
}
public SpoolUpdate UnsetNumber()
{
	NumberIsSet = false;
	Number = default;
	return this;
}
public SpoolUpdate UpdateMaterial(Func<MaterialUpdate, MaterialUpdate> configure)
{
	MaterialIsSet = true;
	Material ??= new MaterialUpdate();
	Material = configure(Material.Value);
	return this;
}
public SpoolUpdate RemoveMaterial()
{
	MaterialIsSet = true;
	Material = null;
	return this;
}
public SpoolUpdate UnsetMaterial()
{
	MaterialIsSet = false;
	Material = null;
	return this;
}
public SpoolUpdate SetGramsMaximum(int? value)
{
	GramsMaximumIsSet = true;
	GramsMaximum = value;
	return this;
}

public SpoolUpdate RemoveGramsMaximum()
{
	GramsMaximumIsSet = true;
	GramsMaximum = default;
	return this;
}
public SpoolUpdate UnsetGramsMaximum()
{
	GramsMaximumIsSet = false;
	GramsMaximum = default;
	return this;
}
public SpoolUpdate SetGramsRemaining(int? value)
{
	GramsRemainingIsSet = true;
	GramsRemaining = value;
	return this;
}

public SpoolUpdate RemoveGramsRemaining()
{
	GramsRemainingIsSet = true;
	GramsRemaining = default;
	return this;
}
public SpoolUpdate UnsetGramsRemaining()
{
	GramsRemainingIsSet = false;
	GramsRemaining = default;
	return this;
}
    public bool TryCreate(out Spool outResult)
    {
		outResult = default;
        if (!NumberIsSet) return false;
        if (!MaterialIsSet || !Material.Value.TryCreate(out var cMaterial)) return false;
        var result = new Spool() { Number = this.Number, Material = cMaterial };
        AppendUpdate(ref result, out _);
		outResult = result;

        return true;
    }
    public SpoolChanges Changes(in Spool spool)
    {
		var __Number_hasChanged = false;
		int? __Number_prev = null;
		int? __Number_new = null;
		if (this.NumberIsSet)
		{
			if (!EqualityComparer<int>.Default.Equals(spool.Number, this.Number))
			{
				__Number_hasChanged = true;
				__Number_prev = spool.Number;
				__Number_new = this.Number;
			}
		}

		MaterialChanges? __Material_changes = null;
		if (this.Material.HasValue)
		{
			var __local_Material = spool.Material;
			var __nested_Material = this.Material.Value.Changes(in __local_Material);
			if (__nested_Material.HasChanged) __Material_changes = __nested_Material;
		}

		var __GramsMaximum_hasChanged = false;
		int? __GramsMaximum_prev = null;
		int? __GramsMaximum_new = null;
		if (this.GramsMaximumIsSet)
		{
			if (!EqualityComparer<int?>.Default.Equals(spool.GramsMaximum, this.GramsMaximum))
			{
				__GramsMaximum_hasChanged = true;
				__GramsMaximum_prev = spool.GramsMaximum;
				__GramsMaximum_new = this.GramsMaximum;
			}
		}

		var __GramsRemaining_hasChanged = false;
		int? __GramsRemaining_prev = null;
		int? __GramsRemaining_new = null;
		if (this.GramsRemainingIsSet)
		{
			if (!EqualityComparer<int?>.Default.Equals(spool.GramsRemaining, this.GramsRemaining))
			{
				__GramsRemaining_hasChanged = true;
				__GramsRemaining_prev = spool.GramsRemaining;
				__GramsRemaining_new = this.GramsRemaining;
			}
		}

		return new SpoolChanges(__Number_hasChanged, __Number_prev, __Number_new, __Material_changes, __GramsMaximum_hasChanged, __GramsMaximum_prev, __GramsMaximum_new, __GramsRemaining_hasChanged, __GramsRemaining_prev, __GramsRemaining_new);
    }

    public void AppendUpdate(ref Spool spool, out SpoolChanges changes)
    {
		changes = Changes(in spool);

		if (this.NumberIsSet)
		{
			spool.Number = this.Number;
		}

		if (this.MaterialIsSet)
		{
			if (this.Material.Value.TryCreate(out var createdMaterial))
				spool.Material = createdMaterial;
		}

		if (this.GramsMaximumIsSet)
		{
			spool.GramsMaximum = this.GramsMaximum;
		}

		if (this.GramsRemainingIsSet)
		{
			spool.GramsRemaining = this.GramsRemaining;
		}

    }

}
