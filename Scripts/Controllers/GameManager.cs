using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Transactions;

public partial class GameManager : Node
{
	[Export] private Player player;

	[Export] private Marker2D spawnMarker;
	[Export] private PackedScene customerTemplate;
	[Export] private ulong customerEnterTimerMSec = 3000;
	[Export] private int baseWantedFrequency = 3;

	private float totalSatisfation = 100f;

	private int wantedFrequency;
	private ulong lastCustomerTime; // Last time a customer was allowed in

    private List<Table> tables;
    private List<Customer> activeCustomers = new List<Customer>();
	
	// Add some variable for wanted attributes that varies from scene to scene
	// Will be passed to the wanted constructor

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
		// Add logic to create new customer
		if(currentTime - lastCustomerTime >= customerEnterTimerMSec)
		{
			createNewCustomer();
			return true;
		}
		return false;
	}

	private void createNewCustomer()
	{
		rand = new Random();
		Customer customer;
		Chair targetChair = null;

		// Pick a target chair
		// For now just first available seat in all tables
		// TODO: ADD A DYNAMIC SYSTEM FOR PICKING CHAIR
		foreach(Table table in tables)
		{
			if (targetChair != null) break;
			foreach(Chair chair in table.GetChairs())
			{
				if (!chair.isOccupied()) 
					targetChair = chair;
			}
		}

		int roll = rand.Next(1, 11);

		if (roll > wantedFrequency)
		{
			customer = new Customer(targetChair);
			// CUSTOMERS NEED TO BE JUST RANDOMIZED CAN'T BE WANTED LOOK
		}
		else
		{
			customer = new Wanted(targetChair);
			// WANTED ENEMIES NEED TO MATCH ONE OF THE POSTERS
		}
        activeCustomers.Add(customer);
		customer.Exit += onCustomerExit;
    }

	private void setWantedAttributes()
	{
		// Creates the attributes to match the wanted customers on the posters
	}

	private void onCustomerExit(bool wasWanted, Customer customerInstance)
	{
		if(wasWanted) { handleWantedCustomer(); }
		else { totalSatisfation += customerInstance.getSatisfaction(); }

		GD.Print($"A {wasWanted} customer has exited the saloon");
		customerInstance.Exit -= onCustomerExit;
		
		if(activeCustomers.Contains(customerInstance))
		{
			activeCustomers.Remove(customerInstance);
		}
	}

	// Handles wanted customer exiting saloon
	private void handleWantedCustomer()
	{

	}
}
