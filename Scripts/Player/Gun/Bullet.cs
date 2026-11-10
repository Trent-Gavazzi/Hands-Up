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
        if(lifetime <= 0f)
        {
            QueueFree();
        }
    }

    private void onBodyEntered(Node2D body)
    {
        if (body == shooter)
        {
            return;
        }
        GD.PushWarning($"Bullet entered {body}.");
        tryHit();
    }

    public bool tryHit()
    {
        if (hasHit)
        {
            return false;
        }
        hasHit = true;
        QueueFree();
        return true;
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
