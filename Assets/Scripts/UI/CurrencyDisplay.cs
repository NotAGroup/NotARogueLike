using UnityEngine;

public class CurrencyDisplay : MonoBehaviour
{
    private GameObject player;
    private Inventory playerInventory;
    private CurrencyDefinitions currencyDefinitions;

    [Header("Rendering")]
    public GameObject slotPrefab;

    public Vector2 slotPositionLeft;
    public Vector2 slotPositionRight;

    // 
    private GameObject[] slots;

    void InitializeSlots() {
        int numSlots = playerInventory.numCurrencySlots;
        slots = new GameObject[numSlots];

        for (int i = 0; i < numSlots; i++) {
            slots[i] = Instantiate(slotPrefab, transform);
            slots[i].transform.localPosition = Vector2.Lerp(slotPositionLeft, slotPositionRight, (float)i / numSlots);
        }
    }

    void UpdateSlots() {
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

    // Update is called once per frame
    void Update()
    {
        // check if slot count has changed
        if (slots == null || playerInventory.numCurrencySlots != slots.Length)
            InitializeSlots();

        UpdateSlots();
    }
}
