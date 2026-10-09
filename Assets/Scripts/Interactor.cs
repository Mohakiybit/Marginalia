using UnityEngine;

/// <summary>
/// Looks forward from the camera. E interacts, Q inspects (reads the clue).
/// Setup: put on the Main Camera (child of Player). Inventory is found
/// automatically on the Player parent.
/// </summary>
public class Interactor : MonoBehaviour
{
    [SerializeField] float range = 2.5f;
    [SerializeField] LayerMask mask = ~0;
    [SerializeField] Inventory inventory;

    /// <summary>True while the inspect panel is open. MouseLook checks this.</summary>
    public static bool IsInspecting { get; private set; }

    public Inventory Inventory => inventory;
    public string CurrentPrompt { get; private set; } = "";
    public string InspectTitle { get; private set; } = "";
    public string InspectBody { get; private set; } = "";
    public string Message { get; private set; } = "";
    public bool HasMessage => messageTimer > 0f;

    IInteractable current;
    float messageTimer;

    void Awake()
    {
        IsInspecting = false;
        if (inventory == null) inventory = GetComponentInParent<Inventory>();
        if (inventory == null) Debug.LogWarning("Interactor: no Inventory found on the Player.", this);
    }

    void OnDisable() { IsInspecting = false; }

    void Update()
    {
        if (messageTimer > 0f) messageTimer -= Time.deltaTime;

        // While reading, Q or E closes the panel
        if (IsInspecting)
        {
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E)) IsInspecting = false;
            return;
        }

        // What are we looking at?
        current = null;
        CurrentPrompt = "";
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit,
                range, mask, QueryTriggerInteraction.Ignore))
        {
            current = hit.collider.GetComponentInParent<IInteractable>();
            if (current != null) CurrentPrompt = current.Prompt;
        }

        if (current != null && Input.GetKeyDown(KeyCode.E)) current.Interact(this);
        if (Input.GetKeyDown(KeyCode.Q)) TryInspect();
    }

    // Inspect the item you're looking at, otherwise the selected inventory item.
    void TryInspect()
    {
        ItemData data = null;
        ItemState state = ItemState.Raw;

        if (current is PickupItem pickup)
        {
            data = pickup.Item;
        }
        else if (inventory != null)
        {
            ItemEntry selected = inventory.GetSelected();
            if (selected != null)
            {
                data = selected.data;
                state = selected.state;
            }
        }

        if (data == null)
        {
            ShowMessage("Nothing to inspect.");
            return;
        }

        InspectTitle = data.displayName;
        InspectBody = data.GetInspectText(state);
        IsInspecting = true;
    }

    public void ShowMessage(string text, float seconds = 2f)
    {
        Message = text;
        messageTimer = seconds;
    }
}