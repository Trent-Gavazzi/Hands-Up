using Godot;
using System;
using System.IO;

public partial class Bullet : Node2D
{
	[Export] private Sprite2D texture;
	[Export] private Area2D hitBox;
 	private float velocity = 500f;
    [Export] private int damage = 10;
    [Export] private float lifetime = 3f; // Seconds before a bullet deletes itself

    private const int BULLET_LAYER = 5;
    private const int WALL_LAYER = 3;

    private Vector2 direction = Vector2.Right;
    private Node2D shooter;
    private bool hasHit = false;

    public override void _Ready()
	{
        hitBox.CollisionLayer = 0;
        hitBox.CollisionMask = 0;
		hitBox.SetCollisionLayerValue(BULLET_LAYER, true);
        hitBox.SetCollisionMaskValue(WALL_LAYER, true);

        hitBox.BodyEntered += onBodyEntered;
    }


    public void init(Vector2 newDirection, Node2D newShooter)
    {
        direction = newDirection.Normalized();
        shooter = newShooter;
        Rotation = direction.Angle();
    }

    public override void _PhysicsProcess(double delta)
    {
        Position += direction * velocity * (float)delta;

        lifetime -= (float)delta;
        if (lifetime <= 0f)
        {
            GD.Print($"{Name}: expired");
            QueueFree();
        }
        else if (hasHit)
        {
            QueueFree();
        }
    }

    private void onBodyEntered(Node2D body)
    {
        tryHit(body);
    }

    public bool tryHit(Node target)
    {
        if (hasHit) return false;          // whatever your existing one-hit guard is
        if (isShooter(target)) return false;

        hasHit = true;
        GD.Print("Bullet: hit registered");
        // existing hit logic (QueueFree, effects, etc.)
        return true;
    }

    // Decides if the body we are colliding with shot us
    private bool isShooter(Node target)
    {
        var shooter = getShooter();
        if (shooter == null || target == null) return false;

        return target == shooter
            || shooter.IsAncestorOf(target)
            || target.IsAncestorOf(shooter);
    }

    public int getDamage()
    {
        return damage;
    }

    public Node getShooter()
    {
        return shooter;
    }
}
