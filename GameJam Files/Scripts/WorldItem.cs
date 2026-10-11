using Godot;

public partial class WorldItem : RigidBody2D
{
	[Export] private Sprite2D _itemSprite;
	[Export] private Area2D _pickupArea;

	[Export] private PackedScene itemPopupScene;
	
	// Launch parameters
	[Export] private float launchForceX = -180.0f; // Negative X propels left!
	[Export] private float launchForceY = -120.0f; // Negative Y gives an upward arc

	private ItemData _itemData;

	public override void _Ready()
	{
		// Hook up the pickup detection on the child Area2D
		if (_pickupArea != null)
		{
			_pickupArea.BodyEntered += OnBodyEntered;
		}
		ApplyImpulse(new Vector2(launchForceX, launchForceY));
	}

	public void Initialize(ItemData data)
	{
		_itemData = data;

		if (_itemSprite != null && _itemData != null)
		{
			_itemSprite.Texture = _itemData.Icon;
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player player)
		{
			GD.Print($"[WorldItem] Picked up {_itemData?.BaseName}!");
			if (itemPopupScene != null)
			{
				var popupInstance = itemPopupScene.Instantiate<ItemPopup>();
				GetTree().CurrentScene.AddChild(popupInstance);
				
				// Position it right above the player's head
				popupInstance.GlobalPosition = player.GlobalPosition + new Vector2(-40, -40);
				popupInstance.Initialize(_itemData.BaseName);
			}
			QueueFree();
		}
	}
}
