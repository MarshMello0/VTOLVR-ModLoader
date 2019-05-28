using BeardedManStudios.Forge.Networking;
using UnityEngine;

namespace BeardedManStudios.Forge.Networking.Generated
{
	public abstract partial class BasicObjectBehavior : INetworkBehavior
	{
		
		public BasicObjectNetworkObject networkObject = null;

		public void Initialize(NetworkObject obj)
		{
			// We have already initialized this object
			if (networkObject != null && networkObject.AttachedBehavior != null)
				return;
			
			networkObject = (BasicObjectNetworkObject)obj;
			networkObject.AttachedBehavior = this;

			networkObject.RegistrationComplete();
		}

		public void Initialize(NetWorker networker, byte[] metadata  = null)
		{
			Initialize(new BasicObjectNetworkObject(networker, metadata: metadata));
		}


		// DO NOT TOUCH, THIS GETS GENERATED PLEASE EXTEND THIS CLASS IF YOU WISH TO HAVE CUSTOM CODE ADDITIONS
	}
}