using Godot;
using System;

public partial class Enemy1 : CharacterBody2D
{
	[Export] public Node2D Target;
	[Export] public PackedScene DroppedItemScene;

	public const float Speed = 40.0f;
	private AnimatedSprite2D _sprite;

	// Centralized Stats & Invincibility
	[Export] private int maxHealth = 20;
	private int currentHealth;
	private bool isInvincible = false;
	private Timer hurtTimer;
	private Timer invincibilityTimer;
	private Vector2 knockbackVelocity = Vector2.Zero;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.Play("default");
		
		currentHealth = maxHealth; // Initialize health

		if (Target == null)
		{
			Target = GetTree().GetFirstNodeInGroup("Player") as Node2D;
		}

		// Hurt Animation Timer
		hurtTimer = new Timer();
		hurtTimer.WaitTime = 0.2f; // Flash duration
		hurtTimer.OneShot = true;
		AddChild(hurtTimer);
		hurtTimer.Timeout += OnHurtTimerTimeout;

		// Invincibility i-frame Timer
		invincibilityTimer = new Timer();
		invincibilityTimer.WaitTime = 0.4f; // Adjust i-frame window here
		invincibilityTimer.OneShot = true;
		AddChild(invincibilityTimer);
		invincibilityTimer.Timeout += () => isInvincible = false;
	}

	public void TakeDamage(int amount, Vector2 knockbackDir, float knockbackForce)
	{
		if (isInvincible) return; // Prevent taking damage during i-frames

		isInvincible = true;
		invincibilityTimer.Start();

		currentHealth -= amount; // Fixed subtraction
		GD.Print($"Enemy health: {currentHealth}/{maxHealth}");

		ApplyKnockback(knockbackDir, knockbackForce);
		Hurt();

		if (currentHealth <= 0)
		{
			CallDeferred(nameof(SpawnDroppedItem));
			QueueFree();
		}
	}

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
		// ... (keep your existing movement and targeting physics process code here) ...
		Vector2 velocity = Velocity;
		if (Target != null)
		{
			Vector2 direction = GlobalPosition.DirectionTo(Target.GlobalPosition);
			_sprite.FlipH = direction.X < 0;
			velocity.X = direction.X * Speed;
		}

		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		velocity += knockbackVelocity;
		knockbackVelocity = knockbackVelocity.MoveToward(Vector2.Zero, 1200.0f * (float)delta);

		Velocity = velocity;
		MoveAndSlide();
	}

	public void SpawnDroppedItem()
	{
		if (DroppedItemScene == null) return;
		RigidBody2D item = DroppedItemScene.Instantiate<RigidBody2D>();
		GetTree().CurrentScene.AddChild(item);
		item.GlobalPosition = GlobalPosition;
		item.ApplyCentralImpulse(new Vector2((float)GD.RandRange(-100, 100), (float)GD.RandRange(-180, -120)));
	}
}
