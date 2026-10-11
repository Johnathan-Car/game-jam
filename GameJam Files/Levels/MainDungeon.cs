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
			
			// Trigger slumped animation & eerie music
			playerInstance.PlaySlumpedAnimation();
			//AudioManager.PlayMusic("eerie_ambient");

			// Mark as awakened so next time they use the door spawn
			data.HasWokenUp = true;
		}
		else
		{
			// --- SCENARIO B: RETURNING FROM ARENA ---
			if (doorSpawnPoint != null && playerInstance != null)
			{
				playerInstance.GlobalPosition = doorSpawnPoint.GlobalPosition;
			}

			// Play normal hub music
			// AudioManager.PlayMusic("hub_theme");
		}
	}
}
