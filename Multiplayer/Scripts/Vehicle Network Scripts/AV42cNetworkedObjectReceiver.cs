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
                    Vector3 newPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    Quaternion newrotation = Quaternion.Euler(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    float speed = reader.ReadSingle();
                    bool landingGear = reader.ReadBoolean();
                    int flaps = reader.ReadInt32();
                    float thrusterAngle = reader.ReadSingle();
                }
                else
                    return;
            }
        }
    }
}
