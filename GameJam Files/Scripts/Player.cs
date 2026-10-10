using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public const float Speed = 350.0f;
	public const float JumpVelocity = -750.0f;

	// For AnimatedSprite2D Character
	private AnimatedSprite2D _sprite;

	// For determining which way the player is facing
	private bool facingLeft = false;

	// For Weapon
	private AnimatedSprite2D sword;

	// For Weapon Pivot
	private Node2D swordPivot;

	// For Registering one attack per sword swing
	//private bool hasHitEnemy = false;			Unused

	// For Attack Animations
	AnimationPlayer _animationPlayer;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		sword = GetNode<AnimatedSprite2D>("SwordPivot/Sword");
		swordPivot = GetNode<Node2D>("SwordPivot");
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

		// Now play the default animation
		_sprite.Play("default");
		//sword.Play("default");
		sword.Visible = false;

		// Connecting finished animation
		_animationPlayer.AnimationFinished += OnAnimationFinished;

	}

	// At the end of the animation hide the sword
	private void OnAnimationFinished(StringName animationName)
	{
    	if (animationName == "attack")
    	{
        	sword.Visible = false;
    	}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		
		//Sprite2D sprite = GetNode<Sprite2D>("Sprite2D"); // Getting Sprite2D
		//sprite.FlipH = direction.X < 0; // Flipping Sprite based on movement direction

		_sprite.FlipH = direction.X < 0; // Flipping Sprite based on movement direction

		if(direction.X < 0)
		{
			facingLeft = true;
		}
		else if (direction.X > 0)
		{
			facingLeft = false;
		}
		_sprite.FlipH = facingLeft;



		if (direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}

		// For Sword Attack Animation
		if(Input.IsActionJustPressed("meleeAttack"))
		{
			sword.Visible = true;
			GD.Print("Left Mouse Button Clicked");
			//GD.Print(_animationPlayer.GetAnimationList());

			// Mirror the sword along with its animation path
			swordPivot.Scale = new Vector2(facingLeft ? -1 : 1, 1);

			_animationPlayer.Play("attack");
			GD.Print("Current animation: " + _animationPlayer.CurrentAnimation);
		}


		Velocity = velocity;
		MoveAndSlide();
	}
}
