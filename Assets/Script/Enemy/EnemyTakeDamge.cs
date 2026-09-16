using StatsManager;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EnemyTakeDamge : StatsAlive
{
    public LayerMask playerMask;
    public float rangeShow = 70f;
    public GameObject HP_Bar;
    bool isShow;
    bool isDeath;

    Animator aim;
    public GameObject me;
    public TextMeshProUGUI damPopUp;

    public int Point;

    internal bool isHurt;
    [Header("---------Items Drop-----------")]
    public GameObject drop;
    public List<Item> itemsDrop;

    public float[] dropRate;
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        aim = GetComponent<Animator>();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        ShowAndHide();
        if(HP_Bar == null) return;
        if(isShow) HP_Bar.SetActive(true);
        else HP_Bar.SetActive(false);
        if(currentHP <= (int)(MaxHP/2f) && !isHurt)
        {
            isHurt = true;
        }
    }

    void ShowAndHide()
    {
        if(playerMask == 0) return;
        Collider[] playerIn = Physics.OverlapSphere(gameObject.transform.position, rangeShow, playerMask);
        if(playerIn.Length > 0 )
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
        switch (type)
        {
            case TypeTakeDamge.Only:
                base.TakeDamge(damge, stunDamge, trueDamge);  
                damPopUp.text = DamPopUp.ToString();
                StartCoroutine(DamgePopUp());
                if(stunDamge > StunResistance)
                {
                    aim.SetTrigger("Hit");
                }
                break;
            case TypeTakeDamge.Branch:
                GetComponent<BrandHpEnemy>().TakeDam(damge, stunDamge, trueDamge);
                break;
            default:
                break;
        }
        if(currentHP <= 0)
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
        FindFirstObjectByType<PlayerAim>().RemoveEnemy(gameObject);
        // Danh sách tạm cho các item được chọn ngẫu nhiên
        List<Item> droppedItems = new List<Item>();

        for (int i = 0; i < itemsDrop.Count; i++)
        {
            if (i >= dropRate.Length) break; // an toàn: tránh lỗi vượt mảng

            if (Random.Range(0f, 1f) <= dropRate[i])
            {
                droppedItems.Add(itemsDrop[i]);
            }
        }

        // Nếu có item được chọn, mới instantiate drop
        if (droppedItems.Count > 0)
        {
            GameObject go = Instantiate(drop,new Vector3(transform.position.x, transform.position.y + 0.75f, transform.position.z), Quaternion.identity);
            var itemPickup = go.GetComponent<ItemPickUp>();

            if (itemPickup != null)
            {
                itemPickup.items.AddRange(droppedItems);
            }
        }
        FindFirstObjectByType<UpgradeStats>().AddPoint(Point);
        if(me != null)
            Destroy(me);
    }
}
