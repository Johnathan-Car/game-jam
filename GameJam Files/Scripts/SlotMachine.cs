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

	// Deduct currency & update cost
	player_data.Instance.AddZombieFlesh(-currentCost);
	currentSpinCount++;
	UpdateCostLabel();

	_sprite?.Play("spin");

	// Pick a random ItemData resource from your pool
	int randomIndex = (int)GD.RandRange(0, ItemPool.Count - 1);
	ItemData randomItem = ItemPool[randomIndex];

	// Spitting out the physical item into the world
	if (worldItemScene != null)
	{
		Node2D itemInstance = worldItemScene.Instantiate<Node2D>();
		GetTree().CurrentScene.AddChild(itemInstance);
		
		itemInstance.GlobalPosition = dropSpawnPoint != null ? dropSpawnPoint.GlobalPosition : GlobalPosition + new Vector2(0, 20);

		// Pass the ItemData resource directly to the world item
		if (itemInstance is WorldItem worldItemScript)
		{
			worldItemScript.Initialize(randomItem);
		}
	}

	GD.Print($"[SlotMachine] Spawned {randomItem.BaseName} (Dmg: {randomItem.BaseDamage}, Luck: {randomItem.BaseLuck})");
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
