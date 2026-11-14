using Godot;
using System;

public partial class Drink : Node2D
{
	public DrinkType type { get; set; }
	[Export] private Sprite2D sprite;

}
