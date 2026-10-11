using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Export] private float baseSpeed = 120.0f;
	private float Speed = 100;
	private float LuckStat = 0;
	[Export] private AnimatedSprite2D _sprite;
	[Export] private AnimatedSprite2D slashSprite;
	[Export] private Area2D attackHitbox;
	[Export] public Label ZombieFleshCount;
	[Export] private float slashOffset = 16.0f;
	[Export] private Timer attackHitboxTimer;
	[Export] public int MaxHP = 10;
	[Export] public HealthBar myHealth;
	private bool isInvincible = false;
	[Export] private float invincibilityDuration = 1.0f; // 1 second of i-frames
	
	public int HP = 10;

	// Combo Management
	private int comboStep = 0; // 0 = Idle/Rest, 1 = First Attack, 2 = Second Attack
	private bool isAttacking = false;
	private bool comboBuffered = false; // Tracks if attack input was pressed during attack 1
	
	private Vector2 knockbackVelocity = Vector2.Zero;
	public const float JumpVelocity = -250.0f;
	private bool facingLeft = false;
	
	private int ZombieFleshValue = 0;

	public override void _Ready()
	{
		base._Ready();
		_sprite.Play("default");
		
		if (player_data.Instance != null)
		{		
			if (ZombieFleshCount != null)
			{
				ZombieFleshCount.Text = ZombieFleshValue.ToString();
			}
		}

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
	
	public void LoadStatsFromPlayerData()
	{
		// Check if player_data singleton exists and is initialized
		if (player_data.Instance == null)
		{
			GD.Print("[Player] player_data.Instance is null. Falling back to base inspector stats.");
			
			return;
		}

		var data = player_data.Instance;
		
		MaxHP += data.MaxHealthBonus;
		myHealth.SetMaxHealth(MaxHP);
		myHealth.SetHealth(HP);		
		
		Speed = Speed + data.SpeedBonus;
		LuckStat = data.LuckStat;
	}
	public void ApplyKnockback(Vector2 direction, float force)
	{
		knockbackVelocity = direction * force;
	}

	public void TakeDamage(int damage)
	{
		// If currently invincible, ignore damage entirely!
		if (isInvincible) return;

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
			return;
		}

		// Trigger I-Frames and sprite blinking
		StartInvincibility();
	}
	
	private async void StartInvincibility()
	{
		isInvincible = true;

		SetCollisionMaskValue(2, false);

		float elapsed = 0f;
		while (elapsed < invincibilityDuration)
		{
			if (_sprite != null)
			{
				// Quick blinking feedback effect
				_sprite.Visible = !_sprite.Visible;
			}
			await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
			elapsed += 0.1f;
		}

		// Restore normal visibility
		if (_sprite != null)
		{
			_sprite.Visible = true;
		}

		SetCollisionMaskValue(2, true);

		isInvincible = false;
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
		player_data.Instance.AddZombieFlesh(1);

		if (ZombieFleshCount != null)
		{
			ZombieFleshCount.Text = player_data.Instance.ZombieFleshValue.ToString();
			
		}
		else
		{
			GD.PrintErr("The ZombieFleshCount label hasn't been assigned");
		}

		GD.Print("Zombie Flesh: " + ZombieFleshValue);
	}

	private void OnSlashAnimationFinished()
	{
		if (comboStep == 1 && comboBuffered)
		{
			ExecuteAttack2();
		}
		else
		{
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
