using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponHolster : MonoBehaviour
{
    public int currentWeapon, weaponAmount;
    void Start()
    {
        SelectWeapon();
        
    }

    private void SelectWeapon()
    {
        int i = 0;
        foreach (Transform weapon in transform)
        {
            if (i == currentWeapon)
                weapon.gameObject.SetActive(true);
            else    
                weapon.gameObject.SetActive(false);

            i++;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetAxis("Mouse ScrollWheel") > 0f)
        {
            currentWeapon++;
            currentWeapon = currentWeapon % 3;
            SelectWeapon();
        } else if (Input.GetAxis("Mouse ScrollWheel") < 0f)
        {
            currentWeapon--;
            currentWeapon = (currentWeapon + 3) % 3;
            SelectWeapon();
        }
        
    }
}
