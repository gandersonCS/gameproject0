using System;

namespace MechaHorde.Gameplay;

/// <summary>
/// The player's Scraplight: the currency they collect from destroyed robots and spend between waves.
/// </summary>
/// <remarks>
/// Scraplight comes in two kinds:
/// <list type="bullet">
/// <item><b>Scrap</b>: the broken metal parts (gears, propellers) that fly off robots as they
/// are shot and when they are destroyed. Common.</item>
/// <item><b>Sparks</b>: the glowing blocks, the upgrade parts robots carry. Rare.</item>
/// </list>
/// What each is spent on is decided by the upgrade screen between waves (not built yet).
/// </remarks>
public class Scraplight
{
    /// <summary>The scrap the player is carrying</summary>
    public int Scrap { get; private set; }

    /// <summary>The sparks the player is carrying</summary>
    public int Sparks { get; private set; }

    /// <summary>
    /// Raised whenever the counts change, with how much each changed by (negative when spent),
    /// e.g. for a HUD flash or a pickup sound
    /// </summary>
    public event Action<int, int> Changed;

    /// <summary>
    /// Adds a collected pickup
    /// </summary>
    public void Collect(PickupKind kind, int amount = 1)
    {
        if (amount <= 0)
            return;
        if (kind == PickupKind.Spark)
            Add(0, amount);
        else
            Add(amount, 0);
    }

    /// <summary>
    /// Adds Scraplight
    /// </summary>
    public void Add(int scrap, int sparks)
    {
        scrap = Math.Max(0, scrap);
        sparks = Math.Max(0, sparks);
        if (scrap == 0 && sparks == 0)
            return;
        Scrap += scrap;
        Sparks += sparks;
        Changed?.Invoke(scrap, sparks);
    }

    /// <summary>
    /// Spends Scraplight, if the player has enough of both
    /// </summary>
    /// <returns>Whether it was spent</returns>
    public bool TrySpend(int scrap, int sparks)
    {
        if (scrap < 0 || sparks < 0 || scrap > Scrap || sparks > Sparks)
            return false;
        Scrap -= scrap;
        Sparks -= sparks;
        if (scrap > 0 || sparks > 0)
            Changed?.Invoke(-scrap, -sparks);
        return true;
    }
}

/// <summary>
/// The kinds of Scraplight pickup
/// </summary>
public enum PickupKind
{
    /// <summary>A broken gear (scrap)</summary>
    ScrapGear,
    /// <summary>A broken propeller (scrap)</summary>
    ScrapPropeller,
    /// <summary>A glowing upgrade block (a spark)</summary>
    Spark,
}
