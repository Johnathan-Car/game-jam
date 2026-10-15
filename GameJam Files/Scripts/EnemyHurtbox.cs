using Godot;
using System;

public partial class EnemyHurtbox : Area2D
{
	[Export] public float KnockbackForce = 200.0f;
	[Export] public int Damage = 2;

	public override void _Ready()
	{
		// Keep body collision if you want touching the enemy to hurt the player
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body.IsInGroup("PLAYER") && body is Player player)
		{
			player.TakeDamage(Damage);
		}
	}
	
	// Called directly by the Player script during an active swing check
	public void TakeHit(int damage, Vector2 knockbackDir)
	{
		Node2D parentNode = GetParent<Node2D>();
		if (parentNode is not Enemy enemyScript)
		{
			GD.Print("Hurtbox parent is not an Enemy1");
			return;
		}

		// Pass everything cleanly to the main enemy script
		enemyScript.TakeDamage(damage, knockbackDir, KnockbackForce);
	}
}
