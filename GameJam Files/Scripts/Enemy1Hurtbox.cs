using Godot;
using System;

public partial class Enemy1Hurtbox : Area2D
{
	[Export] public float KnockbackForce = 200.0f;
	[Export] public int Damage = 2;
	[Export] public Node2D Player;

	public override void _Ready()
	{
		AreaEntered += OnAreaEntered;
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body.IsInGroup("PLAYER") && body is Player player)
		{
			player.TakeDamage(Damage);
		}
	}
	
	public void OnAreaEntered(Area2D area)
	{
		if (!area.IsInGroup("PLAYERATTACK") || !area.Monitoring)
			return;
			
		Node2D parentNode = GetParent<Node2D>();
		if (parentNode is not Enemy1 enemyScript)
		{
			GD.Print("Hurtbox parent is not an Enemy1");
			return;
		}

		// Calculate total damage
		int totalDamage = 1 + (player_data.Instance != null ? player_data.Instance.DamageBonus : 0);

		// Find player reference for knockback direction
		if (Player == null)
		{
			Player = GetTree().GetFirstNodeInGroup("Player") as Node2D;
		}

		Vector2 knockbackDir = Player != null ? (parentNode.GlobalPosition - Player.GlobalPosition).Normalized() : Vector2.Zero;

		// Pass everything cleanly to the main enemy script
		enemyScript.TakeDamage(totalDamage, knockbackDir, KnockbackForce);
	}
}
