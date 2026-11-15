using Godot;
using System;

public partial class DrinkStation : Node2D, IInteractable
{
    [Export] protected Area2D interactArea;
    [Export] protected DrinkType drinkType;
    [Export] protected float waitTime = 5.0f;

    // Hook these up to UI (progress bar, "ready" popup, etc.)
    [Signal] public delegate void BrewStartedEventHandler(float duration);
    [Signal] public delegate void DrinkReadyEventHandler();
    [Signal] public delegate void DrinkTakenEventHandler();

    protected bool isBrewing = false;
    protected bool isDrinkReady = false;
    protected bool interactionEnabled = true;

    protected Timer drinkTimer;

    public override void _Ready()
    {
        drinkTimer = makeTimer(onDrinkReady);

        // Make the station detectable by the player's Interactor
        interactArea.SetCollisionLayerValue(IInteractable.InteractionLayer, true);
    }

    // ---------- IInteractable ----------

    public virtual bool isEnabled()
    {
        return interactionEnabled;
    }

    public virtual void setEnabled(bool value)
    {
        interactionEnabled = value;
    }

    public virtual bool canInteract(Node interactor)
    {
        return isEnabled() && !isBrewing;
    }

    public virtual bool interact(Node interactor)
    {
        if (!canInteract(interactor)) return false;

        if (isDrinkReady)
        {
            return giveDrink(interactor);
        }

        startBrewing();
        return true;
    }

    public virtual string getPrompt()
    {
        if (isBrewing) return "Brewing...";
        return isDrinkReady ? $"Take {drinkType}" : $"Brew {drinkType}";
    }

    // ---------- Brewing ----------

    protected virtual void startBrewing()
    {
        isBrewing = true;
        drinkTimer.Start(waitTime);
        EmitSignal(SignalName.BrewStarted, waitTime);
    }

    // Returns false if the hand-off failed, leaving the drink on the station
    protected virtual bool giveDrink(Node interactor)
    {
        if (interactor is not Player player || !player.tryHoldDrink(drinkType)) return false;
        isDrinkReady = false;
        EmitSignal(SignalName.DrinkTaken);
        return true;
    }

    private void onDrinkReady()
    {
        isBrewing = false;
        isDrinkReady = true;
        EmitSignal(SignalName.DrinkReady);
    }

    private Timer makeTimer(Action onTimeout)
    {
        Timer timer = new Timer();
        timer.OneShot = true;              // fire once, then stop
        timer.Timeout += onTimeout;
        AddChild(timer);                   // must be in the tree to tick
        return timer;
    }
}