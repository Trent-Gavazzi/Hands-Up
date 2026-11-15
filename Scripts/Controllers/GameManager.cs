using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameManager : Node
{
    [Export] private Player player;
    [Export] private Marker2D spawnMarker;
    [Export] private Node2D door;

    // Root node of each scene must have the matching script attached
    [Export] private PackedScene customerScene; // Customer.tscn
    [Export] private PackedScene wantedScene;   // Wanted.tscn

    // Will define visuals for generated customers
    [Export] private PackedScene customerTemplate;

    [Export] private ulong customerEnterTimerMSec = 3000;
    [Export] private int baseWantedFrequency = 3;

    private float totalSatisfation = 100f;

    private int wantedFrequency;
    private ulong lastCustomerTime; // Last time a customer was allowed in

    private List<Table> tables;
    private List<Customer> activeCustomers = new List<Customer>();

    // Add some variable for wanted attributes that varies from scene to scene

    private Random rand = new Random();

    public override void _Ready()
    {
        tables = GetTree().GetNodesInGroup("Tables").OfType<Table>().ToList();
        wantedFrequency = baseWantedFrequency;
        setWantedAttributes();
    }

    public override void _Process(double delta)
    {
        ulong currentTime = Time.GetTicksMsec();
        tryCreateNewCustomer(currentTime);
    }

    private bool tryCreateNewCustomer(ulong currentTime)
    {
        if (currentTime - lastCustomerTime < customerEnterTimerMSec) return false;

        lastCustomerTime = currentTime;
        return createNewCustomer();
    }

    private bool createNewCustomer()
    {
        Chair targetChair = findFreeChair();
        if (targetChair == null) return false; // saloon is full

        Customer customer;
        int roll = rand.Next(1, 11);

        if (roll > wantedFrequency)
        {
            customer = customerScene.Instantiate<Customer>();
            // TODO: CUSTOMERS NEED TO BE JUST RANDOMIZED, CAN'T BE WANTED LOOK
        }
        else
        {
            Wanted wanted = wantedScene.Instantiate<Wanted>();
            wanted.setPlayer(player);
            // TODO: WANTED ENEMIES NEED TO MATCH ONE OF THE POSTERS
            customer = wanted;
        }

        // Must be configured before AddChild, since _Ready reads these values
        customer.setup(targetChair, door, spawnMarker);
        customer.Position = spawnMarker.GlobalPosition;
        customer.Exit += onCustomerExit;

        AddChild(customer);
        activeCustomers.Add(customer);
        return true;
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
        if (wasWanted) { handleWantedCustomer(); }
        else { totalSatisfation += customerInstance.getSatisfaction(); }
        activeCustomers.Remove(customerInstance);

        GD.Print($"A {wasWanted} customer has exited the saloon");
        customerInstance.Exit -= onCustomerExit;
        activeCustomers.Remove(customerInstance);
    }

    // Handles wanted customer exiting saloon
    private void handleWantedCustomer()
    {
    }
}