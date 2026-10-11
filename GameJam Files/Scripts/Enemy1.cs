using Godot;
using System;

public partial class Enemy1 : CharacterBody2D
{
	// Tracking Player
	[Export]
	public Node2D Target;

	// On Death drop items
	[Export]
	public PackedScene DroppedItemScene;

	public const float Speed = 40.0f;
	public const float JumpVelocity = -100.0f;

	// For AnimatedSprite2D
	private AnimatedSprite2D _sprite;

	// Stats
	private int health = 10;

	// Hurt Animation Timer
	private Timer hurtTimer;

	// Enemy Knockback
	private Vector2 knockbackVelocity = Vector2.Zero;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		// Now play the default animation
		_sprite.Play("default");

		// For Hurt Timer
		hurtTimer = new Timer(); // Calls the method after the second has elapsed
		hurtTimer.WaitTime = 1.0; // Wait 1 second
		hurtTimer.OneShot = true; // Runs only once instead of repeating
		AddChild(hurtTimer);

		hurtTimer.Timeout += OnHurtTimerTimeout;
		//_sprite.Play("hurt");

	}

	public void SpawnDroppedItem()
	{
		// Testing if item dropped failed
		if(DroppedItemScene == null)
		{
			GD.PrintErr("DroppedItemScene has not been assigned");
			return;
		}

		// Creating a new instance of the dropped item.
		RigidBody2D item = DroppedItemScene.Instantiate<RigidBody2D>();

		// Add it to the current scene
		GetTree().CurrentScene.AddChild(item);

		// Now place the item where the character is at
		item.GlobalPosition = GlobalPosition;

		// Now generate a random launch for both the sideways firection and upward direction
		float horizontalImpulse = (float)GD.RandRange(-100.0f, 100.0);
		float verticalImpulse = (float)GD.RandRange(-180.0f, -120.0);

		Vector2 launchImpulse = new Vector2(horizontalImpulse, verticalImpulse);

		// Lastly you have to give the item its initial push
		item.ApplyCentralImpulse(launchImpulse);

	}

	// Hurt Animation
	public void Hurt()
	{
		_sprite.Play("hurt");

		hurtTimer.Stop();
		hurtTimer.Start();
	}

	private void OnHurtTimerTimeout()
	{
		_sprite.Play("default");
	}

	public void ApplyKnockback(Vector2 direction, float force)
	{
		knockbackVelocity = direction * force;
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

		// Make the zombie face the player
		if (direction.X < 0)
		{
			_sprite.FlipH = true;
		}
		else if (direction.X > 0)
		{
			_sprite.FlipH = false;
		}

		// For Gravity to work you have to only target horizontal movement
		velocity.X = direction.X * Speed;


		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Now apply the knockback and gradually reduce it

		velocity += knockbackVelocity;
		knockbackVelocity = knockbackVelocity.MoveToward(Vector2.Zero, 1200.0f * (float)delta);

		Velocity = velocity;
		MoveAndSlide();
	}
}
