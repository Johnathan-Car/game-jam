using Godot;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
	// ── Constants ────────────────────────────────────────────────────────────
	public const float JumpVelocity = -300.0f;

	private const string ActionLeft = "ui_left";
	private const string ActionRight = "ui_right";
	private const string ActionJump = "ui_accept";
	private const string ActionAttack = "meleeAttack";
	private const string ActionDown = "ui_down";

	private const string AnimPogo = "pogo";            
	private const string AnimPogoSlash = "pogo_slash"; 
	private const double PogoDuration = 0.25;
	private const float PogoBounceVelocity = -250.0f;

	private const string AnimIdle = "default";
	private const string AnimRun = "run";
	private const string AnimAttack1 = "attack1";
	private const string AnimAttack2 = "attack2";
	private const string AnimSlash1 = "slash1";
	private const string AnimSlash2 = "slash2";
	private const string AnimDeath = "death";
	private const string AnimSlumped = "slumped";

	private const double Attack1HitboxTime = 0.2;
	private const double Attack2HitboxTime = 0.5;
	private const float KnockbackDecay = 800.0f;
	private const float KnockbackControlThreshold = 10.0f;
	private const double DeathSceneDelay = 1.0;
	private const string MainMenuScene = "res://GameJam Files/Levels/main_menu.tscn";

	// ── Inspector references ─────────────────────────────────────────────────
	[Export] private HealthComponent health;
	[Export] private AnimatedSprite2D _sprite;
	[Export] private AnimatedSprite2D slashSprite;
	[Export] private Area2D attackHitbox;
	[Export] private Timer attackHitboxTimer;
	[Export] private Area2D pogoHitbox;
	[Export] private AnimatedSprite2D pogoSlashSprite;
	[Export] public Label ZombieFleshCount;
	[Export] public HealthBar CurrentHealthBar;

	// ── Inspector tuning ─────────────────────────────────────────────────────
	[Export] private float baseSpeed = 100.0f;
	[Export] private float slashOffset = 16.0f;
	[Export] private float pogoOffset = 16.0f; // how far below the player the pogo hitbox/effect sits
	[Export] public int MaxHP = 10;

	// ── Stats ────────────────────────────────────────────────────────────────
	private float speed;
	public float LuckStat { get; private set; }
	public int HP => health.CurrentHealth;

	// ── State ────────────────────────────────────────────────────────────────
	private bool facingLeft;
	private bool dead;
	private bool slumped;
	private Vector2 knockbackVelocity = Vector2.Zero;
	private Tween blinkTween;

	// Combo
	private int comboStep;
	private bool isAttacking;
	private bool comboBuffered;
	private readonly HashSet<Node2D> enemiesHitThisSwing = new();

	// Pogo
	private bool isPogoing;
	private bool pogoBounced;
	private Timer pogoTimer;

	// ── Setup ────────────────────────────────────────────────────────────────
	public override void _Ready()
	{
		health ??= GetNode<HealthComponent>("Health");

		// Connect before Initialize so the health bar gets the starting value.
		health.HealthChanged += OnHealthChanged;
		health.Died += OnDied;
		health.InvincibilityChanged += OnInvincibilityChanged;

		_sprite.Play(AnimIdle);

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

		if (pogoSlashSprite != null)
		{
			pogoSlashSprite.Visible = false;
			pogoSlashSprite.Position = new Vector2(0, pogoOffset);
		}

		if (pogoHitbox != null)
		{
			pogoHitbox.Monitoring = false;
			pogoHitbox.Position = new Vector2(0, pogoOffset);
		}

		// Ends the pogo window
		pogoTimer = new Timer { OneShot = true, WaitTime = PogoDuration };
		AddChild(pogoTimer);
		pogoTimer.Timeout += EndPogo;

		LoadStatsFromPlayerData();
		UpdateFleshLabel();
		UpdateSlashPosition();
	}

	public void LoadStatsFromPlayerData()
	{
		int maxHp = MaxHP;
		speed = baseSpeed;
		LuckStat = 0;

		var data = player_data.Instance;
		if (data != null)
		{
			maxHp += data.MaxHealthBonus;
			speed += data.SpeedBonus;
			LuckStat = data.LuckStat;
		}
		else
		{
			GD.Print("[Player] player_data.Instance is null. Falling back to base inspector stats.");
		}

		health.Initialize(maxHp);
	}

	// ── Health ───────────────────────────────────────────────────────────────
	public void TakeDamage(int damage)
	{
		health.TakeDamage(damage);
	}

	public void ApplyKnockback(Vector2 direction, float force)
	{
		// Horizontal only; direction.X decides which way we get pushed.
		float side = direction.X >= 0 ? 1.0f : -1.0f;
		knockbackVelocity = new Vector2(side * force, 0);
	}

	private void OnHealthChanged(int current, int max)
	{
		if (IsInstanceValid(CurrentHealthBar))
		{
			CurrentHealthBar.SetHealth(current);
		}
		GD.Print($"Player HP: {current}/{max}");
	}

	private void OnInvincibilityChanged(bool active)
	{
		// Ignore enemy bodies (collision layer 2) while invincible.
		SetCollisionMaskValue(2, !active);

		blinkTween?.Kill();

		if (active)
		{
			blinkTween = CreateTween().SetLoops();
			blinkTween.TweenProperty(_sprite, "modulate:a", 0.3f, 0.1f);
			blinkTween.TweenProperty(_sprite, "modulate:a", 1.0f, 0.1f);
		}
		else
		{
			_sprite.Modulate = Colors.White;
		}
	}

	private void OnDied()
	{
		dead = true;
		GD.Print("Player has died!");

		blinkTween?.Kill();
		_sprite.Modulate = Colors.White;
		_sprite.Visible = true;

		ResetComboState();
		EndPogo();
		SetDeferred("collision_layer", 0);
		SetDeferred("collision_mask", 0);

		_sprite.Play(AnimDeath);

		// Let the death animation play before leaving the level.
		GetTree().CreateTimer(DeathSceneDelay).Timeout +=
			() => SceneLoader.Instance.ChangeScene(MainMenuScene);
	}

	// ── Items / HUD ──────────────────────────────────────────────────────────
	public void OnItemCollected()
	{
		if (player_data.Instance == null) return;

		player_data.Instance.AddZombieFlesh(1);
		UpdateFleshLabel();
	}

	private void UpdateFleshLabel()
	{
		if (ZombieFleshCount == null || player_data.Instance == null) return;

		ZombieFleshCount.Text = player_data.Instance.ZombieFleshValue.ToString();
	}

	public void PlaySlumpedAnimation()
	{
		slumped = true;
		_sprite.Play(AnimSlumped);
	}

	// ── Combat ───────────────────────────────────────────────────────────────
	private void StartAttack(int step)
	{
		comboStep = step;
		isAttacking = true;
		comboBuffered = false;
		enemiesHitThisSwing.Clear(); // once per swing, not once per check

		_sprite.Play(step == 1 ? AnimAttack1 : AnimAttack2);

		if (slashSprite != null)
		{
			slashSprite.Visible = true;
			slashSprite.Frame = 0;
			slashSprite.Play(step == 1 ? AnimSlash1 : AnimSlash2);
		}

		if (attackHitbox != null)
		{
			// Overlaps are checked every physics frame while this is on (see _PhysicsProcess).
			attackHitbox.Monitoring = true;
			attackHitboxTimer?.Start(step == 1 ? Attack1HitboxTime : Attack2HitboxTime);
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

		attackHitboxTimer?.Stop();
	}

	private void OnSlashAnimationFinished()
	{
		if (comboStep == 1 && comboBuffered)
		{
			StartAttack(2);
		}
		else
		{
			ResetComboState();
		}
	}

	private void OnAttackHitboxTimerTimeout()
	{
		if (attackHitbox != null)
		{
			attackHitbox.Monitoring = false;
		}
	}

	private void CheckHitboxHits()
	{
		foreach (Area2D area in attackHitbox.GetOverlappingAreas())
		{
			if (area is not EnemyHurtbox hurtbox) continue;
			if (hurtbox.GetParent() is not Node2D enemyNode) continue;
			if (!enemiesHitThisSwing.Add(enemyNode)) continue; // already hit this swing

			int damage = 1 + (player_data.Instance != null ? player_data.Instance.DamageBonus : 0);
			Vector2 knockbackDir = (enemyNode.GlobalPosition - GlobalPosition).Normalized();

			hurtbox.TakeHit(damage, knockbackDir);
		}
	}

	// ── Pogo ─────────────────────────────────────────────────────────────────
	private void StartPogo()
	{
		isPogoing = true;
		pogoBounced = false;
		enemiesHitThisSwing.Clear();

		_sprite.Play(AnimPogo);

		if (pogoSlashSprite != null)
		{
			pogoSlashSprite.Visible = true;
			pogoSlashSprite.Frame = 0;
			pogoSlashSprite.Play(AnimPogoSlash);
		}

		if (pogoHitbox != null)
		{
			pogoHitbox.Monitoring = true;
		}

		pogoTimer.Start();
	}

	private void EndPogo()
	{
		isPogoing = false;
		pogoTimer?.Stop();

		if (pogoSlashSprite != null)
		{
			pogoSlashSprite.Visible = false;
		}

		if (pogoHitbox != null)
		{
			pogoHitbox.Monitoring = false;
		}
	}

	// Returns true if at least one enemy was newly hit this frame.
	private bool CheckPogoHits()
	{
		bool hitSomething = false;

		foreach (Area2D area in pogoHitbox.GetOverlappingAreas())
		{
			if (area is not EnemyHurtbox hurtbox) continue;
			if (hurtbox.GetParent() is not Node2D enemyNode) continue;
			if (!enemiesHitThisSwing.Add(enemyNode)) continue;

			int damage = 1 + (player_data.Instance != null ? player_data.Instance.DamageBonus : 0);

			float side = enemyNode.GlobalPosition.X >= GlobalPosition.X ? 1.0f : -1.0f;
			hurtbox.TakeHit(damage, new Vector2(side, 0));

			hitSomething = true;
		}

		return hitSomething;
	}

	// ── Facing ───────────────────────────────────────────────────────────────
	private void UpdateFacing(float inputX)
	{
		if (inputX < 0 && !facingLeft)
		{
			SetFacing(true);
		}
		else if (inputX > 0 && facingLeft)
		{
			SetFacing(false);
		}
	}

	private void SetFacing(bool left)
	{
		facingLeft = left;
		_sprite.FlipH = left;
		UpdateSlashPosition();
	}

	private void UpdateSlashPosition()
	{
		Vector2 offset = new Vector2(facingLeft ? -slashOffset : slashOffset, 0);

		if (slashSprite != null)
		{
			slashSprite.Position = offset;
			slashSprite.FlipH = facingLeft;
		}

		if (attackHitbox != null)
		{
			attackHitbox.Position = offset;
		}
	}

	// ── Per-frame update ─────────────────────────────────────────────────────
	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		Vector2 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity += GetGravity() * dt;
		}

		if (dead)
		{
			velocity.X = 0;
			Velocity = velocity;
			MoveAndSlide();
			return;
		}

		float inputX = Input.GetAxis(ActionLeft, ActionRight);
		UpdateFacing(inputX);

		// Jump
		if (Input.IsActionJustPressed(ActionJump) && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// Horizontal movement
		knockbackVelocity = knockbackVelocity.MoveToward(Vector2.Zero, KnockbackDecay * dt);

		if (knockbackVelocity.Length() > KnockbackControlThreshold)
		{
			velocity.X = knockbackVelocity.X;
		}
		else
		{
			if (!Mathf.IsZeroApprox(inputX))
			{
				slumped = false;
			}
			velocity.X = inputX * speed;
		}

		// Attack input (1-2 combo)
		if (Input.IsActionJustPressed(ActionAttack))
		{
			// Down + attack in the air = pogo
			if (!IsOnFloor() && Input.IsActionPressed(ActionDown) && !isAttacking && !isPogoing)
			{
				StartPogo();
			}
			else if (!isPogoing)
			{
				if (comboStep == 0)
				{
					StartAttack(1);
				}
				else if (comboStep == 1 && isAttacking)
				{
					comboBuffered = true;
				}
			}
		}

		// Hit detection runs every frame while the hitbox is active.
		if (isAttacking && attackHitbox != null && attackHitbox.Monitoring)
		{
			CheckHitboxHits();
		}

		// Pogo: landing cancels it; a hit bounces us upward (applied to the local velocity).
		if (isPogoing)
		{
			if (IsOnFloor())
			{
				EndPogo();
			}
			else if (pogoHitbox != null && pogoHitbox.Monitoring && CheckPogoHits() && !pogoBounced)
			{
				velocity.Y = PogoBounceVelocity;
				pogoBounced = true;
			}
		}

		// Body animation when not attacking
		if (!isAttacking && !isPogoing && !slumped)
		{
			_sprite.Play(Mathf.IsZeroApprox(inputX) ? AnimIdle : AnimRun);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
