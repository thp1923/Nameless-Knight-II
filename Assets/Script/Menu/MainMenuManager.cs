using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using StatsManager;

public class MainMenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject loadGamePanel;
    public GameObject overwritePanel;

    public TMP_Text[] slotTexts;

    string[] usedSlots = new string[4];
    int usedSlotCount = 0;

    void Start()
    {
        loadGamePanel.SetActive(false);
        overwritePanel.SetActive(false);
    }

    public void OnNewGameClicked()
    {
        usedSlotCount = 0;
        for (int i = 1; i <= 4; i++)
        {
            string key = $"SaveSlot{i}";
            if (LocalSaveStorage.Has(key))
            {
                usedSlots[i - 1] = key;
                usedSlotCount++;
            }
            else
            {
                StartNewGame(key);
                return;
            }
        }

        if (usedSlotCount >= 4) overwritePanel.SetActive(true);
    }
    void StartNewGame(string slot)
    {
        GameAutoSaveManager.Instance.Init(slot, startLevel: 1);
        SceneTransitionManager.Instance.FadeToScene("CutsceneOpening");
    }


    public void OnOverwriteSlot(int index)
    {
        if (index < 0 || index >= usedSlots.Length || string.IsNullOrEmpty(usedSlots[index]))
        {
            //Debug.LogWarning("Ô overwrite không hợp lệ.");
            return;
        }
        string slot = usedSlots[index];
        overwritePanel.SetActive(false);
        StartNewGame(slot);
    }
    public void OnCancelOverwrite()
    {
        overwritePanel.SetActive(false);
    }

    public void OnLoadGameClicked()
    {
        mainMenuPanel.SetActive(false);
        loadGamePanel.SetActive(true);
        for (int i = 1; i <= 4; i++)
        {
            string key = $"SaveSlot{i}";
            if (LocalSaveStorage.TryGet(key, out string json))
            {
                var data = JsonUtility.FromJson<GameStateData>(json);
                slotTexts[i - 1].text = $"Level {data.Level} - {data.LastScene}";
            }
            else
            {
                slotTexts[i - 1].text = "Trống";
            }
        }
            
    }

    public void OnSelectLoadSlot(int index)
    {
        string slot = $"SaveSlot{index + 1}";

        // Ẩn UI load để tránh người chơi click thêm
        loadGamePanel.SetActive(false);
        mainMenuPanel.SetActive(false);

        // Giao toàn bộ cho GameAutoSaveManager
        GameAutoSaveManager.Instance.LoadGame(slot);
    }
    public void OnCancelLoadGame()
    {
        loadGamePanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
}
