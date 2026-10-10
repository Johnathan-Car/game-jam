using Godot;

public partial class SlotMachine : Node2D
{
	[Export] private Label _promptLabel;
	[Export] private AnimatedSprite2D _animatedSprite;

	private bool _isPlayerNearby = false;

	public override void _Ready()
	{
		if (_promptLabel != null)
		{
			_promptLabel.Visible = false;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_isPlayerNearby && @event.IsActionPressed("interact"))
		{
			TriggerInteraction();
		}
	}

	private void TriggerInteraction()
	{
		if (_promptLabel != null)
		{
			_promptLabel.Visible = false;
		}

		GD.Print("Interacting with Slot Machine!");
		
		if (_animatedSprite != null)
		{
			_animatedSprite.Play("spin");
		}
	}

	private void OnInteractionAreaBodyEntered(Node2D body)
	{
		if (body.IsInGroup("Player"))
		{
			_isPlayerNearby = true;
			if (_promptLabel != null)
			{
				_promptLabel.Visible = true;
			}
		}
	}

	private void OnInteractionAreaBodyExited(Node2D body)
	{
		if (body.IsInGroup("Player"))
		{
			_isPlayerNearby = false;
			if (_promptLabel != null)
			{
				_promptLabel.Visible = false;
			}
		}
	}
}
