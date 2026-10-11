using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Export] private float Speed = 120.0f;
	[Export] private AnimatedSprite2D _sprite;
	[Export] private AnimatedSprite2D slashSprite;
	[Export] private Area2D attackHitbox;
	[Export] public Label ZombieFleshCount;
	[Export] private float slashOffset = 16.0f;
	[Export] private Timer attackHitboxTimer;
	[Export] public int MaxHP = 10;
	[Export] public HealthBar myHealth;
	public int HP = 10;

	// Combo Management
	private int comboStep = 0; // 0 = Idle/Rest, 1 = First Attack, 2 = Second Attack
	private bool isAttacking = false;
	private bool comboBuffered = false; // Tracks if attack input was pressed during attack 1
	
	private Vector2 knockbackVelocity = Vector2.Zero;
	public const float JumpVelocity = -250.0f;
	public int LuckStat = 0;

	private bool facingLeft = false;
	public int ZombieFleshValue = 0;

	public override void _Ready()
	{
		base._Ready();
		if(IsInstanceValid(myHealth))
		{
			//myHealth.MaxValue = MaxHP;
			myHealth.SetMaxHealth(MaxHP);
			myHealth.SetHealth(HP);		
		}
		
		_sprite.Play("default");

		if (slashSprite != null)
		{
			slashSprite.Visible = false;
			slashSprite.SpeedScale = 1.8f;
			slashSprite.AnimationFinished += OnSlashAnimationFinished;
		}

		if (attackHitbox != null)
		{
			attackHitbox.Monitoring = false;
		}

		if (attackHitboxTimer != null)
		{
			attackHitboxTimer.Timeout += OnAttackHitboxTimerTimeout;
		}

		UpdateSlashPosition();
	}

	public void ApplyKnockback(Vector2 direction, float force)
	{
		knockbackVelocity = direction * force;
	}

	public void TakeDamage(int damage)
	{
		HP = Mathf.Clamp(HP - damage, 0, MaxHP);

		// For testing
		GD.Print("Player HP: " + HP + "/" + MaxHP);

		if (IsInstanceValid(myHealth))
		{
			myHealth.SetHealth(HP);
		}
		
		if (HP <= 0)
		{
			GD.Print("Player has died!");
			// Death behavior here.
		}
	}

	private void OnAttackHitboxTimerTimeout()
	{
		if (attackHitbox != null)
		{
			attackHitbox.Monitoring = false;
		}
	}

	// Item Collection Callback
	public void OnItemCollected()
	{
		ZombieFleshValue += 1;

		if (ZombieFleshCount != null)
		{
			ZombieFleshCount.Text = ZombieFleshValue.ToString();
		}
		else
		{
			GD.PrintErr("The ZombieFleshCount label hasn't been assigned");
		}

		GD.Print("Zombie Flesh: " + ZombieFleshValue);
	}

	private void OnSlashAnimationFinished()
	{
		// Check if we just finished the first slash and the player buffered the second hit
		if (comboStep == 1 && comboBuffered)
		{
			ExecuteAttack2();
		}
		else
		{
			// Reset back to rest state
			ResetComboState();
		}
	}

	private void ExecuteAttack1()
	{
		comboStep = 1;
		isAttacking = true;
		comboBuffered = false;

		_sprite.Play("attack1"); // Body swing 1

		if (slashSprite != null)
		{
			slashSprite.Visible = true;
			slashSprite.Frame = 0;
			slashSprite.Play("slash1"); // First smear animation
		}

		if (attackHitbox != null)
		{
			attackHitbox.Monitoring = true;

			if (attackHitboxTimer != null)
			{
				attackHitboxTimer.Start(0.2);
			}
		}
	}

	private void ExecuteAttack2()
	{
		comboStep = 2;
		isAttacking = true;
		comboBuffered = false;

		_sprite.Play("attack2"); // Body swing 2

		if (slashSprite != null)
		{
			slashSprite.Visible = true;
			slashSprite.Frame = 0;
			slashSprite.Play("slash2"); // Second smear animation
		}

		if (attackHitbox != null)
		{
			attackHitbox.Monitoring = true;
			
			if (attackHitboxTimer != null)
			{
				attackHitboxTimer.Start(0.5);
			}
		}
	}

	private void ResetComboState()
	{
		comboStep = 0;
		isAttacking = false;
		comboBuffered = false;

		if (slashSprite != null)
		{
			slashSprite.Visible = false;
		}

		if (attackHitbox != null)
		{
			attackHitbox.Monitoring = false;
		}

		if (attackHitboxTimer != null)
		{
			attackHitboxTimer.Stop();
		}
	}

	private void UpdateSlashPosition()
	{
		float directionMultiplier = facingLeft ? -1.0f : 1.0f;
		Vector2 offsetVector = new Vector2(slashOffset * directionMultiplier, 0);

		if (slashSprite != null)
		{
			slashSprite.Position = offsetVector;
			slashSprite.FlipH = facingLeft;
		}

		if (attackHitbox != null)
		{
			attackHitbox.Position = offsetVector;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		velocity += knockbackVelocity;

		knockbackVelocity = knockbackVelocity.MoveToward(Vector2.Zero, 14.0f * (float)delta);

		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

		if (direction.X < 0 && !facingLeft)
		{
			facingLeft = true;
			UpdateSlashPosition();
		}
		else if (direction.X > 0 && facingLeft)
		{
			facingLeft = false;
			UpdateSlashPosition();
		}

		_sprite.FlipH = facingLeft;

		if (knockbackVelocity.Length() > 10.0f)
		{
			// Let the knockback move the player... otherwise
		}

		else if (direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}

		// Input Handling for 1-2 Combo
		if (Input.IsActionJustPressed("meleeAttack"))
		{
			if (comboStep == 0)
			{
				ExecuteAttack1();
			}
			else if (comboStep == 1 && isAttacking)
			{
				comboBuffered = true;
			}
		}

		// Body animations when not attacking
		if (!isAttacking)
		{
			if (attackHitbox != null)
			{
				attackHitbox.Monitoring = false;
			}

			if (direction != Vector2.Zero)
			{
				_sprite.Play("run");
			}
			else
			{
				_sprite.Play("default");
			}
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
