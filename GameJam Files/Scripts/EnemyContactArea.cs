using Godot;
using System;

public partial class EnemyContactArea : Area2D
{
	[Export] public int Damage = 1;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (!body.IsInGroup("PLAYER"))
			return;

		if (body is Player player)
		{
			player.TakeDamage(Damage);
			Vector2 direction = (player.GlobalPosition - GlobalPosition).Normalized();

			player.ApplyKnockback(direction, 14.0f);
		}
	}
}
