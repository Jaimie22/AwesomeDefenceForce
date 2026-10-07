using UnityEngine;



// Adds "Hex > Terrain Type" to the right-click Create menu.
[CreateAssetMenu(fileName = "NewTerrain", menuName = "Hex/Terrain Type")]
public class TerrainType : ScriptableObject
{
    [Header("Info")]

    // A friendly name, handy later for UI.
    public string displayName = "Ground";

    [Header("Gameplay")]

    // Can units stand on this? Untick for water, cliffs, walls, etc.
    public bool walkable = true;

    // How many move points it costs to step onto this terrain.
    [Min(1)] public int moveCost = 1;

    [Header("Visuals")]

    // The 3D model for this terrain (e.g. your Visual_Ground prefab).
    public GameObject visualPrefab;

    // Raises or lowers the tile. Positive = hills, negative = water or pits.
    [Tooltip("Raise (+) or lower (-) this terrain, e.g. 0.3 for hills, -0.15 for water")]
    public float heightOffset = 0f;

    // If ticked, each tile is spun a random amount (in 60 degree steps)
    // so the map doesn't look copy-pasted.
    public bool randomRotation = true;
}
