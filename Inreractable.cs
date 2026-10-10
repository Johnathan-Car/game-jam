using Godot;

public abstract partial class Interactable : Node2D
{
	[Export] private Sprite2D _promptSprite; 
	[Export] private CanvasItem _highlightTarget;

	protected bool IsPlayerNearby = false;
	protected Player CurrentPlayer = null;

	private Vector2 _initialSpritePosition;
	private float _timePassed = 0f;

	public override void _Ready()
	{
		Area2D area = GetNodeOrNull<Area2D>("Area2D");
		if (area != null)
		{
			area.BodyEntered += OnBodyEntered;
			area.BodyExited += OnBodyExited;
		}

		if (_promptSprite != null)
		{
			_promptSprite.Visible = false;
			_initialSpritePosition = _promptSprite.Position;
		}

		if (_highlightTarget != null)
		{
			_highlightTarget.Modulate = Colors.White;
		}
	}

	public override void _Process(double delta)
	{
		// Make the button icon gently bob up and down if visible
		if (_promptSprite != null && _promptSprite.Visible)
		{
			_timePassed += (float)delta * 5f;
			float bobOffset = Mathf.Sin(_timePassed) * 2f;
			_promptSprite.Position = new Vector2(_initialSpritePosition.X, _initialSpritePosition.Y + bobOffset);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (IsPlayerNearby && @event.IsActionPressed("interact"))
		{
			Interact();
		}
	}

	protected abstract void Interact();

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player player)
		{
			CurrentPlayer = player;
			IsPlayerNearby = true;
			
			if (_promptSprite != null) _promptSprite.Visible = true;
			if (_highlightTarget != null) _highlightTarget.Modulate = new Color(1.3f, 1.3f, 1.3f);
		}
	}

	private void OnBodyExited(Node2D body)
	{
		if (body == CurrentPlayer)
		{
			CurrentPlayer = null;
			IsPlayerNearby = false;
			
			if (_promptSprite != null) _promptSprite.Visible = false;
			if (_highlightTarget != null) _highlightTarget.Modulate = Colors.White;
		}
	}
}
