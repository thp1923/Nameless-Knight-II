using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StatsManager;
using TMPro;
using UnityEngine.SceneManagement;

public class HpBossDragon : StatsAlive
{
    Animator aim;
    public GameObject me;
    public TextMeshProUGUI damPopUp;


    public LayerMask playerMask;
    public float rangeShow = 70f;
    public GameObject HP_Bar;
    bool isShow;
    bool isDeath;
    // Start is called before the first frame update
    public int Point;

    [Header("---------Items Drop-----------")]
    public GameObject drop;
    public List<Item> itemsDrop;
    protected override void Start()
    {
        base.Start();
        aim = GetComponent<Animator>();
    }

    protected override void Update()
    {
        base.Update();
        ShowAndHide();
        if (HP_Bar == null) return;
        if (isShow) HP_Bar.SetActive(true);
        else HP_Bar.SetActive(false);
    }

    void ShowAndHide()
    {
        Collider[] playerIn = Physics.OverlapSphere(gameObject.transform.position, rangeShow, playerMask);
        if (playerIn.Length > 0)
        {
            isShow = true;
        }
        else
        {
            isShow = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(gameObject.transform.position, rangeShow);
    }

    public override void TakeDamge(int damge, int stunDamge, int trueDamge)
    {
        if (isDeath) return;
        base.TakeDamge(damge, stunDamge, trueDamge);
        damPopUp.text = DamPopUp.ToString();
        StartCoroutine(DamgePopUp());
        if (currentHP <= MaxHP / 2)
        {
            GetComponent<DragonAttackEffect>().TransPhase();
        }
        if (currentHP <= 0)
        {
            aim.SetBool("IsDeath", true);
            isDeath = true;
        }
    }

    IEnumerator DamgePopUp()
    {
        yield return new WaitForSeconds(0.5f);
        damPopUp.text = null;
    }

    public void Death()
    {
        GameObject go = Instantiate(drop, new Vector3(transform.position.x, transform.position.y + 0.75f, transform.position.z), Quaternion.identity);
        foreach (var itemDrop in itemsDrop)
        {
            go.GetComponent<ItemPickUp>().items.Add(itemDrop);
        }
        FindFirstObjectByType<UpgradeStats>().AddPoint(Point);
        if (me != null)
            Destroy(me);
    }
}
