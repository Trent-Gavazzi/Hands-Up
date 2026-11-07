using Godot;

public enum moveState
{
    Idle,
    Walking,
    Running,
    Rolling
}

// TODO: ADD PLAYER NAVIGATIONAGENT
public partial class PlayerMovement : CharacterBody2D
{
    [ExportGroup("Nodes")]
    [Export] public Gun Gun;               // optional
    [Export] public Sprite2D BodySprite;   // optional; flipped to match the facing side
    [Export] public bool TextureFacesLeft = false;


    [ExportGroup("Speeds")]
    [Export] public float WalkSpeed = 200f;
    [Export] public float RunSpeed = 350f;

    [ExportGroup("Roll")]
    [Export] public float RollSpeed = 600f;
    [Export] public float RollDuration = 0.3f;   // seconds
    [Export] public float RollCooldown = 0.5f;   // seconds, starts when the roll ends

    [ExportGroup("Input")]
    [Export(PropertyHint.Range, "0,0.5,0.01")] public float InputDeadzone = 0.1f;
    // How far off vertical the facing direction must be before the side flips (stops jitter)
    [Export(PropertyHint.Range, "0,0.5,0.01")] public float FacingSideDeadzone = 0.1f;

    [Signal] public delegate void StateChangedEventHandler(moveState previous, moveState next);
    [Signal] public delegate void FacingSideChangedEventHandler(bool facingLeft);

    public moveState CurrentState { get; private set; } = moveState.Idle;

    public Vector2 MoveDirection { get; private set; } = Vector2.Down;   // last non-zero movement input
    public Vector2 FacingDirection { get; private set; } = Vector2.Down; // aim if gun drawn, else movement
    public bool FacingLeft { get; private set; }

    public bool IsRollReady => _rollCooldownTimer <= 0f;

    private Vector2 _moveInput;
    private bool _runHeld;
    private bool _rollPressed;

    private Vector2 _rollDirection;
    private float _rollTimer;
    private float _rollCooldownTimer;

    public override void _Ready()
    {
        // Top-down: no floor/ceiling logic, collisions treated equally on all sides
        MotionMode = MotionModeEnum.Floating;
        EnsureInputActions();
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        TickTimers(dt);

        // GetVector clamps length to 1 (no faster diagonals) and applies the deadzone
        _moveInput = Input.GetVector("move_left", "move_right", "move_up", "move_down", InputDeadzone);
        _runHeld = Input.IsActionPressed("run");
        _rollPressed = Input.IsActionJustPressed("roll");

        if (_moveInput != Vector2.Zero)
            MoveDirection = _moveInput.Normalized();

        UpdateState();
        ApplyState();
        MoveAndSlide(); // stops/slides against any StaticBody2D walls
    }

    // Facing is visual only, so it updates every rendered frame for smooth mouse tracking
    public override void _Process(double delta)
    {
        UpdateFacing();
    }

    private void TickTimers(float dt)
    {
        if (_rollTimer > 0f) _rollTimer -= dt;
        if (_rollCooldownTimer > 0f) _rollCooldownTimer -= dt;
    }

    // ---------- Facing ----------

    private void UpdateFacing()
    {
        if (CurrentState == moveState.Rolling)
            FacingDirection = _rollDirection;
        else if (Gun != null && Gun.IsDrawn)
            FacingDirection = Gun.GetAimDirection();
        else
            FacingDirection = MoveDirection;

        // Straight up/down keeps whichever side the player was already on
        if (Mathf.Abs(FacingDirection.X) > FacingSideDeadzone)
        {
            bool left = FacingDirection.X < 0f;
            if (left != FacingLeft)
            {
                FacingLeft = left;
                EmitSignal(SignalName.FacingSideChanged, FacingLeft);
            }
        }

        if (BodySprite != null)
            BodySprite.FlipH = FacingLeft != TextureFacesLeft;
    }

    // ---------- State machine ----------

