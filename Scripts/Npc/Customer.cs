using Godot;
using System;

public partial class Customer : CharacterBody2D
{
    [Export] private Area2D hitbox; // Gets hit by bullets
    [Export] private NavigationAgent2D navAgent;
    [Export] private Chair targetChair;
    [Export] private float walkSpeed = 50f;
    protected bool isWanted = false;
    private bool isSeated = false;
    private bool isLeaving = false;

    private const int maxHealth = 100;
    private int health;
    private bool isDead = false;

    [Signal] public delegate void HealthChangedEventHandler(int current, int max);
    [Signal] public delegate void DiedEventHandler();
    [Signal] public delegate void ExitEventHandler(bool wasWanted, Customer customerInstance);

    private Node2D door;
    private bool isNavReady = false;
    private bool hasReachedDoor = false;

    // ---------- Drinks & satisfaction ----------

    [Signal] public delegate void DrinkRequestedEventHandler(DrinkType drink);
    [Signal] public delegate void DrinkRequestClearedEventHandler();
    [Signal] public delegate void SatisfactionChangedEventHandler(float current, float max);
    [Signal] public delegate void SatisfactionFilledEventHandler();

    [Export] private float minReqDelay = 15f;
    [Export] private float maxReqDelay = 45f;
    [Export] private float gracePeriod = 5f;
    [Export] private float stayTime = 120f;
    [Export] private float drainRate = 6f;             // satisfaction lost per second while draining
    [Export] private float satisfactionPerDrink = 25f;
    [Export] private float maxSatisfaction = 100f;
    [Export] private float startingSatisfaction = 40f;

    private float satisfaction;
    private bool drinkRequest = false;
    private DrinkType drinkTypeReq;
    private bool isDraining = false;

    private Timer requestTimer;
    private Timer graceTimer;
    private Timer stayTimer;

    public override void _Ready()
    {
        // Timers for customer drink request logic
        requestTimer = makeTimer(onRequestTimeout);
        graceTimer = makeTimer(onGraceTimeout);
        stayTimer = makeTimer(onStayTimeout);

        satisfaction = Mathf.Min(startingSatisfaction, maxSatisfaction);

        // Top-down: no floor/ceiling logic, collisions treated equally on all sides
        MotionMode = MotionModeEnum.Floating;

        health = maxHealth;
        hitbox.SetCollisionMaskValue(5, true); // also listen for the bullet layer
        hitbox.AreaEntered += onHitboxAreaEntered;

        door = GetTree().GetFirstNodeInGroup("door") as Node2D;
        if (door == null)
        {
            GD.PushError($"{Name}: no node in the \"door\" group");
        }

        // The agent handles both the route (navmesh) and avoiding other customers
        navAgent.PathPostprocessing = NavigationPathQueryParameters2D.PathPostProcessing.Edgecentered;
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

        // Never collide with our own chair, so we can sit on it and walk back out of it
        AddCollisionExceptionWith(targetChair.getCollider());

        // The navigation map is empty until the first physics frame, so wait before asking for a path
        Callable.From(setupNavigation).CallDeferred();
    }

