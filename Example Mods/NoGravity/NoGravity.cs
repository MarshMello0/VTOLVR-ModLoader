using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using Valve.VR;
public class NoGravity : VTOLMOD
{
    private bool isDisabled, onCoolDown;
    private float coolDown = 2f;
    private float currentTimer;

    private void Awake()
    {
        Log("Loaded!");
    }

    private void Update()
    {
        if (VRHandController.controllers.Count != 2)
            return;

        if (onCoolDown)
        {
            currentTimer += Time.deltaTime;
            if (currentTimer >= coolDown)
            {
                onCoolDown = false;
                currentTimer = 0;
            }
        }
        else if (VRHandController.controllers[0].thumbButtonPressed &&
            VRHandController.controllers[1].thumbButtonPressed)
        {
            Physics.gravity = new Vector3(0, isDisabled ? 0f : -9.3f, 0);
            isDisabled = !isDisabled;
            Log("Set gravity to " + isDisabled);
            onCoolDown = true;
        }
        
    }
}