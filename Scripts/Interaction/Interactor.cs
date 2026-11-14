using Godot;
using System.Collections.Generic;

// Attach as a child of the Player (needs a CollisionShape2D child for the reach radius).
public partial class Interactor : Area2D
{
    [Export] private Node interactorOwner;                // Passed to interactables; defaults to parent
    [Export] private string interactAction = "interact";  // Add this action in Project Settings > Input Map
    [Export] private bool enabled = true;

    // Emitted when the focused target or its prompt changes.
    // target is null and prompt is empty when nothing is focused.
    [Signal] public delegate void FocusChangedEventHandler(Node target, string prompt);

    private readonly List<IInteractable> inRange = new List<IInteractable>();
    private IInteractable focused;
    private string focusedPrompt = "";

    public override void _Ready()
    {
        if (interactorOwner == null)
        {
            interactorOwner ??= GetParent();
            GD.PushWarning("Player Interactor was not defined and had to be filled.");
        }
        // Only detects, is never detected
        CollisionLayer = 0;
        CollisionMask = 0;
        SetCollisionMaskValue(IInteractable.InteractionLayer, true);
        Monitoring = true;
        Monitorable = false;

        AreaEntered += onAreaEntered;
        AreaExited += onAreaExited;
    }

    public override void _PhysicsProcess(double delta)
    {
        updateFocus();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed(interactAction)) return;

        if (tryInteract())
        {
            GetViewport().SetInputAsHandled();
        }
    }

    // Can also be called directly, e.g. from a touch button
    public bool tryInteract()
    {
        if (!enabled || !isValid(focused)) return false;
        return focused.interact(interactorOwner);
    }

    // ---------- Toggle ----------

    public bool isEnabled()
    {
        return enabled;
    }

    public void setEnabled(bool value)
    {
        enabled = value;
        if (!enabled) setFocus(null);
    }

    public IInteractable getFocused()
    {
        return focused;
    }

    // ---------- Detection ----------

    private void onAreaEntered(Area2D area)
    {
        IInteractable interactable = findInteractable(area);
        if (interactable != null && !inRange.Contains(interactable))
        {
            inRange.Add(interactable);
        }
    }

    private void onAreaExited(Area2D area)
    {
        IInteractable interactable = findInteractable(area);
        if (interactable != null)
        {
            inRange.Remove(interactable);
        }
    }

    // The area itself, its parent (e.g. Customer > Hitbox), or the root of its scene
    private static IInteractable findInteractable(Area2D area)
    {
        if (area is IInteractable self) return self;
        if (area.GetParent() is IInteractable parent) return parent;
        return area.Owner as IInteractable;
    }

    // Picks the highest-priority enabled interactable, breaking ties by distance
    private void updateFocus()
    {
        inRange.RemoveAll(i => !isValid(i)); // drop anything that was freed

        IInteractable best = null;

        if (enabled)
        {
            float bestDist = float.MaxValue;

            foreach (IInteractable candidate in inRange)
            {
                if (!candidate.isEnabled()) continue;

                float dist = GlobalPosition.DistanceSquaredTo(candidate.getInteractionPosition());

                if (best == null ||
                    candidate.getPriority() > best.getPriority() ||
                    (candidate.getPriority() == best.getPriority() && dist < bestDist))
                {
                    best = candidate;
                    bestDist = dist;
                }
            }
        }

        setFocus(best);
    }

    private void setFocus(IInteractable target)
    {
        bool changed = target != focused;

        if (changed)
        {
            if (isValid(focused)) focused.onFocusExited(interactorOwner);
            focused = target;
            focused?.onFocusEntered(interactorOwner);
        }

        // Prompts can change while focused (e.g. a customer starts requesting a drink)
        string prompt = focused?.getPrompt() ?? "";

        if (changed || prompt != focusedPrompt)
        {
            focusedPrompt = prompt;
            EmitSignal(SignalName.FocusChanged, focused as Node, prompt);
        }
    }

    private static bool isValid(IInteractable interactable)
    {
        return interactable != null && GodotObject.IsInstanceValid(interactable as GodotObject);
    }
}