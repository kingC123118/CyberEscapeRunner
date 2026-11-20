using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Renders the main camera (1920x1080) to a PNG for visual verification.
/// Manually simulates all particle systems first so stars / meteors / fire
/// are visible even in Edit mode.
/// Menu: Tools/StarSky/Capture Screenshot
/// </summary>
public static class StarSkyCapture
{
    [MenuItem("Tools/StarSky/Capture Screenshot")]
    public static void Capture()
    {
        var cam = Camera.main;
        if (cam == null) cam = Object.FindObjectOfType<Camera>();
        if (cam == null)
        {
            Debug.LogError("[StarSkyCapture] No camera found in scene.");
            return;
        }

        foreach (var ps in Object.FindObjectsOfType<ParticleSystem>())
        {
            ps.Simulate(4f, true, true);
            ps.Play(true);
        }

        const int w = 1920, h = 1080;
        var rt = new RenderTexture(w, h, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        cam.targetTexture = null;
        RenderTexture.active = null;
        rt.Release();

        string dir = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
            "Trae-CN", "temp_20261109_01");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "StarSky_GameView.png");
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log("[StarSkyCapture] Saved screenshot: " + path);
    }
}
