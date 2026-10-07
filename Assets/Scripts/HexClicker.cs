using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;   

public class HexClicker : MonoBehaviour
{

    // The board. If you leave this empty, it finds HexGrid on the same object.
    public HexGrid grid;

    // The camera we click through. If empty, it uses the Main Camera.
    public Camera cam;

    // How many move points our pretend unit has.
    [Min(0)] public int movePoints = 3;

    // Which layers the click can hit. "Everything" by default.
    public LayerMask tileLayers = ~0;

    // Remembers which tiles are currently lit up, so we can switch
    // them off again before lighting up a new set.
    readonly List<HexTile> highlighted = new List<HexTile>();

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (grid == null) grid = GetComponent<HexGrid>();
    }

    void Update()
    {
        // If there's no mouse, or the left button wasn't JUST clicked
        // this frame, do nothing and wait for the next frame.
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

        // Fire an invisible laser (a "ray") from the camera, through the
        // mouse pointer, into the scene.
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Did the laser hit anything within 500 units? If not, stop here.
        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, tileLayers)) return;

        // The laser hits the hex model, so we look UP through its parents
        // to find the HexTile script. If there isn't one, it wasn't a tile.
        HexTile tile = hit.collider.GetComponentInParent<HexTile>();
        if (tile == null) return;

        // Switch off the tiles that were lit up from the last click.
        foreach (HexTile t in highlighted) t.ResetColour();
        highlighted.Clear();

        // Ask the grid which tiles are reachable from the clicked tile,
        // then light them all up.
        highlighted.AddRange(grid.GetReachable(tile.coord, movePoints));
        foreach (HexTile t in highlighted) t.SetHighlight(true);

        // Print a message in the Console so you can see what happened.
        Debug.Log($"Clicked {tile.coord}: {highlighted.Count} tiles in range");
    }
}
