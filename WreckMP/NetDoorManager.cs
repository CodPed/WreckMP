using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	internal class NetDoorManager : NetManager
	{
		private void Start()
		{
			NetDoorManager.Instance = this;
			List<GameObject> list = (from x in Resources.FindObjectsOfTypeAll<GameObject>()
				where x.name.StartsWith("Door") || (x.name == "coll" && x.transform.parent != null && x.transform.parent.name == "door" && x.transform.root.name == "COMBINE(350-400psi)")
				select x).ToList<GameObject>();
			list.Add(GameObject.Find("YARD").transform.Find("Building/KITCHEN/Fridge/Pivot/Handle").gameObject);
			for (int i = 0; i < list.Count; i++)
			{
				GameObject gameObject = list[i];
				int hashCode = gameObject.transform.position.GetHashCode();
				int _i = i;
				Transform transform = gameObject.transform.Find("Pivot/Handle");
				if (!transform)
				{
					transform = gameObject.transform.Find("Handle");
				}
				if (!transform)
				{
					transform = gameObject.transform;
				}
				if (!(transform.name != "Handle") || (!(transform.name != "coll") && !(transform.root.name != "COMBINE(350-400psi)")))
				{
					Rigidbody[] componentsInChildren = gameObject.GetComponentsInChildren<Rigidbody>();
					if (componentsInChildren.Length != 0)
					{
						for (int j = 0; j < componentsInChildren.Length; j++)
						{
							Object.Destroy(componentsInChildren[j]);
						}
					}
					PlayMakerFSM fsm = transform.gameObject.GetPlayMaker("Use");
					if (!(fsm == null))
					{
						fsm.Initialize();
						if (fsm.FsmEvents.Any((FsmEvent x) => x.Name == "OPENDOOR"))
						{
							FsmBool doorOpen = fsm.FsmVariables.GetFsmBool("DoorOpen");
							FsmEvent fsmEvent = fsm.AddEvent("MP_TOGGLEDOOR");
							fsm.AddGlobalTransition(fsmEvent, "Check position");
							GameEvent gameEvent = new GameEvent(string.Format("DoorToggle{0}", hashCode), delegate(GameEventReader p)
							{
								this.doSync &= ~(1 << _i);
								doorOpen.Value = p.ReadBoolean();
								fsm.Fsm.Event(fsmEvent);
							}, GameScene.GAME);
							Action<ulong> syncDoor = delegate(ulong target)
							{
								using (GameEventWriter gameEventWriter = gameEvent.Writer())
								{
									gameEventWriter.Write(doorOpen.Value != target > 0UL);
									gameEvent.Send(gameEventWriter, target, true, default(GameEvent.RecordingProperties));
								}
							};
							fsm.InsertAction("Check position", delegate
							{
								if ((this.doSync >> _i) % 2 == 1)
								{
									syncDoor(0UL);
								}
								this.doSync |= 1 << _i;
							}, 0, false);
							WreckMPGlobals.OnMemberReady.Add(syncDoor);
							this.doSync |= 1 << _i;
						}
					}
				}
			}
		}

		public static NetDoorManager Instance;

		private static GameEvent toggleDoorEvent;

		private int doSync;
	}
}
