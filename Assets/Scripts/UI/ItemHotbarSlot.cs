using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using System;

using TMPro;

public class ItemHotbarSlot : MonoBehaviour
{
    public Color backgroundColor;
    public Color backgroundColorSelected;

    public GameObject itemText;
    public GameObject itemCount;
    public GameObject itemModel;

    public void SetItem(ItemDefinition item, int count) {
        if (count > 0 && item != null) {
            if (item.itemSprite != null) 
            {
                itemModel.GetComponent<Image>().sprite = item.itemSprite;
                itemModel.SetActive(true);
            }
            else 
            {
                itemModel.GetComponent<Image>().sprite = null;
                itemModel.SetActive(false);
            }
            itemText.GetComponent<TMP_Text>().text = item.displayName;
            itemCount.GetComponent<TMP_Text>().text = count.ToString();
        } else {
            itemModel.GetComponent<Image>().sprite = null;
            itemModel.SetActive(false);
            itemText.GetComponent<TMP_Text>().text = "";
            itemCount.GetComponent<TMP_Text>().text = "";
        }
    }

    public void SetCurrency(Currency currency, int count) {
        itemModel.GetComponent<Image>().sprite = null;
        itemModel.SetActive(false);
        itemText.GetComponent<TMP_Text>().text = Enum.GetName(typeof(Currency), currency);
        if (count > 0) {
            itemCount.GetComponent<TMP_Text>().text = count.ToString();
        } else {
            itemCount.GetComponent<TMP_Text>().text = "";
        }
    }

    public void SetSelected(bool selected) {
        GetComponent<Image>().color = selected ? backgroundColorSelected : backgroundColor;
    }
}
