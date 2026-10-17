using Godot;
using System;

public partial class CameraTrigger : Area2D
{
    [Export] public CameraEnding Camera;
	[Export] public Player Player;

    private bool hasTriggered = false;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (hasTriggered)
		{
			return;
		}

        // Only trigger when the player enters.
        if (!body.IsInGroup("PLAYER"))
		{
			return;
		}

        hasTriggered = true;
		Player.MovementLocked = true;

        Camera.StartCameraSequence();
    }
}

