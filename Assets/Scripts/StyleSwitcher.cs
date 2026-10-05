using UnityEngine;

// HW2 interactivity: press Space to toggle between "Day paper" and "Night print" styles.
// - swaps every Toon / ToonBob material in the scene for its night-palette version
// - flips the global _StyleMode so the full-screen post process switches to a halftone print look
public class StyleSwitcher : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.Space;

    [Header("Day style")]
    public Material dayToon;
    public Material dayHero;

    [Header("Night style")]
    public Material nightToon;
    public Material nightHero;

    static readonly int StyleModeID = Shader.PropertyToID("_StyleMode");
    bool night;

    void Start()
    {
        Shader.SetGlobalFloat(StyleModeID, 0f);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            night = !night;
            Apply();
        }
    }

    void Apply()
    {
        foreach (Renderer r in FindObjectsOfType<Renderer>())
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                mats[i] = Swap(mats[i]);
            r.sharedMaterials = mats;
        }
        Shader.SetGlobalFloat(StyleModeID, night ? 1f : 0f);
    }

    Material Swap(Material m)
    {
        if (night)
        {
            if (m == dayToon) return nightToon;
            if (m == dayHero) return nightHero;
        }
        else
        {
            if (m == nightToon) return dayToon;
            if (m == nightHero) return dayHero;
        }
        return m;
    }

    void OnDisable()
    {
        Shader.SetGlobalFloat(StyleModeID, 0f);
    }
}
