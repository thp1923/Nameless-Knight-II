using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Refs")]
    public GameObject Player;

    [Header("UI/Prefabs")]
    public GameObject bagSlotPrefab;
    public GameObject itemSlotPrefab;
    public Transform contentPanel;
    public List<InventorySlot> equipmentSlots = new List<InventorySlot>();

    [Header("Bootstrap (optional)")]
    public List<Item> testItems;
    public bool buildFromTestItems = true;

    // cache ItemID -> Item
    private readonly Dictionary<int, Item> _itemMap = new Dictionary<int, Item>();

    private void Awake() => Instance = this;

    private void Start()
    {
        BuildItemMap();

        // (tùy chọn) add sẵn item test vào túi
        if (testItems != null)
            foreach (var it in testItems)
                if (it != null) CreateBagSlot(it, 1, autosave: false); // không autosave dữ liệu test

        // Ẩn icon ô trang bị trống
        foreach (var slot in equipmentSlots)
            if (slot != null && slot.IsEmpty() && slot.icon != null) slot.icon.enabled = false;

        MergeAllStackableItems();
    }

    /* ===================== AUTO SAVE ===================== */

    void AutoSave()
    {
        var gsm = GameAutoSaveManager.Instance;
        if (gsm == null) return;

        SaveInventoryForSlot(gsm.saveSlot);
        gsm.SaveCurrentGame();
    }

    /* ========== Helpers: check trùng trong túi / đang trang bị ========== */

    // Có equipment cùng ItemID trong túi?
    private bool ExistsDuplicateInBag(Item item)
    {
        if (item == null || item.itemType != ItemType.Equipment) return false;

        foreach (Transform child in contentPanel)
        {
            var slot = child.GetComponentInChildren<InventorySlot>();
            if (slot == null || slot.IsEmpty()) continue;

            var it = slot.GetItem();
            if (it != null && it.itemType == ItemType.Equipment && it.ItemID == item.ItemID)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Nếu item là Equipment và đã có item cùng ID đang trang bị (đúng allowedSlot):
    /// - Quy đổi đúng bằng item.value * amount → UpgradeStats.AddPoint(...)
    /// - Lưu game
    /// - Trả về true (đã quy đổi, KHÔNG thêm item vào túi)
    /// </summary>
    private bool TryConvertDuplicateEquippedToPoints(Item item, int amount = 1)
    {
        if (item == null || item.itemType != ItemType.Equipment) return false;

        foreach (var eqSlot in equipmentSlots)
        {
            if (!eqSlot || eqSlot.IsEmpty()) continue;
            var equipped = eqSlot.GetItem();

            if (eqSlot.slotType == item.allowedSlot && equipped != null && equipped.ItemID == item.ItemID)
            {
                var up = GetUpgrade();
                if (up != null)
                {
                    int addPoint = Mathf.Max(0, item.value * Mathf.Max(1, amount));
                    up.AddPoint(addPoint); // ✅ dùng API của UpgradeStats

                    GameAutoSaveManager.Instance?.SaveCurrentGame();
                    Debug.Log($"[Inventory] Duplicate EQUIPPED ID={item.ItemID} → +{addPoint} Point (value={item.value}). New={up.Point}");
                }
                return true; // đã xử lý quy đổi
            }
        }
        return false;
    }

    /* ===================== CORE MOVE / EQUIP ===================== */

    public void TryMoveItem(InventorySlot fromSlot)
    {
        if (fromSlot == null) return;
        Item item = fromSlot.GetItem();
        if (item == null || item.itemType != ItemType.Equipment) return;

        ApplyEquipEffects(item);

        foreach (var slot in equipmentSlots)
        {
            if (!slot.isEquipmentSlot || slot.slotType != item.allowedSlot) continue;

            if (!slot.IsEmpty())
            {
                Item tempItem = slot.GetItem();
                int tempCount = slot.GetStackCount();

                slot.AddItem(item, fromSlot.GetStackCount());
                fromSlot.AddItem(tempItem, tempCount);
            }
            else
            {
                slot.AddItem(item, fromSlot.GetStackCount());
                if (!fromSlot.isEquipmentSlot) fromSlot.ClearSlot(true); // destroy ô túi
                else fromSlot.ClearSlot();                               // clear ô trang bị
            }

            AutoSave(); // autosave sau khi equip
            return;
        }
    }

    public void UnequipItem(InventorySlot fromSlot)
    {
        if (fromSlot == null) return;
        Item item = fromSlot.GetItem();
        if (item == null) return;

        // trả về túi (auto stack)
        CreateBagSlot(item, fromSlot.GetStackCount());

        var atk = Player.GetComponent<AttackDamgePlayer>();
        var def = Player.GetComponent<PlayerTakeDamge>();
        var weapon = Player.GetComponent<WeaponEquip>();
        var baseSkill = Player.GetComponent<PlayerBuff>();
        var specialSkill = Player.GetComponent<SpecialSkill>();

        switch (item.allowedSlot)
        {
            case EquidmentSlotType.Weapon: atk.atkBonus = 0; weapon.SwordSwich(0); break;
            case EquidmentSlotType.Ring: atk.critRateBonus = 0; atk.critDamgeBonus = 0; break;
            case EquidmentSlotType.AttackGem: atk.damgeAttack = 0; break;
            case EquidmentSlotType.DefenceGem: def.defenseBonus = 0; break;
            case EquidmentSlotType.BaseSkill: baseSkill.canBuff = false; break;
            case EquidmentSlotType.SpecialSkill: specialSkill.canSkill = false; break;
        }

        fromSlot.ClearSlot();
        AutoSave(); // autosave sau khi tháo
    }

    /* ===================== NHẶT ITEM ===================== */

    public void TryAddToInventory(Item item)
    {
        if (item == null) return;

        // 1) Trùng với món đang trang bị → quy đổi point theo value
        if (TryConvertDuplicateEquippedToPoints(item))
            return;

        // 2) Nếu là Equipment và túi đã có một bản cùng ID → cũng quy đổi
        if (item.itemType == ItemType.Equipment && ExistsDuplicateInBag(item))
        {
            var up = GetUpgrade();
            if (up != null)
            {
                int addPoint = Mathf.Max(0, item.value);
                up.AddPoint(addPoint); // ✅ dùng UpgradeStats.AddPoint
                GameAutoSaveManager.Instance?.SaveCurrentGame();
                //Debug.Log($"[Inventory] Duplicate in BAG ID={item.ItemID} → +{addPoint} Point. New={up.Point}");
            }
            return; // không thêm vào túi
        }

        // 3) Không trùng → hành vi nhặt bình thường (ưu tiên stack)
        foreach (Transform child in contentPanel)
        {
            var slot = child.GetComponentInChildren<InventorySlot>();
            if (slot != null && slot.CanStack(item))
            {
                slot.AddItem(item, 1);
                MergeAllStackableItems();
                AutoSave();
                return;
            }
        }

        // tạo ô mới
        GameObject go = Instantiate(itemSlotPrefab != null ? itemSlotPrefab : bagSlotPrefab, contentPanel);
        var newSlot = go.GetComponentInChildren<InventorySlot>();
        newSlot.AddItem(item, 1);
        MergeAllStackableItems();
        AutoSave();
    }

    public void CreateBagSlot(Item item, int amount = 1, bool autosave = true)
    {
        if (item == null || amount <= 0) return;

        // KHÔNG convert ở đây để tránh unequip bị quy đổi nhầm.
        foreach (Transform child in contentPanel)
        {
            var slot = child.GetComponentInChildren<InventorySlot>();
            if (slot != null && slot.CanStack(item))
            {
                slot.AddItem(item, amount);
                MergeAllStackableItems();
                if (autosave) AutoSave();
                return;
            }
        }

        GameObject slotGO = Instantiate(bagSlotPrefab != null ? bagSlotPrefab : itemSlotPrefab, contentPanel);
        var newSlot = slotGO.GetComponentInChildren<InventorySlot>();
        newSlot.AddItem(item, amount);
        MergeAllStackableItems();
        if (autosave) AutoSave();
    }

    public void MergeAllStackableItems()
    {
        var merged = new Dictionary<int, InventorySlot>(); // ItemID -> slot đầu

        foreach (Transform child in contentPanel)
        {
            var slot = child.GetComponentInChildren<InventorySlot>();
            if (slot == null || slot.IsEmpty()) continue;

            var item = slot.GetItem();
            int id = item.ItemID;

            if (!merged.ContainsKey(id))
            {
                merged[id] = slot;
            }
            else
            {
                var target = merged[id];
                if (!target.CanStack(item)) continue;

                int available = item.maxStack - target.GetStackCount();
                int moving = Mathf.Min(available, slot.GetStackCount());
                if (moving > 0)
                {
                    target.AddItem(item, moving);
                    slot.UpdateStackCount(slot.GetStackCount() - moving);
                }
            }
        }
    }

    /* ===================== SAVE / LOAD LOCAL ===================== */

    public void SaveInventoryForSlot(string saveSlot)
    {
        if (string.IsNullOrEmpty(saveSlot))
        {
            Debug.LogWarning("[SaveInventory] saveSlot null/empty");
            return;
        }

        var bagItems = new List<InventoryItemData>();
        var equipItems = new List<InventoryItemData>();

        // Túi
        foreach (Transform child in contentPanel)
        {
            var slot = child.GetComponentInChildren<InventorySlot>();
            if (slot != null && !slot.IsEmpty())
            {
                bagItems.Add(new InventoryItemData
                {
                    itemId = slot.GetItem().ItemID,
                    quantity = slot.GetStackCount(),
                    slotType = "Bag"
                });
            }
        }

        // Trang bị
        foreach (var slot in equipmentSlots)
        {
            if (slot != null && !slot.IsEmpty())
            {
                equipItems.Add(new InventoryItemData
                {
                    itemId = slot.GetItem().ItemID,
                    quantity = 1,
                    slotType = slot.slotType.ToString()
                });
            }
        }

        var data = new Dictionary<string, string>
        {
            { $"{saveSlot}_Bag",   JsonUtility.ToJson(new InventoryListWrapper { items = bagItems }) },
            { $"{saveSlot}_Equip", JsonUtility.ToJson(new InventoryListWrapper { items = equipItems }) }
        };

        LocalSaveStorage.Set($"{saveSlot}_Bag", data[$"{saveSlot}_Bag"]);
        LocalSaveStorage.Set($"{saveSlot}_Equip", data[$"{saveSlot}_Equip"]);
    }

    public void LoadInventoryForSlot(string saveSlot)
    {
        if (string.IsNullOrEmpty(saveSlot))
        {
            Debug.LogWarning("[LoadInventory] saveSlot null/empty");
            return;
        }

        foreach (Transform child in contentPanel) Destroy(child.gameObject);
        foreach (var slot in equipmentSlots) slot.ClearSlot();

        if (LocalSaveStorage.TryGet($"{saveSlot}_Bag", out string bagJson))
        {
            var bagData = JsonUtility.FromJson<InventoryListWrapper>(bagJson);
            if (bagData?.items != null)
                foreach (var it in bagData.items)
                {
                    var item = GetItemByID(it.itemId);
                    if (item != null) CreateBagSlot(item, it.quantity, autosave: false);
                }
        }

        if (LocalSaveStorage.TryGet($"{saveSlot}_Equip", out string equipJson))
        {
            var equipData = JsonUtility.FromJson<InventoryListWrapper>(equipJson);
            if (equipData?.items != null)
                foreach (var it in equipData.items)
                {
                    var item = GetItemByID(it.itemId);
                    if (item == null) continue;

                    foreach (var slot in equipmentSlots)
                    {
                        if (slot.slotType.ToString() != it.slotType) continue;
                        slot.AddItem(item, 1);
                        ApplyEquipEffects(item);
                        break;
                    }
                }
        }

        MergeAllStackableItems();
    }

    /* ===================== HELPERS ===================== */

    public void ApplyEquipEffects(Item item)
    {
        if (item == null) return;

        var atk = Player.GetComponent<AttackDamgePlayer>();
        var def = Player.GetComponent<PlayerTakeDamge>();
        var weapon = Player.GetComponent<WeaponEquip>();
        var baseSkill = Player.GetComponent<PlayerBuff>();
        var specialSkill = Player.GetComponent<SpecialSkill>();

        switch (item.allowedSlot)
        {
            case EquidmentSlotType.Weapon:
                int weaponBonus = (int)(atk.BaseATK * (item.damgeBonus / 100f));
                atk.atkBonus = weaponBonus;
                weapon.SwordSwich(item.SwordId);
                break;
            case EquidmentSlotType.Ring:
                atk.critRateBonus = item.critRateBonus;
                atk.critDamgeBonus = item.critDamBonus;
                break;
            case EquidmentSlotType.AttackGem:
                atk.damgeAttack = item.damgeBonusGem;
                break;
            case EquidmentSlotType.DefenceGem:
                def.defenseBonus = item.defBonusGem;
                break;
            case EquidmentSlotType.BaseSkill:
                baseSkill.canBuff = true;
                baseSkill.buffTypePlayer = (item.skillBaseType == BaseSkillType.AttackBuff)
                    ? PlayerBuff.BuffType.Atk
                    : PlayerBuff.BuffType.Def;
                break;
            case EquidmentSlotType.SpecialSkill:
                specialSkill.canSkill = true;
                specialSkill.skillTpye = (item.skillSpecialType == SpecialSkillType.GreenFire)
                    ? SpecialSkill.SpecialSkillTpye.GreenFire
                    : SpecialSkill.SpecialSkillTpye.DragonFire;
                break;
        }
    }

    private void BuildItemMap()
    {
        _itemMap.Clear();

        if (buildFromTestItems && testItems != null && testItems.Count > 0)
        {
            foreach (var it in testItems)
                if (it != null) _itemMap[it.ItemID] = it;
        }
        else
        {
            var all = Resources.LoadAll<Item>("Items");
            foreach (var it in all)
                if (it != null) _itemMap[it.ItemID] = it;
        }
    }

    private Item GetItemByID(int id)
    {
        if (_itemMap.TryGetValue(id, out var found)) return found;

        var res = Resources.LoadAll<Item>("Items");
        foreach (var it in res)
            if (it != null && it.ItemID == id)
            {
                _itemMap[id] = it;
                return it;
            }
        return null;
    }

    // wrapper cho JsonUtility (KHÔNG định nghĩa InventoryItemData ở đây)
    [System.Serializable]
    private class InventoryListWrapper
    {
        public List<InventoryItemData> items;
    }

    [SerializeField] private UpgradeStats upgradeStats;

    private UpgradeStats GetUpgrade()
    {
        if (upgradeStats == null) upgradeStats = FindFirstObjectByType<UpgradeStats>();
        return upgradeStats;
    }

}
