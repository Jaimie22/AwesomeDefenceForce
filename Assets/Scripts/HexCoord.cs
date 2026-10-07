using UnityEngine;



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


    public HexCoord Neighbour(int direction)
    {
        HexCoord d = Directions[direction];
        return new HexCoord(q + d.q, r + d.r);
    }


    public static int Distance(HexCoord a, HexCoord b)
    {
        int dq = a.q - b.q;
        int dr = a.r - b.r;
        return (Mathf.Abs(dq) + Mathf.Abs(dq + dr) + Mathf.Abs(dr)) / 2;
    }


    public bool Equals(HexCoord other) => q == other.q && r == other.r;
    public override bool Equals(object obj) => obj is HexCoord other && Equals(other);
    public override int GetHashCode() => (q * 397) ^ r;

    // Makes it print nicely in the Console, e.g. "(2, 3)".
    public override string ToString() => $"({q}, {r})";
}
