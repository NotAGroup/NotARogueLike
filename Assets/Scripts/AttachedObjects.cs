using UnityEngine;

// class for managing objects that are temporarily attached to others (e.g. projectiles that have hit something)
public class AttachedObjects : MonoBehaviour
{
    // ring buffer size
    public int maximumAttachedObjects = 10;

    // information for each attached object
    protected int index = 0;
    protected Transform[] attachmentTargets;
    protected Transform[] attachedObjects;
    protected Vector3[] offsets;
    protected Quaternion[] rotations;

    // only update positions when visible
    protected bool visible = true;
 
    void OnBecameInvisible() {
        visible = false;
    }
 
    void OnBecameVisible() {
        visible = true;
    }

    void FixedUpdate() {
        if (!visible) return;

        UpdatePositions();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public AttachedObjects()
    {
        attachedObjects = new Transform[maximumAttachedObjects];
        attachmentTargets = new Transform[maximumAttachedObjects];
        offsets = new Vector3[maximumAttachedObjects];
        rotations = new Quaternion[maximumAttachedObjects];
    }

    public virtual void Attach(Transform obj) {
        AttachTo(obj, transform);
    }

    protected int AttachTo(Transform obj, Transform target) {
        if (maximumAttachedObjects == 0) {
            return -1;
        }

        // if overwriting, detach first
        if (attachedObjects[index] != null)
            OnDetach(attachedObjects[index]);

        attachedObjects[index] = obj;
        attachmentTargets[index] = target;

        offsets[index] = target.InverseTransformPoint(obj.position);
        rotations[index] =  Quaternion.Inverse(target.rotation) * obj.rotation;

        // call attachement function
        OnAttach(attachedObjects[index]);

        int result = index;
        index = (index + 1) % maximumAttachedObjects;
        return result;
    }

    protected void UpdatePositions() {
        for (int i = 0; i < maximumAttachedObjects; i++) {
            if (attachedObjects[i] == null || attachmentTargets[i] == null) continue;

            attachedObjects[i].SetPositionAndRotation(attachmentTargets[i].TransformPoint(offsets[i]),  attachmentTargets[i].rotation * rotations[i]);
        }
    }

    protected void DetachAll() {
        // update positions now if not done during update function
        if (!visible) UpdatePositions();

        for (int i = 0; i < maximumAttachedObjects; i++) {
            if (attachedObjects[i] == null || attachmentTargets[i] == null) continue;

            OnDetach(attachedObjects[i]);
        }
    }

    void OnDestroy() {
        DetachAll();
    }

    protected void OnAttach(Transform obj) {
	    if (obj.TryGetComponent<Attachable>(out Attachable a)) {
	    	a.OnAttach(this);
	    }
    }

    protected void OnDetach(Transform obj) {
	    if (obj.TryGetComponent<Attachable>(out Attachable a)) {
	    	a.OnDetach();
	    }
    }
}
