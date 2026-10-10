using Godot;
using System;

public partial class Enemy1Hurtbox : Area2D
{
	private int enemy1Health = 3;

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
		if (area.IsInGroup("PLAYERATTACK"))
		{
			// Now push the enemy away from the player
			Node2D enemy = GetParent<Node2D>();
			
			GD.Print("Enemy was hit!");
			enemy1Health--;

			GD.Print("Enemy health:" + enemy1Health);


			// In case you forget toe put Player in the Inspector
			if (Player == null)
			{
				GD.PrintErr("Player has not been assigned to the Enemy1Hurtbox.");
				return;
			}

			Vector2 direction = (enemy.GlobalPosition - Player.GlobalPosition).Normalized();

			if(enemy is Enemy1 enemyScript)
			{
				enemyScript.ApplyKnockback(direction, KnockbackForce);
			}

			if(enemy1Health <= 0)
			{
				enemy.QueueFree();
			}
		}
	}
}
