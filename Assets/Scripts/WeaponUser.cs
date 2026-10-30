using UnityEngine;

/// <summary>
/// Left-click uses the selected, armed item.
/// Flash: stuns the Curator if he is inside the cone and in line of sight.
/// Sleep: same check, but puts him to sleep (Music Box later).
/// Setup: add to the Main Camera. Inventory is found on the Player parent.
/// </summary>
public class WeaponUser : MonoBehaviour
{
    [SerializeField] Inventory inventory;
    [SerializeField] Interactor interactor;
    [Tooltip("Layers that block sight (walls, shelves). Include the Curator's layer too.")]
    [SerializeField] LayerMask sightMask = ~0;
    [SerializeField] float cooldown = 0.6f;
    [Tooltip("Noise made when firing. The GDD tags puzzles at 6; a flash is quiet-ish.")]
    [SerializeField] float fireNoise = 2f;

    float cooldownTimer;
    float flashAlpha;

    void Awake()
    {
        if (inventory == null) inventory = GetComponentInParent<Inventory>();
        if (interactor == null) interactor = GetComponent<Interactor>();
    }

    void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        if (flashAlpha > 0f) flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.deltaTime * 2.5f);

        if (Interactor.IsInspecting || inventory == null) return;
        if (Cursor.lockState != CursorLockMode.Locked) return;
        if (cooldownTimer > 0f || !Input.GetMouseButtonDown(0)) return;

        ItemEntry item = inventory.GetSelected();
        if (item == null) return;

        if (item.state != ItemState.Armed)
        {
            interactor?.ShowMessage("It doesn't work yet. Inspect it (Q) for a clue.");
            return;
        }
        if (item.data.weaponType == WeaponType.None)
        {
            interactor?.ShowMessage("Nothing happens.");
            return;
        }
        if (!inventory.UseSelected())
        {
            interactor?.ShowMessage("Out of charges.");
            return;
        }

        cooldownTimer = cooldown;
        Fire(item.data);
    }

    void Fire(ItemData data)
    {
        if (data.weaponType == WeaponType.Flash) flashAlpha = 1f;

        NoiseSystem.Emit(transform.position, fireNoise, transform.root.gameObject);

        CuratorAI curator = FindCuratorInCone(data);
        if (curator == null)
        {
            interactor?.ShowMessage("Missed.");
            return;
        }

        if (data.weaponType == WeaponType.Flash) curator.Stun(data.effectSeconds);
        else if (data.weaponType == WeaponType.Sleep) curator.Sleep(data.effectSeconds);
        interactor?.ShowMessage("Hit!");
    }

    CuratorAI FindCuratorInCone(ItemData data)
    {
#if UNITY_2023_1_OR_NEWER
        CuratorAI[] all = FindObjectsByType<CuratorAI>(FindObjectsSortMode.None);
#else
        CuratorAI[] all = FindObjectsOfType<CuratorAI>();
#endif
        foreach (CuratorAI c in all)
        {
            Vector3 to = (c.transform.position + Vector3.up) - transform.position;
            if (to.magnitude > data.effectRange) continue;
            if (Vector3.Angle(transform.forward, to) > data.coneAngle * 0.5f) continue;

            // Line of sight: the first thing the ray hits must be the Curator.
            if (Physics.Raycast(transform.position, to.normalized, out RaycastHit hit,
                    data.effectRange, sightMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<CuratorAI>() == c) return c;
            }
        }
        return null;
    }

    // White screen flash. Replace with a UI image or post-processing later.
    void OnGUI()
    {
        if (flashAlpha <= 0f) return;
        GUI.color = new Color(1f, 1f, 1f, flashAlpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}