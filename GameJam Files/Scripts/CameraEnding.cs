using Godot;
using System;
using System.Threading.Tasks;

public partial class CameraEnding : Camera2D
{
	[Export] public Vector2 FinalCameraPosition = new Vector2(600, 200);
	[Export] public float PauseDuration = 1.0f;
	[Export] public float ScrollDuration = 2.0f;

	private bool cameraSequenceStarted = false;

	public void StartCameraSequence()
	{
		if (cameraSequenceStarted)
		{
			return;	
		}

		cameraSequenceStarted = true;
		_ = RunCameraSequence();
	}

	private async Task RunCameraSequence()
	{
		// Pause the camera at its current position.
		await ToSignal(GetTree().CreateTimer(PauseDuration), SceneTreeTimer.SignalName.Timeout);

		// Scroll upward to the destination.
		Tween tween = CreateTween();

		tween.TweenProperty(this, "global_position", FinalCameraPosition, ScrollDuration).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);

		await ToSignal(tween, Tween.SignalName.Finished);

		// Ensure the camera stays at its destination.
		GlobalPosition = FinalCameraPosition;
	}
}
