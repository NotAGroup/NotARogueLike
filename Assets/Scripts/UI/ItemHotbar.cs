using UnityEngine;
using System;

public class ItemHotbar : MonoBehaviour
{
    private GameObject player;
    private Inventory playerInventory;

    [Header("Rendering")]
    public GameObject slotPrefab;

    public Vector2 slotPositionLeft;
    public Vector2 slotPositionRight;

    // 
    private GameObject[] slots;

    public void Awake() {
        player = GameObject.Find("Player");
        playerInventory = player.GetComponent<Inventory>();
    }

    void InitializeSlots() {
        if (slots != null)  // destroy old slots
            foreach (GameObject s in slots)
                GameObject.Destroy(s);

        int numSlots = Math.Min(playerInventory.numHotbarSlots, playerInventory.numItemSlots);
        slots = new GameObject[numSlots];

        for (int i = 0; i < numSlots; i++) {
            slots[i] = Instantiate(slotPrefab, transform);
            slots[i].transform.localPosition = Vector2.Lerp(slotPositionLeft, slotPositionRight, (float)i / (numSlots - 1));
        }
    }

    void UpdateSlots() {
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++) {
            GameObject slot = slots[i];
            ItemHotbarSlot s = slot.GetComponent<ItemHotbarSlot>();
            s.SetItem(playerInventory.container[i].storedItem, playerInventory.container[i].count);
            s.SetSelected(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // check if slot count has changed
        int numSlots = Math.Min(playerInventory.numHotbarSlots, playerInventory.numItemSlots);
        if (slots == null || numSlots != slots.Length)
            InitializeSlots();

        UpdateSlots();
    }
}
