using UnityEngine;

[System.Serializable]
public class DiscordJoinEvent : UnityEngine.Events.UnityEvent<string> { }

[System.Serializable]
public class DiscordSpectateEvent : UnityEngine.Events.UnityEvent<string> { }

[System.Serializable]
public class DiscordJoinRequestEvent : UnityEngine.Events.UnityEvent<DiscordRpc.DiscordUser> { }

public class DiscordController : MonoBehaviour
{
    public DiscordRpc.RichPresence presence = new DiscordRpc.RichPresence();
    public string applicationId = "594269345392099341";
    public string optionalSteamId;
    public DiscordRpc.DiscordUser joinRequest;
    public UnityEngine.Events.UnityEvent onConnect;
    public UnityEngine.Events.UnityEvent onDisconnect;
    public UnityEngine.Events.UnityEvent hasResponded;
    public DiscordJoinEvent onJoin;
    public DiscordJoinEvent onSpectate;
    public DiscordJoinRequestEvent onJoinRequest;

    DiscordRpc.EventHandlers handlers;
    private bool connected;

    public void UpdatePresence(int loadModsCount, string detail, string state)
    {
        
        if (loadModsCount == 0)
            presence.largeImageText = "Running no mods :(";
        else
            presence.largeImageText = "Running " + loadModsCount.ToString() + (loadModsCount == 1 ? " mod" : " mods");

        try
        {
            string vehicleName = PilotSaveManager.currentVehicle.vehicleName;
            switch (vehicleName)
            {
                case "AV-42C":
                    presence.largeImageKey = "av42c";
                    break;
                case "F/A-26B":
                    presence.largeImageKey = "fa26b";
                    break;
                case "F-45A":
                    presence.largeImageKey = "fa45a";
                    break;
                default:
                    presence.largeImageKey = "vtol-logo";
                    break;
            }
        }
        catch (System.Exception)
        {
            presence.largeImageKey = "vtol-logo";
        }

        presence.smallImageKey = "logo";
        presence.smallImageText = "VTOL VR Mod Loader\nhttps://vtolvr-mods.com/";
        presence.details = detail;
        presence.state = state;
        //presence.joinSecret = "MTI4NzM0OjFpMmhuZToxMjMxMjM";

        DiscordRpc.UpdatePresence(presence);
    }

    public void RequestRespondYes()
    {
        Debug.Log("Discord: responding yes to Ask to Join request");
        DiscordRpc.Respond(joinRequest.userId, DiscordRpc.Reply.Yes);
        hasResponded.Invoke();
    }

    public void RequestRespondNo()
    {
        Debug.Log("Discord: responding no to Ask to Join request");
        DiscordRpc.Respond(joinRequest.userId, DiscordRpc.Reply.No);
        hasResponded.Invoke();
    }

    public void ReadyCallback(ref DiscordRpc.DiscordUser connectedUser)
    {
        connected = true;
        onConnect.Invoke();
    }

    public void DisconnectedCallback(int errorCode, string message)
    {
        Debug.Log(string.Format("Discord: disconnect {0}: {1}", errorCode, message));
        onDisconnect.Invoke();
    }

    public void ErrorCallback(int errorCode, string message)
    {
        Debug.Log(string.Format("Discord: error {0}: {1}", errorCode, message));
    }

    public void JoinCallback(string secret)
    {
        Debug.Log(string.Format("Discord: join ({0})", secret));
        onJoin.Invoke(secret);
    }

    public void SpectateCallback(string secret)
    {
        Debug.Log(string.Format("Discord: spectate ({0})", secret));
        onSpectate.Invoke(secret);
    }

    public void RequestCallback(ref DiscordRpc.DiscordUser request)
    {
        Debug.Log(string.Format("Discord: join request {0}#{1}: {2}", request.username, request.discriminator, request.userId));
        joinRequest = request;
        onJoinRequest.Invoke(request);
    }
    
    void Update()
    {
        if (connected)
            DiscordRpc.RunCallbacks();
    }

    void OnEnable()
    {
        handlers = new DiscordRpc.EventHandlers();
        handlers.readyCallback += ReadyCallback;
        handlers.disconnectedCallback += DisconnectedCallback;
        handlers.errorCallback += ErrorCallback;
        handlers.joinCallback += JoinCallback;
        handlers.spectateCallback += SpectateCallback;
        handlers.requestCallback += RequestCallback;
        DiscordRpc.Initialize(applicationId, ref handlers, true, optionalSteamId);
    }

    void OnDisable()
    {
        Debug.Log("Discord: shutdown");
        DiscordRpc.Shutdown();
    }

    void OnDestroy()
    {

    }
}
