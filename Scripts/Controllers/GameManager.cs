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

	private int wantedFrequency;
	private ulong lastCustomerTime; // Last time a customer was allowed in

    private List<Chair> chairs;
    private List<Customer> activeCustomers = new List<Customer>();
	
	// Add some variable for wanted attributes that varies from scene to scene

    private Random rand = new Random();

    public override void _Ready()
	{ 
        chairs = GetTree().GetNodesInGroup("Chairs").OfType<Chair>().ToList();
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

		int roll = rand.Next(1, 11);

		if (roll > wantedFrequency)
		{
			customer = new Customer();
			// CUSTOMERS NEED TO BE JUST RANDOMIZED CAN'T BE WANTED LOOK
		}
		else
		{
			customer = new Wanted();
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
