using Godot;

public partial class SlotMachine : Interactable
{
	[Export] public Godot.Collections.Array<ItemData> ItemPool { get; set; } = new();
	[Export] private AnimatedSprite2D _sprite;

	// This replaces OnBodyEntered/Exited and _UnhandledInput!
	protected override void Interact()
	{
		if (ItemPool == null || ItemPool.Count == 0) return;

		_sprite?.Play("spin");

		int playerLuck = CurrentPlayer != null ? player_data.Instance.LuckStat : 0;
		ItemRarity rolledRarity = RollRarity(playerLuck);

		int randomIndex = (int)GD.RandRange(0, ItemPool.Count - 1);
		ItemData randomTemplate = ItemPool[randomIndex];

		EquipableItem reward = new EquipableItem(randomTemplate, rolledRarity, playerLuck);

		GD.Print($"--- SLOT MACHINE PAYOUT ---");
		GD.Print($"Item Name: {reward.DisplayName}");
		GD.Print($"Rarity:    {reward.Rarity}");
		GD.Print($"Damage:    {reward.FinalDamage}");
		GD.Print($"Luck:      {reward.FinalLuck}");
		GD.Print("---------------------------");

	}

	private ItemRarity RollRarity(int luck)
	{
		float commonWeight = Mathf.Max(10f, 60f - (luck * 2f));
		float rareWeight = 25f + (luck * 0.5f);
		float epicWeight = 10f + (luck * 1.0f);
		float legendaryWeight = 5f + (luck * 1.5f);

		float totalWeight = commonWeight + rareWeight + epicWeight + legendaryWeight;
		float roll = (float)GD.RandRange(0.0, totalWeight);

		if (roll < commonWeight) return ItemRarity.Common;
		roll -= commonWeight;
		if (roll < rareWeight) return ItemRarity.Rare;
		roll -= rareWeight;
		if (roll < epicWeight) return ItemRarity.Epic;

		return ItemRarity.Legendary;
	}
}
