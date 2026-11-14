using Godot;
using System;

public partial class Wanted : Customer
{
    private float aggression = 0;
    [Export] private Player player;
    [Export] private Area2D aggressionZone;

    // TODO: ADD SETUP FOR ATTRIBUTES THAT LINK TO THE WANTED POSTER
    public void setPlayer(Player p)
    {
        player = p;
    }

    public override void _Ready()
    {
        isWanted = true;
        base._Ready();
    }

    public override void _Process(double delta)
    {
        if (player != null)
        {
            // Check if player enters the aggression zone
            foreach (Area2D hitbox in aggressionZone.GetOverlappingAreas())
            {
                if (player.IsAncestorOf(hitbox))
                {

                }
            }
        }
        base._Process(delta);
    }
}