using Godot;
using System.Collections.Generic;

public sealed class RoundDefinition
{
	public List<PackedScene[]> Waves { get; } = new();
	public float HealthMultiplier { get; set; } = 1.0f;
	public bool IsAngry { get; set; }
}
