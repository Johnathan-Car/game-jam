using Godot;
using System;

public partial class Enemy1Hurtbox : Area2D
{
	private int enemy1Health = 3;
	public override void _Ready()
	{
		AreaEntered += OnAreaEntered;
	}

	public void OnAreaEntered(Area2D area)
	{
		// Goal is to check whether the area is the player's sword hitbox
		if (area.IsInGroup("PLAYERATTACK"))
		{
			GD.Print("Enemy was hit!");
			enemy1Health--;

			GD.Print("Enemy health:" + enemy1Health);

			if(enemy1Health <= 0)
			{
    			Node2D enemy = GetParent<Node2D>();
				enemy.QueueFree();
			}
		}
	}
}
