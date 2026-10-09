using UnityEngine;

public enum ItemState { Raw, Armed }

/// <summary>
/// Data for one clue-weapon (Evidence Camera, Music Box, Mirror Shard...).
/// Create in Unity: right-click in Project > Create > Marginalia > Item.
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Marginalia/Item")]
public class ItemData : ScriptableObject
{
    [Tooltip("Unique key used by the inventory Dictionary, e.g. 'camera'.")]
    public string itemId = "item";
    public string displayName = "Item";
    public Sprite icon;

    [Header("Text shown when inspecting (Q)")]
    [TextArea(2, 6)] public string rawDescription = "It barely works.";
    [TextArea(2, 8)] public string clueText = "The clue goes here.";
    [TextArea(2, 6)] public string armedDescription = "It works now.";

    [Header("Weapon")]
    [Tooltip("Uses once armed. 0 = unlimited. Mirror Shard = 3, Camera = film count.")]
    public int armedCharges = 3;

    public string GetInspectText(ItemState state)
    {
        return state == ItemState.Armed
            ? armedDescription
            : rawDescription + "\n\n" + clueText;
    }
}