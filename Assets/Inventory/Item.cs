using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType { Consumable, Equipment}
public enum Rarity { Common, Uncommon, Rare, Epic, Legendary, Mythical }
public enum EquidmentSlotType {BaseSkill, Weapon, SpecialSkill, Ring, AttackGem, DefenceGem}

public enum BaseSkillType { AttackBuff, DefenseBuff }

public enum SpecialSkillType { GreenFire, DragonFire }
public enum ConsumableType { Health, Point}

[CreateAssetMenu(menuName = "InventoryThaiAnh/Item")]
public class Item : ScriptableObject
{
    public int ItemID;
    public ItemType itemType;
    public Rarity rarity;
    public EquidmentSlotType allowedSlot;
    public ConsumableType consumableType;
    public string itemName;
    public Sprite icon;
    public int maxStack = 1;
    public int value; // giá tiền
    public bool isUsable;
    public string description;
    public string descriptionStats;

    #region Consumable
    public int addHeath;
    public int addPoint;
    #endregion

    #region Weapon
    public int SwordId;
    public float damgeBonus;
    #endregion

    #region Ring
    public float critRateBonus;
    public float critDamBonus;
    #endregion

    #region AttackGem
    public float damgeBonusGem;
    #endregion

    #region DefenceGem
    public int defBonusGem;
    #endregion

    #region Skill
    public BaseSkillType skillBaseType;
    public SpecialSkillType skillSpecialType;
    #endregion

    public virtual void Use()
    {
        //Debug.Log("Used " + itemName);
        // Nếu là HP Potion: hồi máu
        // Nếu là Buff: tăng tốc
        // Nếu là Scroll: mở cửa,...
    }
}
[CreateAssetMenu(menuName = "InventoryThaiAnh/HealthPotion")]
public class HealthPotion : Item
{
    public int healAmount;

    public override void Use()
    {
        base.Use();
        //Debug.Log("Healed for " + healAmount + " HP");
        // Gọi PlayerHealth.Instance.Heal(healAmount) chẳng hạn
    }
}


