using Godot;
using System;
using System.Collections.Generic;

public partial class CharacterTemplateManager : Node
{
    // Generation Settings for How many Wanted There Should be At a time
    [Export] public int numWanted = 3;
    public List<VisualAttributes> wantedAttributeList = new List<VisualAttributes>();
    public List<VisualAttributes> unusedWantedAttributes = new List<VisualAttributes>();

    [Export] private Color[] colorTypes;

    public override void _Ready()
    {
        // Randomize our wanted people
        for (int i = 0; i < numWanted; i++)
        {
            // Generate a visual attribute, and then put it into the wanted list
            VisualAttributes n = generateRandomVisualAttribute();
            wantedAttributeList.Add(n);
            unusedWantedAttributes.Add(n);
        }
    }

    public VisualAttributes getNonWanted()
    {
        VisualAttributes n = new VisualAttributes();
        bool done = false;
        while(!done)
        {
            n = generateRandomVisualAttribute();
            done = true;
            foreach (VisualAttributes visualAttributes in wantedAttributeList)
            {
                if(compareAttributesForSame(visualAttributes, n))
                {
                    done = false;
                    break;
                }
            }
        }
        return n;
    }

    public VisualAttributes getWanted() 
    {
        int randomIndex = GD.RandRange(0, unusedWantedAttributes.Count - 1);
        VisualAttributes chosen = unusedWantedAttributes[randomIndex];
        unusedWantedAttributes.RemoveAt(randomIndex);

        return chosen;
    }

    public bool compareAttributesForSame(VisualAttributes v1, VisualAttributes v2)
    {
        // Returns true if they are the same, false other wise
        if (v1.color == v2.color)
        {
            return false;
        }

        return false;
    }

    public VisualAttributes generateRandomVisualAttribute()
    {
        return new VisualAttributes
        {
            color = colorTypes[GD.Randi() % colorTypes.Length]
        };
    }
}

public struct VisualAttributes
{
    // Add any visual attributes here that they have
    public Color color;
}
