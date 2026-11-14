using Godot;
using System;

public partial class Chair : Node2D
{
    // One StaticBody2D: blocks the player and is cut out of the navmesh when baking
    [Export] private CollisionObject2D collider;
    [Export] private Table parentTable;
    private bool isTaken;

    public bool isOccupied()
    {
        return isTaken;
    }

    public void setOccupied(bool occupied)
    {
        isTaken = occupied;
    }

    public CollisionObject2D getCollider()
    {
        return collider;
    }

    public Table getParentTable()
    {
        return parentTable;
    }
}