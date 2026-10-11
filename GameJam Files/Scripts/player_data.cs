using Godot;
using System;
using System.Collections.Generic;

public partial class player_data : Node
{
	public static player_data Instance { get; private set; }

	// Resources & Currencies
	public int ZombieFleshValue { get; set; } = 100;
	public int SkeletonBonesValue { get; set; } = 0;

	// Permanent Slot Machine Stat Ups
	public int DamageBonus { get; set; } = 0;
	public float SpeedBonus { get; set; } = 0.0f;
	public int MaxHealthBonus { get; set; } = 0;
	public int LuckStat { get; set; } = 0;

	public override void _EnterTree()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}
		Instance = this;
	}

	// Slot Machine Reward Handlers
	public void AddDamage(int amount = 1)
	{
		DamageBonus += amount;
		GD.Print($"Permanent Upgrade! Damage Bonus is now: {DamageBonus}");
	}

	public void AddSpeed(float amount = 10.0f)
	{
		SpeedBonus += amount;
		GD.Print($"Permanent Upgrade! Speed Bonus is now: {SpeedBonus}");
	}

	public void AddMaxHealth(int amount = 1)
	{
		MaxHealthBonus += amount;
		GD.Print($"Permanent Upgrade! Max Health Bonus is now: {MaxHealthBonus}");
	}

	public void AddLuck(int amount = 1)
	{
		LuckStat += amount;
		GD.Print($"Permanent Upgrade! Luck Stat is now: {LuckStat}");
	}
	
	public void AddZombieFlesh(int amount = 1)
	{
		ZombieFleshValue += amount;
	}

	public void AddSkeletonBones(int amount = 1)
	{
		SkeletonBonesValue += amount;
	}

	public void ResetData()
	{
		ZombieFleshValue = 0;
		SkeletonBonesValue = 0;
		DamageBonus = 0;
		SpeedBonus = 0.0f;
		MaxHealthBonus = 0;
		LuckStat = 0;
	}
	
	private HashSet<string> _completedFloors = new();

	public bool IsFloorCompleted(string floorId)
	{
		return _completedFloors.Contains(floorId);
	}

	public void MarkFloorCompleted(string floorId)
	{
		if (!_completedFloors.Contains(floorId))
		{
			_completedFloors.Add(floorId);
		}
	}
}
