using UnityEngine;

public class WarriorIKBridge : MonoBehaviour
{
    public Transform rightHandIKTarget;
    public Player playerRoot;

    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (playerRoot == null) playerRoot = GetComponentInParent<Player>();
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(1);

        bool useBowIK = state.IsName("Draw");

        if (useBowIK)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);

            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandIKTarget.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandIKTarget.rotation);
        }
        else
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
        }
    }
}
