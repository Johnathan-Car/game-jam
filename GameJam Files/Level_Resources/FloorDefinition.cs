using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;

/// Everything that differs between floors. One .tres per floor.
///
/// Rounds text format (one wave per line, blank line or "round" starts a new round):
///
///     round 1.0
///     zombie zombie
///     zombie skeleton
///     skeleton*2
///
///     round x1.25
///     zombie*3 skeleton
///     groblin
///
///     round x2.25 angry
///     skeleton*5
///
/// "round [xMULT] [angry]"  - starts a round. Multiplier defaults to 1.0.
/// "name name*N ..."        - a wave. Names come from the Catalog, N repeats an enemy.
/// "# text"                 - comment.
[GlobalClass]
public partial class FloorDefinition : Resource
{
	[Export] public string FloorId { get; set; } = "";
	[Export] public EnemyCatalog Catalog { get; set; }
	[Export(PropertyHint.MultilineText)] public string Rounds { get; set; } = "";

	public List<RoundDefinition> ParseRounds()
	{
		var result = new List<RoundDefinition>();

		if (Catalog == null)
		{
			GD.PrintErr($"[FloorDefinition:{FloorId}] No EnemyCatalog assigned.");
			return result;
		}

		RoundDefinition current = null;
		string[] lines = (Rounds ?? "").Split('\n');

		for (int n = 0; n < lines.Length; n++)
		{
			string line = lines[n];
			int hash = line.IndexOf('#');
			if (hash >= 0) line = line.Substring(0, hash);
			line = line.Trim();
			if (line.Length == 0) continue;

			string[] tokens = line.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);

			// Round header
			if (tokens[0].Equals("round", StringComparison.OrdinalIgnoreCase))
			{
				current = new RoundDefinition();
				for (int t = 1; t < tokens.Length; t++)
				{
					if (tokens[t].Equals("angry", StringComparison.OrdinalIgnoreCase))
					{
						current.IsAngry = true;
					}
					else if (float.TryParse(tokens[t].TrimStart('x', 'X'), NumberStyles.Float, CultureInfo.InvariantCulture, out float mult))
					{
						current.HealthMultiplier = mult;
					}
					else
					{
						GD.PrintErr($"[FloorDefinition:{FloorId}] Line {n + 1}: unrecognized round option '{tokens[t]}'.");
					}
				}
				result.Add(current);
				continue;
			}

			// Wave line. Lines before any header go into an implicit round.
			if (current == null)
			{
				current = new RoundDefinition();
				result.Add(current);
			}

			var wave = new List<PackedScene>();
			foreach (string token in tokens)
			{
				string name = token;
				int count = 1;

				int star = token.IndexOf('*');
				if (star > 0)
				{
					name = token.Substring(0, star);
					if (!int.TryParse(token.Substring(star + 1), out count) || count < 1)
					{
						GD.PrintErr($"[FloorDefinition:{FloorId}] Line {n + 1}: bad count in '{token}'.");
						continue;
					}
				}

				PackedScene scene = Catalog.Get(name);
				if (scene == null)
				{
					GD.PrintErr($"[FloorDefinition:{FloorId}] Line {n + 1}: '{name}' is not in the enemy catalog.");
					continue;
				}

				for (int c = 0; c < count; c++) wave.Add(scene);
			}

			if (wave.Count > 0) current.Waves.Add(wave.ToArray());
		}

		result.RemoveAll(r => r.Waves.Count == 0);
		return result;
	}
}
