using Godot;
using System;

public partial class AngrySkull : Node2D
{
	[ExportGroup("Nodes")]
	[Export] private Timer _phaseTimer;
	[Export] private AnimatedSprite2D _skullSprite;
	[Export] private Staircase advance_staircase; // progresses to the next level
	[Export] private Staircase retreat_staircase; // the "exit" the player can leave through
	[Export] private string _floorId;

	[ExportGroup("Timing (seconds)")]
	[Export] private float _phaseDuration = 3.0f;   // dormant delay before Round 1 begins
	[Export] private float _restDuration = 8.0f;    // dormant window between Rounds 1->2 and 2->3 (exit is open)
	[Export] private float _angryDelay = 15.0f;     // how long the player can idle after Round 3 before Round 4
	[Export] private float _flickerInterval = 0.2f; // Round 4 frame 8/9 flicker speed

	[ExportGroup("Skull Frames")]
	[Export] private int _completedFrame = 0;  // shown when the floor was already beaten on entry
	[Export] private int _dormantFrame = 1;    // eyes closed
	[Export] private int _fullAwakeFrame = 6;  // max awakeness for a normal round
	[Export] private int _successFrame = 7;    // white / finished
	[Export] private int _angryFrameA = 8;
	[Export] private int _angryFrameB = 9;

	private enum SkullState { Waiting, InRound, Finished }
	private SkullState _state = SkullState.Waiting;

	private sealed record RoundConfig(string[][] Waves, float HealthMultiplier, bool IsAngry = false);

	// Tune enemy mixes / difficulty here.
	private static readonly RoundConfig[] Rounds =
	{
		// Round 1
		new(new[]
		{
			new[] { "zombie", "zombie" },
			new[] { "zombie", "skeleton" },
			new[] { "skeleton", "skeleton" },
		}, 1.0f),

		// Round 2: a little tougher
		new(new[]
		{
			new[] { "zombie", "zombie", "skeleton" },
			new[] { "skeleton", "skeleton", "zombie" },
			new[] { "zombie", "skeleton", "skeleton" },
		}, 1.25f),

		// Round 3: tougher again
		new(new[]
		{
			new[] { "zombie", "zombie", "skeleton", "skeleton" },
			new[] { "skeleton", "skeleton", "skeleton" },
			new[] { "zombie", "zombie", "zombie", "skeleton" },
		}, 1.5f),

		// Round 4: the optional angry round
		new(new[]
		{
			new[] { "zombie", "zombie", "skeleton", "skeleton", "skeleton" },
			new[] { "skeleton", "skeleton", "skeleton", "zombie", "zombie" },
			new[] { "zombie", "zombie", "zombie", "skeleton", "skeleton", "skeleton" },
			new[] { "skeleton", "skeleton", "skeleton", "skeleton", "skeleton" },
		}, 2.25f, IsAngry: true),
	};

	private int _roundIndex = 0;
	private WaveManager _waveManager;
	private Tween _angryTween;

	public override void _Ready()
	{
		if (player_data.Instance != null && player_data.Instance.IsFloorCompleted(_floorId))
		{
			SetFrame(_completedFrame);
			advance_staircase?.Unlock();
			retreat_staircase?.Unlock();
			_state = SkullState.Finished;
			GD.Print($"[AngrySkull] Floor '{_floorId}' already completed. Bypassing challenge.");
			return;
		}

		advance_staircase?.Lock();
		retreat_staircase?.Lock();

		if (_phaseTimer == null)
		{
			_phaseTimer = GetNodeOrNull<Timer>("PhaseTimer");
		}
		if (_phaseTimer == null)
		{
			_phaseTimer = new Timer();
			AddChild(_phaseTimer);
		}
		_phaseTimer.OneShot = true;
		_phaseTimer.Timeout += OnPhaseTimeout;

		SetFrame(_dormantFrame);
		_skullSprite?.Stop();

		ScheduleNextRound(_phaseDuration);
	}

	public override void _ExitTree()
	{
		if (_waveManager != null && GodotObject.IsInstanceValid(_waveManager))
		{
			_waveManager.WaveProgressChanged -= OnWaveProgressChanged;
			_waveManager.AllWavesCompleted -= OnRoundCompleted;
		}
		_angryTween?.Kill();
	}

	// ---------------------------------------------------------------- flow

	private void ScheduleNextRound(float delay)
	{
		_state = SkullState.Waiting;
		_phaseTimer.Stop();
		_phaseTimer.Start(Mathf.Max(0.5f, delay));
	}

