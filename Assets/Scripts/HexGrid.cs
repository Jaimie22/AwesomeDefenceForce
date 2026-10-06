using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

// ============================================================================
// HexGrid
// ----------------------------------------------------------------------------
// Put this on an empty GameObject called "HexGrid". It's the board manager.
//
// IN THE EDITOR:
//   Right-click the HexGrid component header > "Generate Map"
//   to build a fresh board of tiles. They stay in your scene, so you can
//   paint terrain, delete tiles, and save the map with the scene.
//
// DURING PLAY:
//   It finds all the tiles under it and keeps a list of them, so it can
//   look tiles up by address and work out movement ranges.
// ============================================================================

public class HexGrid : MonoBehaviour
{
    // The two ways a hex can sit. Shows as a dropdown in the Inspector.
    public enum Orientation { PointyTop, FlatTop }

    // ------------------------------------------------------------------------
    // Inspector settings (these are only used when you click Generate Map)
    // ------------------------------------------------------------------------
    [Header("Map Size")]
    [Min(1)] public int width = 10;    // tiles across
    [Min(1)] public int height = 8;    // tiles deep

    [Header("Tiles")]

    // Your HexTile prefab.
    public HexTile tilePrefab;

    // The terrain every tile starts as when the map is generated (e.g. Ground).
    public TerrainType defaultTerrain;

    [Header("Layout")]

    // Distance from the centre of a hex to a corner. Match your model.
    [Min(0.01f)] public float hexSize = 1f;

    // Spacing between tiles. 0 = tiles touch.
    [Min(0f)] public float gap = 0.05f;

    public Orientation orientation = Orientation.PointyTop;

    // ------------------------------------------------------------------------
    // Private stuff
    // ------------------------------------------------------------------------

    // The "phone book": look up any tile by its address.
    readonly Dictionary<HexCoord, HexTile> tiles = new Dictionary<HexCoord, HexTile>();

    // Square root of 3, used in the hex maths. Worked out once.
    static readonly float Sqrt3 = Mathf.Sqrt(3f);

    // ------------------------------------------------------------------------
    // Awake: runs once when the game starts.
    // ------------------------------------------------------------------------
    void Awake()
    {
        RegisterTiles();
    }

    // ------------------------------------------------------------------------
    // RegisterTiles
    // ------------------------------------------------------------------------
    // Finds every tile sitting under the HexGrid and adds it to the phone book.
    // Because it only registers tiles that exist, any tile you deleted
    // simply isn't part of the map. That's how you make odd-shaped maps.
    public void RegisterTiles()
    {
        tiles.Clear();

        foreach (HexTile tile in GetComponentsInChildren<HexTile>())
        {
            // Two tiles with the same address would confuse everything,
            // e.g. if you duplicated a tile by accident. Warn and skip it.
            if (tiles.ContainsKey(tile.coord))
            {
                Debug.LogWarning($"HexGrid: two tiles share address {tile.coord}. Skipping {tile.name}.", tile);
                continue;
            }

            tiles.Add(tile.coord, tile);
        }
    }

    // ------------------------------------------------------------------------
    // CoordToWorld
    // ------------------------------------------------------------------------
    // Converts a hex address (q, r) into a real position in the scene.
    public Vector3 CoordToWorld(HexCoord c)
    {
        float s = hexSize + gap;
        float x, z;

        if (orientation == Orientation.PointyTop)
        {
            x = s * (Sqrt3 * c.q + Sqrt3 / 2f * c.r);
            z = s * (1.5f * c.r);
        }
        else
        {
            x = s * (1.5f * c.q);
            z = s * (Sqrt3 / 2f * c.q + Sqrt3 * c.r);
        }

        // TransformPoint means moving/rotating HexGrid moves the whole board.
        return transform.TransformPoint(new Vector3(x, 0f, z));
    }

    // ------------------------------------------------------------------------
    // GetTile: find a tile by address. Returns null if there isn't one.
    // ------------------------------------------------------------------------
    public HexTile GetTile(HexCoord c)
    {
        tiles.TryGetValue(c, out HexTile tile);
        return tile;
    }

    // ------------------------------------------------------------------------
    // GetNeighbours: all tiles touching this address (up to 6).
    // ------------------------------------------------------------------------
    public List<HexTile> GetNeighbours(HexCoord c)
    {
        List<HexTile> result = new List<HexTile>();

        for (int d = 0; d < 6; d++)
        {
            HexTile n = GetTile(c.Neighbour(d));
            if (n != null) result.Add(n);
        }

        return result;
    }

