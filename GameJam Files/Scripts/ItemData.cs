using Godot;
public enum ItemType
{
	Necklace,
	Bracelet,
	Boots,
	Ring,
	Belt
}

public enum ItemRarity
{
	Common,
	Rare,
	Epic,
	Legendary
}

[GlobalClass]
public partial class ItemData : Resource
{
	[Export] public string BaseName { get; set; } = "Ring";
	[Export] public ItemType SlotType { get; set; } = ItemType.Ring;
	[Export] public Texture2D Icon { get; set; }
	[Export] public int BaseDamage { get; set; } = 2;
	[Export] public int BaseLuck { get; set; } = 1;
	[Export] public int BaseHealth { get; set; } = 0;
	[Export] public int BaseSpeed { get; set; } = 0;
}