	private void OnPhaseTimeout()
	{
		if (_state == SkullState.Waiting)
		{
			StartRound();
		}
	}

	private void StartRound()
	{
		if (!EnsureWaveManager()) return;

		RoundConfig cfg = Rounds[_roundIndex];
		_state = SkullState.InRound;

		// Nobody leaves (or advances) mid-round
		retreat_staircase?.Lock();
		advance_staircase?.Lock();

		if (cfg.IsAngry)
		{
			StartAngryFlicker();
		}
		else
		{
			SetFrame(_dormantFrame);
		}

		GD.Print($"[AngrySkull] Round {_roundIndex + 1} begins (health x{cfg.HealthMultiplier}).");
		_waveManager.StartWaveSequence(cfg.Waves, cfg.HealthMultiplier);
	}

	private bool EnsureWaveManager()
	{
		if (_waveManager != null && GodotObject.IsInstanceValid(_waveManager)) return true;

		_waveManager = WaveManager.Instance;
		if (_waveManager == null)
		{
			GD.PrintErr("[AngrySkull] WaveManager instance not found in scene!");
			return false;
		}

		_waveManager.WaveProgressChanged += OnWaveProgressChanged;
		_waveManager.AllWavesCompleted += OnRoundCompleted;
		return true;
	}

	// Drives the gradual awakening (frame 1 -> 6) for rounds 1-3
	private void OnWaveProgressChanged(float progress)
	{
		if (_state != SkullState.InRound || Rounds[_roundIndex].IsAngry) return;

		int span = _fullAwakeFrame - _dormantFrame;
		SetFrame(_dormantFrame + Mathf.RoundToInt(Mathf.Clamp(progress, 0f, 1f) * span));
	}

	private void OnRoundCompleted()
	{
		if (_state != SkullState.InRound) return;

		int finished = _roundIndex;
		_angryTween?.Kill();

		GD.Print($"[AngrySkull] Round {finished + 1} complete.");

		switch (finished)
		{
			case 0:
			case 1:
				// Exit opens so the player may leave; next level stays locked.
				SetFrame(_fullAwakeFrame);
				retreat_staircase?.Unlock();
				_roundIndex++;
				ReturnToDormantSoon();
				ScheduleNextRound(_restDuration);
				break;

			case 2:
				// Full completion: mark floor, open everything.
				SetFrame(_fullAwakeFrame);
				MarkFloorComplete();
				retreat_staircase?.Unlock();
				advance_staircase?.Unlock();
				_roundIndex++;
				ReturnToDormantSoon();
				// If the player lingers, the angry round begins.
				ScheduleNextRound(_angryDelay);
				break;

			default:
				// Round 4 beaten
				_state = SkullState.Finished;
				SetFrame(_successFrame);
				retreat_staircase?.Unlock();
				advance_staircase?.Unlock();
				break;
		}
	}

	private void ReturnToDormantSoon()
	{
		// Hold the fully-awake frame for a beat, then drift back to sleep.
		GetTree().CreateTimer(1.5f).Timeout += () =>
		{
			if (GodotObject.IsInstanceValid(this) && _state == SkullState.Waiting)
			{
				SetFrame(_dormantFrame);
			}
		};
	}

	private void MarkFloorComplete()
	{
		// ASSUMPTION: adjust to whatever your player_data exposes for saving completion.
		if (player_data.Instance != null && player_data.Instance.HasMethod("CompleteFloor"))
		{
			player_data.Instance.Call("CompleteFloor", _floorId);
		}
		else
		{
			GD.PrintErr("[AngrySkull] player_data.CompleteFloor(string) not found - floor completion not saved!");
		}
	}

	// ---------------------------------------------------------------- visuals

	private void SetFrame(int frame)
	{
		if (_skullSprite != null)
		{
			_skullSprite.Frame = frame;
		}
	}

	private void StartAngryFlicker()
	{
		_angryTween?.Kill();
		SetFrame(_angryFrameA);

		_angryTween = CreateTween().SetLoops();
		_angryTween.TweenCallback(Callable.From(() => SetFrame(_angryFrameB)));
		_angryTween.TweenInterval(_flickerInterval);
		_angryTween.TweenCallback(Callable.From(() => SetFrame(_angryFrameA)));
		_angryTween.TweenInterval(_flickerInterval);
	}
}
