using Godot;
using System;
using System.Collections.Generic;

public partial class Table : Node
{
	private List<Chair> chairs;
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public List<Chair> GetChairs()
	{
		return chairs;
	}

	public Chair getChair(int index)
	{
		return chairs[index];
	}
}
