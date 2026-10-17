using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class WaveManager : Node
{
	[ExportGroup("Spawn Settings")]
	[Export] private Godot.Collections.Array<Marker2D> _spawnPoints = new();
	[Export] private Sprite2D _warningIndicatorTemplate;
	[Export] private float _warningDuration = 1.5f;
	[Export] private float _breatherDuration = 1.0f; // pause between waves

	[ExportGroup("Enemy Scenes")]
	[Export] private PackedScene _zombieScene;
	[Export] private PackedScene _skeletonScene;

	/// <summary>
	/// Overall progress through the current sequence, 0..1.
	/// Wave k of N reports (k + 1 - 0.5 * fractionAlive) / N, so the value rises the moment a wave
	/// spawns, climbs as enemies die, and hits exactly 1.0 when the final wave is cleared.
	/// </summary>
	[Signal] public delegate void WaveProgressChangedEventHandler(float progress);
	[Signal] public delegate void AllWavesCompletedEventHandler();

	public static WaveManager Instance { get; private set; }
	public bool IsRunning { get; private set; }

	private bool _isCleaningUp = false;

	public override void _EnterTree()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}
		Instance = this;
	}

	public override void _ExitTree()
	{
		_isCleaningUp = true;
		IsRunning = false;

		if (Instance == this)
		{
			Instance = null;
		}
	}

	private bool Aborted() => _isCleaningUp || !GodotObject.IsInstanceValid(this);

	/// <param name="waveConfigs">One string[] of enemy types per wave.</param>
	/// <param name="healthMultiplier">Difficulty scaling applied to every enemy spawned in this sequence.</param>
	public async void StartWaveSequence(string[][] waveConfigs, float healthMultiplier = 1.0f)
	{
		if (IsRunning)
		{
			GD.PrintErr("[WaveManager] A wave sequence is already running; ignoring request.");
			return;
		}

		IsRunning = true;
		int totalWaves = waveConfigs.Length;

		for (int i = 0; i < totalWaves; i++)
		{
			// 1. Telegraph and spawn
			List<Node2D> spawned = await SpawnWaveAsync(waveConfigs[i], healthMultiplier);
			if (Aborted()) return;

			// 2. Wait for the room to clear, reporting progress as enemies fall
			int lastAlive = -1;
			while (true)
			{
				int alive = CountAlive(spawned);
				if (alive == 0) break;

				if (alive != lastAlive)
				{
					lastAlive = alive;
					float aliveFraction = alive / (float)spawned.Count;
					EmitSignal(SignalName.WaveProgressChanged, (i + 1 - 0.5f * aliveFraction) / totalWaves);
				}

				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				if (Aborted()) return;
			}

			EmitSignal(SignalName.WaveProgressChanged, (i + 1) / (float)totalWaves);
			GD.Print($"[WaveManager] Wave {i + 1}/{totalWaves} cleared.");

			// 3. Breather between waves (not after the last one)
			if (i < totalWaves - 1)
			{
				await ToSignal(GetTree().CreateTimer(Mathf.Max(0.0f, _breatherDuration)), SceneTreeTimer.SignalName.Timeout);
				if (Aborted()) return;
			}
		}

		IsRunning = false;
		GD.Print("[WaveManager] Sequence complete.");
		EmitSignal(SignalName.AllWavesCompleted);
	}

	private static int CountAlive(List<Node2D> enemies)
	{
		int alive = 0;
		foreach (var e in enemies)
		{
			if (GodotObject.IsInstanceValid(e) && !e.IsQueuedForDeletion() && e.IsInGroup("Enemies"))
			{
				alive++;
			}
		}
		return alive;
	}

	private async Task<List<Node2D>> SpawnWaveAsync(string[] enemyTypes, float healthMultiplier)
	{
		var spawned = new List<Node2D>();

		if (_spawnPoints.Count == 0)
		{
			GD.PrintErr("[WaveManager] No spawn points assigned!");
			return spawned;
		}

		float safeDuration = Mathf.Max(0.1f, _warningDuration);

		// Shuffle the markers so each wave uses a different spread of the room
		var candidates = new List<Marker2D>();
		foreach (var marker in _spawnPoints)
		{
			if (marker != null) candidates.Add(marker);
		}
		if (candidates.Count == 0)
		{
			GD.PrintErr("[WaveManager] All spawn point references are null!");
			return spawned;
		}

		for (int k = candidates.Count - 1; k > 0; k--)
		{
			int j = (int)(GD.Randi() % (uint)(k + 1));
			(candidates[k], candidates[j]) = (candidates[j], candidates[k]);
		}

		// Warn at exactly the markers that will be used. The flash is what tells the
		// player to get out of the way, so spawning on top of them is intentional.
		int markersUsed = Mathf.Min(enemyTypes.Length, candidates.Count);
		for (int m = 0; m < markersUsed; m++)
		{
			PlaySpawnWarning(candidates[m].GlobalPosition, safeDuration);
		}

		await ToSignal(GetTree().CreateTimer(safeDuration), SceneTreeTimer.SignalName.Timeout);
		if (Aborted()) return spawned;

		int spawnIndex = 0;
		foreach (string enemyType in enemyTypes)
		{
			PackedScene sceneToSpawn = GetEnemyScene(enemyType);
			if (sceneToSpawn == null) continue;

			Marker2D targetMarker = candidates[spawnIndex % candidates.Count];
			spawnIndex++;

			Node2D enemy = sceneToSpawn.Instantiate<Node2D>();

			// Meta is set BEFORE AddChild so an enemy's _Ready() can read it if it wants to.
			enemy.SetMeta("health_multiplier", healthMultiplier);

			GetTree().CurrentScene.AddChild(enemy);
			enemy.GlobalPosition = targetMarker.GlobalPosition;
			enemy.AddToGroup("Enemies");

			// Optional hook: implement `public void ApplyHealthMultiplier(float m)` on your enemy scripts.
			if (!Mathf.IsEqualApprox(healthMultiplier, 1.0f) && enemy.HasMethod("ApplyHealthMultiplier"))
			{
				enemy.Call("ApplyHealthMultiplier", healthMultiplier);
			}

			spawned.Add(enemy);
		}

		return spawned;
	}

	private PackedScene GetEnemyScene(string type)
	{
		return type.ToLower() switch
		{
			"zombie" => _zombieScene,
			"skeleton" => _skeletonScene,
			_ => _zombieScene
		};
	}

	private void PlaySpawnWarning(Vector2 position, float duration)
	{
		Sprite2D indicator = new Sprite2D();
		if (_warningIndicatorTemplate != null)
		{
			indicator.Texture = _warningIndicatorTemplate.Texture;
		}
		indicator.Modulate = new Color(1, 0, 0, 0);

		GetTree().CurrentScene.AddChild(indicator);
		indicator.GlobalPosition = position;

		// 3 loops x (up + down) x duration/6 == duration total
		Tween flash = indicator.CreateTween().SetLoops(3);
		flash.TweenProperty(indicator, "modulate:a", 1.0f, duration / 6.0f);
		flash.TweenProperty(indicator, "modulate:a", 0.2f, duration / 6.0f);
		flash.Finished += () =>
		{
			if (GodotObject.IsInstanceValid(indicator))
			{
				indicator.QueueFree();
			}
		};
	}
}
