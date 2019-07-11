using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using Steamworks;

public class MultiplayerMod : MonoBehaviour
{
    //This is the state which the client is currently in
    public enum ConnectionState { Offline, Loading, Connecting, Connected, Failed }
    public ConnectionState state = ConnectionState.Offline;

    //This is the information about what the player has chosen
    public enum Vehicle { FA26B, AV42C,F45A }
    public Vehicle vehicle = Vehicle.AV42C;
    public string pilotName = "Pilot Name";
    private void OnGUI()
    {
        //Displaying different UI at different states
        switch (state)
        {
            case ConnectionState.Offline:
                GUIOffline();
                break;
            case ConnectionState.Loading:
                GUILoading();
                break;
        }
    }

    public void SwitchVehicle(Vehicle newVehicle)
    {
        vehicle = newVehicle;
        //Changing the buttons colours
        switch (newVehicle)
        {
            case Vehicle.AV42C:
                Console.Log("Switched player's vehicle to AV-42C");
                break;
            case Vehicle.F45A:
                Console.Log("Switched player's vehicle to F-45A");
                break;
            case Vehicle.FA26B:
                Console.Log("Switched player's vehicle to F/A-26B");
                break;
        }
    }
    private void GUIOffline()
    {
        GUI.Label(new Rect(0, 0, 100, 20), "Offline");
    }
    private void GUILoading()
    {
        GUI.Label(new Rect(0, 0, 100, 20), "Loading...");
    }
    private bool CheckIfPilotExists(string name)
    {
        return PilotSaveManager.pilots.ContainsKey(name);
    }
    public void Connect()
    {
        StartCoroutine(LoadLevel());
    }
    private IEnumerator LoadLevel()
    {
        state = ConnectionState.Loading;
        Console.Log("Connection State == Loading");
        VTMapManager.nextLaunchMode = VTMapManager.MapLaunchModes.Scenario;
        LoadingSceneController.LoadScene(7);

        yield return new WaitForSeconds(5);
        //After here we should be in the loader scene

        Console.Log("Setting Pilot");
        PilotSaveManager.current = PilotSaveManager.pilots[pilotName];

        Console.Log("Going though All built in campaigns");
        if (VTResources.GetBuiltInCampaigns() != null)
        {
            foreach (VTCampaignInfo info in VTResources.GetBuiltInCampaigns())
            {

                if (vehicle == Vehicle.AV42C && info.campaignID == "av42cQuickFlight")
                {
                    Console.Log("Setting Campaign");
                    PilotSaveManager.currentCampaign = info.ToIngameCampaign();
                    Console.Log("Setting Vehicle");
                    PilotSaveManager.currentVehicle = VTResources.GetPlayerVehicle(info.vehicle);
                    break;
                }

                if (vehicle == Vehicle.FA26B && info.campaignID == "fa26bFreeFlight")
                {
                    Console.Log("Setting Campaign");
                    PilotSaveManager.currentCampaign = info.ToIngameCampaign();
                    Console.Log("Setting Vehicle");
                    PilotSaveManager.currentVehicle = VTResources.GetPlayerVehicle(info.vehicle);
                    break;
                }
            }
        }
        else
            Console.Log("Campaigns are null");

        Console.Log("Going though All missions in that campaign");
        foreach (CampaignScenario cs in PilotSaveManager.currentCampaign.missions)
        {
            Console.Log("CampaignScenario == " + cs.scenarioID);
            if (cs.scenarioID == "freeFlight" || cs.scenarioID == "Free Flight")
            {
                Console.Log("Setting Scenario");
                PilotSaveManager.currentScenario = cs;
                break;
            }
        }

        VTScenario.currentScenarioInfo = VTResources.GetScenario(PilotSaveManager.currentScenario.scenarioID, PilotSaveManager.currentCampaign);

        Console.Log(string.Format("Loading into game, Pilot:{3}, Campaign:{0}, Scenario:{1}, Vehicle:{2}",
            PilotSaveManager.currentCampaign.campaignName, PilotSaveManager.currentScenario.scenarioName,
            PilotSaveManager.currentVehicle.vehicleName, pilotName));

        LoadingSceneController.instance.PlayerReady(); //<< Auto Ready

        while (SceneManager.GetActiveScene().buildIndex != 7)
        {
            //Pausing this method till the loader scene is unloaded
            yield return null;
        }

        //Adding the networking script to the game which will handle all of the other stuff
        gameObject.AddComponent<NetworkingManager>().mod = this;
    }
}