using UnityEngine;

// ============================================================================
// HexCoord
// ----------------------------------------------------------------------------
// This is NOT a component. You never drag it onto a GameObject.
// It's just a little "address label" for a hex tile, the same way a house
// has a street number. Every hex gets two numbers: q and r.
//
// Using these two numbers makes it really easy to find neighbours and
// measure distances, which is what a turn-based game needs constantly.
// ============================================================================

[System.Serializable]
public struct HexCoord : System.IEquatable<HexCoord>
{
    // The two numbers that make up the hex's address.
    public int q;
    public int r;

    // How to make a new address, e.g. new HexCoord(2, 3).
    public HexCoord(int q, int r)
    {
        this.q = q;
        this.r = r;
    }

    // ------------------------------------------------------------------------
    // The six directions
    // ------------------------------------------------------------------------
    // Every hex has six neighbours. To get to one, add one of these
    // offsets to the address. Listed in order going round the hex.
    public static readonly HexCoord[] Directions =
    {
        new HexCoord(1, 0),   // direction 0
        new HexCoord(1, -1),  // direction 1
        new HexCoord(0, -1),  // direction 2
        new HexCoord(-1, 0),  // direction 3
        new HexCoord(-1, 1),  // direction 4
        new HexCoord(0, 1)    // direction 5
    };

    // ------------------------------------------------------------------------
    // Neighbour: the address of the hex next door in a direction (0 to 5).
    // ------------------------------------------------------------------------
    public HexCoord Neighbour(int direction)
    {
        HexCoord d = Directions[direction];
        return new HexCoord(q + d.q, r + d.r);
    }

    // ------------------------------------------------------------------------
    // Distance: how many steps from A to B, ignoring obstacles.
    // Handy for things like "is the enemy within 3 tiles?"
    // ------------------------------------------------------------------------
    public static int Distance(HexCoord a, HexCoord b)
    {
        int dq = a.q - b.q;
        int dr = a.r - b.r;
        return (Mathf.Abs(dq) + Mathf.Abs(dq + dr) + Mathf.Abs(dr)) / 2;
    }

    // ------------------------------------------------------------------------
    // Behind-the-scenes plumbing: lets the computer compare addresses
    // and use them in lookup tables. Safe to ignore.
    // ------------------------------------------------------------------------
    public bool Equals(HexCoord other) => q == other.q && r == other.r;
    public override bool Equals(object obj) => obj is HexCoord other && Equals(other);
    public override int GetHashCode() => (q * 397) ^ r;

    // Makes it print nicely in the Console, e.g. "(2, 3)".
    public override string ToString() => $"({q}, {r})";
}