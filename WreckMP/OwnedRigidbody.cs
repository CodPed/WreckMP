using System;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	public class OwnedRigidbody
	{
		public ulong OwnerID
		{
			get
			{
				return this.owner;
			}
			internal set
			{
				if (this.Rigidbody != null)
				{
					this.SetKinematic(this.Rigidbody);
				}
				this.owner = value;
			}
		}

		public Rigidbody Rigidbody
		{
			get
			{
				if (this.rigidbody != null)
				{
					return this.rigidbody;
				}
				if (this.remove != null && this.Removal_Rigidbody != null)
				{
					Rigidbody rigidbody = this.Removal_Rigidbody.Value as Rigidbody;
					if (rigidbody != null)
					{
						return rigidbody;
					}
					if (this.Removal_Rigidbody_Cache)
					{
						return this.Removal_Rigidbody_Cache;
					}
					if (this.remove.enabled)
					{
						return null;
					}
					if (Time.time - this.lastRBcheckTime > 0.5f)
					{
						this.lastRBcheckTime = Time.time;
						rigidbody = (this.Removal_Rigidbody_Cache = this.remove.transform.GetComponent<Rigidbody>());
						if (rigidbody != null)
						{
							this.SetKinematic(rigidbody);
						}
						this.Removal_Rigidbody.Value = rigidbody;
						return rigidbody;
					}
					return null;
				}
				else
				{
					if (!(this.remove != null) || this.Removal_Part == null)
					{
						return null;
					}
					if (this.rigidbodyPart)
					{
						return this.rigidbodyPart;
					}
					if (this.Removal_Part.Value != null)
					{
						this.lastRBcheckTime = Time.time;
						this.rigidbodyPart = this.Removal_Part.Value.GetComponent<Rigidbody>();
						if (this.rigidbodyPart != null)
						{
							this.SetKinematic(this.rigidbodyPart);
						}
						return this.rigidbodyPart;
					}
					return null;
				}
			}
		}

		private void SetKinematic(Rigidbody rb)
		{
		}

		private ulong owner = WreckMPGlobals.UserID;

		internal bool defaultKinematic;

		internal int hash;

		internal Rigidbody rigidbody;

		internal Rigidbody rigidbodyPart;

		internal Vector3 cachedPosition;

		internal Vector3 cachedEulerAngles;

		public Transform transform;

		private float lastRBcheckTime;

		private static readonly int datsunLayer = LayerMask.NameToLayer("Datsun");

		internal FsmObject Removal_Rigidbody;

		private Rigidbody Removal_Rigidbody_Cache;

		internal FsmGameObject Removal_Part;

		internal PlayMakerFSM assemble;

		internal PlayMakerFSM remove;

		internal JointCache[] joints;
	}
}
