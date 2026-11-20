using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// One-shot builder for the StarSky night scene: sky dome, stars, moon,
/// moonlight, ground, meteor shower and three campfires with flickering lights.
/// Menu: Tools/StarSky/Build Scene
/// </summary>
public static class StarSkyBuilder
{
    const string MatFolder = "Assets/Art/StarSky/Materials";
    const string ScenePath = "Assets/Scenes/StarSky.unity";

    [MenuItem("Tools/StarSky/Create And Build Scene")]
    public static void CreateAndBuild()
    {
        // EmptyScene: no default camera/light; explicit save path -> no save dialog
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildInternal(scene);
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("[StarSkyBuilder] CreateAndBuild done. Saved=" + saved + " path=" + ScenePath);
    }

    [MenuItem("Tools/StarSky/Build Scene")]
    public static void Build()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.path.EndsWith("StarSky.unity"))
        {
            Debug.LogError("[StarSkyBuilder] Active scene is not StarSky.unity (active: " + scene.path + ")");
            return;
        }
        BuildInternal(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[StarSkyBuilder] StarSky scene rebuilt and saved to " + scene.path);
    }

    static void BuildInternal(Scene scene)
    {
        // Remove leftovers from a previous build and default template objects
        string[] roots = { "Environment", "Ground", "MeteorShower", "Campfires", "Main Camera", "Directional Light", "Moonlight" };
        foreach (var n in roots)
        {
            var go = GameObject.Find(n);
            if (go != null && go.transform.parent == null) Object.DestroyImmediate(go);
        }

        EnsureFolder("Assets/Art/StarSky");
        EnsureFolder(MatFolder);

        // ---------------- Materials ----------------
        var domeMat = EnsureMat("SkyDome", "Custom/StarSkyDome");
        domeMat.SetColor("_Color", new Color(0.039f, 0.039f, 0.165f)); // #0A0A2A

        var moonMat = EnsureMat("Moon", "Unlit/Color");
        moonMat.SetColor("_Color", new Color(1f, 0.98f, 0.92f)); // bright warm white = glowing moon

        var groundMat = EnsureMat("Ground", "Standard");
        groundMat.SetColor("_Color", new Color(0.04f, 0.04f, 0.055f)); // near-black grey
        groundMat.SetFloat("_Glossiness", 0.12f);                    // rough, weak specular

        var logMat = EnsureMat("LogWood", "Standard");
        logMat.SetColor("_Color", new Color(0.23f, 0.12f, 0.06f));    // dark brown
        logMat.SetFloat("_Glossiness", 0.08f);

        var particleMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");

        // ---------------- Environment ----------------
        var env = new GameObject("Environment");

        // Sky dome: giant sphere, only inner faces rendered by the dome shader
        var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dome.name = "SkyDome";
        Object.DestroyImmediate(dome.GetComponent<Collider>());
        dome.transform.SetParent(env.transform, false);
        dome.transform.localPosition = Vector3.zero;
        dome.transform.localScale = Vector3.one * 1000f; // radius ~500
        dome.GetComponent<MeshRenderer>().sharedMaterial = domeMat;

        // Stars: 700 static billboards on the upper hemisphere
        var stars = NewParticle("Stars", env.transform, Vector3.zero, new Vector3(-90f, 0f, 0f));
        var sm = stars.main;
        sm.duration = 10f; sm.loop = true; sm.prewarm = true;
        sm.startLifetime = 99999f; sm.startSpeed = 0f; sm.gravityModifier = 0f;
        sm.maxParticles = 800;
        sm.simulationSpace = ParticleSystemSimulationSpace.World;
        sm.startSize = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
        sm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 1f), new Color(0.72f, 0.82f, 1f, 0.9f));
        var se = stars.emission;
        se.rateOverTime = 0f;
        se.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)700) });
        var ss = stars.shape;
        ss.shapeType = ParticleSystemShapeType.Hemisphere;
        ss.radius = 460f;
        SetupRenderer(stars, particleMat, ParticleSystemRenderMode.Billboard);

        // Moon: emissive white sphere
        var moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moon.name = "Moon";
        Object.DestroyImmediate(moon.GetComponent<Collider>());
        moon.transform.SetParent(env.transform, false);
        moon.transform.position = new Vector3(60f, 110f, 220f);
        moon.transform.localScale = Vector3.one * 30f;
        moon.GetComponent<MeshRenderer>().sharedMaterial = moonMat;

        // ---------------- Moonlight & ambient ----------------
        var lightGo = new GameObject("Moonlight");
        var dl = lightGo.AddComponent<Light>();
        dl.type = LightType.Directional;
        dl.color = new Color(0.55f, 0.65f, 0.95f); // dim blue
        dl.intensity = 0.4f;
        dl.shadows = LightShadows.Soft;
        dl.shadowStrength = 0.7f;
        lightGo.transform.rotation = Quaternion.Euler(26f, -165f, 0f);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.06f, 0.12f);

        // ---------------- Ground ----------------
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0f, 0f, 100f);
        ground.transform.localScale = new Vector3(120f, 1f, 120f); // 1200m x 1200m
        ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

        // ---------------- Meteor shower ----------------
        var met = NewParticle("MeteorShower", null, new Vector3(-45f, 85f, 150f), Vector3.zero);
        var mm = met.main;
        mm.duration = 6f; mm.loop = true; mm.prewarm = false;
        mm.startLifetime = 2.4f; mm.startSpeed = 0f; mm.gravityModifier = 0f;
        mm.maxParticles = 120;
        mm.simulationSpace = ParticleSystemSimulationSpace.World;
        mm.startSize = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
        mm.startColor = new Color(1f, 0.97f, 0.85f); // pale warm white
        var me = met.emission;
        me.rateOverTime = 3.5f;
        var ms = met.shape;
        ms.shapeType = ParticleSystemShapeType.Box;
        ms.scale = new Vector3(60f, 1f, 20f);
        var mv = met.velocityOverLifetime;
        mv.enabled = true;
        mv.space = ParticleSystemSimulationSpace.Local;
        mv.x = new ParticleSystem.MinMaxCurve(26f);
        mv.y = new ParticleSystem.MinMaxCurve(-30f);
        mv.z = new ParticleSystem.MinMaxCurve(0f);
        var mc = met.colorOverLifetime;
        mc.enabled = true;
        mc.color = AlphaFadeGradient(new Color(1f, 0.97f, 0.85f), 1f, 0f);
        var mr = SetupRenderer(met, particleMat, ParticleSystemRenderMode.Stretch);
        mr.lengthScale = 3f;
        mr.velocityScale = 0.08f;

        // ---------------- Campfires ----------------
        var campRoot = new GameObject("Campfires");
        Vector3[] positions = { new Vector3(-18f, 0f, 10f), new Vector3(0f, 0f, 22f), new Vector3(18f, 0f, 6f) };
        string[] names = { "Campfire_A", "Campfire_B", "Campfire_C" };
        for (int i = 0; i < 3; i++)
            BuildCampfire(campRoot.transform, names[i], positions[i], logMat, particleMat, i);

        // ---------------- Camera ----------------
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 2000f; // must reach the dome (r=500)
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.06f);
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(0f, 6f, -30f);
        camGo.transform.rotation = Quaternion.Euler(-13f, 0f, 0f); // slight tilt up

        EditorSceneManager.MarkSceneDirty(scene);
    }

    static void BuildCampfire(Transform parent, string name, Vector3 pos, Material logMat, Material particleMat, int index)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = pos;

        // 5 logs leaning into a teepee shape
        for (int i = 0; i < 5; i++)
        {
            float yaw = i * 72f + 15f + index * 24f;
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            var log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.name = "Log_" + (i + 1);
            log.transform.SetParent(root.transform, false);
            Vector3 basePos = dir * 0.5f + Vector3.up * 0.05f;
            Vector3 topPos = Vector3.up * 1.25f;
            log.transform.localPosition = (basePos + topPos) * 0.5f;
            log.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (topPos - basePos).normalized);
            log.transform.localScale = new Vector3(0.22f, 0.75f, 0.22f); // ~1.5m long, 0.22m thick
            log.GetComponent<MeshRenderer>().sharedMaterial = logMat;
        }

        // Flame particles
        var fire = NewParticle("Fire", root.transform, new Vector3(0f, 0.2f, 0f), new Vector3(-90f, 0f, 0f));
        var f = fire.main;
        f.duration = 2f; f.loop = true; f.prewarm = true;
        f.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
        f.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 3.2f);
        f.startSize = new ParticleSystem.MinMaxCurve(1.0f, 1.9f);
        f.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        f.maxParticles = 200;
        f.simulationSpace = ParticleSystemSimulationSpace.World;
        var fe = fire.emission;
        fe.rateOverTime = 40f;
        var fs = fire.shape;
        fs.shapeType = ParticleSystemShapeType.Cone;
        fs.angle = 14f;
        fs.radius = 0.32f;
        var fc = fire.colorOverLifetime;
        fc.enabled = true;
        var flameGrad = new Gradient();
        flameGrad.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.78f, 0.30f), 0f),   // bright orange
                new GradientColorKey(new Color(1f, 0.40f, 0.06f), 0.45f), // orange-red
                new GradientColorKey(new Color(0.65f, 0.08f, 0.01f), 1f)  // deep red
            },
            new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.80f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        fc.color = flameGrad;
        var fsol = fire.sizeOverLifetime;
        fsol.enabled = true;
        fsol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.35f)));
        var frol = fire.rotationOverLifetime;
        frol.enabled = true;
        frol.z = new ParticleSystem.MinMaxCurve(-1.6f, 1.6f);
        SetupRenderer(fire, particleMat, ParticleSystemRenderMode.Billboard);

        // Rising sparks
        var sparks = NewParticle("Sparks", root.transform, new Vector3(0f, 0.5f, 0f), new Vector3(-90f, 0f, 0f));
        var s = sparks.main;
        s.duration = 2f; s.loop = true; s.prewarm = true;
        s.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        s.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 4.2f);
        s.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
        s.startColor = new Color(1f, 0.75f, 0.3f);
        s.gravityModifier = 0.25f;
        s.maxParticles = 80;
        s.simulationSpace = ParticleSystemSimulationSpace.World;
        var spe = sparks.emission;
        spe.rateOverTime = 14f;
        var sps = sparks.shape;
        sps.shapeType = ParticleSystemShapeType.Cone;
        sps.angle = 18f;
        sps.radius = 0.25f;
        var spc = sparks.colorOverLifetime;
        spc.enabled = true;
        spc.color = AlphaFadeGradient(new Color(1f, 0.75f, 0.3f), 1f, 0f);
        SetupRenderer(sparks, particleMat, ParticleSystemRenderMode.Billboard);

        // Orange point light with flicker script
        var fl = new GameObject("FireLight");
        fl.transform.SetParent(root.transform, false);
        fl.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        var pl = fl.AddComponent<Light>();
        pl.type = LightType.Point;
        pl.color = new Color(1f, 0.55f, 0.18f);
        pl.intensity = 2.6f;
        pl.range = 15f;
        pl.shadows = LightShadows.None;
        var flick = fl.AddComponent<CampfireFlicker>();
        flick.baseIntensity = 2.6f;
        flick.intensityVariation = 0.8f;
        flick.baseRange = 15f;
        flick.rangeVariation = 2f;
        flick.flickerSpeed = 8f + index * 1.7f; // each fire flickers at its own pace
    }

    static ParticleSystem NewParticle(string name, Transform parent, Vector3 pos, Vector3 euler)
    {
        var go = new GameObject(name);
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
        }
        else
        {
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(euler);
        }
        return go.AddComponent<ParticleSystem>();
    }

    static ParticleSystemRenderer SetupRenderer(ParticleSystem ps, Material mat, ParticleSystemRenderMode mode)
    {
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = mode;
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        return r;
    }

    static Gradient AlphaFadeGradient(Color c, float a0, float a1)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(a0, 0f), new GradientAlphaKey(a0 * 0.9f, 0.6f), new GradientAlphaKey(a1, 1f) });
        return g;
    }

    static Material EnsureMat(string name, string shader)
    {
        string path = MatFolder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (mat.shader == null || mat.shader.name != shader)
        {
            mat.shader = Shader.Find(shader);
        }
        return mat;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
