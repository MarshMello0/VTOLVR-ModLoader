using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

namespace NetworkedObjects.Player
{
    public class PlayerHandLeftNetworkedObjectReceiver : Receiver
    {
        public UnityClient client;
        public ushort id;

        public void SetReceiver()
        {
            client.MessageReceived += MessageReceived;
        }


        private void MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                if (message.Tag == (ushort)Tags.PlayerHandLeft_Movement)
                {
                    using (DarkRiftReader reader = message.GetReader())
                    {
                        ushort id = reader.ReadUInt16();
                        Vector3 newPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

                        if (id == this.id)
                        {
                            transform.position = newPosition;
                        }
                    }
                }
                else if (message.Tag == (ushort)Tags.PlayerHandLeft_Rotation)
                {
                    using (DarkRiftReader reader = message.GetReader())
                    {
                        ushort id = reader.ReadUInt16();
                        Quaternion rotation = Quaternion.Euler(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

                        if (id == this.id)
                        {
                            transform.rotation = rotation;
                        }
                    }
                }
                else if (message.Tag == (ushort)Tags.DestroyPlayer)
                {
                    using (DarkRiftReader reader = message.GetReader())
                    {
                        ushort id = reader.ReadUInt16();

                        Destroy(id);
                    }
                }
            }
        }

        public void Destroy(ushort id)
        {
            if (id == this.id)
            {
                client.MessageReceived -= MessageReceived;
                Destroy(this.gameObject);
            }
        }

        public override void DestoryReceiver()
        {
            client.MessageReceived -= MessageReceived;
            Destroy(this.gameObject);
        }
    }
}
