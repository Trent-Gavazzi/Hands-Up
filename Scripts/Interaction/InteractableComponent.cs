using Godot;
using System;

// Attach as a child of any object or NPC (needs a CollisionShape2D child of its own).
// Hook behaviour up through the signals, the condition delegate, or by subclassing.
public partial class InteractableComponent : Area2D, IInteractable
{
    [Export] private bool enabled = true;
    [Export] private string prompt = "Interact";
    [Export] private int priority = 0;
    [Export] private bool oneShot = false;      // Disables itself after the first successful interaction
    [Export] private float cooldown = 0f;       // Seconds between interactions, 0 = none
    [Export] private int interactionLayer = 6;  // Must match the Interactor's mask layer

    [Signal] public delegate void InteractedEventHandler(Node interactor);
    [Signal] public delegate void InteractionDeniedEventHandler(Node interactor);
    [Signal] public delegate void EnabledChangedEventHandler(bool isEnabled);
    [Signal] public delegate void FocusEnteredEventHandler(Node interactor);
    [Signal] public delegate void FocusExitedEventHandler(Node interactor);

    // Optional gate set from code, e.g. i => isSeated && drinkRequest. Null = always allowed.
    public Func<Node, bool> condition;

    private ulong lastInteractMsec;
    private bool hasInteracted = false;

    public override void _Ready()
    {
        // Only needs to be detected, never detects anything itself
        CollisionLayer = 0;
        CollisionMask = 0;
        SetCollisionLayerValue(interactionLayer, true);
        Monitoring = false;
        Monitorable = true;
    }

    // ---------- Toggle ----------

    public bool isEnabled()
    {
        return enabled;
    }

    public void setEnabled(bool value)
    {
        if (enabled == value) return;
        enabled = value;
        EmitSignal(SignalName.EnabledChanged, enabled);
    }

    public void toggle()
    {
        setEnabled(!enabled);
    }

    // ---------- Interaction ----------

    public virtual bool canInteract(Node interactor)
    {
        if (!enabled) return false;

        if (cooldown > 0f && hasInteracted &&
            Time.GetTicksMsec() - lastInteractMsec < (ulong)(cooldown * 1000f))
        {
            return false;
        }

        return condition == null || condition(interactor);
    }

    public virtual bool interact(Node interactor)
    {
        if (!canInteract(interactor))
        {
            EmitSignal(SignalName.InteractionDenied, interactor);
            return false;
        }

        hasInteracted = true;
        lastInteractMsec = Time.GetTicksMsec();
        EmitSignal(SignalName.Interacted, interactor);

        if (oneShot) setEnabled(false);
        return true;
    }

    // ---------- Info ----------

    public string getPrompt()
    {
        return prompt;
    }

    public void setPrompt(string value)
    {
        prompt = value;
    }

    public int getPriority()
    {
        return priority;
    }

    public void setPriority(int value)
    {
        priority = value;
    }

    public Vector2 getInteractionPosition()
    {
        return GlobalPosition;
    }

    // ---------- Focus ----------

    public virtual void onFocusEntered(Node interactor)
    {
        EmitSignal(SignalName.FocusEntered, interactor);
    }

    public virtual void onFocusExited(Node interactor)
    {
        EmitSignal(SignalName.FocusExited, interactor);
    }
}