    private async void setupNavigation()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        navAgent.TargetPosition = targetChair.GlobalPosition;
        isNavReady = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!isNavReady || hasReachedDoor || isDead)
        {
            return;
        }

        if (isLeaving)
        {
            moveToTarget(delta);
            if (navAgent.IsNavigationFinished())
            {
                hasReachedDoor = true;
                navAgent.AvoidanceEnabled = false; // stops blocking doorway
            }
        }
        else if (!isSeated)
        {
            moveToTarget(delta);
            isSeated = sitInChair();
            if (isSeated)
            {
                navAgent.Velocity = Vector2.Zero;
            }
        }
        else
        {
            // Seated: the meter only drains once a request has gone unanswered past the grace period
            if (isDraining)
            {
                satisfaction = Mathf.Max(satisfaction - drainRate * (float)delta, 0f);
                EmitSignal(SignalName.SatisfactionChanged, satisfaction, maxSatisfaction);

                if (satisfaction <= 0f)
                {
                    leaveChair();
                }
            }
        }
    }

    public bool sitInChair()
    {
        // The chair's collider is cut out of the navmesh, so the agent stops at the
        // closest point it can reach. Finishing the path counts as reaching the chair.
        if (navAgent.IsNavigationFinished())
        {
            Velocity = Vector2.Zero;
            GlobalPosition = targetChair.GlobalPosition; // snap onto the seat

            GetTree().CreateTimer(2.0).Timeout += onSatDown;
            return true;
        }
        return false;
    }

    public void leaveChair()
    {
        if (isLeaving || isDead) return;

        // TODO: Animation for getting out of a chair
        clearDrinkState();
        targetChair.setOccupied(false);
        isSeated = false;
        isLeaving = true;

        if (door != null)
        {
            navAgent.TargetPosition = door.GlobalPosition;
        }
    }

    // ---------- Health ----------

    public void takeDamage(int amount)
    {
        if (health <= 0) return;

        health = Mathf.Max(health - amount, 0);
        EmitSignal(SignalName.HealthChanged, health, maxHealth);

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
        clearDrinkState();

        // Free the chair unless we'd already given it up by leaving
        if (!isLeaving)
        {
            targetChair.setOccupied(false);
        }

        // Stop moving and stop other customers steering around the body
        Velocity = Vector2.Zero;
        navAgent.Velocity = Vector2.Zero;
        navAgent.AvoidanceEnabled = false;

        // TODO: Death animation, then QueueFree()
        EmitSignal(SignalName.Exit, isWanted, this);
        EmitSignal(SignalName.Died);
        QueueFree();
    }

    private void onHitboxAreaEntered(Area2D area)
    {
        if (area.GetParent() is not Bullet bullet) return;
        if (!bullet.tryHit(this)) return;

        GD.Print($"{Name} overlapped {bullet.Name}, shooter = {bullet.getShooter()?.Name ?? "null"}");
        takeDamage(bullet.getDamage());
    }

    // ---------- Movement ----------

    // Asks the agent for the next point on its navmesh path and passes the desired velocity on.
    // The agent adjusts it to avoid other customers and returns it in onVelocityComputed.
    private void moveToTarget(double delta)
    {
        if (navAgent.IsNavigationFinished())
        {
            navAgent.Velocity = Vector2.Zero;
            return;
        }

        Vector2 nextPoint = navAgent.GetNextPathPosition();
        Vector2 toNext = nextPoint - GlobalPosition;

        // Slow down near the point so we don't overshoot it in one frame
        float speed = Mathf.Min(walkSpeed, toNext.Length() / (float)delta);
        navAgent.Velocity = toNext.Normalized() * speed;
    }

    private void onVelocityComputed(Vector2 safeVelocity)
    {
        // Seated and dead customers stay put, but seated ones still act as obstacles
        if (hasReachedDoor || isSeated || isDead)
        {
            return;
        }

        Velocity = safeVelocity;
        MoveAndSlide();
    }

    private void onSatDown()
    {
        if (isDead || isLeaving) return;

        // TODO: ANIMATION FOR SITTING GOES HERE
        EmitSignal(SignalName.SatisfactionChanged, satisfaction, maxSatisfaction);
        stayTimer.Start(stayTime);
        scheduleNextDrinkRequest();
    }

    // ---------- Drinks ----------

    public bool hasDrinkRequest()
    {
        return drinkRequest;
    }

    public DrinkType getRequestedDrink()
    {
        return drinkTypeReq;
    }

    public float getSatisfaction()
    {
        return satisfaction;
    }

    // Called by the player/bar. Returns true if the drink was accepted.
    public bool tryServeDrink(DrinkType drink)
    {
        if (isDead || !isSeated || !drinkRequest || drink != drinkTypeReq) return false;

        graceTimer.Stop();
        drinkRequest = false;
        isDraining = false;
        EmitSignal(SignalName.DrinkRequestCleared);

        satisfaction = Mathf.Min(satisfaction + satisfactionPerDrink, maxSatisfaction);
        EmitSignal(SignalName.SatisfactionChanged, satisfaction, maxSatisfaction);

        if (satisfaction >= maxSatisfaction)
        {
            EmitSignal(SignalName.SatisfactionFilled);
            leaveChair();
        }
        else
        {
            scheduleNextDrinkRequest();
        }
        return true;
    }

    private Timer makeTimer(Action onTimeout)
    {
        Timer timer = new Timer();
        timer.OneShot = true;              // fire once, then stop
        timer.Timeout += onTimeout;
        AddChild(timer);                   // must be in the tree to tick
        return timer;
    }

    private void scheduleNextDrinkRequest()
    {
        requestTimer.Start(GD.RandRange(minReqDelay, maxReqDelay));
    }

    // Stops all drink timers and drops any open request (used when leaving or dying)
    private void clearDrinkState()
    {
        requestTimer.Stop();
        graceTimer.Stop();
        stayTimer.Stop();
        isDraining = false;

        if (drinkRequest)
        {
            drinkRequest = false;
            EmitSignal(SignalName.DrinkRequestCleared);
        }
    }

    private void onRequestTimeout()
    {
        if (isDead || !isSeated || isLeaving || drinkRequest) return;

        var drinks = Enum.GetValues<DrinkType>();
        drinkTypeReq = drinks[GD.Randi() % (uint)drinks.Length];
        drinkRequest = true;

        EmitSignal(SignalName.DrinkRequested, (int)drinkTypeReq);
        graceTimer.Start(gracePeriod);
    }

    private void onGraceTimeout()
    {
        if (isDead || !drinkRequest) return;
        isDraining = true;
    }

    private void onStayTimeout()
    {
        if (isDead || isLeaving) return;
        leaveChair();
    }
}