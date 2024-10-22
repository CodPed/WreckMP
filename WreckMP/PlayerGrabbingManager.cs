using System;
using System.Collections;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	internal class PlayerGrabbingManager : NetManager
	{
		public static Rigidbody GrabbedRigidbody
		{
			get
			{
				return PlayerGrabbingManager.handItem_rb;
			}
		}

		private void Start()
		{
			base.StartCoroutine(this.init());
		}

		private IEnumerator init()
		{
			while (!PlayerGrabbingManager.itemPivot.Value)
			{
				yield return null;
			}
			PlayerGrabbingManager.handFSM = PlayerGrabbingManager.itemPivot.Value.transform.parent.GetChild(2).GetComponent<PlayMakerFSM>();
			PlayerGrabbingManager.handItem = PlayerGrabbingManager.handFSM.FsmVariables.FindFsmGameObject("RaycastHitObject");
			this.throwForce = PlayerGrabbingManager.handFSM.FsmVariables.FindFsmVector3("temp_suunta");
			PlayerGrabbingManager.handFSM.InsertAction("State 1", new Action(this.OnItemGrabbed), -1, false);
			PlayerGrabbingManager.handFSM.InsertAction("Wait", new Action(this.OnItemDropped), -1, false);
			PlayerGrabbingManager.handFSM.InsertAction("Look for object", new Action(this.OnItemDropped), -1, false);
			PlayerGrabbingManager.handFSM.InsertAction("Drop part", delegate
			{
				this.throwForce.Value = Vector3.zero;
			}, -1, false);
			yield break;
		}

		private void OnItemGrabbed()
		{
			if (PlayerGrabbingManager.handItem.Value == null)
			{
				return;
			}
			PlayerGrabbingManager.handItem_rb = PlayerGrabbingManager.handItem.Value.GetComponent<Rigidbody>();
			if (PlayerGrabbingManager.handItem_rb == null)
			{
				return;
			}
			Vector3 centerOfMass = PlayerGrabbingManager.handItem_rb.centerOfMass;
			PlayerGrabbingManager.toggleColliders = 0;
			PlayerGrabbingManager.handItem_colls = PlayerGrabbingManager.handItem.Value.GetComponents<Collider>();
			MPItem mpitem = PlayerGrabbingManager.handItem_rb.GetComponent<MPItem>();
			if (mpitem == null)
			{
				mpitem = PlayerGrabbingManager.handItem_rb.gameObject.AddComponent<MPItem>();
			}
			mpitem.doUpdate = true;
			if (PlayerGrabbingManager.handItem_rb.gameObject.tag == "PART")
			{
				PlayerGrabbingManager.handItem_rb.isKinematic = true;
			}
			PlayerGrabbingManager.handItem_rb.centerOfMass = centerOfMass;
			int rigidbodyHash = NetRigidbodyManager.GetRigidbodyHash(PlayerGrabbingManager.handItem_rb);
			if (rigidbodyHash == 0)
			{
				Console.LogError("Rigidbody " + PlayerGrabbingManager.handItem_rb.name + " is not registered!", false);
			}
			else
			{
				Console.Log(string.Format("Grabbed hash {0}", rigidbodyHash), false);
			}
			NetRigidbodyManager.RequestOwnership(PlayerGrabbingManager.handItem_rb);
			this.SendGrabItemEvent(true, rigidbodyHash, Vector3.zero);
		}

		private void OnItemDropped()
		{
			if (PlayerGrabbingManager.handItem_rb == null || PlayerGrabbingManager.handItem_colls == null)
			{
				return;
			}
			this.SendGrabItemEvent(false, NetRigidbodyManager.GetRigidbodyHash(PlayerGrabbingManager.handItem_rb), this.throwForce.Value);
			for (int i = 0; i < PlayerGrabbingManager.handItem_colls.Length; i++)
			{
				if ((PlayerGrabbingManager.toggleColliders >> i) % 2 == 1)
				{
					PlayerGrabbingManager.handItem_colls[i].isTrigger = false;
				}
			}
			MPItem component = PlayerGrabbingManager.handItem_rb.GetComponent<MPItem>();
			component.doUpdate = true;
			component.UpdateOwner();
			if (PlayerGrabbingManager.handItem_rb.gameObject.tag == "PART")
			{
				PlayerGrabbingManager.handItem_rb.isKinematic = false;
			}
			PlayerGrabbingManager.handItem_rb = null;
			PlayerGrabbingManager.handItem_colls = null;
			PlayerGrabbingManager.toggleColliders = 0;
		}

		private void SendGrabItemEvent(bool grab, int hash, Vector3 throwForce)
		{
			using (GameEventWriter gameEventWriter = LocalPlayer.grabItemEvent.Writer())
			{
				gameEventWriter.Write(grab);
				gameEventWriter.Write(hash);
				if (throwForce.sqrMagnitude > 0f)
				{
					gameEventWriter.Write(throwForce);
				}
				LocalPlayer.grabItemEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
				if (GameEventRouter.IsRecordingPackets)
				{
					using (GameEventWriter gameEventWriter2 = LocalPlayer.grabItemRecEvent.Writer())
					{
						gameEventWriter2.Write(grab);
						gameEventWriter2.Write(hash);
						LocalPlayer.grabItemRecEvent.Send(gameEventWriter2, 0UL, true, default(GameEvent.RecordingProperties));
					}
				}
			}
		}

		internal static void ForceDropItem()
		{
			PlayerGrabbingManager.handFSM.SendEvent("DROP_PART");
		}

		private static FsmGameObject itemPivot = FsmVariables.GlobalVariables.FindFsmGameObject("ItemPivot");

		internal static PlayMakerFSM handFSM;

		private static FsmGameObject handItem;

		private static Rigidbody handItem_rb;

		private static Collider[] handItem_colls;

		private static int toggleColliders = 0;

		private FsmVector3 throwForce;
	}
}
