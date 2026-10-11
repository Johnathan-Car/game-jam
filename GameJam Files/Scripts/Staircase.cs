using Godot;
using System;

public partial class Staircase : Interactable
{
	[ExportGroup("Scene Transition")]
	[Export(PropertyHint.File, "*.tscn")] 
	public string TargetScenePath { get; set; } = "";

	protected override void Interact()
	{
		if (string.IsNullOrEmpty(TargetScenePath))
		{
			GD.PrintErr($"[Staircase] TargetScenePath is empty on '{Name}'! Please assign a scene file in the Inspector.");
			return;
		}

		GD.Print($"[Staircase] Loading target scene: {TargetScenePath}");

		// Use your SceneLoader singleton to transition safely with a fade effect
		if (SceneLoader.Instance != null)
		{
			SceneLoader.Instance.ChangeScene(TargetScenePath);
		}
		else
		{
			GD.PrintErr("[Staircase] SceneLoader instance could not be found. Make sure it is added to Autoloads.");
		}
	}
}
