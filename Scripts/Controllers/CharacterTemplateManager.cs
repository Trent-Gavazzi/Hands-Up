using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CharacterTemplateManager : Node
{
    // Generation settings for how many wanted there should be at a time
    [Export] public int numWanted = 1;
    [Export] private Color[] colorTypes;

    public List<VisualAttributes> wantedAttributeList = new List<VisualAttributes>();
    public List<VisualAttributes> unusedWantedAttributes = new List<VisualAttributes>(); // Not currently in the saloon

    public override void _Ready()
    {
        if (colorTypes == null || colorTypes.Length == 0)
        {
            GD.PushError($"{Name}: colorTypes is empty, cannot generate customers.");
            return;
        }

        // At least one look must stay free for innocent customers, or getNonWanted can never finish
        // When more attributes are added, this becomes the product of each attribute's distinct options
        int distinctLooks = colorTypes.Distinct().Count();
        if (numWanted >= distinctLooks)
        {
            GD.PushError($"{Name}: numWanted ({numWanted}) must be less than the number of distinct looks ({distinctLooks}). Clamping.");
            numWanted = distinctLooks - 1;
        }

        // Generate unique wanted looks
        while (wantedAttributeList.Count < numWanted)
        {
            VisualAttributes n = generateRandomVisualAttribute();
            if (isWantedLook(n)) continue;

            wantedAttributeList.Add(n);
            unusedWantedAttributes.Add(n);
        }
    }

    // A random look that doesn't match any wanted poster
    public VisualAttributes getNonWanted()
    {
        VisualAttributes n;
        do
        {
            n = generateRandomVisualAttribute();
        }
        while (isWantedLook(n));

        return n;
    }

    // Takes a wanted look that isn't already in the saloon. Returns false if none are free.
    public bool tryGetWanted(out VisualAttributes chosen)
    {
        if (unusedWantedAttributes.Count == 0)
        {
            chosen = default;
            return false;
        }

        int randomIndex = GD.RandRange(0, unusedWantedAttributes.Count - 1);
        chosen = unusedWantedAttributes[randomIndex];
        unusedWantedAttributes.RemoveAt(randomIndex);
        return true;
    }

    // Puts a wanted look back in the pool (e.g. they escaped and can come back later)
    public void returnWanted(VisualAttributes v)
    {
        bool isWanted = isWantedLook(v);
        bool alreadyFree = unusedWantedAttributes.Exists(u => compareAttributesForSame(u, v));

        if (isWanted && !alreadyFree)
        {
            unusedWantedAttributes.Add(v);
        }
    }

    public bool isWantedLook(VisualAttributes v)
    {
        return wantedAttributeList.Exists(w => compareAttributesForSame(w, v));
    }

    // Returns true if they look the same, false otherwise
    public bool compareAttributesForSame(VisualAttributes v1, VisualAttributes v2)
    {
        // Compare every attribute here as more are added
        return v1.color == v2.color;
    }

    public VisualAttributes generateRandomVisualAttribute()
    {
        return new VisualAttributes
        {
            color = colorTypes[GD.Randi() % (uint)colorTypes.Length]
        };
    }
}

public struct VisualAttributes
{
    // Add any visual attributes here that they have
    public Color color;
}