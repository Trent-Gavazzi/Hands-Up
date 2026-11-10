using Godot;

public enum gunState
{
    Holstered,
    Drawn
}

// Must be a direct child of the PlayerMovement node.
// The gun texture should point to the RIGHT, with the grip at the node's origin.
public partial class Gun : Node2D
{
    [ExportGroup("Nodes")]
    [Export] private Sprite2D GunSprite;
    [Export] private PackedScene BulletScene { get; set; }

    [ExportGroup("Holstered")]
    // Position relative to the player when facing right; mirrored automatically when facing left
    [Export] private Vector2 HolsterOffset = new(8f, 4f);
    [Export(PropertyHint.Range, "-180,180,1,radians_as_degrees")] private float HolsterRotation = Mathf.Pi / 2f; // pointing down

    [ExportGroup("Drawn")]
    // Point on the player the gun orbits around (e.g. the shoulder)
    [Export] private Vector2 ShoulderOffset = new(0f, -4f);
    [Export] private float AimRadius = 12f;
    [Export] private float MuzzleDistance = 10f;
    // Attack speed
    [Export] private ulong gunCooldownMsec = 250;
    private ulong lastFireTime = 0;

    [Signal] private delegate void StateChangedEventHandler(gunState previous, gunState next);

    public gunState CurrentState { get; private set; } = gunState.Holstered;
    private bool IsDrawn => CurrentState == gunState.Drawn;

    private PlayerMovement _player;
    private Vector2 _lastAim = Vector2.Right;

    public override void _Ready()
    {
        _player = GetParent() as PlayerMovement;
        if (_player == null)
            GD.PushError($"{Name}: Gun is not direct child of a PlayerMovement node.");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("toggle_gun"))
            ChangeState(IsDrawn ? gunState.Holstered : gunState.Drawn);
    }

    // Runs after the player's _Process (children process after parents),
    // so the player's facing side is already up to date for this frame.
    public override void _Process(double delta)
    {
        if (_player == null) return;

        bool facingLeft = _player.FacingLeft;

        switch (CurrentState)
        {
            case gunState.Holstered:
                float side = facingLeft ? -1f : 1f;
                Position = new Vector2(HolsterOffset.X * side, HolsterOffset.Y);
                Rotation = facingLeft ? Mathf.Pi - HolsterRotation : HolsterRotation;
                break;

            case gunState.Drawn:
                Vector2 aim = GetAimDirection();
                Position = ShoulderOffset + aim * AimRadius;
                Rotation = aim.Angle();
                if (Input.IsActionJustPressed("shoot"))
                {
                   tryShoot();
                }
                break;
        }

        // Keeps the gun right-side-up on the left side instead of upside down
        if (GunSprite != null)
            GunSprite.FlipV = facingLeft;
    }

    // Unit vector from the shoulder to the mouse, in world space.
    public Vector2 GetAimDirection()
    {
        if (_player == null) return _lastAim;

        Vector2 toMouse = GetGlobalMousePosition() - _player.ToGlobal(ShoulderOffset);
        if (toMouse.LengthSquared() > 1f)
            _lastAim = toMouse.Normalized();

        return _lastAim;
    }

    private void ChangeState(gunState next)
    {
        if (next == CurrentState) return;

        gunState previous = CurrentState;
        CurrentState = next;
        EmitSignal(SignalName.StateChanged, Variant.From(previous), Variant.From(next));
    }

    private void tryShoot()
    {
        ulong currentTime = Time.GetTicksMsec();
        
        if(currentTime - lastFireTime >= gunCooldownMsec)
        {
            shoot();
            lastFireTime = currentTime;
        }
    }

    private void shoot()
    {
        Vector2 aim = GetAimDirection();

        Bullet bulletInstance = BulletScene.Instantiate<Bullet>();
        bulletInstance.init(aim, _player);

        bulletInstance.Position = HolsterOffset;
        GetTree().CurrentScene.AddChild(bulletInstance);
        bulletInstance.GlobalPosition = GlobalPosition + aim * MuzzleDistance;

        GD.Print("Created and fired bullet");
    }

    public bool isDrawn()
    {
        return IsDrawn;
    }
}