using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class TigerAttackShooter : MonoBehaviour
{
    public Camera cam;
    public GameObject projectile;
    public Transform FirePoint;
    public float FireRate = 4;
    private Vector3 destination;
    private float timetoFire;
    private WolftAttackTut wolftAttackscript;

    void Update()
    {
        if (Input.GetButton("Fire1") && Time.time >= timetoFire)
        {
            timetoFire = Time.time + 1 / FireRate;
            ShootProjectile();
        }
    }
    void ShootProjectile()
    {
        if (cam != null)
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            destination = ray.GetPoint(1000);
            InstantiateProjectile();
        }
        else
        {
            Debug.Log("B");
            InstantiateProjectileAtFirePoint();
        }
    }
    void InstantiateProjectile()
    {
        var projectileObj = Instantiate(projectile, FirePoint.position, Quaternion.identity) as GameObject;
        wolftAttackscript = projectileObj.GetComponent<WolftAttackTut>();
        RotateToDestination(projectileObj, destination, true);
        projectileObj.GetComponent<Rigidbody>().linearVelocity = transform.forward * wolftAttackscript.speed;
    }
    void InstantiateProjectileAtFirePoint()
    {
        var projectileObj = Instantiate(projectile, FirePoint.position, Quaternion.identity) as GameObject;
        wolftAttackscript = projectileObj.GetComponent<WolftAttackTut>();
        RotateToDestination(projectileObj, FirePoint.transform.forward * 1000, true);
        projectileObj.GetComponent<Rigidbody>().linearVelocity = FirePoint.transform.forward * wolftAttackscript.speed;
    }
    void RotateToDestination(GameObject obj, Vector3 destination, bool onlyY)
    {
        var direction = destination - obj.transform.position;
        var rotation = Quaternion.LookRotation(direction);
        if (onlyY)
        {
            rotation.x = 0;
            rotation.z = 0;
        }

        obj.transform.localRotation = Quaternion.Lerp(obj.transform.rotation, rotation, 1);
    }
}