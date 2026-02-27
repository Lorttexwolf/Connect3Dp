#nullable enable
namespace Lib3Dp.State;

public struct MaterialUpdate
{
    public string Name { get; private set; }
    public bool NameIsSet { get; private set; }
    public Lib3Dp.State.MaterialColor Color { get; private set; }
    public bool ColorIsSet { get; private set; }
    public string FProfileIDX { get; private set; }
    public bool FProfileIDXIsSet { get; private set; }

public MaterialUpdate SetName(string value)
{
	NameIsSet = true;
	Name = value;
	return this;
}

public MaterialUpdate RemoveName()
{
	NameIsSet = true;
	Name = default;
	return this;
}
public MaterialUpdate UnsetName()
{
	NameIsSet = false;
	Name = default;
	return this;
}
public MaterialUpdate SetColor(Lib3Dp.State.MaterialColor value)
{
	ColorIsSet = true;
	Color = value;
	return this;
}

public MaterialUpdate RemoveColor()
{
	ColorIsSet = true;
	Color = default;
	return this;
}
public MaterialUpdate UnsetColor()
{
	ColorIsSet = false;
	Color = default;
	return this;
}
public MaterialUpdate SetFProfileIDX(string value)
{
	FProfileIDXIsSet = true;
	FProfileIDX = value;
	return this;
}

public MaterialUpdate RemoveFProfileIDX()
{
	FProfileIDXIsSet = true;
	FProfileIDX = default;
	return this;
}
public MaterialUpdate UnsetFProfileIDX()
{
	FProfileIDXIsSet = false;
	FProfileIDX = default;
	return this;
}
    public bool TryCreate(out Material outResult)
    {
		outResult = default;
        if (!NameIsSet) return false;
        if (!ColorIsSet) return false;
        var result = new Material() { Name = this.Name, Color = this.Color };
        AppendUpdate(ref result, out _);
		outResult = result;

        return true;
    }
    public MaterialChanges Changes(in Material material)
    {
		var __Name_hasChanged = false;
		string? __Name_prev = null;
		string? __Name_new = null;
		if (this.NameIsSet)
		{
			if (!EqualityComparer<string>.Default.Equals(material.Name, this.Name))
			{
				__Name_hasChanged = true;
				__Name_prev = material.Name;
				__Name_new = this.Name;
			}
		}

		var __Color_hasChanged = false;
		Lib3Dp.State.MaterialColor? __Color_prev = null;
		Lib3Dp.State.MaterialColor? __Color_new = null;
		if (this.ColorIsSet)
		{
			if (!EqualityComparer<Lib3Dp.State.MaterialColor>.Default.Equals(material.Color, this.Color))
			{
				__Color_hasChanged = true;
				__Color_prev = material.Color;
				__Color_new = this.Color;
			}
		}

		var __FProfileIDX_hasChanged = false;
		string? __FProfileIDX_prev = null;
		string? __FProfileIDX_new = null;
		if (this.FProfileIDXIsSet)
		{
			if (!EqualityComparer<string>.Default.Equals(material.FProfileIDX, this.FProfileIDX))
			{
				__FProfileIDX_hasChanged = true;
				__FProfileIDX_prev = material.FProfileIDX;
				__FProfileIDX_new = this.FProfileIDX;
			}
		}

		return new MaterialChanges(__Name_hasChanged, __Name_prev, __Name_new, __Color_hasChanged, __Color_prev, __Color_new, __FProfileIDX_hasChanged, __FProfileIDX_prev, __FProfileIDX_new);
    }

    public void AppendUpdate(ref Material material, out MaterialChanges changes)
    {
		changes = Changes(in material);

		if (this.NameIsSet)
		{
			material.Name = this.Name;
		}

		if (this.ColorIsSet)
		{
			material.Color = this.Color;
		}

		if (this.FProfileIDXIsSet)
		{
			material.FProfileIDX = this.FProfileIDX;
		}

    }

}
