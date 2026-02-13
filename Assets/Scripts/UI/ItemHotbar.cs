using UnityEngine;
using System;

public class ItemHotbar : MonoBehaviour
{
    private Player player;
    private Inventory playerInventory;

    // 
    private GameObject[] slots;

    public void Awake() 
    {
        player = GameObject.Find("Player").GetComponent<Player>();
        playerInventory = GameObject.Find("Player").GetComponent<Inventory>();
    }

    void InitializeSlots() {
        if (slots != null)  // destroy old slots
            foreach (GameObject s in slots)
                GameObject.Destroy(s);

        int numSlots = Math.Min(player.numHotbarSlots, playerInventory.numItemSlots);
        slots = new GameObject[numSlots];

        UIGrid grid = GetComponent<UIGrid>();
        for (int i = 0; i < numSlots; i++) {
            slots[i] = grid.InstantiateGridEntry(i, 0, numSlots, 1);
        }
        grid.Commit();
    }

    void OnEnable()
    {
        UpdateSlots();
    }

    public void UpdateSlots() {
        // check if slot count has changed
        int numSlots = Math.Min(player.numHotbarSlots, playerInventory.numItemSlots);
        if (slots == null || numSlots != slots.Length)
            InitializeSlots();

        for (int i = 0; i < slots.Length; i++) {
            GameObject slot = slots[i];
            ItemHotbarSlot s = slot.GetComponent<ItemHotbarSlot>();
            s.SetItem(playerInventory.items[i].storedItem, playerInventory.items[i].count);
            s.SetUnselected();
        }
    }
}
