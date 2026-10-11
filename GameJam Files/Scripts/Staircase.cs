using Godot;
using System;

public partial class Staircase : Interactable
{
	[ExportGroup("Scene Transition")]
	[Export(PropertyHint.File, "*.tscn")] 
	public string TargetScenePath { get; set; } = "";
	[Export] private string _doorType = "1";
	
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
	protected override void OnBodyEntered(Node2D body)
	{
		if (_doorType == "locked" || _doorType == "jailed") return;
		base.OnBodyEntered(body);
	}

	protected override void Interact()
	{
		if (string.IsNullOrEmpty(TargetScenePath))
		{
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
	
	public void Unlock(){
		if(_doorType == "locked"){
			_doorType = "opening";
			string animName = $"door_{_doorType}";
			_doorSprite.Play(animName);
		}
		else if(_doorType == "jailed"){
			_doorType = "freed";
			string animName = $"door_{_doorType}";
			_doorSprite.Play(animName);
		}
	}
	
	public void Lock(){
		if(_doorType != "locked"){
			_doorType = "jailed";
			string animName = $"door_{_doorType}";
			_doorSprite.Play(animName);
		}
	}
}
