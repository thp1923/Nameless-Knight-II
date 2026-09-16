using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
public class ShaderWarmup : MonoBehaviour
{
    [Header("Shader")]
    [SerializeField] private ShaderVariantCollection shaderVariants;

    [Header("Next Scene")]
    [SerializeField] private string nextScene = "MainMenu";

    [Header("Loading UI")]
    [SerializeField] private GameObject loadingScreen;

    private IEnumerator Start()
    {
        // Cho Loading Screen render trước
        yield return null;

        if (shaderVariants != null)
        {
            Debug.Log("Starting shader warmup...");

            shaderVariants.WarmUp();

            Debug.Log("Shader warmup completed.");
        }
        else
        {
            Debug.LogWarning("Shader Variant Collection chưa được gán!");
        }

        // Cho UI cập nhật
        yield return null;

        // Load Main Menu
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextScene);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}
