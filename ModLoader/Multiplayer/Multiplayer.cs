using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using Steamworks;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using System.Net;
using UnityEngine.UI;

public class MultiplayerMod : MonoBehaviour
{
    //This is the state which the client is currently in
    public enum ConnectionState { Offline, Connecting, Lobby, Loading, InGame }
    public ConnectionState state = ConnectionState.Offline;

    //This is the information about what the player has chosen
    public enum Vehicle { FA26B, AV42C,F45A }
    public Vehicle vehicle = Vehicle.AV42C;
    public string pilotName = "Pilot Name";
    

    public UnityClient client { private set; get; }
    public ModLoader.ModLoader modLoader;
    public Text serverInfoText;

    private string currentMap;
    private void Start()
    {
        client = gameObject.AddComponent<UnityClient>();
        client.MessageReceived += MessageReceived;
        client.Disconnected += Disconnected;
    }

    public void ConnectToServer(string ip = "86.154.179.6", int port = 4296)
    {
        state = ConnectionState.Connecting;
        try
        {
            //This causes an error if it doesn't connect
            client.Connect(IPAddress.Parse(ip), port, DarkRift.IPVersion.IPv4);

        }
        catch
        {
            //Failed to connect
            return;
        }

        //Sending a message of our information
        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(SteamUser.GetSteamID().m_SteamID);
            writer.Write(SteamFriends.GetPersonaName());
            using (Message message = Message.Create((ushort)Tags.UserInfo, writer))
                client.SendMessage(message, SendMode.Reliable);
        } 
    }

    private void MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        //This should only need to handle one message, 
        //which is displaying the info about the server to the user
        using (Message message = e.GetMessage())
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                ushort tag = (ushort)message.Tag;
                switch (tag)
                {
                    case (ushort)Tags.LobbyInfo:
                        while (reader.Position < reader.Length)
                        {
                            string serverName = reader.ReadString();
                            string mapName = reader.ReadString();
                            int playerCount = reader.ReadInt32();
                            int maxPlayerCount = reader.ReadInt32();
                            string playersNames = reader.ReadString();
                            
                            currentMap = mapName;
                            serverInfoText.text = "Name: " + serverName + "\nMap: " + mapName
                                + "\nPlayers: " + playerCount + "/" + maxPlayerCount + "\n"
                                + playersNames;

                            state = ConnectionState.Lobby;
                            modLoader.SwitchPage(ModLoader.ModLoader.Page.mpServerInfo);
                        }
                        break;
                }

                //Need to check if the bann message was returned

            }
        }
    }

    private void Disconnected(object sender, DisconnectedEventArgs e)
    {
        //When we press the button to go back, we disconnect then once fully disconnected we can switch page
        if (modLoader)
            modLoader.SwitchPage(ModLoader.ModLoader.Page.mpIPPort);
        currentMap = "NULL";
    }

    public void JoinGame()
    {
        StartCoroutine(JoinGameEnumerator());
    }
    private IEnumerator JoinGameEnumerator()
    {
        state = ConnectionState.Loading;
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
        NetworkingManager nm = gameObject.AddComponent<NetworkingManager>();
        nm.mod = this;
        nm.client = client;
        nm.StartProcedure();
        client.MessageReceived -= MessageReceived;// Removing multiplayer.cs one as its not needed any more
        client.MessageReceived += nm.MessageReceived;
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
    
}