using System;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	internal class SleepingBag : MonoBehaviour
	{
		public bool LayingDown
		{
			get
			{
				return this.sleepTrigger.owner > 0UL;
			}
		}

		internal static void CreateSleepTrigger(bool makeCollider, out byte id, GameObject gameObject, GameObject sleepTriggerObj, out SleepTrigger _sleepTrigger, out GameEvent _laydownEvent, Vector3 pivotPos, Vector3 pivotRot)
		{
			byte b = SleepingBag.freeID;
			SleepingBag.freeID = b + 1;
			id = b;
			SleepTrigger sleepTrigger = sleepTriggerObj.AddComponent<SleepTrigger>();
			_sleepTrigger = sleepTrigger;
			GameEvent laydownEvent = new GameEvent("SleepingBagLaydown" + id.ToString(), delegate(GameEventReader p)
			{
				bool flag = p.ReadBoolean();
				sleepTrigger.owner = (flag ? p.sender : 0UL);
				if (gameObject != null)
				{
					gameObject.layer = (flag ? 16 : 19);
				}
				sleepTrigger.canLaydown = !flag;
				NetSleepingManager.occupiedBags += (flag ? 1 : (-1));
			}, GameScene.GAME);
			_laydownEvent = laydownEvent;
			sleepTrigger.laydown = delegate(bool down)
			{
				using (GameEventWriter gameEventWriter = laydownEvent.Writer())
				{
					gameEventWriter.Write(down);
					laydownEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
					NetSleepingManager.occupiedBags += (down ? 1 : (-1));
				}
			};
			if (makeCollider)
			{
				SphereCollider sphereCollider = sleepTrigger.gameObject.AddComponent<SphereCollider>();
				sphereCollider.isTrigger = true;
				sphereCollider.radius = 0.4f;
			}
			Transform transform = new GameObject("Pivot").transform;
			transform.parent = sleepTrigger.transform;
			transform.localPosition = pivotPos;
			transform.localEulerAngles = pivotRot;
			WreckMPGlobals.OnMemberReady.Add(delegate(ulong u)
			{
				if (sleepTrigger.owner == WreckMPGlobals.UserID)
				{
					using (GameEventWriter gameEventWriter2 = laydownEvent.Writer())
					{
						gameEventWriter2.Write(true);
						laydownEvent.Send(gameEventWriter2, u, true, default(GameEvent.RecordingProperties));
					}
				}
			});
			WreckMPGlobals.OnMemberExit = (Action<ulong>)Delegate.Combine(WreckMPGlobals.OnMemberExit, new Action<ulong>(delegate(ulong u)
			{
				if (sleepTrigger.owner == u)
				{
					sleepTrigger.owner = 0UL;
					if (gameObject != null)
					{
						gameObject.layer = 19;
					}
					sleepTrigger.canLaydown = true;
					NetSleepingManager.occupiedBags--;
				}
			}));
		}

		private void Start()
		{
			this.cols = base.GetComponents<Collider>();
			this.blanket = base.transform.GetChild(1).gameObject;
			SleepingBag.CreateSleepTrigger(true, out this.id, base.gameObject, new GameObject("SleepTrigger"), out this.sleepTrigger, out this.laydownEvent, new Vector3(0f, 0.09f, 0.61f), new Vector3(0f, 270f, 90f));
			this.sleepTrigger.transform.parent = this.blanket.transform;
			this.sleepTrigger.transform.localPosition = new Vector3(0f, 0f, 0f);
			NetRigidbodyManager.AddRigidbody(base.GetComponent<Rigidbody>(), ("sleeping bag " + this.id.ToString()).GetHashCode());
			this.unpackEvent = new GameEvent("SleepingBagUnpackEvent" + this.id.ToString(), delegate(GameEventReader p)
			{
				this.bagOpen = p.ReadBoolean();
				this.Unpack(this.bagOpen);
			}, GameScene.GAME);
			WreckMPGlobals.OnMemberReady.Add(delegate(ulong u)
			{
				if (WreckMPGlobals.IsHost && this.bagOpen)
				{
					using (GameEventWriter gameEventWriter = this.unpackEvent.Writer())
					{
						gameEventWriter.Write(true);
						this.unpackEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
					}
				}
			});
		}

		public void Sleep()
		{
			this.sleepTrigger.TriggerSleep();
		}

		private void Update()
		{
			bool flag = Raycaster.Raycast(this.cols[this.bagOpen ? 1 : 0], 1f, 524289);
			if (flag != this._guiuse)
			{
				if (!flag)
				{
					this.guiuse.Value = false;
				}
				this._guiuse = flag;
			}
			if (flag)
			{
				this.guiuse.Value = true;
				if (cInput.GetKeyDown("Use"))
				{
					this.bagOpen = !this.bagOpen;
					this.Unpack(this.bagOpen);
					using (GameEventWriter gameEventWriter = this.unpackEvent.Writer())
					{
						gameEventWriter.Write(this.bagOpen);
						this.unpackEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
					}
				}
			}
		}

		private void Unpack(bool bagOpen)
		{
			this.blanket.SetActive(bagOpen);
			this.cols[0].enabled = !bagOpen;
			this.cols[1].enabled = bagOpen;
			if (bagOpen)
			{
				base.transform.position += Vector3.up * 0.1f;
				base.transform.eulerAngles = Vector3.up * base.transform.eulerAngles.y;
			}
		}

		private static byte freeID;

		private FsmBool guiuse = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");

		private bool _guiuse;

		private Collider[] cols;

		private bool bagOpen;

		private GameObject blanket;

		private SleepTrigger sleepTrigger;

		private byte id;

		private GameEvent unpackEvent;

		private GameEvent laydownEvent;
	}
}
