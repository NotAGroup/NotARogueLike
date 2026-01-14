using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using TMPro;
using System.Text;

public class InventoryUI : MonoBehaviour
{
    private Player player;
    private Inventory inventory;

    [Header("Rendering")]
    public GameObject slotPrefab;
    public Transform slotParent;

    // transforms that define slot positions
    public RectTransform slotPositionBotLeft;
    public RectTransform slotPositionBotRight;
    public RectTransform slotPositionTopLeft;

    // positions
    private Vector2 slotPositionOrigin;
    private Vector2 slotPositionRight;
    private Vector2 slotPositionBottom;

    [Header("Selected Item")]
    public TMP_Text itemNameText;
    public TMP_Text itemDescriptionText;
    public TMP_Text itemBuffText;
    
    public int  numColumns;
    private int numHotbarSlots;
    private int numSlots;

    // selection state
    public int currentSlot;
    public bool grabbed;

    // instantiated slots
    private GameObject[] slots = null;

    void UpdateSelectedItem(ItemDefinition item, int count)
    {
        if (item == null)
        {
            return;
        }

        // name and count
        if (count > 1) 
        {
            itemNameText.text = String.Format("{0}x {1}", count, item.displayName);
        } 
        else 
        {
            itemNameText.text = item.displayName;
        }


        // description
        itemDescriptionText.text = item.description;

        // buffs
        StringBuilder builder = new();
        if (item.itemBuffs != null && item.itemBuffs.definitions.Length > 0)
        {
            foreach (var pair in item.itemBuffs.definitions)
            {
                string stat = pair.key;
                float value = pair.def.value;
                char op = pair.def.operation == ScalingFormula.Operation.Multiplication ? 'x' : '+';
                builder.AppendFormat("\n\t{0}: {1}{2}", stat, op, value);
            }
        }
        itemBuffText.text = builder.ToString();
    }

    void InitializeSlots() {
        numSlots = inventory.numItemSlots;
        numHotbarSlots = inventory.numHotbarSlots;
        slots = new GameObject[numSlots];

        for (int i = 0; i < numSlots; i++) {
            slots[i] = Instantiate(slotPrefab, slotParent);
            // left-to-right
            float right = (float)(i % numColumns) / (numColumns - 1);
            Vector2 pos = slotPositionOrigin + slotPositionRight * right;
            // top-to-bottom
            float down = (float)(i / numColumns) / (float)Math.Ceiling((double)numSlots / numColumns - 1);
            pos += slotPositionBottom * down;
            // slots[i].GetComponent<RectTransform>().anchoredPosition = pos;
            slots[i].transform.localPosition = pos;
        }
    }

    void UpdateSlots() {
        for (int i = 0; i < slots.Length; i++) {
            GameObject slot = slots[i];
            ItemHotbarSlot s = slot.GetComponent<ItemHotbarSlot>();
            s.SetItem(inventory.container[i].storedItem, inventory.container[i].count);
            s.SetSelected(i == currentSlot);
            slot.GetComponent<Button>().onClick.AddListener(delegate { OnClick(i); });
        }

        ItemSlot selected = inventory.container[currentSlot];
        UpdateSelectedItem(selected.storedItem, selected.count);
    }

    void OnClick(int slot) {
        Debug.Log("slot " + slot + " has been clicked");
    }

    private void SwitchSlot(bool right) {
        int newSlot = right ? ((currentSlot + 1 + numSlots) % numSlots) : (currentSlot - 1 + numSlots) % numSlots;
        if (grabbed)
            inventory.container.SwapItems(currentSlot, newSlot);
        currentSlot = newSlot;
    }

    public void MoveSelection(Vector2 delta) {
        if (numSlots == 0) return;
        int newSlot = (currentSlot + (int)Math.Round(delta.x) + numSlots) % numSlots;
        newSlot = (newSlot + (int)Math.Round(delta.y) * numColumns + numSlots) % numSlots;
        if (grabbed)
            inventory.container.SwapItems(currentSlot, newSlot);
        currentSlot = newSlot;
    }

    public void ToggleItemGrabbed() {
        grabbed = !grabbed;
    }

    void Start() {
        player = GameObject.Find("Player").GetComponent<Player>();
        inventory = GameObject.Find("Player").GetComponent<Inventory>();
    }

    void Awake() {
        slotPositionOrigin = slotPositionBotLeft.localPosition;
        slotPositionRight  = slotPositionBotRight.localPosition;
        slotPositionRight -= slotPositionOrigin;
        slotPositionBottom = slotPositionTopLeft.localPosition;
        slotPositionBottom-= slotPositionOrigin;
    }

    // Update is called once per frame
    void Update()
    {
        // check if slot count has changed
        numSlots = inventory.numItemSlots;
        if (slots == null || numSlots != slots.Length)
            InitializeSlots();

        UpdateSlots();
    }
}
