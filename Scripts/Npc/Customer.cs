using Godot;
using System;

public partial class Customer : Node2D
{
	[Export] private Area2D hitbox;
	[Export] private NavigationAgent2D navAgent;
	[Export] private Chair targetChair;
	[Export] private float walkSpeed = 100f;
	protected bool isWanted = false;
	private bool isSeated = false;
	private bool isLeaving = false;

	// How far ahead the game looks for a collision
    [Export] private float lookAhead = 24f;

	// How close to the chair we get
	[Export] private float arriveDistance = 4f;


	private Path2D path;
	private float pathDistance;
	private bool hasReachedDoor;
    public override void _Ready()
    {
        // The agent is only used for avoidance; the route comes from the chair's path
        navAgent.AvoidanceEnabled = true;
        navAgent.MaxSpeed = walkSpeed;
        navAgent.VelocityComputed += onVelocityComputed;

        if (targetChair.isOccupied())
        {
            // Write code for selecting a new chair if current is occupied
        }
        else
        {
            targetChair.setOccupied(true);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (hasReachedDoor)
        {
            return;
        }

        // The chair builds its path after the first physics frame, so wait until it exists
        if (path == null)
        {
            if (!targetChair.hasPathToDoor())
            {
                return;
            }
            path = targetChair.getPath();
        }

        if (isLeaving)
        {
            moveAlongPath(delta, true);
        }
        else if (!isSeated)
        {
            moveAlongPath(delta, false);
            isSeated = sitInChair();
            if (isSeated)
            {
                navAgent.Velocity = Vector2.Zero;
            }
        }
    }
    public bool sitInChair()
	{
		if (hitbox.OverlapsArea(targetChair.getArea()) && !targetChair.isOccupied())
		{
			targetChair.setOccupied(true);

            // TODO: ANIMATION FOR SITTING GOES HERE

            return true;
		}
		return false;
	}

	public void leaveChair()
	{
		// TODO: Animation for getting our of a chair
		// Customer needs to leave after some amount of time
		targetChair.setOccupied(false);
		isSeated = false;
		isLeaving = true;
	}
    
    // Works out where the customer wants to go and passes to nav agent
	private void moveAlongPath(double delta, bool towardDoor)
	{
        Curve2D curve = path.Curve;
		float pathLength = path.Curve.GetBakedLength();

        pathDistance = curve.GetClosestOffset(path.ToLocal(GlobalPosition));

        Vector2 endPoint = path.ToGlobal(curve.SampleBaked(towardDoor ? pathLength : 0f, true));
        if(GlobalPosition.DistanceTo(endPoint) <= arriveDistance)
        {
            navAgent.Velocity = Vector2.Zero;
            if (towardDoor)
            {
                hasReachedDoor = true;
                navAgent.AvoidanceEnabled = false; // stops blocking doorway
            }
            return;
        }

        float targetDistance;
        if (towardDoor)
		{
			targetDistance = Mathf.Min(pathDistance + lookAhead, pathLength);
		}
		else
		{
            targetDistance = Mathf.Min(pathDistance - lookAhead, 0f);
        }
        Vector2 target = path.ToGlobal(curve.SampleBaked(targetDistance, true));
        Vector2 toTarget = target - GlobalPosition;

        // Slow down near the end so we don't overshoot it in one frame
        float speed = Mathf.Min(walkSpeed, toTarget.Length() / (float)delta);
        navAgent.Velocity = toTarget.Normalized() * speed;
    }

    private void onVelocityComputed(Vector2 safeVelocity)
    {
        // Seated customers stay put, but still act as obstacles for everyone else
        if (hasReachedDoor || isSeated)
        {
            return;
        }

        GlobalPosition += safeVelocity * (float)GetPhysicsProcessDeltaTime();
    }
}
