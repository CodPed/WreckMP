using System;
using UnityEngine;

namespace WreckMP
{
	public class NetVehicleDriverPivots
	{
		public Transform gearStick
		{
			get
			{
				if (this.gearSticks == null)
				{
					return null;
				}
				for (int i = 0; i < this.gearSticks.Length; i++)
				{
					if (this.gearSticks[i].gameObject.activeInHierarchy)
					{
						return this.gearSticks[i];
					}
				}
				if (this.gearSticks.Length == 0)
				{
					return null;
				}
				return this.gearSticks[0];
			}
			set
			{
				this.gearSticks = new Transform[] { value };
			}
		}

		public Transform throttlePedal;

		public Transform brakePedal;

		public Transform clutchPedal;

		public Transform steeringWheel;

		public Transform driverParent;

		internal Transform[] gearSticks;
	}
}
