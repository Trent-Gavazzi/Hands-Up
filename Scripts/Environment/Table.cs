using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Table : Node
{
	private List<Chair> chairs;
	
	public override void _Ready()
	{
        chairs = GetChildren().OfType<Chair>().ToList();
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
