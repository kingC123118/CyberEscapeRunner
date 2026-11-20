using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Visual-only night upgrade for MainScene: star sky dome, stars, moon,
/// dim blue moonlight, meteor shower and symmetric campfires beside the track.
/// Everything is added under a single root "NightSkyDecor". No gameplay
/// object (Player / Managers / UI / prefab content) is created, moved or
/// modified; the only tweak to an existing object is the Directional
/// Light's color/intensity (explicitly requested).
/// Menu: Tools/StarSky/Upgrade Main Scene To Night
/// </summary>
public static class MainSceneNightUpgrade
{
    const string MatFolder = "Assets/Art/StarSky/Materials";

    [MenuItem("Tools/StarSky/Upgrade Main Scene To Night")]
    public static void Run()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.path.EndsWith("MainScene.unity"))
        {
            Debug.LogError("[NightUpgrade] Active scene is not MainScene.unity (active: " + scene.path + ")");
            return;
        }

        // Idempotent rebuild
        var old = GameObject.Find("NightSkyDecor");
        if (old != null) Object.DestroyImmediate(old);

        // ---------- Materials (reuse StarSky assets, recreate if missing) ----------
        var domeMat = LoadOrCreateMat("SkyDome", "Custom/StarSkyDome");
        domeMat.SetColor("_Color", new Color(0.039f, 0.039f, 0.165f)); // #0A0A2A

        var moonMat = LoadOrCreateMat("Moon", "Unlit/Color");
        moonMat.SetColor("_Color", new Color(1f, 0.98f, 0.92f));

        var groundMat = LoadOrCreateMat("Ground", "Standard");
        groundMat.SetColor("_Color", new Color(0.04f, 0.04f, 0.055f));
        groundMat.SetFloat("_Glossiness", 0.12f);

        var logMat = LoadOrCreateMat("LogWood", "Standard");
        logMat.SetColor("_Color", new Color(0.23f, 0.12f, 0.06f));
        logMat.SetFloat("_Glossiness", 0.08f);

        var particleMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");

        // ---------- Root ----------
        var root = new GameObject("NightSkyDecor");

        // SkyRig follows the player's Z so the sky never falls behind
        var skyRig = new GameObject("SkyRig");
        skyRig.transform.SetParent(root.transform, false);
        skyRig.AddComponent<FollowPlayerZ>();

        // Sky dome (radius 450, inside-out unlit shader)
        var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dome.name = "SkyDome";
        Object.DestroyImmediate(dome.GetComponent<Collider>());
        dome.transform.SetParent(skyRig.transform, false);
        dome.transform.localPosition = Vector3.zero;
        dome.transform.localScale = Vector3.one * 900f; // radius 450 (< camera far 1000)
        dome.GetComponent<MeshRenderer>().sharedMaterial = domeMat;

        // Stars: 700 static billboards on the upper hemisphere
        var stars = NewParticle("Stars", skyRig.transform, Vector3.zero, new Vector3(-90f, 0f, 0f));
        var sm = stars.main;
        sm.duration = 10f; sm.loop = true; sm.prewarm = true;
        sm.startLifetime = 99999f; sm.startSpeed = 0f; sm.gravityModifier = 0f;
        sm.maxParticles = 800;
        sm.simulationSpace = ParticleSystemSimulationSpace.World;
        sm.startSize = new ParticleSystem.MinMaxCurve(0.7f, 1.8f);
        sm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 1f), new Color(0.72f, 0.82f, 1f, 0.9f));
        var se = stars.emission;
        se.rateOverTime = 0f;
        se.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)700) });
        var ss = stars.shape;
        ss.shapeType = ParticleSystemShapeType.Hemisphere;
        ss.radius = 415f;
        SetupRenderer(stars, particleMat, ParticleSystemRenderMode.Billboard);

        // Moon
        var moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moon.name = "Moon";
        Object.DestroyImmediate(moon.GetComponent<Collider>());
        moon.transform.SetParent(skyRig.transform, false);
        moon.transform.localPosition = new Vector3(60f, 60f, 320f);
        moon.transform.localScale = Vector3.one * 25f;
        moon.GetComponent<MeshRenderer>().sharedMaterial = moonMat;

        // Meteor shower high above the track ahead of the player
        var met = NewParticle("MeteorShower", skyRig.transform, new Vector3(-40f, 55f, 190f), Vector3.zero);
        var mm = met.main;
        mm.duration = 6f; mm.loop = true; mm.prewarm = false;
        mm.startLifetime = 2.4f; mm.startSpeed = 0f; mm.gravityModifier = 0f;
        mm.maxParticles = 120;
        mm.simulationSpace = ParticleSystemSimulationSpace.World;
        mm.startSize = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
        mm.startColor = new Color(1f, 0.97f, 0.85f);
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

        // Dark side-ground following the player (visual filler beside the road)
        var side = GameObject.CreatePrimitive(PrimitiveType.Plane);
        side.name = "SideGround";
        Object.DestroyImmediate(side.GetComponent<Collider>());
        side.transform.SetParent(skyRig.transform, false);
        side.transform.localPosition = new Vector3(0f, -0.15f, 120f);
        side.transform.localScale = new Vector3(60f, 1f, 40f); // 600 x 400
        side.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

        // ---------- Campfires: 3 symmetric pairs beside the track (track edge |x|=5) ----------
        var lane = new GameObject("CampfireLane");
        lane.transform.SetParent(root.transform, false);
        float[] zs = { 20f, 55f, 90f };
        float[] xs = { 11f, 12f, 11f };
        int idx = 0;
        for (int i = 0; i < zs.Length; i++)
        {
            BuildCampfire(lane.transform, "Campfire_L" + (i + 1), new Vector3(-xs[i], 0f, zs[i]), logMat, particleMat, idx++);
            BuildCampfire(lane.transform, "Campfire_R" + (i + 1), new Vector3(xs[i], 0f, zs[i]), logMat, particleMat, idx++);
        }

        // ---------- Existing directional light -> dim blue moonlight (requested) ----------
        var dlGo = GameObject.Find("Directional Light");
        if (dlGo != null)
        {
            var dl = dlGo.GetComponent<Light>();
            if (dl != null && dl.type == LightType.Directional)
            {
                dl.color = new Color(0.42f, 0.52f, 0.95f);
                dl.intensity = 0.4f; // rotation untouched
            }
        }
        else
        {
            var lightGo = new GameObject("Moonlight");
            lightGo.transform.SetParent(root.transform, false);
            var nl = lightGo.AddComponent<Light>();
            nl.type = LightType.Directional;
            nl.color = new Color(0.42f, 0.52f, 0.95f);
            nl.intensity = 0.4f;
            lightGo.transform.rotation = Quaternion.Euler(50f, 330f, 0f);
        }

        // Dim blue ambient for the night mood
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.06f, 0.12f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[NightUpgrade] MainScene night upgrade applied and saved.");
    }

    static void BuildCampfire(Transform parent, string name, Vector3 pos, Material logMat, Material particleMat, int index)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = pos;

        for (int i = 0; i < 5; i++)
        {
            float yaw = i * 72f + 15f + index * 24f;
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            var log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.name = "Log_" + (i + 1);
            // Remove collider: decor must never affect gameplay collisions
            var col = log.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            log.transform.SetParent(root.transform, false);
            Vector3 basePos = dir * 0.5f + Vector3.up * 0.05f;
            Vector3 topPos = Vector3.up * 1.25f;
            log.transform.localPosition = (basePos + topPos) * 0.5f;
            log.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (topPos - basePos).normalized);
            log.transform.localScale = new Vector3(0.22f, 0.75f, 0.22f);
            log.GetComponent<MeshRenderer>().sharedMaterial = logMat;
        }

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
                new GradientColorKey(new Color(1f, 0.78f, 0.30f), 0f),
                new GradientColorKey(new Color(1f, 0.40f, 0.06f), 0.45f),
                new GradientColorKey(new Color(0.65f, 0.08f, 0.01f), 1f)
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
        flick.flickerSpeed = 8f + index * 1.3f;
    }

    static ParticleSystem NewParticle(string name, Transform parent, Vector3 pos, Vector3 euler)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
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

    static Material LoadOrCreateMat(string name, string shader)
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
}
