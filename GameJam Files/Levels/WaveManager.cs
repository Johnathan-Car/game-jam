using Godot;
using System;
using System.Threading.Tasks;

public partial class WaveManager : Node
{
	[ExportGroup("Spawn Settings")]
	[Export] private Godot.Collections.Array<Marker2D> _spawnPoints = new();
	[Export] private Sprite2D _warningIndicatorTemplate; 
	[Export] private float _warningDuration = 1.5f; 

	[ExportGroup("Enemy Scenes")]
	[Export] private PackedScene _zombieScene;
	[Export] private PackedScene _skeletonScene;

	[Signal] public delegate void AllWavesCompletedEventHandler();

	public static WaveManager Instance { get; private set; }

	public override void _EnterTree()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}
		Instance = this;
	}

	public async void StartWaveSequence(string[][] waveConfigs)
	{
		foreach (var waveEnemies in waveConfigs)
		{
			// 1. Telegraph and Spawn the Wave
			await SpawnWaveAsync(waveEnemies);
			if (_isCleaningUp || !GodotObject.IsInstanceValid(this)) return;

			// 2. Wait until all spawned enemies are defeated
			GD.Print("[WaveManager] Wave active. Waiting for room to clear...");
			while (GetTree().GetNodesInGroup("Enemies").Count > 0)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				if (_isCleaningUp || !GodotObject.IsInstanceValid(this)) return;
			}

			GD.Print("[WaveManager] Wave cleared!");
			
			// Optional short breather pause between waves
			await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
			if (_isCleaningUp || !GodotObject.IsInstanceValid(this)) return;
		}

	GD.Print("[WaveManager] All waves completed! Arena unlocked.");
	
	if (!_isCleaningUp)
	{
		EmitSignal(SignalName.AllWavesCompleted);
	}
}

	private async Task SpawnWaveAsync(string[] enemyTypes)
	{
		if (_spawnPoints.Count == 0) return;

		float safeDuration = Mathf.Max(0.1f, _warningDuration);

		// Play warnings
		foreach (var marker in _spawnPoints)
		{
			if (marker != null)
			{
				PlaySpawnWarning(marker.GlobalPosition, safeDuration);
			}
		}

		await ToSignal(GetTree().CreateTimer(safeDuration), "timeout");
		if (!GodotObject.IsInstanceValid(this)) return;
		
		int spawnIndex = 0;
		foreach (string enemyType in enemyTypes)
		{
			PackedScene sceneToSpawn = GetEnemyScene(enemyType);
			if (sceneToSpawn == null) continue;

			Marker2D targetMarker = _spawnPoints[spawnIndex % _spawnPoints.Count];
			if (targetMarker == null) continue;
			
			Node2D enemyInstance = sceneToSpawn.Instantiate<Node2D>();
			
			// Add to current scene tree
			GetTree().CurrentScene.AddChild(enemyInstance);
			enemyInstance.GlobalPosition = targetMarker.GlobalPosition;

			// Tag as an active enemy for room tracking
			enemyInstance.AddToGroup("Enemies");

			spawnIndex++;
		}
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
	
	private bool _isCleaningUp = false;

	public override void _ExitTree()
	{
		// Flag that we are leaving the tree so async tasks stop immediately
		_isCleaningUp = true;
		
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private async void PlaySpawnWarning(Vector2 position, float duration)
	{
		Sprite2D warningIndicator = new Sprite2D();
		if (_warningIndicatorTemplate != null)
		{
			warningIndicator.Texture = _warningIndicatorTemplate.Texture;
		}
		
		warningIndicator.GlobalPosition = position;
		warningIndicator.Modulate = new Color(1, 0, 0, 0); 
		
		GetTree().CurrentScene.CallDeferred("add_child", warningIndicator);
		await ToSignal(warningIndicator, CanvasItem.SignalName.Ready);

		Tween flashTween = CreateTween().SetLoops(3);
		flashTween.TweenProperty(warningIndicator, "modulate:a", 1.0f, duration / 6.0f);
		flashTween.TweenProperty(warningIndicator, "modulate:a", 0.2f, duration / 6.0f);

		await ToSignal(GetTree().CreateTimer(duration), "timeout");
		if (GodotObject.IsInstanceValid(warningIndicator))
		{
			warningIndicator.QueueFree();
		}
	}
}
