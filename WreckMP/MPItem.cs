using System;
using UnityEngine;

namespace WreckMP
{
	internal class MPItem : MonoBehaviour
	{
		internal void UpdateOwner()
		{
			if (MPItem.vehicleItemCollidersTransforms.Length != NetVehicleManager.vehicles.Count + 1 || MPItem.vehicleItemCollidersRadiuses.Length != NetVehicleManager.vehicles.Count + 1)
			{
				MPItem.vehicleItemCollidersTransforms = new Vector3[NetVehicleManager.vehicles.Count + 1];
				MPItem.vehicleItemCollidersRadiuses = new float[NetVehicleManager.vehicles.Count + 1];
				MPItem.vehicleItemCollidersTransforms[0] = NetBoatManager.instance.itemCollider.transform.localPosition;
				MPItem.vehicleItemCollidersRadiuses[0] = NetBoatManager.instance.itemCollider.radius;
			}
			if (!this.doUpdate)
			{
				return;
			}
			if (!this.RB.Rigidbody)
			{
				this.doUpdate = false;
				return;
			}
			if (Player.grabbedItemsHashes.Contains(this.RB.hash))
			{
				return;
			}
			int i = 0;
			while (i < MPItem.vehicleItemCollidersTransforms.Length)
			{
				Vector3 vector = ((i == 0) ? NetBoatManager.instance.boat.transform : NetVehicleManager.vehicles[i - 1].Transform).position + MPItem.vehicleItemCollidersTransforms[i];
				float num = MPItem.vehicleItemCollidersRadiuses[i];
				num *= num;
				if ((base.transform.position - vector).sqrMagnitude < num)
				{
					ulong num2 = ((i == 0) ? NetBoatManager.instance.owner : NetVehicleManager.vehicles[i - 1].Owner);
					if (this.RB.OwnerID != num2)
					{
						NetRigidbodyManager.RequestOwnership(this.RB, num2);
						return;
					}
					break;
				}
				else
				{
					i++;
				}
			}
		}

		internal OwnedRigidbody RB;

		internal bool doUpdate = true;

		private static Vector3[] vehicleItemCollidersTransforms = new Vector3[0];

		private static float[] vehicleItemCollidersRadiuses = new float[0];
	}
}
