using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One carried item and its current state.</summary>
public class ItemEntry
{
    public ItemData data;
    public ItemState state = ItemState.Raw;
    public int charges;
}

/// <summary>
/// Player inventory. A Dictionary (itemId -> entry) gives fast lookups,
/// and a List keeps slot order for the 1 to 5 keys.
/// Put on the Player object.
/// </summary>
public class Inventory : MonoBehaviour
{
    public const int MaxSlots = 5;

    readonly Dictionary<string, ItemEntry> items = new Dictionary<string, ItemEntry>();
    readonly List<string> slotOrder = new List<string>();

    public int SelectedSlot { get; private set; } = -1;
    public int Count => slotOrder.Count;

    /// <summary>Fires whenever items, state, charges or selection change.</summary>
    public event Action OnChanged;

    void Update()
    {
        // Keys 1 to 5 select a slot (GDD controls)
        for (int i = 0; i < MaxSlots; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) Select(i);
        }
    }

    public bool Has(string id) => items.ContainsKey(id);

    public bool TryGet(string id, out ItemEntry entry) => items.TryGetValue(id, out entry);

    public ItemEntry GetSlot(int slot)
    {
        if (slot < 0 || slot >= slotOrder.Count) return null;
        return items[slotOrder[slot]];
    }

    public ItemEntry GetSelected() => GetSlot(SelectedSlot);

    public bool Add(ItemData data)
    {
        if (data == null || items.ContainsKey(data.itemId)) return false;
        if (slotOrder.Count >= MaxSlots) return false;

        items[data.itemId] = new ItemEntry { data = data, state = ItemState.Raw, charges = 0 };
        slotOrder.Add(data.itemId);

        if (SelectedSlot < 0) SelectedSlot = 0; // auto-select the first item
        OnChanged?.Invoke();
        return true;
    }

    public void Select(int slot)
    {
        if (slot < 0 || slot >= slotOrder.Count) return;
        SelectedSlot = slot;
        OnChanged?.Invoke();
    }

    /// <summary>
    /// Call this from a puzzle when the clue is solved.
    /// Example: inventory.Arm("camera");
    /// </summary>
    public bool Arm(string id)
    {
        if (!items.TryGetValue(id, out ItemEntry entry) || entry.state == ItemState.Armed) return false;
        entry.state = ItemState.Armed;
        entry.charges = entry.data.armedCharges;
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>Spend one charge of the selected item. Returns false if it can't be used.</summary>
    public bool UseSelected()
    {
        ItemEntry entry = GetSelected();
        if (entry == null || entry.state != ItemState.Armed) return false;

        if (entry.data.armedCharges > 0)
        {
            if (entry.charges <= 0) return false;
            entry.charges--;
            OnChanged?.Invoke();
        }
        return true;
    }
}