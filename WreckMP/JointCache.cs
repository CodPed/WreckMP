using System;
using UnityEngine;

namespace WreckMP
{
	internal class JointCache : MonoBehaviour
	{
		public void OwnerChanged(ulong oldOwner, ulong newOwner)
		{
		}

		public OwnedRigidbody connectedBody;

		public OwnedRigidbody mainBody;

		public Joint joint;

		public Type jointType;

		private FieldCacher cachedProperties;
	}
}
