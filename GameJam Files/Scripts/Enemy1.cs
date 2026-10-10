using Godot;
using System;

public partial class Enemy1 : CharacterBody2D
{
	// Tracking Player
	[Export]
	public Node2D Target;

	public const float Speed = 300.0f;
	public const float JumpVelocity = -400.0f;

	// For AnimatedSprite2D
	private AnimatedSprite2D _sprite;

	// Stats
	private int health = 3;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		// Now play the default animation
		_sprite.Play("default");
		//_sprite.Play("hurt");

	}

	// Hurt Animation
	public void Hurt()
	{
		//_sprite.Play("hurt");
	}

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
}
