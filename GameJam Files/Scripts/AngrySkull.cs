using Godot;
using System;
using System.Collections.Generic;

public partial class AngrySkull : Node2D
{
	[ExportGroup("Nodes")]
	[Export] private Timer _phaseTimer;
	[Export] private AnimatedSprite2D _skullSprite;
	[Export] private Staircase advance_staircase; // progresses to the next level
	[Export] private Staircase retreat_staircase; // the "exit" the player can leave through
	[Export] private string _floorId = "Floor1";  // unique per floor; used for save data

	// Each array element is ONE WAVE: enemy names separated by spaces.
	// Leave a round's array empty to skip it. Round 4 is the optional angry round.
	// NOTE: no inline defaults on these exports. Godot's source generator mangles string[]
	// initializers into invalid code. If rounds 1-3 are ALL empty, the Floor 1 defaults
	// below are used, so an existing scene keeps working untouched.
	[ExportGroup("Rounds - one wave per entry, enemy names separated by spaces")]
	[Export] private string[] _round1;
	[Export] private string[] _round2;
	[Export] private string[] _round3;
	[Export] private string[] _round4Angry;
	[Export] private float[] _healthMultipliers; // one per round, in order (missing = 1.0)

	private static readonly string[] DefaultRound1 = { "zombie zombie", "zombie skeleton", "skeleton skeleton" };
	private static readonly string[] DefaultRound2 = { "zombie zombie skeleton", "skeleton skeleton zombie", "groblin" };
	private static readonly string[] DefaultRound3 = { "zombie zombie skeleton skeleton", "groblin skeleton", "zombie zombie zombie skeleton" };
	private static readonly string[] DefaultRound4 =
	{
		"zombie zombie skeleton skeleton skeleton",
		"skeleton skeleton skeleton zombie zombie",
		"zombie zombie zombie skeleton skeleton skeleton",
		"skeleton skeleton skeleton skeleton skeleton",
	};
	private static readonly float[] DefaultMultipliers = { 1.0f, 1.25f, 1.5f, 2.25f };

	[ExportGroup("Timing (seconds)")]
	[Export] private float _phaseDuration = 3.0f;   // dormant delay before Round 1 begins
	[Export] private float _restDuration = 8.0f;    // dormant window between normal rounds (exit is open)
	[Export] private float _angryDelay = 15.0f;     // how long the player can idle after the last normal round before the angry round
	[Export] private float _flickerInterval = 0.2f; // angry round frame flicker speed

	[ExportGroup("Skull Frames")]
	[Export] private int _completedFrame = 0;  // shown when the floor was already beaten on entry
	[Export] private int _dormantFrame = 1;    // eyes closed
	[Export] private int _fullAwakeFrame = 5;  // max awakeness for a normal round
	[Export] private int _successFrame = 5;    // white / finished
	[Export] private int _angryFrameA = 9;
	[Export] private int _angryFrameB = 10;

	private enum SkullState { Waiting, InRound, Finished }
	private SkullState _state = SkullState.Waiting;

	private sealed record RoundConfig(string[][] Waves, float HealthMultiplier, bool IsAngry = false);

	private readonly List<RoundConfig> _rounds = new();
	private int _roundIndex = 0;
	private int _lastNormalRoundIndex = -1; // the round that "completes" the floor
	private WaveManager _waveManager;
	private Tween _angryTween;

	private RoundConfig CurrentRound => _rounds[_roundIndex];

	public override void _Ready()
	{
		BuildRounds();
		if (_lastNormalRoundIndex < 0)
		{
			GD.PrintErr("[AngrySkull] No normal rounds defined (round 1-3 arrays are all empty).");
			return;
		}

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

	// ---------------------------------------------------------------- setup

	private void BuildRounds()
	{
		static bool IsEmpty(string[] a) => a == null || a.Length == 0;

		string[][] sources = { _round1, _round2, _round3, _round4Angry };
		float[] multipliers = _healthMultipliers;

		// Nothing configured in the inspector: fall back to the Floor 1 mixes.
		if (IsEmpty(_round1) && IsEmpty(_round2) && IsEmpty(_round3))
		{
			sources = new[] { DefaultRound1, DefaultRound2, DefaultRound3, DefaultRound4 };
			multipliers = DefaultMultipliers;
		}

		for (int i = 0; i < sources.Length; i++)
		{
			string[] waveLines = sources[i];
			if (waveLines == null || waveLines.Length == 0) continue;

			var waves = new List<string[]>();
			foreach (string line in waveLines)
			{
				string[] enemies = (line ?? "").Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
				if (enemies.Length > 0) waves.Add(enemies);
			}
			if (waves.Count == 0) continue;

			float mult = (multipliers != null && i < multipliers.Length) ? multipliers[i] : 1.0f;
			bool angry = i == 3;

			_rounds.Add(new RoundConfig(waves.ToArray(), mult, angry));
			if (!angry) _lastNormalRoundIndex = _rounds.Count - 1;
		}
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

		RoundConfig cfg = CurrentRound;
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

	// Drives the gradual awakening (dormant -> full awake) for normal rounds
	private void OnWaveProgressChanged(float progress)
	{
		if (_state != SkullState.InRound || CurrentRound.IsAngry) return;

		int span = _fullAwakeFrame - _dormantFrame;
		SetFrame(_dormantFrame + Mathf.RoundToInt(Mathf.Clamp(progress, 0f, 1f) * span));
	}

	private void OnRoundCompleted()
	{
		if (_state != SkullState.InRound) return;

		int finished = _roundIndex;
		bool wasAngry = _rounds[finished].IsAngry;
		_angryTween?.Kill();

		GD.Print($"[AngrySkull] Round {finished + 1} complete.");

		if (wasAngry)
		{
			// Bonus round beaten
			_state = SkullState.Finished;
			SetFrame(_successFrame);
			retreat_staircase?.Unlock();
			advance_staircase?.Unlock();
			return;
		}

		SetFrame(_fullAwakeFrame);
		retreat_staircase?.Unlock();

		bool completesFloor = finished == _lastNormalRoundIndex;
		if (completesFloor)
		{
			MarkFloorComplete();
			advance_staircase?.Unlock();
		}

		_roundIndex++;

		if (_roundIndex >= _rounds.Count)
		{
			// Nothing left to run (no angry round defined).
			_state = SkullState.Finished;
			return;
		}

		ReturnToDormantSoon();

		// After the floor is complete, the next round is the optional angry one:
		// if the player lingers, it begins.
		ScheduleNextRound(completesFloor ? _angryDelay : _restDuration);
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
		if (player_data.Instance != null)
		{
			player_data.Instance.MarkFloorCompleted(_floorId);
		}
		else
		{
			GD.PrintErr("[AngrySkull] player_data.Instance is null - floor completion not saved!");
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
