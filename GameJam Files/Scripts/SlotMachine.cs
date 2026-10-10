using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class SlotMachine : Node2D
{
	[Export] public Godot.Collections.Array<ItemData> ItemPool { get; set; } = new();
	[Export] private Label _promptLabel;
	[Export] private AnimatedSprite2D _sprite;

	private bool _isPlayerNearby = false;
	private Player _player; 

	public override void _Ready()
	{
		Area2D interactionArea = GetNode<Area2D>("Area2D");
		interactionArea.BodyEntered += OnBodyEntered;
		interactionArea.BodyExited += OnBodyExited;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_isPlayerNearby && @event.IsActionPressed("interact")) // Ensure "interact" is mapped in Input Map
		{
			GD.Print("Spinning SLots");
			SpinSlotMachine();
		}
	}

	private void SpinSlotMachine()
	{	
		GD.Print("RunningMachine");
		
		if (ItemPool == null || ItemPool.Count == 0)
		{
			GD.PrintErr("Error: ItemPool is null or empty in SlotMachine! Did you assign templates in the Inspector?");
			return;
		}
		
		_sprite?.Play("spin");

		//int playerLuck = _player.LuckStat; 
		ItemRarity rolledRarity = RollRarity(0);

		// 2. Pick a random base item template from your pool
		int randomIndex = (int)GD.RandRange(0, ItemPool.Count - 1);
		ItemData randomTemplate = ItemPool[randomIndex];
		
		EquipableItem reward = new EquipableItem(randomTemplate, rolledRarity, 0);	
		
		GD.Print("--- SLOT MACHINE PAYOUT ---");
		GD.Print($"Item Name: {reward.DisplayName}");
		GD.Print($"Slot Type: {reward.SlotType}");
		GD.Print($"Rarity:    {reward.Rarity}");
		GD.Print($"Damage:    {reward.FinalDamage}");
		GD.Print($"Luck:      {reward.FinalLuck}");
		GD.Print("---------------------------");
		
		// TODO: Pass 'reward' to Player Equipment / Inventory
}

	private ItemRarity RollRarity(int luck)
	{
		// Base weights: Common = 60%, Rare = 25%, Epic = 10%, Legendary = 5%
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

	private void OnBodyEntered(Node2D body)
	{
		GD.Print("PlayerNear");
		if (body.IsInGroup("Player"))
		{
			_isPlayerNearby = true;
			if (_promptLabel != null) _promptLabel.Visible = true;
		}
	}

	private void OnBodyExited(Node2D body)
	{
		if (body.IsInGroup("Player"))
		{
			_player = null;
			_isPlayerNearby = false;
			if (_promptLabel != null) _promptLabel.Visible = false;
		}
	}
}
