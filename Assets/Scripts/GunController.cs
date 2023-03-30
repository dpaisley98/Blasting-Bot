using System.Collections;
using UnityEngine;
using TMPro;

public class GunController : MonoBehaviour
{
    public GameObject player;
    Rigidbody playerBody;
    public GameObject bullet, muzzleFlash;
    public float shootForce, upwardForce, recoilForce;
    public float spread, reloadTime, shotsInterval, shootingInterval;
    public int magazineSize, bulletsPerTap;
    public bool allowButtonHold;
    int bulletsLeft, bulletsShot;
    bool shooting, readyToShoot, reloading, allowInvoke;
    public Camera playerCam;
    public Transform gunPoint;
    private Vector3 middleOfScreen;


    private void Awake() 
    {
        bulletsLeft = magazineSize;
        readyToShoot = true;
        allowInvoke = true;
        middleOfScreen = new Vector3(0.5f, 0.5f, 0);
        playerBody = player.GetComponent<Rigidbody>();
    }

    void Update() 
    {
        if (allowButtonHold)
            shooting = Input.GetKey(KeyCode.Mouse0);
        else
            shooting = Input.GetKeyDown(KeyCode.Mouse0);

        if (CanReload() | CannotShoot())
            Reload();
            
        if (CanShoot())
        {
            bulletsShot = 0;
            Shoot();
        } 
    }

    bool CanShoot() 
    {
        return readyToShoot && shooting && !reloading && bulletsLeft > 0;
    }

    bool CannotShoot() 
    {
        return readyToShoot && shooting && !reloading && bulletsLeft <= 0;
    }

    bool CanReload() 
    {
        return Input.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && !reloading;
    }

    void Shoot()
    {
        readyToShoot = false;

        Ray ray = playerCam.ViewportPointToRay(middleOfScreen);
        RaycastHit hit;

        Vector3 targetPoint;
        if(Physics.Raycast(ray, out hit))
            targetPoint = hit.point;
        else
            targetPoint = ray.GetPoint(75);

        Vector3 bulletDirection = targetPoint - gunPoint.position;

        float xSpread = UnityEngine.Random.Range(-spread, spread);
        float ySpread = UnityEngine.Random.Range(-spread, spread);

        Vector3 bulletDirectionWithSpread = bulletDirection + new Vector3(xSpread, ySpread);

        GameObject currentBullet = Instantiate(bullet, gunPoint.position, Quaternion.identity);
        currentBullet.transform.forward = bulletDirectionWithSpread.normalized;
        currentBullet.GetComponent<Rigidbody>().AddForce(bulletDirectionWithSpread.normalized * shootForce, ForceMode.Impulse);
        currentBullet.GetComponent<Rigidbody>().AddForce(playerCam.transform.up.normalized * upwardForce, ForceMode.Impulse);

        if (muzzleFlash != null)
            Instantiate(muzzleFlash, gunPoint.position, Quaternion.identity);

        bulletsLeft--;
        bulletsShot++;

        if(allowInvoke) {
            Invoke("ResetShot", shootingInterval);
            allowInvoke = false;
            bool cancelPreviousForce = playerBody.velocity.magnitude > 0;
            StartCoroutine(ApplyRecoilForceForDuration(-bulletDirectionWithSpread.normalized, recoilForce, 5f, false));
        }

        if (bulletsShot < bulletsPerTap && bulletsLeft > 0)
            Invoke("ResetShot", shotsInterval);
    }

    void ResetShot()
    {
        readyToShoot = true; 
        allowInvoke = true;
    }

    void Reload()
    {
        reloading = true;
        Invoke("ReloadFinished", reloadTime);
    }

    void ReloadFinished() 
    {
        bulletsLeft = magazineSize;
        reloading = false;
    }

    IEnumerator ApplyRecoilForceForDuration(Vector3 forceDirection, float forceMagnitude, float duration, bool cancelPreviousForce = false)
    {
        float terminalVelocity = player.GetComponent<PlayerMovement>().terminalVelocity;
        float initialVelocity = playerBody.velocity.magnitude;

        if (cancelPreviousForce)
            playerBody.velocity = Vector3.zero;
        
        playerBody.AddForce(forceDirection * forceMagnitude, ForceMode.Impulse);
        playerBody.AddForce(forceDirection);

        while (playerBody.velocity.magnitude > terminalVelocity)
        {
            yield return null;

            Vector3 currentDirection = playerBody.velocity.normalized;
            float currentSpeed = playerBody.velocity.magnitude;

            float decelerationForceMagnitude = (currentSpeed * currentSpeed) / (2 * (transform.position - playerBody.position).magnitude);
            Vector3 decelerationForce = -currentDirection * decelerationForceMagnitude;

            playerBody.velocity += decelerationForce * Time.deltaTime;
        }

        playerBody.velocity = playerBody.velocity.normalized * terminalVelocity;
    }


}

