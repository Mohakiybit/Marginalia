using UnityEngine;

/// <summary>
/// Placeholder HUD drawn with OnGUI so you can test without building Canvas UI.
/// Crosshair, interact prompt, inventory slots, and the inspect panel.
/// Setup: put on the Main Camera next to the Interactor.
/// Replace with real UI later (the GDD wants it mostly empty).
/// </summary>
[RequireComponent(typeof(Interactor))]
public class InteractionHUD : MonoBehaviour
{
    Interactor interactor;
    Inventory inventory;
    GUIStyle text, title, body, centered;

    void Awake()
    {
        interactor = GetComponent<Interactor>();
        inventory = GetComponentInParent<Inventory>();
    }

    void BuildStyles()
    {
        text = new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = Color.white } };
        centered = new GUIStyle(text) { alignment = TextAnchor.MiddleCenter };
        title = new GUIStyle(text) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        body = new GUIStyle(text) { fontSize = 17, wordWrap = true, alignment = TextAnchor.UpperLeft };
    }

    void OnGUI()
    {
        if (text == null) BuildStyles();
        float cx = Screen.width / 2f, cy = Screen.height / 2f;

        // Small crosshair
        GUI.color = new Color(1f, 1f, 1f, 0.7f);
        GUI.DrawTexture(new Rect(cx - 2, cy - 2, 4, 4), Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (Interactor.IsInspecting)
        {
            DrawInspectPanel(cx, cy);
            return;
        }

        // Interact prompt
        if (!string.IsNullOrEmpty(interactor.CurrentPrompt))
            GUI.Label(new Rect(cx - 250, cy + 30, 500, 28), interactor.CurrentPrompt, centered);

        // Short message ("Picked up: ...")
        if (interactor.HasMessage)
            GUI.Label(new Rect(cx - 250, cy + 60, 500, 28), interactor.Message, centered);

        DrawInventory();
    }

    void DrawInventory()
    {
        if (inventory == null) return;

        float x = Screen.width - 280f;
        float y = Screen.height - 20f - Inventory.MaxSlots * 24f;

        for (int i = 0; i < Inventory.MaxSlots; i++)
        {
            ItemEntry e = inventory.GetSlot(i);
            string line;
            if (e == null)
            {
                line = $"{i + 1}.  -";
                GUI.color = new Color(1f, 1f, 1f, 0.35f);
            }
            else
            {
                string state = e.state == ItemState.Armed
                    ? (e.data.armedCharges > 0 ? $"Armed x{e.charges}" : "Armed")
                    : "Raw";
                line = $"{i + 1}.  {e.data.displayName}  [{state}]";
                GUI.color = (i == inventory.SelectedSlot) ? Color.yellow : Color.white;
            }
            GUI.Label(new Rect(x, y + i * 24f, 270, 24), line, text);
        }
        GUI.color = Color.white;
    }

    void DrawInspectPanel(float cx, float cy)
    {
        Rect panel = new Rect(cx - 280, cy - 180, 560, 360);
        GUI.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(panel.x + 20, panel.y + 15, panel.width - 40, 36), interactor.InspectTitle, title);
        GUI.Label(new Rect(panel.x + 25, panel.y + 65, panel.width - 50, panel.height - 110), interactor.InspectBody, body);
        GUI.Label(new Rect(panel.x, panel.yMax - 35, panel.width, 28), "[Q] or [E] to close", centered);
    }
}