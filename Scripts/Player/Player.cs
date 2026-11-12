using Godot;
using System;

public partial class Player : Node2D
{
	[Export] private PlayerMovement player;
    [Export] private Area2D hitbox;
    [Export] private NavigationObstacle2D navObstacle;
    private const int maxHealth = 100;
	private int health;

	[Signal] public delegate void HealthChangedEventHandler(int current, int max);
	[Signal] public delegate void DiedEventHandler();

	public override void _Ready()
	{
		health = maxHealth;
        hitbox.SetCollisionMaskValue(5, true); // listen for the bullet layer
        hitbox.AreaEntered += onHitboxAreaEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        navObstacle.Velocity = player.Velocity;
    }

    public void takeDamage(int amount)
	{
		if (health <= 0) return;

		health = Mathf.Max(health - amount, 0);
		EmitSignal(SignalName.HealthChanged, health, maxHealth);

		if(health == 0)
		{
			EmitSignal(SignalName.Died);
		}	
	}

	public void heal(int amount)
	{
		if (health <= 0) return;

		health = Mathf.Min(health + amount, maxHealth);
		EmitSignal(SignalName.HealthChanged, health, maxHealth);
	}

    private void onHitboxAreaEntered(Area2D area)
    {
        if (area.GetParent() is not Bullet bullet) return;
        if (!bullet.tryHit(this)) return;

        GD.Print($"{Name} hit by {bullet.Name}, shooter = {bullet.getShooter()?.Name ?? "null"}");
        takeDamage(bullet.getDamage());
    }

    public int getHealth()
    {
        return health;
    }
}
