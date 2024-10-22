using System;
using UnityEngine;

namespace WreckMP
{
	internal class JointWatcher : MonoBehaviour
	{
		private void OnJointBreak(float breakForce)
		{
			Action action = this.jointBroken;
			if (action != null)
			{
				action();
			}
			Object.Destroy(this);
		}

		public Action jointBroken;
	}
}
