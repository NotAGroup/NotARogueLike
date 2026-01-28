using UnityEngine;

[DisallowMultipleComponent]
public class SlimeWobble : MonoBehaviour
{
    [SerializeField] Renderer targetRenderer;
    [SerializeField] CharacterController cc;

    [SerializeField] float maxSpeed = 6f;
    [SerializeField] float targetGain = 1.2f;

    [SerializeField] float frequency = 1.6f;
    [Range(0.05f, 0.95f)]
    [SerializeField] float dampingRatio = 0.45f;

    [SerializeField] float outputScale = 1.0f;
    [SerializeField] float dirSmooth = 12f;
    [SerializeField] float speedDeadZone = 0.03f;
    [SerializeField] bool planarWhenGrounded = true;

    MaterialPropertyBlock mpb;

    Vector3 lastPos;
    Vector3 lastVel;
    Vector3 dirWS;

    float x;
    float v;

    static readonly int ID_DeformDir = Shader.PropertyToID("_DeformDir");
    static readonly int ID_DeformAmp = Shader.PropertyToID("_DeformAmp");
    static readonly int ID_Wobble = Shader.PropertyToID("_Wobble");

    void OnEnable()
    {
        if (!targetRenderer) targetRenderer = GetComponentInChildren<Renderer>();
        if (!cc) cc = GetComponent<CharacterController>();
        if (mpb == null) mpb = new MaterialPropertyBlock();

        lastPos = transform.position;
        lastVel = Vector3.zero;
        dirWS = transform.forward;

        x = 0f;
        v = 0f;
    }

    void Update()
    {
        if (!targetRenderer) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 pos = transform.position;
        Vector3 velRaw = (pos - lastPos) / dt;

        bool grounded = cc && cc.isGrounded;
        Vector3 vel = velRaw;

        if (planarWhenGrounded && grounded)
            vel = Vector3.ProjectOnPlane(vel, Vector3.up);

        float speed = vel.magnitude;

        if (speed > speedDeadZone)
        {
            Vector3 targetDir = vel / speed;
            dirWS = Vector3.Slerp(dirWS, targetDir, 1f - Mathf.Exp(-dirSmooth * dt));
        }

        float target = Mathf.Clamp01((speed * targetGain) / Mathf.Max(1e-3f, maxSpeed));

        float omega = frequency * 2f * Mathf.PI;
        float zeta = dampingRatio;

        float a = -2f * zeta * omega * v - omega * omega * (x - target);
        v += a * dt;
        x += v * dt;

        float signedAmp = Mathf.Clamp(x * outputScale, -1f, 1f);

        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetVector(ID_DeformDir, dirWS);
        mpb.SetFloat(ID_DeformAmp, signedAmp);
        mpb.SetFloat(ID_Wobble, 0f);
        targetRenderer.SetPropertyBlock(mpb);

        lastPos = pos;
        lastVel = velRaw;
    }
}
