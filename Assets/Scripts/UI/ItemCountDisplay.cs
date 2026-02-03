using UnityEngine;
using System.Collections.Generic;

public class ItemCountDisplay : MonoBehaviour
{
    private GameObject player;
    private Inventory playerInventory;
    private ItemDefinitions itemDefinitions;

    public ItemType itemsToTrack;

    // 
    private List<ItemDefinition> itemDefsToTrack;
    private GameObject[] slots;

    void InitializeSlots() {
        itemDefsToTrack = new();
        foreach(ItemDefinition def in itemDefinitions.definitions)
        {
            if ((def.type & itemsToTrack) != 0)
                itemDefsToTrack.Add(def);
        }

        int numSlots = itemDefsToTrack.Count;
        slots = new GameObject[numSlots];

        UIGrid grid = GetComponent<UIGrid>();
        for (int i = 0; i < numSlots; i++) {
            slots[i] = grid.InstantiateGridEntry(i, 0, numSlots, 1);
        }

        grid.Commit();
    }

    public void UpdateSlots() {
        // check if slot count has changed
        if (slots == null || 1 != slots.Length)
            InitializeSlots();

        int slotIndex = 0;
        for (int itemIndex = 0; itemIndex < itemDefsToTrack.Count; itemIndex++) {
            GameObject slot = slots[slotIndex];
            ItemHotbarSlot s = slot.GetComponent<ItemHotbarSlot>();

            // count number of items
            ItemDefinition def = itemDefsToTrack[itemIndex];
            int count = 0;
            foreach(ItemSlot itemSlot in playerInventory.items.slots)
            {
                if (itemSlot.storedItem == def)
                    count += itemSlot.count;
            }

            // show in slot
            if (count > 0)
            {
                slot.SetActive(true);
                s.SetItem(def, count);
                s.SetSelected(false);
                slotIndex++;
            }
        }

        // disable other slots
        for (; slotIndex < slots.Length; slotIndex++)
        {
            slots[slotIndex].SetActive(false);
        }
    }

    void OnEnable()
    {
        UpdateSlots();
    }

    void Awake() {
        player = GameObject.Find("Player");
        playerInventory = player.GetComponent<Inventory>();
        itemDefinitions = GameObject.Find("Definitions").GetComponent<ItemDefinitions>();
    }
}
