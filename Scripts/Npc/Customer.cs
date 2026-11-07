using Godot;
using System;

public partial class Customer : Node2D
{
    // Used both to detect the chair and to get hit by bullets
    [Export] private Area2D hitbox;
    [Export] private Sprite2D sprite;
    [Export] private NavigationAgent2D navAgent;
    [Export] private Chair targetChair;
    [Export] private float walkSpeed = 100f;
    protected bool isWanted = false;
    private bool isSeated = false;
    private bool isLeaving = false;

    private const int maxHealth = 10;
    private int health;
    private bool isDead = false;

    [Signal] public delegate void HealthChangedEventHandler(int current, int max);
    [Signal] public delegate void DiedEventHandler();

    // How far ahead along the path the customer aims (higher = smoother corners, more corner-cutting)
    [Export] private float lookAhead = 24f;

    // How close to the end of the path (chair or door) counts as arrived
    [Export] private float arriveDistance = 4f;

    private Path2D path;
    private float pathDistance;
    private bool hasReachedDoor;

    public override void _Ready()
    {
        health = maxHealth;
        hitbox.SetCollisionMaskValue(5, true); // also listen for the bullet layer
        hitbox.AreaEntered += onHitboxAreaEntered;

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
        if (hasReachedDoor || isDead)
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
        // Chair was already reserved in _Ready, so only check that we've arrived
        if (hitbox.OverlapsArea(targetChair.getArea()))
        {
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

    // ---------- Health ----------

    public void takeDamage(int amount)
    {
        if (health <= 0) return;

        health = Mathf.Max(health - amount, 0);
        EmitSignal(SignalName.HealthChanged, health, maxHealth);
        GD.Print($"NPC took damage now health is {health}");
        if (health == 0)
        {
            die();
        }
    }

    public void heal(int amount)
    {
        if (health <= 0) return; // no healing the dead

        health = Mathf.Min(health + amount, maxHealth);
        EmitSignal(SignalName.HealthChanged, health, maxHealth);
    }

    public int getHealth()
    {
        return health;
    }

    private void die()
    {
        isDead = true;

        // Free the chair unless we'd already given it up by leaving
        if (!isLeaving)
        {
            targetChair.setOccupied(false);
        }

        // Stop moving and stop other customers steering around the body
        navAgent.Velocity = Vector2.Zero;
        navAgent.AvoidanceEnabled = false;

        // TODO: Death animation, then QueueFree()
        EmitSignal(SignalName.Died);
        QueueFree();
    }

    private void onHitboxAreaEntered(Area2D area)
    {
        if (area.GetParent() is Bullet bullet && bullet.getShooter() != this && bullet.tryHit())
        {
            takeDamage(bullet.getDamage());
        }
    }

    // ---------- Movement ----------

    // Works out where the customer wants to go and passes to nav agent
    private void moveAlongPath(double delta, bool towardDoor)
    {
        Curve2D curve = path.Curve;
        float pathLength = curve.GetBakedLength();

        pathDistance = curve.GetClosestOffset(path.ToLocal(GlobalPosition));

        Vector2 endPoint = path.ToGlobal(curve.SampleBaked(towardDoor ? pathLength : 0f, true));
        if (GlobalPosition.DistanceTo(endPoint) <= arriveDistance)
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
            targetDistance = Mathf.Max(pathDistance - lookAhead, 0f);
        }
        Vector2 target = path.ToGlobal(curve.SampleBaked(targetDistance, true));
        Vector2 toTarget = target - GlobalPosition;

        // Slow down near the end so we don't overshoot it in one frame
        float speed = Mathf.Min(walkSpeed, toTarget.Length() / (float)delta);
        navAgent.Velocity = toTarget.Normalized() * speed;
    }

    private void onVelocityComputed(Vector2 safeVelocity)
    {
        // Seated and dead customers stay put
        if (hasReachedDoor || isSeated || isDead)
        {
            return;
        }

        GlobalPosition += safeVelocity * (float)GetPhysicsProcessDeltaTime();
    }
}