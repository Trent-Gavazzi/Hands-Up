using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameManager : Node
{
    // Make the game manager an easily accessible instance
    public static GameManager Instance { get; private set; }

    [ExportGroup("Scene References")]
    // Player, door, and spawning
    [Export] private Player player;
    [Export] private Marker2D spawnMarker;
    [Export] private Node2D door;

    // Root node of each scene must have the matching script attached
    [Export] private PackedScene customerScene; // Customer.tscn
    [Export] private PackedScene wantedScene;   // Wanted.tscn

    // Defines visuals for generated customers
    [Export] private PackedScene customerTemplateManager;
    public CharacterTemplateManager characterTemplateManager { get; private set; }

    [ExportGroup("Level Flow")]
    [Export] private float minSpawnDelay = 3f;   // Seconds between arrivals, randomized in this range
    [Export] private float maxSpawnDelay = 8f;
    [Export] private int maxActiveCustomers = 0; // 0 = limited only by free chairs
    [Export] private int customersPerLevel = 0;  // 0 = endless
    [Export(PropertyHint.Range, "0,10,1")] private int baseWantedFrequency = 3; // Chance out of 10

    [ExportGroup("Rating")]
    [Export(PropertyHint.Range, "0,100,1")] private float startRating = 100f;
    // How far each departing customer pulls the rating toward their satisfaction (0-1)
    [Export(PropertyHint.Range, "0,1,0.05")] private float ratingWeight = 0.25f;
    [Export] private float innocentKillPenalty = 25f;

    private int wantedFrequency;
    private float saloonRating;

    // Level stats
    private int customersSpawned = 0;
    private int customersLeft = 0;
    private int customersKilled = 0;
    private int wantedCaught = 0;
    private bool levelOver = false;

    private Timer spawnTimer;
    private List<Table> tables;
    private readonly List<Customer> activeCustomers = new List<Customer>();

    private readonly Random rand = new Random();

    public override void _Ready()
    {
        Instance = this;

        tables = GetTree().GetNodesInGroup("Tables").OfType<Table>().ToList();
        wantedFrequency = baseWantedFrequency;
        saloonRating = startRating;

        // Must be in the tree so its _Ready generates the wanted looks
        characterTemplateManager = customerTemplateManager.Instantiate<CharacterTemplateManager>();
        AddChild(characterTemplateManager);

        spawnTimer = new Timer();
        spawnTimer.OneShot = true;
        spawnTimer.Timeout += onSpawnTimeout;
        AddChild(spawnTimer);

        string activeCap = maxActiveCustomers > 0 ? maxActiveCustomers.ToString() : "chairs";
        string levelSize = customersPerLevel > 0 ? customersPerLevel.ToString() : "endless";
        GD.Print($"[Saloon] Level start | spawn {minSpawnDelay}-{maxSpawnDelay}s | max active: {activeCap} | " +
                 $"customers: {levelSize} | wanted: {wantedFrequency}/10 | rating: {saloonRating:F0}");
        printWantedPosters();

        scheduleNextCustomer();
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    // ---------- Spawning ----------

    private void scheduleNextCustomer()
    {
        spawnTimer.Start(GD.RandRange(minSpawnDelay, maxSpawnDelay));
    }

    private void onSpawnTimeout()
    {
        if (levelOver) return;

        // Everyone for this level has arrived, wait for them to leave
        if (customersPerLevel > 0 && customersSpawned >= customersPerLevel) return;

        tryCreateNewCustomer();
        scheduleNextCustomer();
    }

    private bool tryCreateNewCustomer()
    {
        if (maxActiveCustomers > 0 && activeCustomers.Count >= maxActiveCustomers) return false;

        Chair targetChair = findFreeChair();
        if (targetChair == null) return false; // saloon is full

        createNewCustomer(targetChair);
        return true;
    }

    private void createNewCustomer(Chair targetChair)
    {
        Customer customer;
        VisualAttributes looks;
        bool isWanted = false;

        // Falls back to a regular customer if every wanted look is already in the saloon
        if (rand.Next(1, 11) <= wantedFrequency && characterTemplateManager.tryGetWanted(out looks))
        {
            Wanted wanted = wantedScene.Instantiate<Wanted>();
            wanted.setPlayer(player);
            customer = wanted;
            isWanted = true;
        }
        else
        {
            customer = customerScene.Instantiate<Customer>();
            looks = characterTemplateManager.getNonWanted();
        }

        // Must be configured before AddChild, since _Ready reads these values
        customer.setup(targetChair, door, spawnMarker, looks);
        customer.Position = spawnMarker.GlobalPosition;
        customer.Exit += onCustomerExit;

        AddChild(customer);
        activeCustomers.Add(customer);
        customersSpawned++;

        GD.Print($"[Saloon] Customer #{customersSpawned} arrived{(isWanted ? " (wanted)" : "")} " +
                 $"| color: #{looks.color.ToHtml(false)} | active: {activeCustomers.Count}");
    }

    // For now just the first available seat across all tables
    // TODO: ADD A DYNAMIC SYSTEM FOR PICKING CHAIR
    private Chair findFreeChair()
    {
        foreach (Table table in tables)
        {
            foreach (Chair chair in table.GetChairs())
            {
                if (!chair.isOccupied()) return chair;
            }
        }
        return null;
    }

    // Console stand-in for the poster UI
    private void printWantedPosters()
    {
        IEnumerable<string> colors = characterTemplateManager.wantedAttributeList
            .Select(w => "#" + w.color.ToHtml(false));
        GD.Print($"[Saloon] Wanted posters: {string.Join(", ", colors)}");
    }

    // ---------- Exits & rating ----------

    private void onCustomerExit(bool wasWanted, Customer customerInstance)
    {
        customerInstance.Exit -= onCustomerExit;
        activeCustomers.Remove(customerInstance);

        bool wasKilled = customerInstance.getHealth() <= 0;
        if (wasKilled) customersKilled++;
        else customersLeft++;

        if (wasWanted)
        {
            handleWantedCustomer(customerInstance, wasKilled);
        }
        else if (wasKilled)
        {
            setRating(saloonRating - innocentKillPenalty, "an innocent customer was shot");
        }
        else
        {
            float satisfaction = customerInstance.getSatisfaction();
            setRating(Mathf.Lerp(saloonRating, satisfaction, ratingWeight),
                      $"customer left at {satisfaction:F0} satisfaction");
        }

        checkLevelComplete();
    }

    // Handles wanted customer exiting saloon
    private void handleWantedCustomer(Customer customerInstance, bool wasKilled)
    {
        if (wasKilled)
        {
            // Caught for good: their look stays out of the pool
            wantedCaught++;
            GD.Print($"[Saloon] Wanted customer taken down ({wantedCaught}/{characterTemplateManager.wantedAttributeList.Count})");

            if (wantedCaught >= characterTemplateManager.wantedAttributeList.Count)
            {
                GD.Print("[Saloon] All wanted outlaws caught");
            }
        }
        else
        {
            // Escaped: they can come back later
            characterTemplateManager.returnWanted(customerInstance.getAttributes());
            GD.Print("[Saloon] Wanted customer escaped");
            // TODO: Penalty for letting them escape
        }
    }

    private void setRating(float value, string reason)
    {
        float previous = saloonRating;
        saloonRating = Mathf.Clamp(value, 0f, 100f);
        GD.Print($"[Saloon] Rating {previous:F0} -> {saloonRating:F0} ({reason})");
        // TODO: Lose condition / UI hook once the UI is stable
    }

    public float getSaloonRating()
    {
        return saloonRating;
    }

    private void checkLevelComplete()
    {
        if (customersPerLevel <= 0) return;
        if (customersSpawned < customersPerLevel || activeCustomers.Count > 0) return;

        levelOver = true;
        spawnTimer.Stop();
        GD.Print($"[Saloon] Level complete | rating: {saloonRating:F0} | left: {customersLeft} | " +
                 $"killed: {customersKilled} | wanted caught: {wantedCaught}");
    }
}