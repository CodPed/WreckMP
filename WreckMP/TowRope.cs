using System;
using UnityEngine;

namespace WreckMP
{
	internal class TowRope : MonoBehaviour
	{
		internal void _Start()
		{
			this.a = base.transform.GetChild(0);
			this.b = base.transform.GetChild(1);
			this.connectEvent = new GameEvent("ConnectTowRope" + this.id.ToString(), delegate(GameEventReader p)
			{
				int num = p.ReadInt32();
				if (!NetTowHookManager.towHooks.ContainsKey(num))
				{
					return;
				}
				this.ConnectB_MP(num);
			}, GameScene.GAME);
			this.destroyEvent = new GameEvent("DestroyTowRope" + this.id.ToString(), delegate(GameEventReader p)
			{
				this.isValid = false;
				JointWatcher component = this.a.root.GetComponent<JointWatcher>();
				if (component != null)
				{
					Object.Destroy(component);
					Object.Destroy(this.a.root.GetComponent<SpringJoint>());
				}
				if (NetTowHookManager.ropeInHand == this)
				{
					NetTowHookManager.ropeInHand = null;
				}
				NetTowHookManager.SetFreeRope(this);
				this.owner.playerAnimationManager.SetTowhook(false);
			}, GameScene.GAME);
		}

		public void ConnectB_MP(int hash)
		{
			TowHookTrigger towHookTrigger = NetTowHookManager.towHooks[hash];
			this.SetConnectB(towHookTrigger.transform, 0);
			towHookTrigger.rope = this;
			towHookTrigger.hookIsA = false;
			towHookTrigger.AddRopeJointMP();
			this.owner.playerAnimationManager.SetTowhook(false);
		}

		public void SetConnect(Transform a, Transform b)
		{
			this.SetConnectA(a);
			this.SetConnectB(b, 0);
		}

		public void SetConnectA(Transform a)
		{
			this.a.transform.parent = a ?? base.transform;
			this.a.transform.localPosition = Vector3.zero;
		}

		public void SetConnectB(Transform b, int towhookHash)
		{
			this.b.transform.parent = b ?? base.transform;
			this.b.transform.localPosition = Vector3.zero;
			if (towhookHash != 0)
			{
				using (GameEventWriter gameEventWriter = this.connectEvent.Writer())
				{
					gameEventWriter.Write(towhookHash);
					this.connectEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
				}
			}
		}

		public Transform a;

		public Transform b;

		public bool isValid = true;

		internal byte id;

		internal GameEvent connectEvent;

		internal GameEvent destroyEvent;

		internal Player owner;
	}
}
