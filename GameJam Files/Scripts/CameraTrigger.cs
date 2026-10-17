using Godot;
using System;

public partial class CameraTrigger : Area2D
{
    [Export] public CameraController Camera;

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
        Camera.StartCameraSequence();
    }
}

