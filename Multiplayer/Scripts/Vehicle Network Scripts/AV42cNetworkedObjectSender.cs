using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using System.Collections;

namespace NetworkedObjects.Vehicles
{
    public class AV42cNetworkedObjectSender : MonoBehaviour
    {
        public UnityClient client;

        //Classes we use to find the information out
        private FlightInfo flightInfo;
        private WheelsController wheelsController;
        private AeroController aeroController;
        private TiltController tiltController;

        //Information which gets sent over the network 
        //(These variables are also the last sent ones over the network which is compaired in CheckVariabes() )
        private float positionX, positionY, positionZ;
        private float rotationX, rotationY, rotationZ;
        private float speed = 0;
        private bool landingGear = true;
        private float flaps; //0 = 0, 0.5 = 1, 1 = 1
        private float thrusterAngle = 90;

        private float minDistance = 0.1f;
        private float minRotation = 0.1f;
        private float minSpeed = 0.1f;
        private void Start()
        {
            flightInfo = GetComponent<FlightInfo>();
            wheelsController = GetComponent<WheelsController>();
            aeroController = GetComponent<AeroController>();
            //At this state there should only just be the player in the game, hopefully with 1 tilt controller
            tiltController = FindObjectOfType<TiltController>();
        }

        private void Update()
        {
            CheckVariables();
        }

        private void CheckVariables()
        {
            if (Vector3.Distance(new Vector3(positionX, positionY, positionZ), transform.position) >= minDistance ||
                Vector3.Distance(new Vector3(rotationX, rotationY, rotationZ), transform.rotation.eulerAngles) >= minRotation ||
                Mathf.Abs(speed - flightInfo.airspeed) >= minSpeed ||
                landingGear != LandingGearState() ||
                flaps != aeroController.flaps ||
                thrusterAngle != tiltController.currentTilt)
            {
                UpdateVariables(true);
            }
        }

        private bool LandingGearState()
        {
            return wheelsController.gearAnimator.GetCurrentState() == GearAnimator.GearStates.Extended;
        }

        private void UpdateVariables(bool sendInfo = false)
        {
            Vector3 position = transform.position;
            positionX = position.x;
            positionY = position.y;
            positionZ = position.z;

            Vector3 rotation = transform.rotation.eulerAngles;
            rotationX = rotation.x;
            rotationY = rotation.y;
            rotationZ = rotation.z;

            speed = flightInfo.airspeed;

            landingGear = LandingGearState();


            flaps = aeroController.flaps;

            thrusterAngle = tiltController.currentTilt;

            if (sendInfo)
                SendVariables();
        }

        private void SendVariables()
        {
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(positionX);
                writer.Write(positionY);
                writer.Write(positionZ);
                writer.Write(rotationX);
                writer.Write(rotationY);
                writer.Write(rotationZ);
                writer.Write(speed);
                writer.Write(landingGear);
                writer.Write(flaps);
                writer.Write(thrusterAngle);

                using (Message message = Message.Create((ushort)Tags.AV42c_General, writer))
                    client.SendMessage(message, SendMode.Unreliable);
            }
        }
    }
}
