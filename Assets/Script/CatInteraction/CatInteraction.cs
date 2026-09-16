using UnityEngine;
public class CatInteraction : MonoBehaviour
{
    public Animator catAnimator;
    public AudioSource audioSource;

    private bool playerNear = false;
    private bool hasInteracted = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = false;
        }
    }

    void Update()
    {
        if (playerNear && !hasInteracted && Input.GetKeyDown(KeyCode.F)
            && catAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
        {
            hasInteracted = true;
            StartCoroutine(DoOiaCatSequence());
        }
    }

    System.Collections.IEnumerator DoOiaCatSequence()
    {
        catAnimator.SetTrigger("Spin");

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.Play();
        }

        yield return new WaitUntil(() => catAnimator.GetCurrentAnimatorStateInfo(0).IsName("Spin"));

        float animLength = catAnimator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(animLength);

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        yield return new WaitForSeconds(1f);

        hasInteracted = false;
    }
}
