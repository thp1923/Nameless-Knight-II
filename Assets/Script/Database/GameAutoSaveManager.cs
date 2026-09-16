using UnityEngine;
using UnityEngine.SceneManagement;
using StatsManager;
using System.Collections;
using System.Collections.Generic;

public class GameAutoSaveManager : MonoBehaviour
{
    public static GameAutoSaveManager Instance;

    [Header("Debug")]
    public bool VerboseLogging = false;

    [Header("Save Slot")]
    public string saveSlot;

    [Header("Scene Settings")]
    [Tooltip("Tên các scene là cutscene, không chạy save/load PlayFab trong đó.")]
    [SerializeField] private List<string> cutsceneScenes = new List<string>();

    [Header("Player State (cached)")]
    public int level = 1;
    public int playTime = 0;
    public int MaxHP = 0;
    public int Defense = 0;
    public int StunResistance = 0;
    public int currentHP = 0;
    public int BaseATK = 0;
    public float CritRate = 0;
    public float CritDamge = 0;
    public int StaminaMax = 0;
    public int Point = 0;
    public int HeathCount = 0;
    public int AtkBonus = 0;
    public float DamgeAttack = 0;
    public int AtkBonusSkill = 0;
    public float CritRateBonus = 0;
    public float CritDamgeBonus = 0;
    public bool CanSkill;
    public int SpecialSkillId = 0;
    public bool CanBuff;
    public int BuffTypeId = 0;

    [Header("Teleport on load")]
    public Vector3 nextPlayerPosition = Vector3.zero;

    // playtime counter
    float playTimeAccum = 0f;

    private bool _isSceneChange = false;
    private bool _isRespawning = false;
    private bool _isSaving = false;

