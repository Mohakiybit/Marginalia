using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Debug overlay for tuning movement and noise. Press F1 to show/hide.
/// Shows state, speed, a noise bar, and a log of recent noise events.
/// Also draws the "hearing radius" in the Scene view (and Game view if Gizmos is on).
/// Setup: add to the Player object and assign the PlayerMovement reference
/// (it auto-finds it if left empty). Remove or disable before the final build.
/// </summary>
public class PlayerDebugOverlay : MonoBehaviour
{
    [SerializeField] PlayerMovement player;
    [SerializeField] bool visible = true;

    [Header("Noise tuning preview")]
    [Tooltip("How many meters of hearing range each point of loudness gives. Try 2 to 4.")]
    [SerializeField] float metersPerNoiseUnit = 3f;
    [SerializeField] float maxNoiseForBar = 10f; // tuning fork is the loudest in the GDD

    [Header("Event log")]
    [SerializeField] int maxLogLines = 6;

    CharacterController controller;
    readonly Queue<string> log = new Queue<string>();
    GUIStyle label;
    Vector3 lastPos;
    float measuredSpeed;

    void Awake()
    {
        if (player == null) player = GetComponent<PlayerMovement>();
        controller = GetComponent<CharacterController>();
        lastPos = transform.position;
    }

    void OnEnable() { NoiseSystem.OnNoise += HandleNoise; }
    void OnDisable() { NoiseSystem.OnNoise -= HandleNoise; }

    void HandleNoise(NoiseEvent e)
    {
        log.Enqueue($"t={e.Time:F1}  loudness {e.Loudness:F0}  from {(e.Source != null ? e.Source.name : "?")}");
        while (log.Count > maxLogLines) log.Dequeue();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) visible = !visible;

        // Real speed measured from position change (horizontal only)
        Vector3 delta = transform.position - lastPos;
        delta.y = 0f;
        measuredSpeed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        lastPos = transform.position;
    }

    void OnGUI()
    {
        if (!visible || player == null) return;

        if (label == null)
        {
            label = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.white } };
        }

        const float x = 12f, w = 320f;
        float y = 12f;

        // Background panel
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(x - 6, y - 6, w + 12, 150 + log.Count * 18), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(x, y, w, 20), $"State:  {player.State}", label); y += 20;
        GUI.Label(new Rect(x, y, w, 20), $"Speed:  {measuredSpeed:F2} m/s", label); y += 20;
        GUI.Label(new Rect(x, y, w, 20), $"Crouching:  {player.IsCrouching}   Height: {(controller != null ? controller.height : 0):F2}", label); y += 20;
        GUI.Label(new Rect(x, y, w, 20), $"Noise:  {player.CurrentNoise:F0}   (hearing range ~{player.CurrentNoise * metersPerNoiseUnit:F1} m)", label); y += 24;

        // Noise bar, colored green -> red as it gets louder
        float t = Mathf.Clamp01(player.CurrentNoise / maxNoiseForBar);
        GUI.color = new Color(1f, 1f, 1f, 0.25f);
        GUI.DrawTexture(new Rect(x, y, w, 14), Texture2D.whiteTexture);
        GUI.color = Color.Lerp(Color.green, Color.red, t);
        GUI.DrawTexture(new Rect(x, y, w * t, 14), Texture2D.whiteTexture);
        GUI.color = Color.white;
        y += 24;

        GUI.Label(new Rect(x, y, w, 20), "Recent noise events (F1 hides):", label); y += 20;
        foreach (string line in log)
        {
            GUI.Label(new Rect(x, y, w + 100, 20), line, label);
            y += 18;
        }
    }

    // Hearing radius preview, so you can see how far a sound "reaches".
    void OnDrawGizmos()
    {
        if (player == null) player = GetComponent<PlayerMovement>();
        if (player == null || !Application.isPlaying) return;

        float radius = player.CurrentNoise * metersPerNoiseUnit;
        if (radius <= 0f) return;

        Gizmos.color = Color.Lerp(Color.green, Color.red, Mathf.Clamp01(player.CurrentNoise / maxNoiseForBar));
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, radius);
    }
}