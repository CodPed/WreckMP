using System;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	internal class TowHookTrigger : MonoBehaviour
	{
		private void Start()
		{
			this.guiuse = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");
			this.guiineraction = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");
			this.coll = base.GetComponent<Collider>();
		}

		private void Update()
		{
			if (this.rope != null && !this.rope.isValid)
			{
				if (this.joint != null)
				{
					NetTowHookManager.SetFreeRope(this.rope);
					Object.Destroy(this.joint);
					this.joint = null;
				}
				this.rope = null;
			}
			bool flag = Raycaster.Raycast(this.coll, 1f, 2048);
			if (flag || flag != this._hit)
			{
				this._hit = flag;
				this.guiuse.Value = flag;
				this.guiineraction.Value = (flag ? ((this.rope == null) ? "TOWING HOOK" : "REMOVE TOW HOOK") : "");
				if (flag && Input.GetMouseButtonDown(0))
				{
					if (this.rope == null)
					{
						bool flag2 = NetTowHookManager.ropeInHand == null;
						this.rope = (flag2 ? NetTowHookManager.GetFreeRope(this.hash, true) : NetTowHookManager.ropeInHand);
						this.hookIsA = flag2;
						if (this.hookIsA)
						{
							this.rope.SetConnect(base.transform, null);
							NetTowHookManager.ropeInHand = this.rope;
							return;
						}
						this.rope.SetConnectB(base.transform, this.hash);
						NetTowHookManager.ropeInHand = null;
						this.joint = this.rope.a.parent.parent.gameObject.AddComponent<SpringJoint>();
						this.joint.autoConfigureConnectedAnchor = false;
						this.joint.anchor = this.rope.a.parent.localPosition;
						this.joint.connectedAnchor = this.rope.b.parent.localPosition;
						this.joint.enableCollision = true;
						this.joint.spring = 52000f;
						this.joint.maxDistance = Vector3.Distance(this.rope.a.position, this.rope.b.position);
						this.joint.connectedBody = this.rope.b.parent.parent.GetComponent<Rigidbody>();
						this.joint.breakForce = (this.joint.breakTorque = 55000f);
						this.joint.gameObject.AddComponent<JointWatcher>().jointBroken = new Action(this.DestroyRope);
						return;
					}
					else
					{
						this.DestroyRope();
					}
				}
			}
		}

		internal void CreateRopeMP(Player target)
		{
			this.rope = NetTowHookManager.GetFreeRope(0, false);
			this.rope.owner = target;
			this.hookIsA = true;
			this.rope.SetConnect(base.transform, target.playerAnimationManager.towHookPivot);
			target.playerAnimationManager.SetTowhook(true);
		}

		internal void AddRopeJointMP()
		{
			this.joint = this.rope.a.root.gameObject.AddComponent<SpringJoint>();
			this.joint.autoConfigureConnectedAnchor = false;
			this.joint.anchor = this.rope.a.parent.localPosition;
			this.joint.connectedAnchor = this.rope.b.parent.localPosition;
			this.joint.enableCollision = true;
			this.joint.spring = 52000f;
			this.joint.maxDistance = Vector3.Distance(this.rope.a.position, this.rope.b.position);
			this.joint.connectedBody = this.rope.b.root.GetComponent<Rigidbody>();
			this.joint.breakForce = (this.joint.breakTorque = 55000f);
			this.joint.gameObject.AddComponent<JointWatcher>().jointBroken = new Action(this.DestroyRope);
		}

		private void DestroyRope()
		{
			this.rope.destroyEvent.SendEmpty(0UL, true);
			if (this.rope == null || !this.rope.isValid)
			{
				return;
			}
			this.rope.isValid = false;
			JointWatcher component = this.rope.a.root.GetComponent<JointWatcher>();
			if (component != null)
			{
				Object.Destroy(component);
				Object.Destroy(this.rope.a.root.GetComponent<SpringJoint>());
			}
			if (NetTowHookManager.ropeInHand == this.rope)
			{
				NetTowHookManager.ropeInHand = null;
			}
			NetTowHookManager.SetFreeRope(this.rope);
		}

		private Collider coll;

		private FsmBool guiuse;

		private FsmString guiineraction;

		private bool _hit;

		private SpringJoint joint;

		internal TowRope rope;

		internal bool hookIsA;

		internal int hash;

		private const int mask = 2048;
	}
}
