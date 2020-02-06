using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using Valve.VR;
using UnityEngine.Events;

public class NoGravity : VTOLMOD
{
    private bool isDisabled, onCoolDown;
    private float coolDown = 2f;
    private float currentTimer;
    private static Settings setting;
    private static UnityAction<float> AmountChanged;
    private static float gravityAmount = 0;

    public override void ModLoaded()
    {
        base.ModLoaded();
        AmountChanged += ChangedValue;
        setting = new Settings(this);
        setting.CreateFloatSetting("Toggled Amount", AmountChanged, gravityAmount);
        VTOLAPI.CreateSettingsMenu(setting);
    }

    public void ChangedValue(float amount)
    {
        gravityAmount = amount;
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
            Physics.gravity = new Vector3(0, isDisabled ? gravityAmount : -9.3f, 0);
            isDisabled = !isDisabled;
            Log("Set gravity to " + isDisabled);
            onCoolDown = true;
        }
        
    }
}