using Godot;

public partial class SlotMachine : Interactable
{
	[Export] public Godot.Collections.Array<ItemData> ItemPool { get; set; } = new();
	[Export] private AnimatedSprite2D _sprite;
	[Export] private Label cost_label;
	
	[Export] private int baseCost = 5;       // Initial cost for the first spin
	[Export] private int costIncrease = 5;
	[Export] private int currentCost;
	
	private int currentSpinCount = 0;
	
	protected override void Interact()
	{
		if (ItemPool == null || ItemPool.Count == 0) return;
		
		currentCost = baseCost + (currentSpinCount * costIncrease);
		if (player_data.Instance == null || player_data.Instance.ZombieFleshValue < currentCost)
		{
			GD.Print("[SlotMachine] Not enough Zombie Flesh to spin!");
			return;
		}
		
		// Deduct currency
		player_data.Instance.AddZombieFlesh(-currentCost);

		// Increment spin tracker for next time
		currentSpinCount++;
		UpdateCostLabel();
		
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
	
	private void UpdateCostLabel()
	{
		if (cost_label != null)
		{
			int currentCost = baseCost + (currentSpinCount * costIncrease);
			cost_label.Text = $"C: {currentCost} \n O: {player_data.Instance.ZombieFleshValue}";
		}
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
