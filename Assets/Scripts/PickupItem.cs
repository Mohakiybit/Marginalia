using UnityEngine;

/// <summary>
/// Anything the player can look at and press E on (items, doors, lamps...).
/// </summary>
public interface IInteractable
{
    string Prompt { get; }
    void Interact(Interactor who);
}

/// <summary>
/// Put on a world object (with a Collider) to make it a pickup.
/// Assign an ItemData asset.
/// </summary>
public class PickupItem : MonoBehaviour, IInteractable
{
    [SerializeField] ItemData item;

    public ItemData Item => item;
    public string Prompt => item != null ? $"[E] Pick up {item.displayName}   [Q] Inspect" : "";

    public void Interact(Interactor who)
    {
        if (item == null || who.Inventory == null) return;

        if (who.Inventory.Has(item.itemId))
        {
            who.ShowMessage("You already have this.");
            return;
        }

        if (who.Inventory.Add(item))
        {
            who.ShowMessage($"Picked up: {item.displayName}");
            Destroy(gameObject);
        }
        else
        {
            who.ShowMessage("Your hands are full.");
        }
    }
}