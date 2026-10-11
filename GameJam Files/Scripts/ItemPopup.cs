using Godot;

public partial class ItemPopup : Control
{
	[Export] private Label _label;

	public async void Initialize(string itemName)
	{
		if (_label != null)
		{
			_label.Text = $"+ Obtained {itemName}!";
		}

		// Float upward and fade out over 1.2 seconds
		float duration = 1.2f;
		float elapsed = 0f;
		Vector2 startPos = Position;

		while (elapsed < duration)
		{
			float delta = (float)GetProcessDeltaTime();
			elapsed += delta;

			// Drift upward
			Position += new Vector2(0, -25f * delta);
			
			// Fade out alpha
			Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, 1.0f - (elapsed / duration));

			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		QueueFree();
	}
}
