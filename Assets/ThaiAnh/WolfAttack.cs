using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WolfAttack : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 30f;
    public float slowDownRate = 0.01f;
    public float detectingDistance = 0.01f;
    public float destroyDelay = 5f;

    [Header("Detaching")]
    public float objectsToDetachDelay = 2f;
    public List<GameObject> objectsToDetach = new List<GameObject>();

    [Header("Erosion")]
    public float erodeInRate = 0.06f;
    public float erodeOutRate = 0.03f;
    public float erodeRefreshRate = 0.01f;
    public float erodeAwayDelay = 1.25f;
    public List<SkinnedMeshRenderer> objectsToErode = new List<SkinnedMeshRenderer>();

    private Rigidbody rb;
    private bool stopped;

    void Start()
    {
        // Lock Y to ground level
        transform.position = new Vector3(transform.position.x, 0f, transform.position.z);

        if (GetComponent<Rigidbody>() != null)
        {
            rb = GetComponent<Rigidbody>();
            StartCoroutine(SlowDown());
        }
        else
        {
            Debug.Log("No Rigidbody");
        }

        if (objectsToDetach != null && objectsToDetach.Count > 0)
        {
            StartCoroutine(DetachObjects());
        }

        if (objectsToErode != null && objectsToErode.Count > 0)
        {
            StartCoroutine(ErodeObjects());
        }

        Destroy(gameObject, destroyDelay);
    }

    void FixedUpdate()
    {
        if (stopped)
        {
            RaycastHit hit;
            Vector3 rayStart = new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z);

            if (Physics.Raycast(rayStart, transform.TransformDirection(Vector3.down), out hit, detectingDistance))
            {
                transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
            }
            else
            {
                transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
            }

            Debug.DrawRay(rayStart, transform.TransformDirection(Vector3.down) * detectingDistance, Color.red);
        }
    }

    IEnumerator SlowDown()
    {
        float t = 1f;

        while (t > 0f)
        {
            rb.linearVelocity = Vector3.Lerp(Vector3.zero, rb.linearVelocity, t);
            t -= slowDownRate;
            yield return new WaitForSeconds(0.1f);
        }

        stopped = true;
    }

    IEnumerator DetachObjects()
    {
        yield return new WaitForSeconds(objectsToDetachDelay);

        for (int i = 0; i < objectsToDetach.Count; i++)
        {
            if (objectsToDetach[i] != null)
            {
                objectsToDetach[i].transform.parent = null;
                Destroy(objectsToDetach[i], objectsToDetachDelay);
            }
        }
    }

    IEnumerator ErodeObjects()
    {
        // Phase 1: erode in (disappear)
        for (int i = 0; i < objectsToErode.Count; i++)
        {
            float t = 1f;
            while (t > 0f)
            {
                t -= erodeInRate;
                objectsToErode[i].material.SetFloat("_Erode", t);
                yield return new WaitForSeconds(erodeRefreshRate);
            }
        }

        yield return new WaitForSeconds(erodeAwayDelay);

        // Phase 2: erode out (reappear)
        for (int i = 0; i < objectsToErode.Count; i++)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += erodeOutRate;
                objectsToErode[i].material.SetFloat("_Erode", t);
                yield return new WaitForSeconds(erodeRefreshRate);
            }
        }
    }
}
