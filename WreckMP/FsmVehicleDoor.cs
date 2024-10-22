using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace WreckMP
{
	internal class FsmVehicleDoor
	{
		public FsmVehicleDoor(PlayMakerFSM fsm, bool isHayosikoSidedoor = false)
		{
			this.fsm = fsm;
			this.hash = fsm.transform.GetGameobjectHashString().GetHashCode();
			this.owner = WreckMPGlobals.HostID;
			fsm.Initialize();
			if (isHayosikoSidedoor)
			{
				this.SetupFSMSidedoor();
			}
			else
			{
				this.SetupFSM();
			}
			if (FsmVehicleDoor.doorRotationEvent == null)
			{
				FsmVehicleDoor.doorRotationEvent = new GameEvent<FsmVehicleDoor>("Rot", new Action<ulong, GameEventReader>(FsmVehicleDoor.OnDoorRotation), GameScene.GAME);
			}
			if (FsmVehicleDoor.doorToggleEvent == null)
			{
				FsmVehicleDoor.doorToggleEvent = new GameEvent<FsmVehicleDoor>("Toggle", new Action<ulong, GameEventReader>(FsmVehicleDoor.OnDoorToggle), GameScene.GAME);
			}
			if (FsmVehicleDoor.requestOwnershipEvent == null)
			{
				FsmVehicleDoor.requestOwnershipEvent = new GameEvent<FsmVehicleDoor>("SetOwner", new Action<ulong, GameEventReader>(FsmVehicleDoor.OnSetOwner), GameScene.GAME);
			}
			if (FsmVehicleDoor.initSync == null)
			{
				FsmVehicleDoor.initSync = new GameEvent<FsmVehicleDoor>("Init", delegate(ulong s, GameEventReader p)
				{
					while (p.UnreadLength() > 0)
					{
						int hash = p.ReadInt32();
						bool flag = p.ReadBoolean();
						FsmVehicleDoor fsmVehicleDoor = FsmVehicleDoor.doors.FirstOrDefault((FsmVehicleDoor d) => d.hash == hash);
						if (fsmVehicleDoor == null)
						{
							Console.LogError(string.Format("Failed to init sync fsm car door: the hash {0} was not found", hash), false);
							return;
						}
						fsmVehicleDoor.owner = s;
						if (flag)
						{
							fsmVehicleDoor.updatingFsm = true;
							fsmVehicleDoor.fsm.SendEvent("MP_OPEN");
						}
					}
				}, GameScene.GAME);
				WreckMPGlobals.OnMemberReady.Add(delegate(ulong user)
				{
					using (GameEventWriter gameEventWriter = FsmVehicleDoor.initSync.Writer())
					{
						for (int i = 0; i < FsmVehicleDoor.doors.Count; i++)
						{
							if (FsmVehicleDoor.doors[i] == null)
							{
								FsmVehicleDoor.doors.RemoveAt(i);
								i--;
							}
							else if (FsmVehicleDoor.doors[i].owner == WreckMPGlobals.UserID && FsmVehicleDoor.doors[i].doorOpen != null)
							{
								gameEventWriter.Write(FsmVehicleDoor.doors[i].hash);
								gameEventWriter.Write(FsmVehicleDoor.doors[i].doorOpen.Value);
							}
						}
						FsmVehicleDoor.initSync.Send(gameEventWriter, user, true, default(GameEvent.RecordingProperties));
					}
				});
			}
			FsmVehicleDoor.doors.Add(this);
			CoreManager.sceneLoaded = (Action<GameScene>)Delegate.Combine(CoreManager.sceneLoaded, new Action<GameScene>(delegate(GameScene a)
			{
				if (FsmVehicleDoor.doors.Contains(this))
				{
					FsmVehicleDoor.doors.Remove(this);
				}
			}));
		}

		private static void OnDoorRotation(ulong sender, GameEventReader packet)
		{
			int hash = packet.ReadInt32();
			FsmVehicleDoor fsmVehicleDoor = FsmVehicleDoor.doors.FirstOrDefault((FsmVehicleDoor d) => d.hash == hash);
			if (fsmVehicleDoor == null)
			{
				Console.LogError(string.Format("Failed to rotate fsm car door: the hash {0} was not found", hash), false);
				return;
			}
			if (!fsmVehicleDoor.doorOpen.Value)
			{
				return;
			}
			float num = packet.ReadSingle();
			Vector3 localEulerAngles = fsmVehicleDoor.door.Value.transform.localEulerAngles;
			localEulerAngles[fsmVehicleDoor.axis] = num;
			fsmVehicleDoor.door.Value.transform.localEulerAngles = localEulerAngles;
		}

		private static void OnDoorToggle(ulong sender, GameEventReader packet)
		{
			int hash = packet.ReadInt32();
			FsmVehicleDoor fsmVehicleDoor = FsmVehicleDoor.doors.FirstOrDefault((FsmVehicleDoor d) => d.hash == hash);
			if (fsmVehicleDoor == null)
			{
				Console.LogError(string.Format("Failed to toggle fsm car door: the hash {0} was not found", hash), false);
				return;
			}
			fsmVehicleDoor.updatingFsm = true;
			bool flag = packet.ReadBoolean();
			fsmVehicleDoor.fsm.SendEvent(flag ? "MP_OPEN" : "MP_CLOSE");
		}

		private static void OnSetOwner(ulong sender, GameEventReader packet)
		{
			int hash = packet.ReadInt32();
			FsmVehicleDoor fsmVehicleDoor = FsmVehicleDoor.doors.FirstOrDefault((FsmVehicleDoor d) => d.hash == hash);
			if (fsmVehicleDoor == null)
			{
				Console.LogError(string.Format("Failed to set fsm car door ownership: the hash {0} was not found", hash), false);
				return;
			}
			fsmVehicleDoor.owner = sender;
		}

		public void FixedUpdate()
		{
			if (this.doorOpen != null && this.owner == WreckMPGlobals.UserID && this.doorOpen.Value)
			{
				using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter(""))
				{
					gameEventWriter.Write(this.hash);
					gameEventWriter.Write(this.door.Value.transform.localEulerAngles[this.axis]);
					FsmVehicleDoor.doorRotationEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
				}
			}
		}

		private void SetupFSMSidedoor()
		{
			string text = "Open door";
			string text2 = "Close door";
			FsmEvent fsmEvent = this.fsm.AddEvent("MP_OPEN");
			this.fsm.AddGlobalTransition(fsmEvent, text);
			FsmEvent fsmEvent2 = this.fsm.AddEvent("MP_CLOSE");
			this.fsm.AddGlobalTransition(fsmEvent2, text2);
			this.fsm.InsertAction(text, new Action(this.OpenDoor), 0, false);
			this.fsm.InsertAction(text2, new Action(this.CloseDoor), 0, false);
		}

		private void SetupFSM()
		{
			try
			{
				bool flag = false;
				this.doorOpen = this.fsm.FsmVariables.FindFsmBool("Open");
				this.door = this.fsm.FsmVariables.FindFsmGameObject("Door");
				if (this.door == null)
				{
					flag = true;
					this.door = this.fsm.FsmVariables.FindFsmGameObject("Bootlid");
					if (this.door == null)
					{
						this.door = this.fsm.gameObject;
						flag = false;
					}
				}
				string text = (flag ? "Open hood" : "Open door");
				string text2 = (flag ? (this.fsm.HasState("Drop") ? "Drop" : "State 2") : "Sound");
				GetRotation getRotation = this.fsm.GetState(text).Actions.First((FsmStateAction a) => a is GetRotation) as GetRotation;
				if (getRotation.xAngle != null && !getRotation.xAngle.IsNone)
				{
					this.axis = 0;
				}
				else if (getRotation.yAngle != null && !getRotation.yAngle.IsNone)
				{
					this.axis = 1;
				}
				else if (getRotation.zAngle != null && !getRotation.zAngle.IsNone)
				{
					this.axis = 2;
				}
				FsmEvent fsmEvent = this.fsm.AddEvent("MP_OPEN");
				this.fsm.AddGlobalTransition(fsmEvent, text);
				FsmEvent fsmEvent2 = this.fsm.AddEvent("MP_CLOSE");
				this.fsm.AddGlobalTransition(fsmEvent2, text2);
				this.fsm.InsertAction(text, new Action(this.OpenDoor), 0, false);
				if (!flag)
				{
					this.fsm.InsertAction(this.fsm.HasState("Open door 2") ? "Open door 2" : "Open door 3", new Action(this.RequestOwnership), 0, false);
				}
				this.fsm.InsertAction(text2, new Action(this.CloseDoor), 0, false);
			}
			catch (Exception ex)
			{
				Console.LogError(string.Format("Failed to setup door {0} ({1}): {2}, {3}, {4}", new object[]
				{
					this.hash,
					this.fsm.transform.GetGameobjectHashString(),
					ex.GetType(),
					ex.Message,
					ex.StackTrace
				}), false);
			}
		}

		private void RequestOwnership()
		{
			if (WreckMPGlobals.UserID == this.owner)
			{
				return;
			}
			this.owner = WreckMPGlobals.UserID;
			using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter(""))
			{
				gameEventWriter.Write(this.hash);
				FsmVehicleDoor.requestOwnershipEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void CloseDoor()
		{
			if (this.updatingFsm)
			{
				this.updatingFsm = false;
				return;
			}
			this.RequestOwnership();
			this.SendDoorToggleEvent(false);
		}

		private void OpenDoor()
		{
			if (this.updatingFsm)
			{
				this.updatingFsm = false;
				return;
			}
			this.RequestOwnership();
			this.SendDoorToggleEvent(true);
		}

		private void SendDoorToggleEvent(bool open)
		{
			using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter(""))
			{
				gameEventWriter.Write(this.hash);
				gameEventWriter.Write(open);
				FsmVehicleDoor.doorToggleEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private PlayMakerFSM fsm;

		private FsmBool doorOpen;

		private FsmGameObject door;

		private int hash;

		private int axis = -1;

		private ulong owner;

		private bool updatingFsm;

		private const string openDoorFsmEvent = "MP_OPEN";

		private const string closeDoorFsmEvent = "MP_CLOSE";

		private static GameEvent<FsmVehicleDoor> doorRotationEvent;

		private static GameEvent<FsmVehicleDoor> doorToggleEvent;

		private static GameEvent<FsmVehicleDoor> requestOwnershipEvent;

		private static GameEvent<FsmVehicleDoor> initSync;

		private static List<FsmVehicleDoor> doors = new List<FsmVehicleDoor>();
	}
}
