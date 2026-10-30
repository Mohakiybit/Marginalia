using UnityEngine;

/// <summary>
/// Two hits and you are sent back to the last reading lamp (GDD).
/// Listens to CuratorAI.OnHitPlayer. Also shows a red flash and a
/// "You fainted" screen. Put on the Player object.
/// Without a lamp, you respawn where the scene started.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] int maxHits = 2;
    [Tooltip("Seconds of safety after being hit, so one chase can't double-hit instantly.")]
    [SerializeField] float invulnerableSeconds = 1.5f;
    [SerializeField] float deathScreenSeconds = 2.5f;

    public int HitsTaken { get; private set; }
    public int HitsLeft => maxHits - HitsTaken;
    public bool IsDead { get; private set; }

    CharacterController controller;
    Vector3 checkpointPos;
    Quaternion checkpointRot;
    float invulnTimer;
    float deathTimer;
    float hurtAlpha;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        checkpointPos = transform.position;
        checkpointRot = transform.rotation;
    }

    void OnEnable() { CuratorAI.OnHitPlayer += TakeHit; }
    void OnDisable() { CuratorAI.OnHitPlayer -= TakeHit; }

    /// <summary>Called by ReadingLamp when the player lights it.</summary>
    public void SetCheckpoint(Vector3 pos, Quaternion rot)
    {
        checkpointPos = pos;
        checkpointRot = rot;
        HitsTaken = 0; // lighting a lamp heals you
    }

    /// <summary>True while the faint screen is showing. Movement and look check this.</summary>
    public static bool Frozen { get; private set; }

    void Update()
    {
        Frozen = IsDead;
        if (invulnTimer > 0f) invulnTimer -= Time.deltaTime;
        if (hurtAlpha > 0f) hurtAlpha = Mathf.MoveTowards(hurtAlpha, 0f, Time.deltaTime * 0.8f);

        if (IsDead)
        {
            deathTimer -= Time.deltaTime;
            if (deathTimer <= 0f) Respawn();
        }
    }

    void TakeHit()
    {
        if (IsDead || invulnTimer > 0f) return;

        HitsTaken++;
        invulnTimer = invulnerableSeconds;
        hurtAlpha = 0.6f;

        if (HitsTaken >= maxHits)
        {
            IsDead = true;
            deathTimer = deathScreenSeconds;
            hurtAlpha = 1f;
        }
    }

    void Respawn()
    {
        // CharacterController must be off or it fights the teleport.
        controller.enabled = false;
        transform.SetPositionAndRotation(checkpointPos, checkpointRot);
        controller.enabled = true;

        HitsTaken = 0;
        IsDead = false;
        invulnTimer = invulnerableSeconds;
        hurtAlpha = 0f;

#if UNITY_2023_1_OR_NEWER
        foreach (CuratorAI c in FindObjectsByType<CuratorAI>(FindObjectsSortMode.None)) c.ResetToStart();
#else
        foreach (CuratorAI c in FindObjectsOfType<CuratorAI>()) c.ResetToStart();
#endif
    }

    void OnGUI()
    {
        if (hurtAlpha > 0f)
        {
            GUI.color = new Color(0.7f, 0f, 0f, hurtAlpha * 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        }

        if (IsDead)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.9f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "You fainted...\nreturning to the last reading lamp", style);
        }

        GUI.color = Color.white;
        GUI.Label(new Rect(12, Screen.height - 54, 300, 20), $"Hits left: {HitsLeft}");
    }
}