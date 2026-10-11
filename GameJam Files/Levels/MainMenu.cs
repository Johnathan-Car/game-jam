using Godot;
using System;

public partial class MainMenu : Control
{
	private Button _startButton;

	public override void _Ready()
	{
		_startButton = GetNode<Button>("StartButton");
		
		_startButton.Pressed += OnStartButtonPressed;
	}

	private void OnStartButtonPressed()
	{
		GD.Print("BeginGame");
		string hubScenePath = "res://GameJam Files/Levels/main_dungeon.tscn"; 
		Error err = GetTree().ChangeSceneToFile(hubScenePath);
	}
}
