using System;
using UnityEngine;

namespace WreckMP
{
	internal class HeaderDebug : MonoBehaviour
	{
		private void Start()
		{
			this.rb = base.GetComponent<Rigidbody>();
		}

		private void OnGUI()
		{
			GUI.Label(new Rect(100f, 100f, 1000f, 1000f), string.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n{7}\n{8}\n{9}\n{10}\n{11}\n{12}\n{13}", new object[]
			{
				this.rb.mass,
				this.rb.drag,
				this.rb.angularDrag,
				this.rb.useGravity,
				this.rb.isKinematic,
				this.rb.interpolation,
				this.rb.collisionDetectionMode,
				this.rb.freezeRotation,
				this.rb.constraints,
				this.rb.detectCollisions,
				this.rb.velocity,
				this.rb.angularVelocity,
				this.rb.centerOfMass,
				this.rb.worldCenterOfMass
			}));
		}

		private Rigidbody rb;
	}
}
