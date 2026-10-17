using Godot;
using System;

/// Maps short names "zombie" to enemy scenes.
[GlobalClass]
public partial class EnemyCatalog : Resource
{
	[Export] public Godot.Collections.Dictionary<string, PackedScene> Enemies { get; set; } = new();

	/// Case-insensitive lookup. Returns null if the name isn't in the catalog.
	public PackedScene Get(string name)
	{
		foreach (var pair in Enemies)
		{
			if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
			{
				return pair.Value;
			}
		}
		return null;
	}
}
