using Godot;
using System;

public partial class PickupAreaSkeleton : Area2D
{
	private bool collected = false;

	public override void _Ready()
	{
		// Detect when any type of physics body enters the pickup area.
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (collected) // The item can only be collected once.
		{
			return;
		}

		if (body is Player player)
		{
			collected = true;

			// Update their inventory count
			player.OnBonesCollected();

			// Remove item
			GetParent().QueueFree();
		}
	}
}
