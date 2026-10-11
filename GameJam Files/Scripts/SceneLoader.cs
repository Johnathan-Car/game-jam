using Godot;
using System;

public partial class SceneLoader : Node
{
	public static SceneLoader Instance { get; private set; }

	private CanvasLayer transitionLayer;
	private ColorRect fadeRect;

	public override void _EnterTree()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}
		Instance = this;
	}

	public override void _Ready()
	{
		// Set up overlay for smooth black fade transitions
		transitionLayer = new CanvasLayer { Layer = 100 };
		fadeRect = new ColorRect
		{
			Color = new Color(0, 0, 0, 0),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			LayoutMode = 1, // Full Rect
			AnchorsPreset = (int)Control.LayoutPreset.FullRect
		};

		transitionLayer.AddChild(fadeRect);
		AddChild(transitionLayer);
	}
	
	public void ChangeScene(string scenePath)
	{
		CallDeferred(nameof(DeferredChangeScene), scenePath);
	}

	private async void DeferredChangeScene(string scenePath)
	{
		// 1. Block player input and fade screen to black
		fadeRect.MouseFilter = Control.MouseFilterEnum.Stop;
		Tween fadeOut = CreateTween();
		fadeOut.TweenProperty(fadeRect, "color:a", 1.0f, 0.3f);
		await ToSignal(fadeOut, Tween.SignalName.Finished);

		// 2. Change scene safely in Godot SceneTree
		// (player_data Autoload node stays alive in memory above the active scene tree)
		Error error = GetTree().ChangeSceneToFile(scenePath);
		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to load target scene: {scenePath}");
		}

		// 3. Fade back in
		Tween fadeIn = CreateTween();
		fadeIn.TweenProperty(fadeRect, "color:a", 0.0f, 0.3f);
		await ToSignal(fadeIn, Tween.SignalName.Finished);

		fadeRect.MouseFilter = Control.MouseFilterEnum.Ignore;
	}
}