    private void UpdateState()
    {
        bool hasInput = _moveInput != Vector2.Zero;

        switch (CurrentState)
        {
            case moveState.Idle:
                if (hasInput) ChangeState(_runHeld ? moveState.Running : moveState.Walking);
                break;

            case moveState.Walking:
                if (!hasInput) ChangeState(moveState.Idle);
                else if (_runHeld) ChangeState(moveState.Running);
                break;

            case moveState.Running:
                if (!hasInput) ChangeState(moveState.Idle);
                else if (_rollPressed && IsRollReady) ChangeState(moveState.Rolling);
                else if (!_runHeld) ChangeState(moveState.Walking);
                break;

            case moveState.Rolling:
                // Roll can't be cancelled; resolve to whatever the player is holding when it ends
                if (_rollTimer <= 0f)
                {
                    if (!hasInput) ChangeState(moveState.Idle);
                    else ChangeState(_runHeld ? moveState.Running : moveState.Walking);
                }
                break;
        }
    }

    private void ApplyState()
    {
        switch (CurrentState)
        {
            case moveState.Idle:
                Velocity = Vector2.Zero;
                break;
            case moveState.Walking:
                Velocity = _moveInput * WalkSpeed;
                break;
            case moveState.Running:
                Velocity = _moveInput * RunSpeed;
                break;
            case moveState.Rolling:
                Velocity = _rollDirection * RollSpeed;
                break;
        }
    }

    private void ChangeState(moveState next)
    {
        if (next == CurrentState) return;

        moveState previous = CurrentState;
        ExitState(previous);
        CurrentState = next;
        EnterState(next);

        EmitSignal(SignalName.StateChanged, Variant.From(previous), Variant.From(next));
    }

    private void EnterState(moveState state)
    {
        switch (state)
        {
            case moveState.Idle:
                Velocity = Vector2.Zero;
                break;
            case moveState.Walking:
                break;
            case moveState.Running:
                break;
            case moveState.Rolling:
                // Roll follows movement input, not aim, so you can roll away from where you're shooting
                _rollDirection = MoveDirection;
                _rollTimer = RollDuration;
                break;
        }
    }

    private void ExitState(moveState state)
    {
        switch (state)
        {
            case moveState.Idle:
                break;
            case moveState.Walking:
                break;
            case moveState.Running:
                break;
            case moveState.Rolling:
                _rollCooldownTimer = RollCooldown;
                break;
        }
    }

    // ---------- Input setup ----------
    // Creates default bindings only if the action isn't already defined in
    // Project Settings > Input Map, so editor bindings always take priority.
    // Called once on startup

    private static void EnsureInputActions()
    {
        EnsureAction("move_left", KeyEvent(Key.A), KeyEvent(Key.Left), AxisEvent(JoyAxis.LeftX, -1f));
        EnsureAction("move_right", KeyEvent(Key.D), KeyEvent(Key.Right), AxisEvent(JoyAxis.LeftX, 1f));
        EnsureAction("move_up", KeyEvent(Key.W), KeyEvent(Key.Up), AxisEvent(JoyAxis.LeftY, -1f));
        EnsureAction("move_down", KeyEvent(Key.S), KeyEvent(Key.Down), AxisEvent(JoyAxis.LeftY, 1f));
        EnsureAction("run", KeyEvent(Key.Shift), new InputEventJoypadButton { ButtonIndex = JoyButton.B });
        EnsureAction("roll", KeyEvent(Key.Space), new InputEventJoypadButton { ButtonIndex = JoyButton.A });
        EnsureAction("toggle_gun", KeyEvent(Key.Q),
            new InputEventMouseButton { ButtonIndex = MouseButton.Right },
            new InputEventJoypadButton { ButtonIndex = JoyButton.Y });
    }

    private static void EnsureAction(string name, params InputEvent[] events)
    {
        if (InputMap.HasAction(name)) return;

        InputMap.AddAction(name);
        foreach (InputEvent e in events)
            InputMap.ActionAddEvent(name, e);
    }

    private static InputEventKey KeyEvent(Key key) => new() { PhysicalKeycode = key };

    private static InputEventJoypadMotion AxisEvent(JoyAxis axis, float value) =>
        new() { Axis = axis, AxisValue = value };
}