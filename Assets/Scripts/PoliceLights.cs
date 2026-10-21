using UnityEngine;

public class PoliceLights : MonoBehaviour
{
    // The different flashing styles you can pick from in the Inspector.
    public enum FlashPattern
    {
        Alternate,      // red, blue, red, blue...
        DoubleFlash,    // red-red, blue-blue (the classic police look)
        Together        // both flash on and off at the same time
    }

    [Header("Lights")]

    // Your red Point Light.
    public Light redLight;

    // Your blue Point Light.
    public Light blueLight;

    [Header("Optional Glowing Lenses")]

    // A small object that glows red when the red light is on (can be empty).
    public Renderer redLens;

    // A small object that glows blue when the blue light is on (can be empty).
    public Renderer blueLens;

    // How bright the lenses glow. Higher = brighter.
    [Min(0f)] public float lensGlow = 4f;

    // The name of the glow setting in the material.
    // URP = "_EmissionColor". HDRP = "_EmissiveColor".
    public string emissionProperty = "_EmissionColor";

    [Header("Flashing")]

    // Pick the flash style from the dropdown.
    public FlashPattern pattern = FlashPattern.DoubleFlash;

    // How long one full round of the pattern takes, in seconds.
    // Smaller = faster flashing.
    [Min(0.05f)] public float cycleTime = 0.8f;

    // Are the lights flashing? Untick to switch the siren lights off.
    public bool lightsOn = true;

    // Starts each car at a random point in its pattern, so if you have
    // several police cars they don't all flash in perfect sync.
    public bool randomStart = true;


    static readonly int[] alternateRed = { 1, 0 };
    static readonly int[] alternateBlue = { 0, 1 };

    static readonly int[] doubleRed = { 1, 0, 1, 0, 0, 0, 0, 0 };
    static readonly int[] doubleBlue = { 0, 0, 0, 0, 1, 0, 1, 0 };

    static readonly int[] togetherRed = { 1, 0 };
    static readonly int[] togetherBlue = { 1, 0 };


    float timer;                       // counts up to keep track of the pattern
    MaterialPropertyBlock block;       // lets us change one lens's glow only

    void Start()
    {
        block = new MaterialPropertyBlock();

        // Jump to a random point in the pattern, if ticked.
        if (randomStart) timer = Random.Range(0f, cycleTime);
    }

    void Update()
    {
        // Lights switched off? Make sure everything is dark and stop here.
        if (!lightsOn)
        {
            SetRed(false);
            SetBlue(false);
            return;
        }

        // Pick the right pair of slot lists for the chosen pattern.
        int[] red, blue;
        switch (pattern)
        {
            case FlashPattern.Alternate:
                red = alternateRed; blue = alternateBlue; break;
            case FlashPattern.Together:
                red = togetherRed; blue = togetherBlue; break;
            default:
                red = doubleRed; blue = doubleBlue; break;
        }

        // Move the timer on, looping it back round after each cycle.
        timer = (timer + Time.deltaTime) % cycleTime;

        // Work out which slot we're in right now.
        // e.g. halfway through a cycle with 8 slots = slot 4.
        int slot = Mathf.FloorToInt(timer / cycleTime * red.Length) % red.Length;

        // Switch each light on or off to match that slot.
        SetRed(red[slot] == 1);
        SetBlue(blue[slot] == 1);
    }

  
    // Switch the red light (and its lens) on or off.
   
    void SetRed(bool on)
    {
        if (redLight != null && redLight.enabled != on) redLight.enabled = on;
        SetLens(redLens, on, redLight != null ? redLight.color : Color.red);
    }

   
    // Switch the blue light (and its lens) on or off.
  
    void SetBlue(bool on)
    {
        if (blueLight != null && blueLight.enabled != on) blueLight.enabled = on;
        SetLens(blueLens, on, blueLight != null ? blueLight.color : Color.blue);
    }

  
    // Make a lens glow (or go dark).
    
    void SetLens(Renderer lens, bool on, Color colour)
    {
        // No lens assigned? Nothing to do.
        if (lens == null) return;

        // Glowing colour when on, black (no glow) when off.
        Color glow = on ? colour * lensGlow : Color.black;

        lens.GetPropertyBlock(block);
        block.SetColor(emissionProperty, glow);
        lens.SetPropertyBlock(block);
    }

    
    // Other scripts can call this to switch the lights on or off,
    // e.g. when a police car arrives on the scene.
   
    public void SetLightsOn(bool on)
    {
        lightsOn = on;
    }
}
