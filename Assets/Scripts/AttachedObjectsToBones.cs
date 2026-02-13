using UnityEngine;

public class AttachedObjectsToBones : AttachedObjects
{
    public SkinnedMeshRenderer mesh;

    // decreases the distance to the selected bone by this factor
    public float boneSnappingFactor;

    // 
    public override void Attach(Transform obj) {
        if (mesh == null) {
            base.Attach(obj);
            return;
        }

        // find bone to attach to
        Transform bone = mesh.bones[0];
        float sqrDist = float.MaxValue;
        foreach (var b in mesh.bones) {
            float d = b.InverseTransformPoint(obj.position).sqrMagnitude;
            if (d < sqrDist) {
                bone = b;
                sqrDist = d;
            }
        }


        int idx = base.AttachTo(obj, bone);
        if (idx > 0)
            offsets[idx] *= boneSnappingFactor;
    }

    void FixedUpdate() {
        if (visible)
            base.UpdatePositions();
    }

    void OnDestroy() {
        base.DetachAll();
    }
}

