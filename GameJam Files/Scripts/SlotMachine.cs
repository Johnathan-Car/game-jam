using Godot;
using System;

public partial class SlotMachine : Interactable
{
	[Export] public Godot.Collections.Array<ItemData> ItemPool { get; set; } = new();
	[Export] private AnimatedSprite2D _sprite;
	[Export] private Label cost_label;
	
	[Export] private int baseCost = 5;       // Initial cost for the first spin
	[Export] private int costIncrease = 5;
	
	private int currentSpinCount = 0;
	
	[Export] private PackedScene worldItemScene; // Assign your WorldItem.tscn here in the Inspector
	[Export] private Marker2D dropSpawnPoint;     // Where the item pops out of the machine

	protected override void Interact()
	{
		if (ItemPool == null || ItemPool.Count == 0) return;

		int currentCost = baseCost + (currentSpinCount * costIncrease);

		if (player_data.Instance == null || player_data.Instance.ZombieFleshValue < currentCost)
		{
			GD.Print("[SlotMachine] Not enough Zombie Flesh to spin!");
			return;
		}

		player_data.Instance.AddZombieFlesh(-currentCost);
		currentSpinCount++;
		UpdateCostLabel();
		_sprite?.Play("spin");

		int playerLuck = player_data.Instance != null ? player_data.Instance.LuckStat : 0;
		ItemRarity rolledRarity = RollRarity(playerLuck);

		int randomIndex = (int)GD.RandRange(0, ItemPool.Count - 1);
		ItemData randomTemplate = ItemPool[randomIndex];
		RolledItemStats rolledStats = GenerateItemStats(randomTemplate, rolledRarity, playerLuck);

		if (worldItemScene != null)
		{
			Node2D itemInstance = worldItemScene.Instantiate<Node2D>();
			GetTree().CurrentScene.AddChild(itemInstance);
			
			itemInstance.GlobalPosition = dropSpawnPoint != null ? dropSpawnPoint.GlobalPosition : GlobalPosition + new Vector2(0, 20);
			
			if (itemInstance is WorldItem worldItemScript)
			{
				worldItemScript.Initialize(rolledStats);
			}
		}

		GD.Print($"[SlotMachine] Spawned [{rolledRarity}] {randomTemplate.BaseName} | Dmg: {rolledStats.Damage}, Luck: {rolledStats.Luck}, HP: {rolledStats.Health}, Speed: {rolledStats.Speed}");
	}

	private RolledItemStats GenerateItemStats(ItemData template, ItemRarity rarity, int luck)
	{
		var stats = new RolledItemStats
		{
			Template = template,
			Rarity = rarity
		};

		// Determine how many stat modifiers to apply based on rarity (1 to 4 stats)
		int statCount = rarity switch
		{
			ItemRarity.Common => 1,
			ItemRarity.Rare => (int)GD.RandRange(1, 2),
			ItemRarity.Epic => (int)GD.RandRange(2, 3),
			ItemRarity.Legendary => (int)GD.RandRange(3, 4),
			_ => 1
		};

		// Scaling multiplier based on rarity and player luck
		float rarityMultiplier = rarity switch
		{
			ItemRarity.Common => 1.0f,
			ItemRarity.Rare => 1.5f,
			ItemRarity.Epic => 2.2f,
			ItemRarity.Legendary => 3.0f,
			_ => 1.0f
		};

		// Pool of possible stats to roll: 0 = Damage, 1 = Luck, 2 = Health, 3 = Speed
		System.Collections.Generic.List<int> availableStats = new() { 0, 1, 2, 3 };
		
		for (int i = 0; i < statCount && availableStats.Count > 0; i++)
		{
			int statIndex = (int)GD.RandRange(0, availableStats.Count - 1);
			int chosenStat = availableStats[statIndex];
			availableStats.RemoveAt(statIndex); // Don't roll the exact same stat twice on one item

			switch (chosenStat)
			{
				case 0: // Damage
					stats.Damage = Mathf.RoundToInt((template.BaseDamage + GD.RandRange(1, 3)) * rarityMultiplier);
					break;
				case 1: // Luck
					stats.Luck = Mathf.RoundToInt((template.BaseLuck + GD.RandRange(0, 2)) * rarityMultiplier);
					break;
				case 2: // Health
					stats.Health = Mathf.RoundToInt(GD.RandRange(1, 3) * rarityMultiplier);
					break;
				case 3: // Speed
					float rawSpeed = (float)GD.RandRange(5.0f, 15.0f) * rarityMultiplier;
					stats.Speed = Mathf.Round(rawSpeed * 10.0f) / 10.0f;
					break;
			}
		}

		return stats;
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

// Data container class to hold the randomized output of a single roll
public class RolledItemStats
{
	public ItemData Template;
	public ItemRarity Rarity;
	public int Damage = 0;
	public int Luck = 0;
	public int Health = 0;
	public float Speed = 0f;
	
	public string DisplayName => $"{Rarity} {Template.BaseName}";
}
