using Godot;
using System;

public partial class SkeletonHurtbox : Area2D
{
	private int skeletonHealth = 10;
	
	// Death flag to make sure it only activates once
	private bool isDead = false;

	// For Knockback
	[Export]
	public float KnockbackForce = 200.0f;

	[Export]
	public int Damage = 2;

	[Export]
	public Node2D Player;
	public override void _Ready()
	{
		AreaEntered += OnAreaEntered;
		BodyEntered += OnBodyEntered;
	}


	private bool canTakeDamage = true;
	[Export] 
	public float DamageCooldown = 0.5f;

	private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("PLAYER"))
            return;

        if (body is Player player)
        {
            player.TakeDamage(Damage);
        }
    }
	
	public void OnAreaEntered(Area2D area)
	{
		// Goal is to check whether the area is the player's sword hitbox
		if (!area.IsInGroup("PLAYERATTACK") || isDead || area.Monitoring == false)
		{
			return;
		}
			
		// Now push the enemy away from the player
		Node2D enemy = GetParent<Node2D>();
		
		// Confirming whether or not the parent is an Skeleton
		if(enemy is not Skeleton enemyScript)
		{
			GD.Print("Hurtbox parent is not a Skeleton");
			return;
		}

		GD.Print("Enemy was hit!");
		skeletonHealth--;

		GD.Print("Enemy health:" + skeletonHealth);


		// In case you forget to put Player in the Inspector, check if player is assigned
		if (Player == null)
		{
			GD.PrintErr("Player has not been assigned to the SkeletonHurtbox.");
			return;
		}

		// Calculate knockback direction
		Vector2 direction = (enemy.GlobalPosition - Player.GlobalPosition).Normalized();

		// Apply the knockback now. Then play the hurt animation
		enemyScript.ApplyKnockback(direction, KnockbackForce);
		enemyScript.Hurt();
		
		// Checking if the enemy has died
		if(skeletonHealth <= 0)
		{
			// Enemy is dead
			isDead = true;
		
			// Spawn dropped item
			enemyScript.CallDeferred(nameof(Skeleton.SpawnDroppedItem));

			// Remove the enemy
			enemy.QueueFree();
		}
	}

	// Temp
	private async void TakeDamage()
	{
		canTakeDamage = false;

		skeletonHealth--;
		GD.Print("Enemy health: " + skeletonHealth);

		if (Player != null)
		{
			Node2D enemy = GetParent<Node2D>();

			if (enemy is Skeleton enemyScript)
			{
				Vector2 direction =
					(enemy.GlobalPosition - Player.GlobalPosition).Normalized();

				enemyScript.ApplyKnockback(direction, KnockbackForce);
				enemyScript.Hurt();

				if (skeletonHealth <= 0)
				{
					isDead = true;
					enemy.CallDeferred(nameof(Skeleton.SpawnDroppedItem));
					enemy.QueueFree();
					return;
				}
			}
		}

		await ToSignal(
			GetTree().CreateTimer(DamageCooldown),
			SceneTreeTimer.SignalName.Timeout
		);

		canTakeDamage = true;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (isDead)
			return;

		foreach (Area2D area in GetOverlappingAreas())
		{
			if (!area.IsInGroup("PLAYERATTACK"))
				continue;

			if (!area.Monitoring)
				continue;

			// The player's attack hitbox is overlapping the enemy.
			GD.Print("Player attack is touching the enemy!");
			TakeDamage();
	   		break;
		}
	}
}
