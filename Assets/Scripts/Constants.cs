using UnityEngine;

public class Constants : MonoBehaviour
{
    public int itemSlots;
    public int hotbarSlots;

    public ItemType[] inventoryMask;

    // definitions for new run
    public int initialGold;
    public int initialXP;
    public int initialAmmo;

    public void OnValidate() 
    {
        if (!(hotbarSlots >= 0 && hotbarSlots <= itemSlots))
        {
            Debug.LogError("hotbarSlots has be positive and smaller than itemSlots");
        }

        if(inventoryMask.Length != hotbarSlots)
        {
            Debug.LogError("length of inventoryMask has to be equal to itemSlots");
        }
    }
}
