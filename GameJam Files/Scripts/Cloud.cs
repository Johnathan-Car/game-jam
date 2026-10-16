using Godot;
using System;

public partial class Cloud : Sprite2D
{
    private Vector2 startpos;
    [Export] private float finalX = 1250;
    private Vector2 finalpos;
    [Export] private float speed = 500f;

    private float progress = 0;

    public override void _Ready()
    {
        startpos = GlobalPosition;
        finalpos = new Vector2(finalX, startpos.Y);
        progress = GD.Randf();

        GlobalPosition = startpos.Lerp(finalpos, progress);
    }

    

    
    public override void _PhysicsProcess(double delta)
    {
        float distance = startpos.DistanceTo(finalpos);
        progress += (float)delta * speed / distance;

        if (progress >= 1)
        {
            progress = 0;
            GlobalPosition = startpos;
            return;
        }

        GlobalPosition = startpos.Lerp(finalpos, progress);
    }
}



