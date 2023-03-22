using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunShoot : MonoBehaviour
{
    public float fireRate = 0.5f;                                      
    public float weaponRange = 50f;                                      
    public float hitForce = 100f;                                       
    private Vector3 recoilVelocity;
    public Transform gunEnd;                                            
    public Camera cam;                                               
    private WaitForSeconds shotDuration = new WaitForSeconds(0.07f);   
    private LineRenderer laserLine;                                        
    private float nextFire = 0f;                                               


    void Start () 
    {
        laserLine = cam.GetComponentInChildren<LineRenderer>();
    }


    public Vector3 Shoot() 
    {
        if (Time.time > nextFire) 
        {
            nextFire = Time.time + fireRate;
            StartCoroutine (ShotEffect());
            Vector3 rayOrigin = cam.ViewportToWorldPoint (new Vector3(0.5f, 0.5f, 0.0f));
            RaycastHit hit;
            laserLine.SetPosition (0, gunEnd.position);

            if (Physics.Raycast (rayOrigin, cam.transform.forward, out hit, weaponRange))
            {
                laserLine.SetPosition (1, hit.point);

                if (hit.rigidbody != null)
                {
                    hit.rigidbody.AddForce (-hit.normal * hitForce);
                }
            }
            else
            {
                laserLine.SetPosition (1, rayOrigin + (cam.transform.forward * weaponRange));
            }

            return cam.transform.forward;
        }

        return cam.transform.forward;
    }


    private IEnumerator ShotEffect()
    {
        laserLine.enabled = true;
        yield return shotDuration;
        laserLine.enabled = false;
    }
}

