using UnityEngine;

public class TitleScreen : MonoBehaviour
{
    public GameObject pressAnyButtonText;
    public GameObject mainMenuPanel;

    private bool canPress = false;

    void Start()
    {
        pressAnyButtonText.SetActive(false);
        mainMenuPanel.SetActive(false);
        Invoke(nameof(ShowPressAnyButton), 5f);
    }

    void ShowPressAnyButton()
    {
        pressAnyButtonText.SetActive(true);
        canPress = true;
    }

    void Update()
    {
        if (canPress && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
        {
            pressAnyButtonText.SetActive(false);
            mainMenuPanel.SetActive(true);
            enabled = false;
        }
    }
}
