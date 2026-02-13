using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimpleWASDMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private bool rotateToMove = false;
    [SerializeField] private float turnSpeed = 720f;

    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedStick = -2f;

    [SerializeField] private bool driveCamera = true;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 2.5f, -5f);
    [SerializeField] private float cameraFollowSpeed = 10f;
    [SerializeField] private float cameraLookHeight = 1.0f;

    private CharacterController cc;
    private Camera mainCam;
    private float verticalVelocity;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        mainCam = Camera.main;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        bool grounded = cc.isGrounded;
        if (grounded && verticalVelocity < 0f)
            verticalVelocity = groundedStick;

        if (grounded && Input.GetButtonDown("Jump"))
            verticalVelocity = Mathf.Sqrt(2f * jumpHeight * -gravity);

        verticalVelocity += gravity * dt;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 1f) input.Normalize();

        Vector3 moveDir = input;
        if (mainCam != null)
        {
            Vector3 camF = mainCam.transform.forward;
            camF.y = 0f;
            camF.Normalize();

            Vector3 camR = mainCam.transform.right;
            camR.y = 0f;
            camR.Normalize();

            moveDir = camF * input.z + camR * input.x;
        }

        if (rotateToMove && moveDir.sqrMagnitude > 1e-6f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * dt);
        }

        Vector3 velocity = moveDir * moveSpeed;
        velocity.y = verticalVelocity;

        cc.Move(velocity * dt);
    }

    private void LateUpdate()
    {
        if (!driveCamera) return;

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        Vector3 desiredPos = transform.position + cameraOffset;
        mainCam.transform.position = Vector3.Lerp(
            mainCam.transform.position,
            desiredPos,
            1f - Mathf.Exp(-cameraFollowSpeed * Time.deltaTime)
        );

        Vector3 lookTarget = transform.position + Vector3.up * cameraLookHeight;
        mainCam.transform.LookAt(lookTarget);
    }
}
