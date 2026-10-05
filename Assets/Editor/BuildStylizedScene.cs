using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Menu: HW2 > Build Stylized Forest Scene
// Builds a small clover-forest clearing (inspired by the stefscribbles concept art) out of primitives,
// applies the toon materials, sets up lights, camera turntable and the Space-key style switcher.
public static class BuildStylizedScene
{
    const string M = "Assets/Materials/";
    static Material Mat(string n) => AssetDatabase.LoadAssetAtPath<Material>(M + n + ".mat");

    [MenuItem("HW2/Build Stylized Forest Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Material ground = Mat("Default"), leaf = Mat("ToonLeaf"), rock = Mat("ToonRock"),
                 flower = Mat("ToonFlower"), hero = Mat("ToonBob");

        var root = new GameObject("Forest Clearing").transform;

        // ground
        var g = Prim(PrimitiveType.Cylinder, "Ground", root, new Vector3(0, -0.05f, 0), Vector3.zero, new Vector3(9, 0.05f, 9), ground);

        // clover bushes ring the clearing
        Random.InitState(7);
        for (int i = 0; i < 9; i++)
        {
            float a = i / 9f * Mathf.PI * 2f + 0.3f;
            float r = 3.1f + Random.Range(-0.3f, 0.4f);
            var bush = new GameObject("Clover Bush " + i).transform;
            bush.SetParent(root);
            bush.localPosition = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
            for (int k = 0; k < 3; k++)
            {
                float ka = k / 3f * Mathf.PI * 2f;
                float s = Random.Range(0.6f, 0.95f);
                Prim(PrimitiveType.Sphere, "Leaf " + k, bush,
                    new Vector3(Mathf.Cos(ka) * 0.32f, 0.25f + k * 0.05f, Mathf.Sin(ka) * 0.32f),
                    new Vector3(0, ka * Mathf.Rad2Deg, 0), new Vector3(s, s * 0.6f, s), (i % 3 == 0) ? leaf : ground);
            }
        }

        // trees at the back
        Tree(root, new Vector3(-2.2f, 0, 2.4f), 1.2f, rock, leaf);
        Tree(root, new Vector3(2.6f, 0, 1.8f), 1.0f, rock, leaf);
        Tree(root, new Vector3(0.6f, 0, 3.6f), 1.4f, rock, leaf);

        // rocks
        Prim(PrimitiveType.Cube, "Rock A", root, new Vector3(-1.6f, 0.2f, -1.2f), new Vector3(15, 30, 10), new Vector3(0.8f, 0.5f, 0.7f), rock);
        Prim(PrimitiveType.Cube, "Rock B", root, new Vector3(1.9f, 0.15f, -1.6f), new Vector3(-10, 55, 20), new Vector3(0.5f, 0.35f, 0.6f), rock);
        Prim(PrimitiveType.Sphere, "Rock C", root, new Vector3(-0.9f, 0.12f, 2.0f), Vector3.zero, new Vector3(0.7f, 0.35f, 0.6f), rock);

        // flowers
        Vector3[] fp = { new Vector3(1.7f, 0, 1.1f), new Vector3(-1.8f, 0, 0.6f), new Vector3(0.6f, 0, -2.1f), new Vector3(-2.4f, 0, -0.6f), new Vector3(2.4f, 0, -0.2f) };
        for (int i = 0; i < fp.Length; i++)
        {
            var f = new GameObject("Flower " + i).transform; f.SetParent(root); f.localPosition = fp[i]; f.localScale = Vector3.one * 1.8f;
            Prim(PrimitiveType.Cylinder, "Stem", f, new Vector3(0, 0.15f, 0), Vector3.zero, new Vector3(0.05f, 0.15f, 0.05f), leaf);
            for (int k = 0; k < 5; k++)
            {
                float ka = k / 5f * 360f;
                var p = Prim(PrimitiveType.Sphere, "Petal " + k, f, Quaternion.Euler(0, ka, 0) * new Vector3(0.12f, 0.32f, 0), new Vector3(0, ka, 0), new Vector3(0.18f, 0.06f, 0.12f), flower);
            }
            Prim(PrimitiveType.Sphere, "Centre", f, new Vector3(0, 0.34f, 0), Vector3.zero, Vector3.one * 0.09f, ground);
        }

        // hero: a Kenney cube-pet animal that bobs (ToonBob vertex animation + texture support). It is on the
        // "No Normal" layer because its animated vertices are not in the normal buffer.
        Material petDay, petNight;
        MakePetMaterials(out petDay, out petNight);
        var pet = SpawnHeroAnimal(root, petDay);

        // lights: warm sun + two coloured "firefly" point lights
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional; sun.color = new Color(1f, 0.96f, 0.85f); sun.intensity = 1.1f;
        sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(50, -35, 0);
        Firefly("Firefly Pink", new Vector3(-2.5f, 0.8f, -1.5f), new Color(1f, 0.55f, 0.75f));
        Firefly("Firefly Cyan", new Vector3(2.6f, 0.8f, 1.4f), new Color(0.4f, 1f, 0.85f));

        // camera on a turntable rig for the turnaround video
        var rig = new GameObject("Camera Rig");
        rig.AddComponent<Turntable>().rotationSpeed = 15f;
        var camGO = new GameObject("Main Camera"); camGO.tag = "MainCamera";
        camGO.transform.SetParent(rig.transform);
        camGO.transform.localPosition = new Vector3(0, 3.2f, -5.6f);
        camGO.transform.localRotation = Quaternion.Euler(28, 0, 0);
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.13f, 0.14f);
        cam.fieldOfView = 50;
        camGO.AddComponent<AudioListener>();
        var sw = camGO.AddComponent<StyleSwitcher>();
        sw.dayToon = ground; sw.nightToon = Mat("ToonNight");
        sw.dayHero = petDay; sw.nightHero = petNight;
        sw.extraDay = new[] { leaf, rock, flower, hero };
        sw.extraNight = new[] { Mat("ToonLeafNight"), Mat("ToonRockNight"), Mat("ToonFlowerNight"), Mat("ToonBobNight") };
        rig.AddComponent<TurnaroundCapture>().switcher = sw;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.35f, 0.4f, 0.4f);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Stylized Forest.unity");
        Debug.Log("HW2: built Assets/Scenes/Stylized Forest.unity");
    }

    // ---------- hero animal ----------
    static readonly string[] Animals = { "bunny", "fox", "cat", "deer", "chick", "panda", "penguin", "koala" };
    static string HeroAnimal => EditorPrefs.GetString("HW2HeroAnimal", "bunny");
    static void SetHero(string a) { EditorPrefs.SetString("HW2HeroAnimal", a); Build(); }
    [MenuItem("HW2/Hero Animal/1 Bunny")]   static void H1() => SetHero("bunny");
    [MenuItem("HW2/Hero Animal/2 Fox")]     static void H2() => SetHero("fox");
    [MenuItem("HW2/Hero Animal/3 Cat")]     static void H3() => SetHero("cat");
    [MenuItem("HW2/Hero Animal/4 Deer")]    static void H4() => SetHero("deer");
    [MenuItem("HW2/Hero Animal/5 Chick")]   static void H5() => SetHero("chick");
    [MenuItem("HW2/Hero Animal/6 Panda")]   static void H6() => SetHero("panda");
    [MenuItem("HW2/Hero Animal/7 Penguin")] static void H7() => SetHero("penguin");
    [MenuItem("HW2/Hero Animal/8 Koala")]   static void H8() => SetHero("koala");

    static void MakePetMaterials(out Material day, out Material night)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/CubePets/Textures/colormap.png");
        day = PetMat("ToonPet", tex, new Color(1.05f, 1.02f, 1f), new Color(0.82f, 0.76f, 0.86f), new Color(0.50f, 0.40f, 0.62f), new Color(1f, 0.92f, 0.95f));
        night = PetMat("ToonPetNight", tex, new Color(0.75f, 1f, 1f), new Color(0.45f, 0.75f, 0.95f), new Color(0.18f, 0.30f, 0.58f), new Color(0.7f, 1f, 1f));
    }

    static Material PetMat(string name, Texture2D tex, Color hi, Color mid, Color sh, Color rim)
    {
        string path = M + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Mat("ToonBob"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.shader = Mat("ToonBob").shader;
        m.CopyPropertiesFromMaterial(Mat("ToonBob"));
        m.SetTexture("_BaseMap", tex);
        m.SetTexture("_ShadowTex", null);   // keep the textured hero clean (no hatching on the atlas UVs)
        m.SetColor("_Highlight", hi); m.SetColor("_Midtone", mid); m.SetColor("_Shadow", sh); m.SetColor("_RimColor", rim);
        m.SetFloat("_BobAmplitude", 0.25f); m.SetFloat("_BobSpeed", 3.5f); m.SetFloat("_ShadowScale", 1f); m.SetFloat("_RimPower", 6f);
        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        return m;
    }

    static GameObject SpawnHeroAnimal(Transform root, Material mat)
    {
        string a = HeroAnimal;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/CubePets/animal-" + a + ".fbx");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = "Hero " + a;
        go.transform.SetParent(root, false);
        go.transform.localRotation = Quaternion.Euler(0, 180, 0);   // face the camera
        int layer = LayerMask.NameToLayer("No Normal");
        foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
        }
        // normalise size to ~1.3 units tall and stand it on the ground
        Bounds b = new Bounds(go.transform.position, Vector3.zero);
        foreach (var r in go.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
        float scale = 1.6f / Mathf.Max(b.size.y, 0.0001f);
        go.transform.localScale *= scale;
        b = new Bounds(go.transform.position, Vector3.zero);
        foreach (var r in go.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
        go.transform.position += new Vector3(-b.center.x, -b.min.y + 0.02f, -b.center.z);
        return go;
    }

    [MenuItem("HW2/Record Turnaround")]
    public static void Record()
    {
        SessionState.SetBool("HW2Capture", true);
        EditorApplication.isPlaying = true;
    }

    static GameObject Prim(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 euler, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(t);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static void Tree(Transform root, Vector3 pos, float s, Material trunk, Material leaves)
    {
        var t = new GameObject("Tree").transform; t.SetParent(root); t.localPosition = pos; t.localScale = Vector3.one * s;
        Prim(PrimitiveType.Cylinder, "Trunk", t, new Vector3(0, 0.6f, 0), Vector3.zero, new Vector3(0.25f, 0.6f, 0.25f), trunk);
        Prim(PrimitiveType.Sphere, "Canopy", t, new Vector3(0, 1.55f, 0), Vector3.zero, new Vector3(1.3f, 1.0f, 1.3f), leaves);
        Prim(PrimitiveType.Sphere, "Canopy Top", t, new Vector3(0.2f, 2.05f, 0.1f), Vector3.zero, new Vector3(0.8f, 0.7f, 0.8f), leaves);
    }

    static void Firefly(string name, Vector3 pos, Color c)
    {
        var l = new GameObject(name).AddComponent<Light>();
        l.type = LightType.Point; l.color = c; l.range = 2.4f; l.intensity = 0.6f; l.shadows = LightShadows.None;
        l.transform.position = pos;
    }
}
