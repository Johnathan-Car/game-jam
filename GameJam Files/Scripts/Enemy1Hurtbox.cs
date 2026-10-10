using Godot;
using System;

public partial class Enemy1Hurtbox : Area2D
{
	private int enemy1Health = 3;
	
	// Death flag to make sure it only activates once
	private bool isDead = false;

	// For Knockback
	[Export]
	public float KnockbackForce = 800.0f;

	[Export]
	public Node2D Player;
	public override void _Ready()
	{
		AreaEntered += OnAreaEntered;
	}

	public void OnAreaEntered(Area2D area)
	{
		// Goal is to check whether the area is the player's sword hitbox
		if (!area.IsInGroup("PLAYERATTACK") || isDead)
		{
			return;
		}
			
		// Now push the enemy away from the player
		Node2D enemy = GetParent<Node2D>();
		
		// Confirming whether or not the parent is an Enemy1
		if(enemy is not Enemy1 enemyScript)
		{
			GD.Print("Hurtbox parent is not an enemy1");
			return;
		}

		GD.Print("Enemy was hit!");
		enemy1Health--;

		GD.Print("Enemy health:" + enemy1Health);


		// In case you forget to put Player in the Inspector, check if player is assigned
		if (Player == null)
		{
			GD.PrintErr("Player has not been assigned to the Enemy1Hurtbox.");
			return;
		}

		// Calculate knockback direction
		Vector2 direction = (enemy.GlobalPosition - Player.GlobalPosition).Normalized();

		// Apply the knockback now. Then play the hurt animation
		enemyScript.ApplyKnockback(direction, KnockbackForce);
		enemyScript.Hurt();
		
		// Checking if the enemy has died
		if(enemy1Health <= 0)
		{
			// Enemy is dead
			isDead = true;
		
			// Spawn dropped item
			enemyScript.CallDeferred(nameof(Enemy1.SpawnDroppedItem));

			// Remove the enemy
			enemy.QueueFree();
		}
	}
}
