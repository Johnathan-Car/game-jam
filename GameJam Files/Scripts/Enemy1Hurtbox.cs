using Godot;
using System;

public partial class Enemy1Hurtbox : Area2D
{
	private int enemy1Health = 10;
	private bool isDead = false;

	[Export]
	public float KnockbackForce = 200.0f;

	[Export]
	public int Damage = 2;

	[Export]
	public Node2D Player;

	public override void _Ready()
	{
		AreaEntered += OnAreaEntered;
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (!body.IsInGroup("PLAYER"))
			return;

		if (body is Player player)
		{
			player.TakeDamage(Damage);
		}
	}
	
	public void OnAreaEntered(Area2D area)
	{
		// Check whether the area is the player's sword hitbox
		if (!area.IsInGroup("PLAYERATTACK") || isDead || !area.Monitoring)
		{
			return;
		}
			
		Node2D enemy = GetParent<Node2D>();
		
		if (enemy is not Enemy1 enemyScript)
		{
			GD.Print("Hurtbox parent is not an Enemy1");
			return;
		}
		
		int totalDamage = 5 + (player_data.Instance != null ? player_data.Instance.DamageBonus : 0);
		GD.Print("Enemy was hit!");
		enemy1Health=-totalDamage;


		GD.Print("Enemy health: " + enemy1Health);

		if (Player == null)
		{
			// Fallback auto-assign player if unassigned in Inspector
			Player = GetTree().GetFirstNodeInGroup("Player") as Node2D;
		}

		if (Player != null)
		{
			Vector2 direction = (enemy.GlobalPosition - Player.GlobalPosition).Normalized();
			enemyScript.ApplyKnockback(direction, KnockbackForce);
		}
		
		enemyScript.Hurt();
		
		// Check if the enemy has died
		if (enemy1Health <= 0)
		{
			isDead = true;
			enemyScript.CallDeferred(nameof(Enemy1.SpawnDroppedItem));
			enemy.QueueFree();
		}
	}
}
