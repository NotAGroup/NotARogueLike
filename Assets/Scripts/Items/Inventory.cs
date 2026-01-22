using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    public int numHotbarSlots  {get; protected set;}
    public int numItemSlots {get => items.slots.Length;}
    public ItemContainer items { get; protected set;}
    private ItemType[] mask;

    // amount of currencies
    public int numCurrencySlots {get; protected set;} = Enum.GetValues(typeof(Currency)).Length;
    public CurrencyContainer currency { get; protected set; }

    public Inventory() {
        items = RunData.Instance.items;// new(RunData.Instance.items, mask);
        currency = RunData.Instance.currencies;

        GameSaver.subscribe(currency);
    }

    public void Start() {
        Constants defs = GameObject.Find("Definitions").GetComponent<Constants>();
        numHotbarSlots = defs.hotbarSlots;
        mask = defs.inventoryMask;
    }

    public void AddCurrency(Currency currency, int amount) 
    {
        this.currency[currency] += amount;
    }

    public void AddItem(ItemDefinition item, int count) 
    {
        items.AddItem(item, count, mask);
    }
}

public class ItemSlot {
    public ItemDefinition storedItem = null;
    public int count = 0;
}


[Serializable]
public class CurrencyContainer {
    [SaveAble]
    public int[] balances;

    public CurrencyContainer() {
        balances = new int[Enum.GetValues(typeof(Currency)).Length];
        for (int i = 0; i < balances.Length; i++) 
        {
            balances[i] = 0;
        }
    }

    public ref int this[Currency c] {
        get => ref balances[(int)c];
    }
}

public class ItemContainer {
    public ItemSlot[] slots {get; protected set;} = new ItemSlot[0];

    public ItemSlot this[int slot] {
        get => slots[slot];
    }

    public void PrintState() {
        StringBuilder output = new();
        for (int i = 0; i < slots.Length; i++) {
            output.AppendFormat("{0} : {1}({2}x), ", i, (slots[i].storedItem != null) ? slots[i].storedItem.name : "none", slots[i].count);
        }
        Debug.Log("inventory: " + output.ToString());
    }

    public void Resize(int newSize) {
        ItemSlot[] nslots = new ItemSlot[newSize];
        for (int i = 0; i < newSize; i++) 
        {
            nslots[i] = new();
        }

        // transfer items
        if (slots != null) {
            for(int i = 0; i < slots.Length && i < nslots.Length; i++) {
                nslots[i] = slots[i];
            }
        }

        slots = nslots;
    }

    public void Clear() {
        for(int i = 0; i < slots.Length; i++) {
            slots[i].count = 0;
            slots[i].storedItem = null;
        }
    }

    public void SwapItems(int first, int second) {
        if (first == second) return;

        ItemSlot tmp = slots[first];
        slots[first] = slots[second];
        slots[second] = tmp;
    }

    public void ConsumeItem(int slot) {
        if (slots[slot].count > 0)
        {
            slots[slot].count -= 1;
            if(slots[slot].count == 0)
                slots[slot].storedItem = null;
        }
    }

    // returns the number of items that have been added to the slot
    public int AddItem(int slot, ItemDefinition item, int count) {
        // there was a different item, overwrite it
        if (slots[slot].storedItem != item)
            slots[slot].count = 0;
        slots[slot].storedItem = item;

        int remainingSpace = item.maxPerInventorySlot - slots[slot].count;
        int actualCount = (remainingSpace < count) ? remainingSpace : count;
        slots[slot].count += actualCount;

        return actualCount;
    }

    // Selects an appropriate slot to add item, i.e. either a slot containing same item type or a free one
    public void AddItem(ItemDefinition item, int count) {
        while (count > 0) {
            int slot = GetSlotWithCapacity(item, 1);
            if (slot < 0)
                slot = GetFreeSlot();

            if (slot >= 0) {
                count -= AddItem(slot, item, count);
            } else {    // no free slots
                count = 0;
            }
        }
    }

    public int GetSlotWithCapacity(ItemDefinition item, int requiredRemainingCapacity = 1) {
        for (int i = 0; i < slots.Length; i++) {
            if (slots[i].storedItem == item && slots[i].count <= item.maxPerInventorySlot - requiredRemainingCapacity) {
                return i;
            }
        }
        return -1;
    }

    public int GetSlotContaining(ItemDefinition item, int requiredAmount = 1) 
    {
        for (int i = 0; i < slots.Length; i++) 
        {
            if (slots[i].storedItem == item && slots[i].count >= requiredAmount) 
            {
                return i;
            }
        }
        return -1;
    }

    public int GetFreeSlot() 
    {
        for (int i = 0; i < slots.Length; i++) 
        {
            if (slots[i].count == 0)
                return i;
        }
        return -1;
    }

    // Selects an appropriate slot to add item, i.e. either a slot containing same item type or a free one
    public void AddItem(ItemDefinition item, int count, ItemType[] mask) 
    {
        while (count > 0) 
        {
            int slot = GetSlotWithCapacity(item, 1);

            if (slot < 0)
            {
                slot = GetFreeSlotFor(item, mask);
            }

            if (slot >= 0) 
            {
                count -= AddItem(slot, item, count);
            }
            else
            {    // no free slots
                count = 0;
            }
        }
    }


    public int GetFreeSlotFor(ItemDefinition item, ItemType[] mask)
    {
        for (int i = 0; i < slots.Length; i++) 
        {
            if (mask != null && i < mask.Length && (item.type & mask[i]) == 0) 
            {
                continue;
            }

            if (slots[i].count == 0)
            {
                return i;
            }
        }
        return -1;
    }
}
