using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;                    // editor-only tools (not included in the final game)
using UnityEditor.SceneManagement;
#endif



public class HexTile : MonoBehaviour
{

    [Header("Terrain")]

    // What kind of ground this is. Drag a TerrainType asset in here.
    public TerrainType terrain;

    [Header("Parts (set these up in the prefab)")]

    // An EMPTY child object. The terrain's 3D model gets placed inside it.
    // Keep it empty in the prefab; the script fills it in.
    public Transform visualHolder;

    // The see-through overlay that shows when the tile is in range.
    public GameObject highlight;

    // How high the overlay floats above the tile's top surface,
    // so it doesn't flicker by fighting with the ground.
    [Min(0f)] public float highlightLift = 0.02f;

    [Header("Address (filled in by HexGrid, don't edit)")]

    // This tile's position on the board. HexGrid sets it when generating.
    public HexCoord coord;

    // Remembers which terrain was last built, so the tile only rebuilds
    // when you actually change it. Hidden, as it's just bookkeeping.
    [SerializeField, HideInInspector] TerrainType lastTerrain;


    // "=>" is a shortcut meaning "work this out whenever someone asks".

    // Can a unit stand here? (No terrain set = not walkable, to be safe.)
    public bool Walkable => terrain != null && terrain.walkable;

    // What does it cost to step onto this tile? (Defaults to 1 if no terrain.)
    public int MoveCost => terrain != null ? terrain.moveCost : 1;


    void Awake()
    {
        SetHighlight(false);
    }

    // SetHighlight(true) shows the overlay, SetHighlight(false) hides it.
    public void SetHighlight(bool on)
    {
        if (highlight != null) highlight.SetActive(on);
    }

    // A shortcut that just means "turn the highlight off".
    public void ResetColour()
    {
        SetHighlight(false);
    }


    // Throws away the old 3D model and builds the one for the current terrain.
    public void ApplyTerrain()
    {
        // Can't do anything without somewhere to put the model.
        if (visualHolder == null) return;

       
        // We count backwards so removing items doesn't mess up the counting.
        for (int i = visualHolder.childCount - 1; i >= 0; i--)
        {
            GameObject child = visualHolder.GetChild(i).gameObject;

            // During Play we use Destroy. In the editor we need DestroyImmediate.
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }

        // Remember what we built (used by the "only rebuild if changed" check).
        lastTerrain = terrain;

       
        if (terrain == null || terrain.visualPrefab == null)
        {
            MarkChanged();
            return;
        }

       
        GameObject visual;
#if UNITY_EDITOR
        // In the editor, keep the link to the original prefab, so if you
        // later edit your Ground model, every ground tile updates too.
        if (!Application.isPlaying)
            visual = (GameObject)PrefabUtility.InstantiatePrefab(terrain.visualPrefab, visualHolder);
        else
#endif
            visual = Instantiate(terrain.visualPrefab, visualHolder);

        
        // (We ADD to the model's own position so your prefab setup is kept.)
        visual.transform.localPosition += new Vector3(0f, terrain.heightOffset, 0f);

       
        // times 60 degrees gives one of the six angles a hex can face.
        if (terrain.randomRotation)
        {
            float spin = Random.Range(0, 6) * 60f;
            visual.transform.localRotation = Quaternion.Euler(0f, spin, 0f) * visual.transform.localRotation;
        }

        // Step 6: move the highlight overlay to sit on top of the new height.
        if (highlight != null)
            highlight.transform.localPosition = new Vector3(0f, terrain.heightOffset + highlightLift, 0f);

        MarkChanged();
    }


    // Tells Unity "this tile changed, please save it with the scene".
    // Without this, changes made by code can get lost when you save.
    void MarkChanged()
    {
#if UNITY_EDITOR
        if (Application.isPlaying) return;
        PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        if (highlight != null) PrefabUtility.RecordPrefabInstancePropertyModifications(highlight.transform);
        if (gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

#if UNITY_EDITOR

    // Right-click the HexTile component header to force a rebuild,
    // e.g. after changing a TerrainType's height, or to re-roll its rotation.
    [ContextMenu("Reapply Terrain")]
    void ReapplyTerrainMenu()
    {
        ApplyTerrain();
    }


    // Unity runs this automatically whenever you change something in the
    // Inspector. We use it to rebuild the tile when you swap its terrain.
    void OnValidate()
    {
        // Don't do this during Play mode.
        if (Application.isPlaying) return;

        // Don't do this on the prefab file itself, only on tiles in the scene.
        if (PrefabUtility.IsPartOfPrefabAsset(this)) return;

        // Don't do this while you're editing the prefab in Prefab Mode,
        // otherwise a model would get baked into the prefab by accident.
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.IsPartOfPrefabContents(gameObject)) return;

        // Only rebuild if the terrain actually changed.
        if (terrain == lastTerrain) return;

        // Unity doesn't allow creating or deleting objects inside OnValidate,
        // so we ask it to do the rebuild a split second later instead.
        EditorApplication.delayCall += () =>
        {
            if (this == null) return; 
            ApplyTerrain();
        };
    }
#endif
}
