using UnityEngine;
using System;

public class DroppedItem : MonoBehaviour
{
    [Header("Rendering")]
    public Vector3 modelPosition;
    public Vector3 modelScale;
    public Quaternion modelRotation;

    // 
    public ItemDefinition item {get; private set;}
    public Currency currency {get; private set;}
    public int count {get; private set;}

    // how many instances to pick up with single interaction
    public int countPerInteraction;

    public string interactionHintFormat;

    private void ComputeCollider(Transform instance)
    {
        // compute extent of collider (box collider for ease of use)
        BoxCollider myCollider = GetComponent<BoxCollider>();
        if (instance.TryGetComponent<Collider>(out Collider coll)) {
            Bounds bounds = coll.bounds;
            myCollider.center = transform.InverseTransformPoint(instance.TransformPoint(bounds.center));
            myCollider.size = transform.InverseTransformVector(instance.TransformVector(bounds.size));
        } else if (instance.TryGetComponent<MeshFilter>(out MeshFilter mesh)) {
            Bounds bounds = mesh.mesh.bounds;
            myCollider.center = transform.InverseTransformPoint(instance.TransformPoint(bounds.center));
            myCollider.size = transform.InverseTransformVector(instance.TransformVector(bounds.size));
        }
    }

    public void SetItem(ItemDefinition item, int count) {
        this.item = item;
        this.count = count;

        if (countPerInteraction < count) countPerInteraction = count;

        // put item name into interaction hint
        GetComponent<InteractionHint>().Text = String.Format(interactionHintFormat, item.displayName, count);

        // instantiate prefab as child
        Transform instance = Instantiate(item.itemModel, transform).transform;

        ComputeCollider(instance);
    }

    public void SetCurrency(Currency currency, int count) {
        this.item = null;
        this.count = count;
        this.currency = currency;

        if (countPerInteraction < count) countPerInteraction = count;


        // put item name into interaction hint
        GetComponent<InteractionHint>().Text = String.Format(interactionHintFormat, Enum.GetName(typeof(Currency), currency), count);

        // instantiate prefab as child
        CurrencyDefinitions currencyDefs = GameObject.Find("Definitions").GetComponent<CurrencyDefinitions>();
        Transform instance = Instantiate(currencyDefs[currency].currencyModel, transform).transform;

        ComputeCollider(instance);
    }

    public void Disable() {
        count -= countPerInteraction;
        if (count <= 0)
            Destroy(gameObject); //suicide this gameobject
    }

    public int Cost { get => item.cost; }
    public int Count { get => count; }
    public string ItemName { get => item.name; }

}
