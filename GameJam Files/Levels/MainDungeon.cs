using Godot;

public partial class MainDungeon : Node
{
	[Export] private Marker2D wakeupSpawnPoint;
	[Export] private Marker2D doorSpawnPoint;
	[Export] private Player playerInstance; 

	public override void _Ready()
	{
		ConfigureBasementScenario();
	}

	private void ConfigureBasementScenario()
	{
		var data = player_data.Instance;

		if (data != null && !data.HasWokenUp)
		{
			if (wakeupSpawnPoint != null && playerInstance != null)
			{
				playerInstance.GlobalPosition = wakeupSpawnPoint.GlobalPosition;
			}
			
			playerInstance.PlaySlumpedAnimation();
			data.HasWokenUp = true;
		}
		else
		{
			if (doorSpawnPoint != null && playerInstance != null)
			{
				playerInstance.GlobalPosition = doorSpawnPoint.GlobalPosition;
			}
		}
	}
}
