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
    public class AV42cNetworkedObjectReceiver : MonoBehaviour
    {
        public UnityClient client;
        public NetworkingManager manager;
        
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
                switch(tag)
                {
                    case (ushort)Tags.AV42c_General:
                        AV42CGeneralReceived(message.GetReader());
                        break;
                }
            }
        }

        private void AV42CGeneralReceived(DarkRiftReader reader)
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
                    float thrusterAngle = reader.ReadSingle();

                    for (int i = 0; i < manager.playersInfo.Count; i++)
                    {
                        if (manager.playersInfo[i].id == id)
                        {
                            manager.playersInfo[i].SetPosition(positionX, positionY, positionZ);
                            manager.playersInfo[i].SetRotation(rotationX, rotationY, rotationZ);
                            manager.playersInfo[i].speed = speed;
                            manager.playersInfo[i].landingGear = landingGear;
                            manager.playersInfo[i].flaps = flaps;
                            manager.playersInfo[i].thrusterAngle = thrusterAngle;
                            break;
                        }
                    }

                    manager.UpdatePlayerListString();
                }
                else
                    return;
            }
        }
    }
}
