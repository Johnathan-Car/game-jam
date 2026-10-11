using Godot;
using System;

public partial class AngrySkull : Node2D
{
	[Export] private Timer _phaseTimer;
	[Export] private float _phaseDuration = 3.0f;
	[Export] private float _enragedWaveInterval = 5.0f;
	[Export] private AnimatedSprite2D _skullSprite;
	[Export] private Staircase advance_staircase; 
	[Export] private Staircase retreat_staircase; 
	[Export] private string _floorId; 

	private enum SkullState { Asleep, EnragedCycle, Unlocked }
	private SkullState _currentState = SkullState.Asleep;

	public override void _Ready()
	{
		if (player_data.Instance != null && player_data.Instance.IsFloorCompleted(_floorId))
		{	
			if (_skullSprite != null)
			{
				_skullSprite.Frame = 0;
				_skullSprite.Stop();
			}

			if (advance_staircase != null && retreat_staircase != null)
			{
				advance_staircase.Unlock();
				retreat_staircase.Unlock();
			}

			GD.Print($"[AngrySkull] Floor '{_floorId}' already completed. Bypassing challenge.");
			return;
		}
		
		retreat_staircase.Lock();
		if (_phaseTimer == null)
		{
			_phaseTimer = GetNodeOrNull<Timer>("PhaseTimer");
		}

		if (_phaseTimer != null)
		{
			_phaseDuration = Mathf.Max(1.0f, _phaseDuration);
			_enragedWaveInterval = Mathf.Max(1.0f, _enragedWaveInterval);

			_phaseTimer.WaitTime = _phaseDuration;
			_phaseTimer.Timeout += OnPhaseTimeout;
			_phaseTimer.Start();
		}

		if (_skullSprite != null)
		{
			_skullSprite.Frame = 0; // Start dark/asleep
			_skullSprite.Stop();
		}
	}

	private void OnPhaseTimeout()
	{
		if (_currentState == SkullState.Asleep)
		{
			
			_currentState = SkullState.EnragedCycle;
			_phaseTimer.Stop(); 

			if (_skullSprite != null)
			{
				_skullSprite.Frame = 8; // Set to red/enraged frame (adjust index to your sprite sheet)
			}

			GD.Print("[AngrySkull] Skull awakened! Starting clear-the-room wave sequence.");

			// Define your arena wave progression config
			string[][] arenaWaves = new string[][]
			{
				new string[] { "zombie", "zombie" },                          // Wave 1
				new string[] { "zombie", "skeleton" },                        // Wave 2
				new string[] { "skeleton", "skeleton"}             // Wave 3 (Final Wave)
			};

			// Subscribe to WaveManager completion signal
			if (WaveManager.Instance != null)
			{
				WaveManager.Instance.AllWavesCompleted += OnAllWavesCompleted;
				WaveManager.Instance.StartWaveSequence(arenaWaves);
			}
			else
			{
				GD.PrintErr("[AngrySkull] WaveManager instance not found in scene!");
			}
		}
	}

	private void OnAllWavesCompleted()
	{
		_currentState = SkullState.Unlocked;

		// Turn skull white / success state
		if (_skullSprite != null)
		{
			_skullSprite.Frame = 7; 
		}

		// Unlock the staircase
		if (advance_staircase != null)
		{
			advance_staircase.Unlock();
			retreat_staircase.Unlock();
		}

		GD.Print("[AngrySkull] All waves defeated! Staircase unlocked.");
	}
}
