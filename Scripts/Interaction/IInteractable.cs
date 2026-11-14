using Godot;

// Implement on any object or NPC script (Customer, Door, Bar, ...).
// The object needs an Area2D child on InteractionLayer so an Interactor can detect it.
// The Interactor checks the area, its parent, then its scene owner for this interface.
public interface IInteractable
{
    // Physics layer used for interaction detection (shared by Interactor and interactables)
    public const int InteractionLayer = 6;

    // ---------- Required ----------

    bool isEnabled();
    void setEnabled(bool value);

    // Checked when the interact button is pressed. Return false to block the interaction.
    bool canInteract(Node interactor);

    // Returns true if the interaction went through
    bool interact(Node interactor);

    // ---------- Optional (defaults below) ----------

    string getPrompt()
    {
        return "Interact";
    }

    // Higher wins when several are in range
    int getPriority()
    {
        return 0;
    }

    // Used to pick the closest one
    Vector2 getInteractionPosition()
    {
        return this is Node2D node ? node.GlobalPosition : Vector2.Zero;
    }

    // Called by the Interactor when this becomes / stops being the focused target
    void onFocusEntered(Node interactor) { }
    void onFocusExited(Node interactor) { }
}