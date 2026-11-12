using Godot;
using System;
using System.Runtime.InteropServices;

public partial class Wanted : Customer
{
	private float aggression = 0;
	[Export] private Player player;
	[Export] private Area2D aggressionZone;
	


	public override void _Ready()
	{
		isWanted = true;
		base._Ready();
		
	}
	public override void _Process(double delta)
	{
		// Check if player enters the aggression zone
		foreach(Area2D hb in aggressionZone.GetOverlappingAreas())
		{
			if(player.IsAncestorOf(hb))
			{

			}
		}
		base._Process(delta);
	}
}
