using Godot;

public partial class EquipableItem : RefCounted
{
	public ItemData BaseData { get; private set; }
	public ItemRarity Rarity { get; private set; }
	public ItemType SlotType => BaseData.SlotType;
	
	public string DisplayName { get; private set; }
	public int FinalDamage { get; private set; }
	public int FinalLuck { get; private set; }

	public EquipableItem(ItemData template, ItemRarity rarity, int playerLuck)
	{
		BaseData = template;
		Rarity = rarity;

		// 1. Calculate a multiplier based on rarity
		float rarityMultiplier = rarity switch
		{
			ItemRarity.Common => 1.0f,
			ItemRarity.Rare => 1.5f,
			ItemRarity.Epic => 2.2f,
			ItemRarity.Legendary => 3.5f,
			_ => 1.0f
		};

		// 2. Compute randomized bonus stats
		int bonusDamage = GD.RandRange(1, 4) + (int)(playerLuck * 0.5f);
		int bonusLuck = GD.RandRange(0, 2);

		// 3. Final stats combining base values and multipliers
		FinalDamage = Mathf.RoundToInt(template.BaseDamage * rarityMultiplier) + bonusDamage;
		FinalLuck = Mathf.RoundToInt(template.BaseLuck * rarityMultiplier) + bonusLuck;

		// 4. Construct display name (e.g., "Legendary Iron Ring")
		DisplayName = rarity == ItemRarity.Common ? template.BaseName : $"{rarity} {template.BaseName}";
	}
}
