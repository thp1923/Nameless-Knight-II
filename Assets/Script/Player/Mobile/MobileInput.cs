using UnityEngine;

public class MobileInput : MonoBehaviour
{
    public static MobileInput Instance { get; private set; }

    [Header("Movement")]
    public Vector2 moveInput;

    [Header("Buttons")]
    public bool sprint;
    public bool attack;
    public bool dodge;
    public bool lockTarget;
    public bool skillE;
    public bool skillQ;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // =========================
    // MOVEMENT
    // =========================

    public void SetMove(Vector2 value)
    {
        moveInput = Vector2.ClampMagnitude(value, 1f);
    }

    public void SetHorizontal(float value)
    {
        moveInput.x = Mathf.Clamp(value, -1f, 1f);
    }

    public void SetVertical(float value)
    {
        moveInput.y = Mathf.Clamp(value, -1f, 1f);
    }

    public void StopMove()
    {
        moveInput = Vector2.zero;
    }

    // =========================
    // BUTTONS
    // =========================

    public void SprintDown()
    {
        sprint = true;
    }

    public void SprintUp()
    {
        sprint = false;
    }

    public void AttackDown()
    {
        attack = true;
    }

    public void AttackUp()
    {
        attack = false;
    }

    public void DodgeDown()
    {
        dodge = true;
    }

    public void DodgeUp()
    {
        dodge = false;
    }

    public void LockTarget()
    {
        lockTarget = true;
    }

    public void UseSkillE()
    {
        skillE = true;
    }

    public void UseSkillQ()
    {
        skillQ = true;
    }

    // =========================
    // CONSUME ONE-SHOT INPUT
    // =========================

    public bool ConsumeAttack()
    {
        if (!attack)
            return false;

        attack = false;
        return true;
    }

    public bool ConsumeDodge()
    {
        if (!dodge)
            return false;

        dodge = false;
        return true;
    }

    public bool ConsumeLock()
    {
        if (!lockTarget)
            return false;

        lockTarget = false;
        return true;
    }

    public bool ConsumeSkillE()
    {
        if (!skillE)
            return false;

        skillE = false;
        return true;
    }

    public bool ConsumeSkillQ()
    {
        if (!skillQ)
            return false;

        skillQ = false;
        return true;
    }
}