    // ------------------------------------------------------------------------
    // GetReachable
    // ------------------------------------------------------------------------
    // Every tile a unit could move to from 'start' with 'movePoints'.
    // Like pouring water: it spreads out from the start, can't enter
    // unwalkable tiles, costs more to cross rough terrain, and stops
    // when it runs out of move points.
    public List<HexTile> GetReachable(HexCoord start, int movePoints)
    {
        // Cheapest known cost to reach each tile. The start costs nothing.
        Dictionary<HexCoord, int> costSoFar = new Dictionary<HexCoord, int> { [start] = 0 };

        // Tiles still waiting to spread outward.
        List<HexCoord> frontier = new List<HexCoord> { start };

        while (frontier.Count > 0)
        {
            // Spread from the cheapest waiting tile first.
            int best = 0;
            for (int i = 1; i < frontier.Count; i++)
                if (costSoFar[frontier[i]] < costSoFar[frontier[best]]) best = i;

            HexCoord current = frontier[best];
            frontier.RemoveAt(best);

            // Check all six neighbours.
            for (int d = 0; d < 6; d++)
            {
                HexCoord next = current.Neighbour(d);

                // No tile there, or can't walk on it? Skip.
                if (!tiles.TryGetValue(next, out HexTile tile) || !tile.Walkable) continue;

                // Cost to step onto it.
                int newCost = costSoFar[current] + tile.MoveCost;

                // Can't afford it? Skip.
                if (newCost > movePoints) continue;

                // Already found a route there that's as cheap or cheaper? Skip.
                if (costSoFar.TryGetValue(next, out int oldCost) && oldCost <= newCost) continue;

                // New or better route found: remember it and keep spreading.
                costSoFar[next] = newCost;
                frontier.Add(next);
            }
        }

        // Turn the addresses into tiles, leaving out the starting tile.
        List<HexTile> result = new List<HexTile>();
        foreach (HexCoord c in costSoFar.Keys)
            if (!c.Equals(start)) result.Add(tiles[c]);

        return result;
    }

#if UNITY_EDITOR
    // ========================================================================
    // EDITOR-ONLY TOOLS (these don't exist in the finished game)
    // ========================================================================

    // ------------------------------------------------------------------------
    // Right-click menu option: "Generate Map"
    // ------------------------------------------------------------------------
    [ContextMenu("Generate Map")]
    void GenerateMap()
    {
        // This is for building maps in the editor, not during Play.
        if (Application.isPlaying)
        {
            Debug.LogWarning("HexGrid: generate the map outside of Play mode.");
            return;
        }

        if (tilePrefab == null)
        {
            Debug.LogWarning("HexGrid: no tile prefab assigned.");
            return;
        }

        // Find the existing tiles (if any).
        HexTile[] oldTiles = GetComponentsInChildren<HexTile>();

        // If there's already a map, double-check before wiping it!
        if (oldTiles.Length > 0)
        {
            bool sure = EditorUtility.DisplayDialog(
                "Generate Map",
                "This will DELETE the current map and build a fresh one. Are you sure?",
                "Yes, rebuild",
                "Cancel");

            if (!sure) return;
        }

        // Delete the old tiles. (Only tiles, so other children are left alone.)
        foreach (HexTile t in oldTiles)
            DestroyImmediate(t.gameObject);

        // Build the new board, row by row (or column by column).
        // The "offset" keeps the map a neat rectangle.
        if (orientation == Orientation.PointyTop)
        {
            for (int r = 0; r < height; r++)
            {
                int offset = Mathf.FloorToInt(r / 2f);
                for (int q = -offset; q < width - offset; q++)
                    SpawnTile(new HexCoord(q, r));
            }
        }
        else
        {
            for (int q = 0; q < width; q++)
            {
                int offset = Mathf.FloorToInt(q / 2f);
                for (int r = -offset; r < height - offset; r++)
                    SpawnTile(new HexCoord(q, r));
            }
        }

        // Tell Unity the scene has changed so it asks you to save.
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    // ------------------------------------------------------------------------
    // SpawnTile: creates one tile at an address, in the editor.
    // ------------------------------------------------------------------------
    void SpawnTile(HexCoord c)
    {
        // Create the tile as a proper prefab copy (keeps the prefab link,
        // so editing the HexTile prefab later updates every tile).
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab.gameObject, transform);

        // Put it in the right place, facing the same way as the grid.
        go.transform.position = CoordToWorld(c);
        go.transform.rotation = transform.rotation;

        // Readable name in the Hierarchy, e.g. "Hex (2, 3)".
        go.name = $"Hex {c}";

        // Give the tile its address and starting terrain, then build its model.
        HexTile tile = go.GetComponent<HexTile>();
        tile.coord = c;
        tile.terrain = defaultTerrain;
        tile.ApplyTerrain();
    }

    // ------------------------------------------------------------------------
    // Right-click menu option: "Refresh All Tiles"
    // ------------------------------------------------------------------------
    // Rebuilds every tile's model. Use this after changing a TerrainType's
    // settings (like its height) so the whole map catches up.
    [ContextMenu("Refresh All Tiles")]
    void RefreshAllTiles()
    {
        foreach (HexTile tile in GetComponentsInChildren<HexTile>())
            tile.ApplyTerrain();

        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif
}