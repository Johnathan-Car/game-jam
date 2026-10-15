using Godot;

// Add as a child Node of Player or Enemy. Owns health and i-frames only.
public partial class HealthComponent : Node
{
	[Signal] public delegate void HealthChangedEventHandler(int current, int max);
	[Signal] public delegate void DamagedEventHandler(int amount);
	[Signal] public delegate void DiedEventHandler();
	[Signal] public delegate void InvincibilityChangedEventHandler(bool active);

	[Export] public int MaxHealth { get; set; } = 10;
	[Export] public float InvincibilityDuration { get; set; } = 0.4f;

	public int CurrentHealth { get; private set; }
	public bool IsInvincible { get; private set; }
	public bool IsDead { get; private set; }

	private Timer _iFrameTimer;

	public override void _Ready()
	{
		CurrentHealth = MaxHealth;

		_iFrameTimer = new Timer { OneShot = true };
		AddChild(_iFrameTimer);
		_iFrameTimer.Timeout += OnIFramesEnded;
	}

	// Call from the owner's _Ready once stat bonuses are known.
	public void Initialize(int maxHealth)
	{
		MaxHealth = maxHealth;
		CurrentHealth = maxHealth;
		IsDead = false;
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
	}

	// Returns true if the hit actually landed (false during i-frames or when dead).
	public bool TakeDamage(int amount)
	{
		if (IsDead || IsInvincible || amount <= 0) return false;

		CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0, MaxHealth);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
		EmitSignal(SignalName.Damaged, amount);

		if (CurrentHealth == 0)
		{
			IsDead = true;
			EmitSignal(SignalName.Died);
			return true;
		}

		if (InvincibilityDuration > 0f)
		{
			IsInvincible = true;
			EmitSignal(SignalName.InvincibilityChanged, true);
			_iFrameTimer.Start(InvincibilityDuration);
		}

		return true;
	}

	public void Heal(int amount)
	{
		if (IsDead || amount <= 0) return;

		CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, MaxHealth);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
	}

	private void OnIFramesEnded()
	{
		IsInvincible = false;
		EmitSignal(SignalName.InvincibilityChanged, false);
	}
}
