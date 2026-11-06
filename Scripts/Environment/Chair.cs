using Godot;
using System;
using System.Threading.Tasks;

public partial class Chair : Node2D
{
	[Export] private CollisionObject2D collider;
	[Export] private Area2D area;
	[Export] private Path2D pathToDoor;
	private bool isTaken;
	private bool isPathReady = false;

	public override async void _Ready()
	{
		// The navigation map is empty until the first physics frame is run
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		buildPathToDoor();
	}

	public override void _Process(double delta)
	{
	}

	public void buildPathToDoor()
	{
		Node2D door = GetTree().GetFirstNodeInGroup("door") as Node2D;
		if(door == null)
		{
			GD.PushError($"{Name}: no node in the \"door\" group");
			return;
		}

        Rid map = GetWorld2D().NavigationMap;
        Vector2[] points = NavigationServer2D.MapGetPath(map, GlobalPosition, door.GlobalPosition, true);

        Curve2D curve = new Curve2D();
        foreach (Vector2 point in points)
        {
            // Nav points are in world space, the curve is local to the Path2D
            curve.AddPoint(pathToDoor.ToLocal(point));
        }
        pathToDoor.Curve = curve;

        isPathReady = points.Length > 1;
        if (!isPathReady)
        {
            GD.PushWarning($"{Name}: no navigation path to the door was found.");
        }
    }
	public bool isOccupied()
	{
		return isTaken;
	}

	public Path2D getPath()
	{
		return pathToDoor;
	}

	public Area2D getArea()
	{
		return area;
	}

	public void setOccupied(bool occupied)
	{
		isTaken = occupied;
	}
	
	public bool hasPathToDoor()
	{
        return isPathReady;
    }
}
