using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;


namespace NetworkedObjects.Vehicles
{
    public class FA26BNetworkedObjectReceiver : MonoBehaviour
    {
        public UnityClient client;
        public Transform worldCenter;
        public NetworkingManager manager;
        public Player player;
        public ushort id;

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

                    player.SetPosition(positionX, positionY, positionZ);
                    player.SetRotation(rotationX, rotationY, rotationZ);
                    player.speed = speed;
                    player.landingGear = landingGear;
                    player.flaps = flaps;

                    manager.UpdatePlayerListString();

                    transform.position = worldCenter.position - new Vector3(positionX, positionY, positionZ);
                    transform.rotation = Quaternion.Euler(rotationX, rotationY, rotationZ);
                }
                else
                    return;
            }
        }
    }
}
