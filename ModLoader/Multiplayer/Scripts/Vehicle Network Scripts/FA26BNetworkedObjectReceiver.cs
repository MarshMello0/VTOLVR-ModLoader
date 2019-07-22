using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;


namespace ModLoader.Multiplayer.NetworkedObjects.Vehicles
{
    public class FA26BNetworkedObjectReceiver : MonoBehaviour
    {
        public UnityClient client;
        public Transform worldCenter;
        public NetworkingManager manager;
        public Player player;
        public ushort id;

        //Classes we use to set the information
        private AeroController aeroController;
        private AIPilot aiPilot;
        private AutoPilot autoPilot;
        private WheelsController wheelController;

        private void Start()
        {
            gameObject.AddComponent<FloatingOriginTransform>();

            aeroController = GetComponent<AeroController>();
            aiPilot = GetComponent<AIPilot>();
            autoPilot = GetComponent<AutoPilot>();
            wheelController = GetComponent<WheelsController>();

            aiPilot.commandState = AIPilot.CommandStates.Override;
            Console.Log("The Ai Pilot command state is == " + aiPilot.commandState);
            aiPilot.kPlane.SetGear(true);
        }

        public void SetReceiver()
        {
            if (client)
                client.MessageReceived += MessageReceived;
        }

        private void MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                ushort tag = message.Tag;
                switch (tag)
                {
                    case (ushort)Tags.FA26B_General:
                        FA26BGeneralReceived(message.GetReader());
                        break;
                }
            }
        }

        private void FA26BGeneralReceived(DarkRiftReader reader)
        {
            while (reader.Position < reader.Length)
            {
                ushort id = reader.ReadUInt16();

                if (this.id == id)
                {
                    float positionX = reader.ReadSingle();
                    float positionY = reader.ReadSingle();
                    float positionZ = reader.ReadSingle();

                    float rotationX = reader.ReadSingle();
                    float rotationY = reader.ReadSingle();
                    float rotationZ = reader.ReadSingle();

                    float speed = reader.ReadSingle();
                    bool landingGear = reader.ReadBoolean();
                    float flaps = reader.ReadSingle();

                    float pitch = reader.ReadSingle();
                    float yaw = reader.ReadSingle();
                    float roll = reader.ReadSingle();
                    float breaks = reader.ReadSingle();
                    float throttle = reader.ReadSingle();
                    float wheels = reader.ReadSingle();

                    player.SetPosition(positionX, positionY, positionZ);
                    player.SetRotation(rotationX, rotationY, rotationZ);
                    player.speed = speed;
                    player.landingGear = landingGear;
                    player.flaps = flaps;
                    player.pitch = pitch;
                    player.yaw = yaw;
                    player.roll = roll;
                    player.breaks = breaks;
                    player.throttle = throttle;
                    player.wheels = wheels;

                    manager.UpdatePlayerListString();

                    transform.position = (worldCenter.position - new Vector3(positionX, positionY, positionZ)) + new Vector3(0, 0.5f, 0);
                    transform.rotation = Quaternion.Euler(rotationX, rotationY, rotationZ);

                    UpdateAI();
                }
                else
                    return;
            }
        }

        private void UpdateAI()
        {
            Vector3 input = player.GetPitchYawRoll();
            float breaks = player.breaks;
            bool landingGear = player.landingGear;
            float flaps = player.flaps;
            float throttle = player.throttle;
            float wheels = player.wheels;

            if (aiPilot)
            {
                aiPilot.commandState = AIPilot.CommandStates.Override;
            }
            else
                Console.Log("Missing Ai Pilot");


            //wheelsController.SetGear(landingGear);
            if (aeroController)
            {
                if (aeroController.flaps != flaps)
                    aeroController.SetFlaps(flaps);
                if (aeroController.input != input)
                    aeroController.input = input;
            }
            else
                Console.Log("Missing Aero Controller");

            if (autoPilot)
            {
                autoPilot.OverrideSetThrottle(throttle);
            }
            else
                Console.Log("Missing Auto Pilot");

            if (wheelController)
            {
                wheelController.SetBrakes(breaks);
                wheelController.SetBrakeLock(-1);
                wheelController.SetWheelSteer(wheels);
            }
            else
                Console.Log("Missing Wheel Controller");
            
        }
    }
}
