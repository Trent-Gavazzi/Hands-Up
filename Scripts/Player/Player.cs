using Godot;
using System;
using System.Collections.Generic;


public partial class Player : Node2D
{
	[Export] private PlayerMovement player;
    [Export] private Area2D hitbox;
    [Export] private NavigationObstacle2D navObstacle;
    [Export] private int maxDrinks = 2;
    // Animate the player
    [Export] private AnimatedSprite2D sprite;

    private const int maxHealth = 100;
	private int health;
    private readonly List<DrinkType> drinkInv = new List<DrinkType>();

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
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        if (sprite == null || player == null) return;

        bool isMoving = player.Velocity.LengthSquared() > 1f;
        string targetAnim = "default";

        if (isMoving)
        {
            bool movingLeft = player.Velocity.X < -1f;
            bool movingRight = player.Velocity.X > 1f;
            bool isRunningBackward = (player.FacingLeft && movingRight) || (!player.FacingLeft && movingLeft);

            targetAnim = isRunningBackward ? "BackRun" : "Run";
        }

        if (sprite.Animation != targetAnim)
            sprite.Play(targetAnim);
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

    public bool tryHoldDrink(DrinkType drink)
    {
        if(!isDrinksMaxed()) return false;
        
        drinkInv.Add(drink);
        return true;
    }

    public bool isDrinksMaxed()
    {
        return drinkInv.Count >= maxDrinks;
    }

    public bool hasDrink(DrinkType drink)
    {
        return drinkInv.Contains(drink);
    }

    public bool tryRemoveDrink(DrinkType drink)
    {
        return drinkInv.Remove(drink);
    }
    
    public IReadOnlyList<DrinkType> getDrinks()
    {
        return drinkInv;
    }
}