    private GameStateData _lastSavedSnapshot = null;
    private GameStateData _pendingLoadData;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded -= OnSceneLoadedAfterLoad;
    }

    private void Update()
    {
        // đếm playtime
        playTimeAccum += Time.deltaTime;
        if (playTimeAccum >= 1f)
        {
            int add = Mathf.FloorToInt(playTimeAccum);
            playTime += add;
            playTimeAccum -= add;
        }
    }

    private bool IsCutsceneScene(string sceneName)
    {
        return cutsceneScenes.Contains(sceneName);
    }

    public void Init(string slot, int startLevel = 1, int startSouls = 0)
    {
        saveSlot = slot;
        level = startLevel;
        playTime = 0;
        SaveCurrentGame();
    }

    public void PrepareSceneChange()
    {
        _isSceneChange = true;
        playTimeAccum = 0f;
    }

    public void SaveCurrentGame(System.Action onDone = null)
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (IsCutsceneScene(sceneName))
        {
            if (VerboseLogging) Debug.Log("[AutoSave] Skip save in cutscene scene: " + sceneName);
            onDone?.Invoke();
            return;
        }

        var player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            onDone?.Invoke();
            return;
        }

        var statsAlive        = player.GetComponent<StatsAlive>();
        var attackDamgePlayer = FindFirstObjectByType<AttackDamgePlayer>();
        var stamina           = FindFirstObjectByType<Stamina>();
        var upgradeStats      = FindFirstObjectByType<UpgradeStats>();
        var specialSkill      = FindFirstObjectByType<SpecialSkill>();
        var playerBuff        = FindFirstObjectByType<PlayerBuff>();

        if (statsAlive == null || attackDamgePlayer == null || stamina == null || upgradeStats == null)
        {
            onDone?.Invoke();
            return;
        }

        currentHP = (statsAlive.HpSlider != null)
            ? Mathf.RoundToInt(statsAlive.HpSlider.value)
            : statsAlive.currentHP;

        MaxHP          = statsAlive.MaxHP;
        Defense        = statsAlive.Defense;
        StunResistance = statsAlive.StunResistance;

        Vector3 pos = player.transform.position;

        GameStateData data = new GameStateData
        {
            Level          = upgradeStats.Level,
            Point          = upgradeStats.Point,
            MaxHP          = statsAlive.MaxHP,
            Defense        = statsAlive.Defense,
            StunResistance = statsAlive.StunResistance,
            currentHP      = statsAlive.currentHP,

            BaseATK        = attackDamgePlayer.BaseATK,
            critRate       = attackDamgePlayer.critRate,
            critDamge      = attackDamgePlayer.critDamge,
            atkBonus       = attackDamgePlayer.atkBonus,
            damgeAttack    = attackDamgePlayer.damgeAttack,
            critRateBonus  = attackDamgePlayer.critRateBonus,
            critDamgeBonus = attackDamgePlayer.critDamgeBonus,

            StaminaMax     = stamina.StaminaMax,

            canSkill       = (specialSkill != null && specialSkill.canSkill),
            SpecialSkillId = specialSkill != null ? specialSkill.SpecialSkillId : 0,
            canBuff        = (playerBuff != null && playerBuff.canBuff),
            buffTypeId     = playerBuff != null ? playerBuff.buffTypeId : 0,

            LastScene      = sceneName,
            PlayTime       = playTime,
            posX           = pos.x,
            posY           = pos.y,
            posZ           = pos.z,
        };

        _lastSavedSnapshot = data;

        if (_isSaving)
        {
            onDone?.Invoke();
            return;
        }

        _isSaving = true;
        LocalSaveStorage.Set(saveSlot, JsonUtility.ToJson(data));
        _isSaving = false;
        onDone?.Invoke();
    }

    public void OnBonfireRest()
    {
        InventoryManager.Instance?.SaveInventoryForSlot(saveSlot);
        SaveCurrentGame();
    }

    public void OnPlayerDie()
    {
        LoadGame(saveSlot);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var sceneName = scene.name;
        if (IsCutsceneScene(sceneName))
        {
            if (VerboseLogging) Debug.Log("[AutoSave] Entered cutscene scene, skip load/apply.");
            return;
        }

        if (nextPlayerPosition != Vector3.zero)
        {
            var playerSet = GameObject.FindWithTag("Player");
            if (playerSet != null) playerSet.transform.position = nextPlayerPosition;
            nextPlayerPosition = Vector3.zero;
        }

        if (_isSceneChange && _lastSavedSnapshot != null)
        {
            StartCoroutine(ApplyLoadedStateNextFrame(_lastSavedSnapshot));
            _isSceneChange = false;
            InventoryManager.Instance?.LoadInventoryForSlot(saveSlot);
            return;
        }

        if (_pendingLoadData != null)
        {
            StartCoroutine(ApplyLoadedStateNextFrame(_pendingLoadData));
            _pendingLoadData = null;

            _isRespawning = false;
            enabled = true;
            InventoryManager.Instance?.LoadInventoryForSlot(saveSlot);
        }
    }

    public void LoadGame(string slot)
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (IsCutsceneScene(sceneName))
        {
            Debug.Log("[AutoSave] Skip load in cutscene scene: " + sceneName);
            return;
        }

        saveSlot = slot;
        _isRespawning = true;

        if (LocalSaveStorage.TryGet(saveSlot, out string json))
        {
            GameStateData data = JsonUtility.FromJson<GameStateData>(json);
            nextPlayerPosition = new Vector3(data.posX, data.posY, data.posZ);

            SceneManager.sceneLoaded -= OnSceneLoadedAfterLoad;
            SceneManager.sceneLoaded += OnSceneLoadedAfterLoad;
            SceneManager.LoadScene(data.LastScene);
            _pendingLoadData = data;
            return;
        }

        _isRespawning = false;
        Debug.LogWarning($"[AutoSave] No data for slot {saveSlot}");
    }

    private void OnSceneLoadedAfterLoad(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoadedAfterLoad;

        var sceneName = scene.name;
        if (IsCutsceneScene(sceneName))
        {
            if (VerboseLogging) Debug.Log("[AutoSave] Entered cutscene after respawn, skip apply.");
            return;
        }

        if (nextPlayerPosition != Vector3.zero)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) player.transform.position = nextPlayerPosition;
            nextPlayerPosition = Vector3.zero;
        }

        if (_pendingLoadData != null)
        {
            StartCoroutine(ApplyLoadedStateNextFrame(_pendingLoadData));
            _pendingLoadData = null;
        }

        _isRespawning = false;
        enabled = true;
        InventoryManager.Instance?.LoadInventoryForSlot(saveSlot);
    }

    private IEnumerator ApplyLoadedStateNextFrame(GameStateData data)
    {
        yield return null;

        var player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            yield return null;
            player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[AutoSave] Player not found to apply state.");
                yield break;
            }
        }

        ApplyLoadedStateInternal(player, data);
    }

    private void ApplyLoadedStateInternal(GameObject player, GameStateData data)
    {
        level    = Mathf.Max(1, data.Level);
        playTime = Mathf.Max(0, data.PlayTime);

        var statsAlive = player.GetComponent<StatsAlive>();
        var atk        = FindFirstObjectByType<AttackDamgePlayer>();
        var stamina    = FindFirstObjectByType<Stamina>();
        var upgrade    = FindFirstObjectByType<UpgradeStats>();
        var special    = FindFirstObjectByType<SpecialSkill>();
        var playerBuff = FindFirstObjectByType<PlayerBuff>();

        if (upgrade != null)
        {
            upgrade.Level = Mathf.Max(1, data.Level);
            upgrade.Point = Mathf.Max(0, data.Point);
        }

        if (statsAlive != null)
        {
            statsAlive.MaxHP          = Mathf.Max(1, data.MaxHP);
            statsAlive.Defense        = Mathf.Max(0, data.Defense);
            statsAlive.StunResistance = Mathf.Max(0, data.StunResistance);
            statsAlive.currentHP      = Mathf.Clamp(data.currentHP, 0, statsAlive.MaxHP);

            if (statsAlive.HpSlider != null)
            {
                statsAlive.HpSlider.maxValue = statsAlive.MaxHP;
                statsAlive.HpSlider.value    = statsAlive.currentHP;
            }
        }

        if (atk != null)
        {
            atk.BaseATK        = Mathf.Max(0,  data.BaseATK);
            atk.critRate       = Mathf.Max(0f, data.critRate);
            atk.critDamge      = Mathf.Max(0f, data.critDamge);
            atk.atkBonus       = Mathf.Max(0,  data.atkBonus);
            atk.damgeAttack    = Mathf.Max(0f, data.damgeAttack);
            atk.critRateBonus  = Mathf.Max(0f, data.critRateBonus);
            atk.critDamgeBonus = Mathf.Max(0f, data.critDamgeBonus);
        }

        if (stamina != null)
        {
            stamina.StaminaMax = Mathf.Max(0, data.StaminaMax);
        }

        if (special != null)
        {
            special.canSkill       = data.canSkill;
            special.SpecialSkillId = data.SpecialSkillId;
        }

        if (playerBuff != null)
        {
            playerBuff.canBuff    = data.canBuff;
            playerBuff.buffTypeId = data.buffTypeId;
        }

        string currentScene = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(data.LastScene) && data.LastScene == currentScene)
        {
            player.transform.position = new Vector3(data.posX, data.posY, data.posZ);
        }
    }

    private void ApplyLoadedState(GameStateData data)
    {
        StartCoroutine(ApplyLoadedStateNextFrame(data));
    }
}
