using Godot;
using System;

public partial class Enemy2 : CharacterBody2D
{
	// Tracking Player
	[Export]
	public Node2D Target;

	public const float Speed = 300.0f;
	public const float JumpVelocity = -400.0f;


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
		velocity.X = direction.X * Speed;


		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump. This won't be used for now
		//if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		//{
		//	velocity.Y = JumpVelocity;
		//}

		Velocity = velocity;
		MoveAndSlide();
	}

	// This will be for the ranged attack the Skeleton will do
	//public attack2();
}
