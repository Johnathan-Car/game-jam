using Godot;
using System;

public partial class Enemy3 : CharacterBody2D
{
	// Tracking Player
	[Export]
	public Node2D Target;

	public const float Speed = 100.0f;
	//public const float JumpVelocity = -400.0f;


	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// Targeting Player Logic
		if(Target == null)
		{
			return;
		}

		Vector2 direction = GlobalPosition.DirectionTo(Target.GlobalPosition);

		// For Gravity to work you have to only target horizontal movement
		velocity = direction * Speed;


		// Add the gravity. Will be ignored for the Wyvern as it is a flying enemy
		//if (!IsOnFloor())
		//{
		//	velocity += GetGravity() * (float)delta;
		//}

		// Handle Jump. This won't be used for now
		//if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		//{
		//	velocity.Y = JumpVelocity;
		//}

		Velocity = velocity;
		MoveAndSlide();
	}

	// This will be for the dive attack the Wyvern will do
	//public attack3();
}
