using Godot;
 
/// One wave: every enemy scene listed here spawns together.
[GlobalClass]
public partial class WaveDefinition : Resource
{
	[Export] public PackedScene[] Enemies { get; set; } = System.Array.Empty<PackedScene>();
}
 
