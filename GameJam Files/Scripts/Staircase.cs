using Godot;
using System;

public partial class Staircase : Interactable
{
	[ExportGroup("Scene Transition")]
	[Export(PropertyHint.File, "*.tscn")] 
	public string TargetScenePath { get; set; } = "";
	[Export(PropertyHint.Range, "1,5,1")] private int _doorType = 1;
	
	[ExportGroup("Door Appearance")]
	[Export] private AnimatedSprite2D _doorSprite;
	

	public override void _Ready()
	{
		base._Ready();

		// Play the matching animation based on the selected door type (e.g., "door_1", "door_2", etc.)
		if (_doorSprite != null)
		{
			string animName = $"door_{_doorType}";
			if (_doorSprite.SpriteFrames.HasAnimation(animName))
			{
				_doorSprite.Play(animName);
			}
			else
			{
				GD.PrintT($"[Staircase] Animation '{animName}' not found in SpriteFrames.");
			}
		}
	}

	protected override void Interact()
	{
		if (string.IsNullOrEmpty(TargetScenePath))
		{
			GD.PrintErr($"[Staircase] TargetScenePath is empty on '{Name}'! Please assign a scene file in the Inspector.");
			return;
		}

		if (SceneLoader.Instance != null)
		{
			SceneLoader.Instance.ChangeScene(TargetScenePath);
		}
		else
		{
			GD.PrintErr("[Staircase] SceneLoader instance could not be found.");
		}
	}
}
