using UnityEngine;
using Invector.vCharacterController;
using System.Collections;

public class MoveManager : MonoBehaviour
{
    vThirdPersonController tcp;
    Animator anim;
    Rigidbody rb;
    public int staminaLost = 3;
    public float timeLostStaminaRun;
    float _timeLostStaminaRun;
    // Start is called before the first frame update
    void Start()
    {
        tcp = GetComponent<vThirdPersonController>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        StartCoroutine(IsKinematic(false));
    }

    // Update is called once per frame
    void Update()
    {
        _timeLostStaminaRun -= Time.deltaTime;

        if (anim.GetBool("IsSprinting") && _timeLostStaminaRun <= 0 && GetComponent<Stamina>().stamina >= staminaLost)
        {
            GetComponent<Stamina>().TakeStamina(staminaLost);
            _timeLostStaminaRun = timeLostStaminaRun;
        }
    }

    IEnumerator IsKinematic(bool isKine)
    {
        yield return new WaitForSeconds(0.5f);
        rb.isKinematic = isKine;
    }

    public void CheckLockMove(bool Lock)
    {
        tcp.lockMovement = Lock;
        tcp.lockRotation = Lock;
        if (Lock)
            anim.SetFloat("InputMagnitude", -1f);
        else
            return;
    }

    public void CheckSleep(bool Check)
    {
        if (Check)
            rb.Sleep();
        else 
            rb.WakeUp();
    }

    public void CheckDrag(bool Drag)
    {
        if (Drag)
            rb.linearDamping = 100f;
        else
            rb.linearDamping = 0f;
    }
}
