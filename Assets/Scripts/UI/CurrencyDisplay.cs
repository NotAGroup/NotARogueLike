using UnityEngine;

public class CurrencyDisplay : MonoBehaviour
{
    private GameObject player;
    private Inventory playerInventory;
    private CurrencyDefinitions currencyDefinitions;

    // 
    private GameObject[] slots;

    void InitializeSlots() {
        int numSlots = playerInventory.numCurrencySlots;
        slots = new GameObject[numSlots];

        UIGrid grid = GetComponent<UIGrid>();
        for (int i = 0; i < numSlots; i++) {
            slots[i] = grid.InstantiateGridEntry(i, 0, numSlots, 1);
        }

        grid.Commit();
    }

    public void UpdateSlots() {
        // check if slot count has changed
        if (slots == null || playerInventory.numCurrencySlots != slots.Length)
            InitializeSlots();

        for (int i = 0; i < slots.Length; i++) {
            GameObject slot = slots[i];
            ItemHotbarSlot s = slot.GetComponent<ItemHotbarSlot>();
            s.SetCurrency(currencyDefinitions[(Currency)i], playerInventory.currency[(Currency)i]);
            s.SetSelected(false);
        }
    }

    void Awake() {
        player = GameObject.Find("Player");
        playerInventory = player.GetComponent<Inventory>();
        currencyDefinitions = GameObject.Find("Definitions").GetComponent<CurrencyDefinitions>();
    }
}
