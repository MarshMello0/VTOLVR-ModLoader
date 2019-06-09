using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

namespace NetworkedObjects.Players
{
    public class PlayerHeadNetworkedObjectSender : Sender
    {
        public UnityClient client;
        private float moveDistance = 0.05f;
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private void Update()
        {
            if (!client.Connected)
                return;
            //We don't need to send all of the time if they are not moving.
            if (Vector3.Distance(lastPosition, transform.position) > moveDistance)
            {
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    writer.Write(transform.position.x);
                    writer.Write(transform.position.y);
                    writer.Write(transform.position.z);

                    using (Message message = Message.Create((ushort)Tags.PlayerHead_Movement, writer))
                        client.SendMessage(message, SendMode.Unreliable);
                }

                lastPosition = transform.position;
            }

            if (lastRotation != transform.rotation)
            {
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    writer.Write(transform.rotation.eulerAngles.x);
                    writer.Write(transform.rotation.eulerAngles.y);
                    writer.Write(transform.rotation.eulerAngles.z);

                    using (Message message = Message.Create((ushort)Tags.PlayerHead_Rotation, writer))
                        client.SendMessage(message, SendMode.Unreliable);
                }

                lastRotation = transform.rotation;
            }
        }
    }
}