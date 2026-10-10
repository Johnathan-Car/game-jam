using Godot;
using System;

public partial class EnemySpawner : Node2D
{
	// This is the enemy scene that will be spawned.
	[Export]
	public PackedScene EnemyScene;
	
	// The Node that will hold the spawned enemies.
	[Export]
	public Node2D EnemyHandler;

	// This is so that the Enemies can target the player
	[Export]
	public Node2D Player;

	// How long between each enemy spawned
	[Export]
	public float SpawnInterval = 2.0f;

	private Timer _timer;

	public override void _Ready()
	{
		// Find the Timer child if the spawner
		_timer = GetNode<Timer>("Timer");

		// How often the timer triggers
		_timer.WaitTime = SpawnInterval;

		// Have SpawnEnemy call whenever the timer reaches zero.
		_timer.Timeout += SpawnEnemy;

		// Start the timer.
		_timer.Start();

	}

	private void SpawnEnemy()
	{
		// A check just in case
		if (EnemyScene == null || EnemyHandler == null)
		{
			return;
		}

		// Creating a new instance of the enemy scene.
		Node2D enemy = EnemyScene.Instantiate<Node2D>();

		// Assigning the player as each of the unique enemies target
		if(enemy is Enemy1 e1)
		{
			e1.Target = Player;

			// Also assign the player to Enemy1's hurtbox as well
			Enemy1Hurtbox hurtbox = e1.GetNode<Enemy1Hurtbox>("Hurtbox");

			hurtbox.Player = Player;
		}
		else if(enemy is Enemy2 e2)
		{
			e2.Target = Player;
		}
		else if(enemy is Enemy3 e3)
		{
			e3.Target = Player;
		}

		// Add the enemy to the scene tree.
		EnemyHandler.AddChild(enemy);

		// Placing the enemy at the spawner's global position.
		enemy.GlobalPosition = GlobalPosition;
	}


	public void timerTimeout()
	{
		//var ene = enemy.instantiate()
		//ene.position = position
		//get_parent().get_node("EnemyHandler").add_child(ene)
	}
}
