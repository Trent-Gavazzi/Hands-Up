using Godot;
using System;
using System.Runtime.InteropServices;

public partial class Wanted : Customer
{
	private float aggression = 0;
	[Export] private Player player;
	public override void _Ready()
	{
		isWanted = true;
		base._Ready();
	}
	public override void _Process(double delta)
	{
		
	}
}
