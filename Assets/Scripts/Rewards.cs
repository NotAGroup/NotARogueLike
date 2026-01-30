using UnityEngine;

public class Rewards : MonoBehaviour
{
    [Header("Dropped Item")]
    public GameObject droppedItemPrefab;

    [Header("Rewards")]
    public ItemSlot[] itemSlots;
    public int gold;
    public int xp;

    private Bounds placementBox;

    public void SetSingleItem(ItemSlot slot) 
    {
        if (slot.count > 0 && slot.storedItem != null)
            itemSlots = new ItemSlot[] {slot};
        else
            itemSlots = null;
    }

    public void SetItems(ItemSlot[] slots) 
    {
        itemSlots = slots;
    }

    void Start() 
    {
        // compute extent of placement box from collider or mesh
        if (TryGetComponent<BoxCollider>(out BoxCollider box)) 
        {
            placementBox.center = box.center;
            placementBox.size = box.size;
        } 
        else if (TryGetComponent<Collider>(out Collider coll)) 
        {
            // bounds are in world space, transform to local
            Bounds bounds = coll.bounds;
            placementBox.center = transform.InverseTransformPoint(bounds.center);
            placementBox.size = transform.InverseTransformVector(bounds.size);
        } 
        else if (TryGetComponent<MeshFilter>(out MeshFilter mesh)) 
        {
            Bounds bounds = mesh.mesh.bounds;
            placementBox.center = bounds.center;
            placementBox.size = bounds.size;
        }
    }

    public void Drop() 
    {
        if (itemSlots != null) 
        {
            for (int i = 0; i < itemSlots.Length; i++)
            {
                ItemSlot slot = itemSlots[i];
                GameObject instance = Instantiate(droppedItemPrefab, SamplePosition(), SampleRotation());
                instance.GetComponent<DroppedItem>().SetItem(slot.storedItem, slot.count);
                instance.name = slot.storedItem.name;
            }
        }

        if (gold > 0) {
            GameObject instance = Instantiate(droppedItemPrefab, SamplePosition(), SampleRotation());
            instance.GetComponent<DroppedItem>().SetCurrency(Currency.Gold, gold);
            instance.name = "Gold";
        }

        if (xp > 0) {
            GameObject instance = Instantiate(droppedItemPrefab, SamplePosition(), SampleRotation());
            instance.GetComponent<DroppedItem>().SetCurrency(Currency.XP, xp);
            instance.name = "XP";
        }
    }

    private Vector3 SamplePosition() 
    {
        return transform.TransformPoint(placementBox.center + new Vector3(Random.Range(-placementBox.min.x, placementBox.max.x), Random.Range(placementBox.min.y, placementBox.max.y), Random.Range(-placementBox.min.z, placementBox.max.z)));
    }

    private Quaternion SampleRotation() 
    {
        return Quaternion.Euler(0.0f, Random.Range(0.0f, 360.0f), 0.0f);
    }
}
