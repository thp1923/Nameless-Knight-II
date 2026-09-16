using Invector.vCharacterController;
using System.Collections;
using UnityEngine;

public class PlayerDodge : MonoBehaviour
{
    private Animator anim;
    internal bool isDodging = false;
    private Vector3 dodgeDirection;
    private Rigidbody rb;
    private vThirdPersonController controller;

    [Header("Dodge Settings")]
    public float dodgeSpeed = 6f;
    public KeyCode dodgeKey = KeyCode.Space;
    public Transform positionToSpawn;
    public int staminaLost = 30;

    private float holdTime = 0f;
    public float holdThreshold = 0.3f;
    private bool isHolding = false;
    private bool holdActionTriggered = false;

    [Header("Skinned Mesh Relashed")]
    private SkinnedMeshRenderer[] skinnedMeshRenderers;

    [Header("Shader Relashed")]
    public Material mat;
    public string shaderVarRef;
    public float shaderVarRate = 0.1f;
    public float shaderVarRefreshRate = 0.05f;

    [Header("Mesh Relashed")]
    public float meshRefreshRate = 0.1f;
    public float activeTime = 2f;
    public float meshDestroyDelay = 3f;

    private bool isActiveTrail;

    private PlayerAttackController attackCtrl;
    LockController lockCtrl;


    private void Start()
    {
        attackCtrl = GetComponent<PlayerAttackController>();
        lockCtrl = GetComponent<LockController>();
        anim = GetComponent<Animator>();
        controller = GetComponent<vThirdPersonController>();
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // Bắt đầu nhấn
        if (Input.GetKeyDown(dodgeKey) && (!lockCtrl.Inventory.activeSelf || !lockCtrl.Quest.activeSelf))
        {
            holdTime = 0f;
            isHolding = true;
            holdActionTriggered = false;
        }

        // Đang giữ
        if (isHolding && Input.GetKey(dodgeKey) && (!lockCtrl.Inventory.activeSelf || !lockCtrl.Quest.activeSelf))
        {
            holdTime += Time.deltaTime;

            if (holdTime >= holdThreshold && !holdActionTriggered)
            {
                holdActionTriggered = true;
                return; // 👉 gọi ngay khi vừa qua ngưỡng giữ
            }
        }
        if (Input.GetKeyUp(dodgeKey) && (!lockCtrl.Inventory.activeSelf || !lockCtrl.Quest.activeSelf))
        {
            if (!holdActionTriggered)
            {
                if (!isDodging && GetComponent<Stamina>().stamina >= staminaLost && !attackCtrl.isAttacking)
                {
                    Vector3 input = controller.input;

                    if (controller.isStrafing && input.sqrMagnitude > 0.01f)
                    {
                        Vector2 inputDir = new Vector2(input.x, input.z);
                        Vector2 clampedInput = new Vector2(
                            inputDir.x != 0 ? Mathf.Sign(inputDir.x) : 0,
                            inputDir.y != 0 ? Mathf.Sign(inputDir.y) : 0
                        );

                        anim.SetFloat("DodgeX", clampedInput.x);
                        anim.SetFloat("DodgeY", clampedInput.y);
                    }
                    else
                    {
                        anim.SetFloat("DodgeX", 0f);
                        anim.SetFloat("DodgeY", 1f); // default forward
                    }

                    anim.SetTrigger("Dodge");
                    GetComponent<Stamina>().TakeStamina(staminaLost);
                }
            }

            isHolding = false;
            holdActionTriggered = false;
        }
        // Bắt đầu hiệu ứng trail nếu đang dodge
        if (isDodging && !isActiveTrail)
        {
            isActiveTrail = true;
            StartCoroutine(ActivateTrail(activeTime));
        }
    }

    private void FixedUpdate()
    {
        if (isDodging)
        {
            Vector3 move = dodgeDirection * dodgeSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + move);
        }
    }

    public void StartDodge()
    {
        isDodging = true;

        if (controller.isStrafing)
        {
            dodgeDirection = controller.moveDirection.normalized;
            if (dodgeDirection.sqrMagnitude < 0.01f)
                dodgeDirection = transform.forward;
        }
        else
        {
            dodgeDirection = transform.forward;
        }

        controller.lockMovement = true;
        controller.lockRotation = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void EndDodge()
    {
        isDodging = false;

        controller.lockMovement = false;
        controller.lockRotation = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private IEnumerator ActivateTrail(float timeActive)
    {
        if (!isDodging)
            yield break;

        while (timeActive > 0)
        {
            timeActive -= meshRefreshRate;

            if (skinnedMeshRenderers == null)
                skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (var smr in skinnedMeshRenderers)
            {
                GameObject gObj = new GameObject("TrailMesh");
                gObj.transform.SetLocalPositionAndRotation(positionToSpawn.position, positionToSpawn.rotation);

                MeshRenderer mr = gObj.AddComponent<MeshRenderer>();
                MeshFilter mf = gObj.AddComponent<MeshFilter>();
                Mesh mesh = new Mesh();

                smr.BakeMesh(mesh);
                mf.mesh = mesh;
                mr.material = mat;

                StartCoroutine(AnimateMaterialFloat(mr.material, 0, shaderVarRate, shaderVarRefreshRate));
                Destroy(gObj, meshDestroyDelay);
            }

            yield return new WaitForSeconds(meshRefreshRate);
        }

        isActiveTrail = false;
    }

    private IEnumerator AnimateMaterialFloat(Material mat, float goal, float rate, float refreshRate)
    {
        float valueToAnimate = mat.GetFloat(shaderVarRef);

        while (valueToAnimate > goal)
        {
            valueToAnimate -= rate;
            mat.SetFloat(shaderVarRef, valueToAnimate);
            yield return new WaitForSeconds(refreshRate);
        }
    }
}
