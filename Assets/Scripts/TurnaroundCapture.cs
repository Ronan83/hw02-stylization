using System.Collections;
using System.IO;
using UnityEngine;

// Renders a fixed-framerate turnaround of the scene to JPG frames in <project>/Recordings/frames.
// Started from the editor menu HW2 > Record Turnaround; frames are stitched into a video with ffmpeg.
// Halfway through it toggles the Space-key "night print" style so the video shows the interactivity too.
public class TurnaroundCapture : MonoBehaviour
{
    public int fps = 24;
    public float duration = 24f;     // one full turn at 15 deg/s
    public float nightAt = 12f;
    public float dayAt = 20f;
    public StyleSwitcher switcher;

    IEnumerator Start()
    {
#if UNITY_EDITOR
        if (!UnityEditor.SessionState.GetBool("HW2Capture", false)) yield break;
        UnityEditor.SessionState.SetBool("HW2Capture", false);
#else
        yield break;
#endif
        Application.runInBackground = true;   // keep rendering even if the editor loses focus
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Recordings/frames"));
        Directory.CreateDirectory(dir);
        foreach (var f in Directory.GetFiles(dir, "*.jpg")) File.Delete(f);

        Time.captureFramerate = fps;
        bool wentNight = false, wentDay = false;
        int total = Mathf.RoundToInt(duration * fps);
        for (int frame = 0; frame < total; frame++)
        {
            yield return new WaitForEndOfFrame();
            float t = frame / (float)fps;
            if (switcher && !wentNight && t >= nightAt) { switcher.Toggle(); wentNight = true; }
            if (switcher && !wentDay && t >= dayAt) { switcher.Toggle(); wentDay = true; }
            Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(dir, string.Format("frame_{0:D4}.jpg", frame)), tex.EncodeToJPG(92));
            Destroy(tex);
        }
        Time.captureFramerate = 0;
        Debug.Log("HW2: captured " + total + " frames to " + dir);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
