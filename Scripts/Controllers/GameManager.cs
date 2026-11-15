using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameManager : Node
{
    [ExportGroup("Scene References")]
    // Make the game manager an easily accessible instance
    public static GameManager Instance { get; private set; }

    // Player, door, and spawning
    [Export] private Player player;
    [Export] private Marker2D spawnMarker;
    [Export] private Node2D door;

    // Root node of each scene must have the matching script attached
    [Export] private PackedScene customerScene; // Customer.tscn
    [Export] private PackedScene wantedScene;   // Wanted.tscn

    // Will define visuals for generated customers
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
    private ulong lastCustomerTime; // Last time a customer was allowed in

    // Level stats
    private int customersSpawned = 0;
    private int customersLeft = 0;
    private int customersKilled = 0;
    private bool levelOver = false;

    private Timer spawnTimer;
    private List<Table> tables;
    private List<Customer> activeCustomers = new List<Customer>();

    // Add some variable for wanted attributes that varies from scene to scene

    private readonly Random rand = new Random();

    public override void _Ready()
    {
        // Make the game manager have an easily accessible static instance
        Instance = this;

        // Set up the tables and wanted frequency
        tables = GetTree().GetNodesInGroup("Tables").OfType<Table>().ToList();
        wantedFrequency = baseWantedFrequency;
        saloonRating = startRating;
        setWantedAttributes();
        spawnTimer = new Timer();
        spawnTimer.OneShot = true;
        spawnTimer.Timeout += onSpawnTimeout;
        AddChild(spawnTimer);

        string activeCap = maxActiveCustomers > 0 ? maxActiveCustomers.ToString() : "chairs";
        string levelSize = customersPerLevel > 0 ? customersPerLevel.ToString() : "endless";
        GD.Print($"[Saloon] Level start | spawn {minSpawnDelay}-{maxSpawnDelay}s | max active: {activeCap} | " +
                 $"customers: {levelSize} | wanted: {wantedFrequency}/10 | rating: {saloonRating:F0}");

        scheduleNextCustomer();
        // Create the customer template manager
        characterTemplateManager = customerTemplateManager.Instantiate<CharacterTemplateManager>();
    }

    private void scheduleNextCustomer()
    {
        spawnTimer.Start(GD.RandRange(minSpawnDelay, maxSpawnDelay));
    }

    private void onSpawnTimeout()
    {
        if (levelOver) return;

        if (customersPerLevel > 0 && customersSpawned >= customersPerLevel) return;

        tryCreateNewCustomer();
        scheduleNextCustomer();
    }

    private bool tryCreateNewCustomer()
    {
        if (maxActiveCustomers > 0 && activeCustomers.Count >= maxActiveCustomers) return false;

        Chair targetChair = findFreeChair();
        if (targetChair == null) return false;

        createNewCustomer(targetChair);
        return true;
    }

    private void createNewCustomer(Chair targetChair)
    {
        Customer customer;
        bool isWanted = rand.Next(1, 11) <= wantedFrequency;

        if (!isWanted)
        {
            customer = customerScene.Instantiate<Customer>();
            // Set the customers template manager
            customer.characterTemplateManager = characterTemplateManager;
        }
        else
        {
            Wanted wanted = wantedScene.Instantiate<Wanted>();
            wanted.setPlayer(player);
            customer = wanted;
            customer.characterTemplateManager = characterTemplateManager;
        }

        // Must be configured before AddChild, since _Ready reads these values
        customer.setup(targetChair, door, spawnMarker);
        customer.Position = spawnMarker.GlobalPosition;
        customer.Exit += onCustomerExit;

        AddChild(customer);
        activeCustomers.Add(customer);
        customersSpawned++;

        GD.Print($"[Saloon] Customer #{customersSpawned} arrived{(isWanted ? " (wanted)" : "")} | active: {activeCustomers.Count}");
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

    private void setWantedAttributes()
    {
        // Creates the attributes to match the wanted customers on the posters
    }

    private void onCustomerExit(bool wasWanted, Customer customerInstance)
    {
        customerInstance.Exit -= onCustomerExit;
        activeCustomers.Remove(customerInstance);

        bool wasKilled = customerInstance.getHealth() <= 0;
        if (wasKilled) customersKilled++;
        else customersLeft++;

        if (wasWanted)
        {
            handleWantedCustomer(wasKilled);
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
        GD.Print($"[Saloon] Level complete | rating: {saloonRating:F0} | left: {customersLeft} | killed: {customersKilled}");
    }

    // Handles wanted customer exiting saloon
    private void handleWantedCustomer(bool wasKilled)
    {
        // TODO: Reward for catching, penalty for letting them escape
        GD.Print(wasKilled ? "[Saloon] Wanted customer was taken down" : "[Saloon] Wanted customer escaped");
    }
}