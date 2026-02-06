using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System;
using TMPro;
using System.Text;

public class InventoryUI : MonoBehaviour
{
    private Player player;
    private Inventory inventory;

    [Header("Selected Item")]
    public TMP_Text itemNameText;
    public TMP_Text itemDescriptionText;
    public TMP_Text itemBuffText;
    
    public int  numColumns;
    private int numSlots;

    // selection state
    public int currentSlot;
    private int grabbedSourceSlot;
    private bool grabbed { get => grabbedSourceSlot >= 0; }

    // instantiated slots
    private GameObject[] slots = null;

    void UpdateSelectedItem(ItemDefinition item, int count)
    {
        if (item == null)
        {
            itemNameText.text = "Nothing";
            itemDescriptionText.text = "This is a free slot in your bag";
            itemBuffText.text = "...";
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
        grabbedSourceSlot = -1;
        currentSlot = -1;
        numSlots = inventory.numItemSlots;
        slots = new GameObject[numSlots];

        UIGrid grid = GetComponent<UIGrid>();
        for (int i = 0; i < numSlots; i++) {
            // coefficients
            int numRows = numSlots / numColumns;
            slots[i] = grid.InstantiateGridEntry(i % numColumns, i / numColumns, numColumns, numRows);

            // box current index to pass it as reference to the lambda
            object index = i;
            slots[i].GetComponent<Button>().onClick.AddListener(() => OnClick((int)index));
        }

        grid.Commit();
    }

    void UpdateSlots() {
        if (inventory == null || inventory.items == null) return;

        for (int i = 0; i < slots.Length; i++) {
            GameObject slot = slots[i];
            ItemHotbarSlot s = slot.GetComponent<ItemHotbarSlot>();

            ItemSlot slotToShow = inventory.items[i];
            // if currently moving an item, show preview of items after swap
            if (grabbed)
            {
                if (i == currentSlot)
                {
                    slotToShow = inventory.items[grabbedSourceSlot];
                    s.SetGrabbed();
                }
                else if (i == grabbedSourceSlot && currentSlot >= 0 && currentSlot < inventory.numItemSlots)
                {
                    slotToShow = inventory.items[currentSlot];
                    s.SetSelected();
                }
                else
                {
                    s.SetUnselected();
                }
            }
            else
            {
                if (i == currentSlot)
                    s.SetSelected();
                else
                    s.SetUnselected();
            }

            s.SetItem(slotToShow.storedItem, slotToShow.count);
        }

        if (currentSlot >= 0 && currentSlot < inventory.numItemSlots)
        {
            ItemSlot selected = inventory.items[currentSlot];
            UpdateSelectedItem(selected.storedItem, selected.count);
        }
        else
        {
            UpdateSelectedItem(null, 0);
        }
    }

    void OnClick(int slot) {
        if (numSlots == 0) return;

        currentSlot = slot;
        ToggleItemGrabbed(true);
    }

    public void MoveSelection(Vector2 delta) {
        if (numSlots == 0) return;

        int newSlot = (currentSlot + (int)Math.Round(delta.x) + numSlots) % numSlots;
        newSlot = (newSlot + (int)Math.Round(delta.y) * numColumns + numSlots) % numSlots;
        currentSlot = newSlot;

        UpdateSlots();
    }

    public void ToggleItemGrabbed() {
        ToggleItemGrabbed(false);
    }

    public void ToggleItemGrabbed(bool resetSelection = false) {
        if (grabbed)
        {
            inventory.items.SwapItems(currentSlot, grabbedSourceSlot);
            grabbedSourceSlot = -1;
            if (resetSelection) currentSlot = -1;
        }
        else if (currentSlot >= 0)
        {
            grabbedSourceSlot = currentSlot;
        }

        UpdateSlots();
    }

    void OnEnable() {
        player = GameObject.Find("Player").GetComponent<Player>();
        inventory = GameObject.Find("Player").GetComponent<Inventory>();

        currentSlot = -1;
    
        if (slots == null || slots.Length != inventory.numItemSlots)
            InitializeSlots();

        UpdateSlots();
    }
}
