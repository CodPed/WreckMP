using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace WreckMP
{
	internal class NetPartManager : NetManager
	{
		private void Start()
		{
			new GameEvent<NetPartManager>("PartAssemble", new Action<ulong, GameEventReader>(this.OnAssemble), GameScene.GAME);
			NetPartManager.removeEvent = new GameEvent("PartRemove", new Action<GameEventReader>(this.OnRemove), GameScene.GAME);
			new GameEvent<NetPartManager>("InitAssemble", new Action<ulong, GameEventReader>(this.OnInitSyncAssemble), GameScene.GAME);
			new GameEvent<NetPartManager>("Screw", new Action<ulong, GameEventReader>(this.OnTightnessChange), GameScene.GAME);
			new GameEvent<NetPartManager>("InitScrew", new Action<ulong, GameEventReader>(this.OnInitBolts), GameScene.GAME);
			WreckMPGlobals.OnMemberReady.Add(delegate(ulong user)
			{
				this.InitSyncAssemble(user);
				this.InitSyncBolts(user);
			});
		}

		internal static void AddAssembleFsm(int hash, PlayMakerFSM fsm)
		{
			if (NetPartManager.assemblesHashes.Contains(hash))
			{
				Console.LogError(string.Format("<b>FATAL ERROR!</b> FSM Hash {0} of FSM '{1}' on path '{2}' already exists!", hash, fsm.FsmName, fsm.transform.GetPath()), false);
				return;
			}
			NetPartManager.assembles.Add(fsm);
			NetPartManager.assemblesHashes.Add(hash);
		}

		internal static bool AddBolt(PlayMakerFSM fsm, int hash)
		{
			NetPartManager.Bolt bolt = new NetPartManager.Bolt(fsm, hash, new Action<int, byte>(NetPartManager.TightnessChangeEvent));
			if (bolt.stage == null && bolt.alignment == null && bolt.rot == null)
			{
				return false;
			}
			NetPartManager.bolts.Add(bolt);
			return true;
		}

		private void OnInitSyncAssemble(ulong sender, GameEventReader packet)
		{
			while (packet.UnreadLength() > 0)
			{
				int num = packet.ReadInt32();
				int num2 = packet.ReadInt32();
				int num3 = NetRigidbodyManager.rigidbodyHashes.IndexOf(num);
				if (num3 == -1)
				{
					Console.LogError(string.Format("NetRigidbodyManager.OnInitSyncAssemble(ulong sender, GameEventReader packet): the item hash {0} does not exist", num), false);
				}
				else
				{
					OwnedRigidbody ownedRigidbody = NetRigidbodyManager.ownedRigidbodies[num3];
					PlayMakerFSM playMakerFSM = null;
					if (num2 != 0)
					{
						int num4 = NetPartManager.assemblesHashes.IndexOf(num2);
						if (num4 == -1)
						{
							Console.LogError(string.Format("NetRigidbodyManager.OnInitSyncAssemble(ulong sender, GameEventReader packet): the assemble hash {0} does not exist", num2), false);
							continue;
						}
						playMakerFSM = NetPartManager.assembles[num4];
					}
					Console.Log(string.Concat(new string[]
					{
						"InitAssemble: ",
						(playMakerFSM == null) ? "null" : playMakerFSM.transform.name,
						", ",
						(ownedRigidbody.assemble == null) ? "null" : ownedRigidbody.assemble.transform.name,
						", ",
						ownedRigidbody.transform.name
					}), false);
					if (playMakerFSM != ownedRigidbody.assemble)
					{
						if (playMakerFSM == null)
						{
							this.Remove(num);
						}
						else
						{
							this.Assemble(num2, num);
						}
					}
				}
			}
		}

		private void InitSyncAssemble(ulong user)
		{
			if (!WreckMPGlobals.IsHost)
			{
				return;
			}
			using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter("InitAssemble"))
			{
				for (int i = 0; i < NetRigidbodyManager.ownedRigidbodies.Count; i++)
				{
					Console.Log("Init assemble send: " + NetRigidbodyManager.ownedRigidbodies[i].transform.name, false);
					gameEventWriter.Write(NetRigidbodyManager.rigidbodyHashes[i]);
					int num = 0;
					if (NetRigidbodyManager.ownedRigidbodies[i].assemble != null)
					{
						num = NetPartManager.assemblesHashes[NetPartManager.assembles.IndexOf(NetRigidbodyManager.ownedRigidbodies[i].assemble)];
					}
					gameEventWriter.Write(num);
				}
				GameEvent<NetPartManager>.Send("InitAssemble", gameEventWriter, user, true);
			}
		}

		private void OnDestroy()
		{
			NetPartManager.assemblesHashes.Clear();
			NetPartManager.assembles.Clear();
			NetPartManager.bolts.Clear();
		}

		private void OnAssemble(ulong user, GameEventReader p)
		{
			int num = p.ReadInt32();
			int num2 = p.ReadInt32();
			this.Assemble(num, num2);
		}

		private void OnRemove(GameEventReader p)
		{
			int num = p.ReadInt32();
			this.Remove(num);
		}

		private static void SendAssembleEvent(int fsmHash, FsmGameObject part)
		{
			using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter("PartAssemble"))
			{
				gameEventWriter.Write(fsmHash);
				if (part != null)
				{
					if (part.Value == null)
					{
						Console.LogError("NetRigidbodyManager.SendAssembeEvent: Attached gameobject is null!", false);
						return;
					}
					int num = NetRigidbodyManager.ownedRigidbodies.FindIndex((OwnedRigidbody r) => r.Rigidbody == part.Value.GetComponent<Rigidbody>());
					if (num == -1)
					{
						Console.LogError("NetRigidbodyManager.SendAssembeEvent: Attached gameobject '" + part.Value.transform.GetPath() + "' does not have a hash!", false);
						return;
					}
					NetRigidbodyManager.ownedRigidbodies[num].assemble = NetPartManager.assembles[NetPartManager.assemblesHashes.IndexOf(fsmHash)];
					gameEventWriter.Write(NetRigidbodyManager.rigidbodyHashes[num]);
				}
				else
				{
					gameEventWriter.Write(0);
				}
				GameEvent<NetPartManager>.Send("PartAssemble", gameEventWriter, 0UL, true);
			}
		}

		private static void SendRemoveEvent(int fsmHash)
		{
			using (GameEventWriter gameEventWriter = NetPartManager.removeEvent.Writer())
			{
				gameEventWriter.Write(fsmHash);
				NetPartManager.removeEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		internal static void SetupRemovalPlaymaker(PlayMakerFSM fsm, int hash)
		{
			try
			{
				fsm.Initialize();
				NetPartManager.removesHashes.Add(hash);
				NetPartManager.removes.Add(fsm);
				FsmTransition fsmTransition = fsm.FsmStates[0].Transitions.FirstOrDefault((FsmTransition t) => t.EventName.Contains("REMOV"));
				string text;
				if (fsmTransition == null)
				{
					text = "Remove part";
				}
				else
				{
					text = fsmTransition.ToState;
				}
				FsmEvent fsmEvent = fsm.FsmEvents.FirstOrDefault((FsmEvent e) => e.Name == "MP_REMOVE");
				if (fsmEvent == null)
				{
					FsmEvent[] array = new FsmEvent[fsm.FsmEvents.Length + 1];
					fsm.FsmEvents.CopyTo(array, 0);
					fsmEvent = new FsmEvent("MP_REMOVE");
					FsmEvent.AddFsmEvent(fsmEvent);
					array[array.Length - 1] = fsmEvent;
					fsm.Fsm.Events = array;
				}
				FsmTransition[] array2 = new FsmTransition[fsm.FsmGlobalTransitions.Length + 1];
				fsm.FsmGlobalTransitions.CopyTo(array2, 0);
				array2[array2.Length - 1] = new FsmTransition
				{
					FsmEvent = fsmEvent,
					ToState = text
				};
				fsm.Fsm.GlobalTransitions = array2;
				fsm.InsertAction(text, delegate
				{
					NetPartManager.RemoveEvent(hash);
				}, fsm.GetState(text).Actions.Length - 1, false);
				fsm.Initialize();
				if (!WreckMPGlobals.IsHost)
				{
					NetPartManager.SetupBoltCheck(fsm.gameObject.GetPlayMaker("BoltCheck"));
					Joint component = fsm.gameObject.GetComponent<Joint>();
					if (component != null)
					{
						component.breakForce = (component.breakForce = float.MaxValue);
					}
				}
			}
			catch (Exception ex)
			{
				Console.LogError(string.Format("NetAttachmentManager.SetupPlaymaker(PlaymakerFSM): fsm {0} with hash {1} ({2}) failed with exception {3}", new object[]
				{
					fsm.FsmName,
					hash,
					fsm.transform.name,
					ex
				}), false);
			}
		}

		internal static void SetupBoltCheck(PlayMakerFSM fsm)
		{
			if (fsm == null)
			{
				return;
			}
			fsm.Initialize();
			for (int i = 0; i < fsm.FsmStates.Length; i++)
			{
				if (fsm.FsmStates[i].Name.ToLower().Contains("bolts on"))
				{
					for (int j = 0; j < fsm.FsmStates[i].Actions.Length; j++)
					{
						SetProperty setProperty = fsm.FsmStates[i].Actions[j] as SetProperty;
						if (setProperty != null)
						{
							setProperty.targetProperty.FloatParameter = float.MaxValue;
						}
					}
					return;
				}
			}
		}

		internal static void SetupAssemblePlaymaker(PlayMakerFSM fsm, int hash)
		{
			try
			{
				fsm.Initialize();
				string fsmName = fsm.FsmName;
				FsmTransition[] array2;
				if (!(fsmName == "Assembly"))
				{
					if (!(fsmName == "Assemble"))
					{
						goto IL_446;
					}
				}
				else
				{
					if (fsm.transform.name == "Insert")
					{
						FsmEvent[] array = new FsmEvent[fsm.FsmEvents.Length + 1];
						fsm.FsmEvents.CopyTo(array, 0);
						array[array.Length - 1] = new FsmEvent("MP_ASSEMBLE");
						FsmEvent.AddFsmEvent(array[array.Length - 1]);
						fsm.Fsm.Events = array;
						array2 = new FsmTransition[fsm.FsmGlobalTransitions.Length + 1];
						fsm.FsmGlobalTransitions.CopyTo(array2, 0);
						FsmTransition[] array3 = array2;
						int num = array2.Length - 1;
						FsmTransition fsmTransition = new FsmTransition();
						fsmTransition.FsmEvent = fsm.FsmEvents.First((FsmEvent e) => e.Name == "MP_ASSEMBLE");
						fsmTransition.ToState = "Add battery";
						array3[num] = fsmTransition;
						fsm.Fsm.GlobalTransitions = array2;
						fsm.InsertAction("Add battery", delegate
						{
							NetPartManager.BatteryToRadioOrFlashlightEvent(hash, fsm.FsmVariables.FindFsmGameObject("Part"));
						}, -1, false);
						goto IL_446;
					}
					if (fsm.transform.name == "TriggerCharger")
					{
						fsm.InsertAction("Init", delegate
						{
							NetPartManager.BatteryOnChargerEvent(hash, fsm.FsmVariables.FindFsmGameObject("Battery"));
						}, -1, false);
						goto IL_446;
					}
				}
				FsmTransition fsmTransition2 = fsm.FsmStates[0].Transitions.FirstOrDefault((FsmTransition t) => t.EventName.Contains("ASSEMBL"));
				bool flag = fsm.FsmGlobalTransitions.Any((FsmTransition t) => t.EventName == "RESETWIRING");
				string text;
				if (flag)
				{
					text = "Sound";
					NetPartManager.FixWiringFsm(fsm);
				}
				else if (fsmTransition2 == null)
				{
					FsmState fsmState = fsm.FsmStates.FirstOrDefault((FsmState s) => s.Name.ToLower().Contains("assemble"));
					if (fsmState == null)
					{
						Console.LogError(string.Format("NetAttachmentManager.SetupPlaymaker(PlaymakerFSM): fsm {0} with hash {1} ({2}) failed because there was no state 'assemble' nor event 'ASSEMBLE'", fsm.FsmName, hash, fsm.transform.name), false);
						return;
					}
					text = fsmState.Name;
				}
				else
				{
					text = fsmTransition2.ToState;
				}
				FsmEvent fsmEvent = fsm.FsmEvents.FirstOrDefault((FsmEvent e) => e.Name == "MP_ASSEMBLE");
				if (fsmEvent == null)
				{
					FsmEvent[] array4 = new FsmEvent[fsm.FsmEvents.Length + 1];
					fsm.FsmEvents.CopyTo(array4, 0);
					fsmEvent = new FsmEvent("MP_ASSEMBLE");
					FsmEvent.AddFsmEvent(fsmEvent);
					array4[array4.Length - 1] = fsmEvent;
					fsm.Fsm.Events = array4;
				}
				array2 = new FsmTransition[fsm.FsmGlobalTransitions.Length + 1];
				fsm.FsmGlobalTransitions.CopyTo(array2, 0);
				array2[array2.Length - 1] = new FsmTransition
				{
					FsmEvent = fsmEvent,
					ToState = text
				};
				fsm.Fsm.GlobalTransitions = array2;
				FsmState[] array5 = fsm.FsmStates.Where((FsmState s) => s.Name.ToLower().Contains("assemble")).ToArray<FsmState>();
				if (flag || array5.Length == 0)
				{
					fsm.InsertAction(text, delegate
					{
						NetPartManager.AssembleEvent(hash);
					}, -1, false);
				}
				else
				{
					Action <>9__9;
					for (int i = 0; i < array5.Length; i++)
					{
						PlayMakerFSM fsm2 = fsm;
						string name = array5[i].Name;
						Action action;
						if ((action = <>9__9) == null)
						{
							action = (<>9__9 = delegate
							{
								NetPartManager.AssembleEvent(hash);
							});
						}
						fsm2.InsertAction(name, action, -1, false);
					}
				}
				IL_446:
				FsmFloat fsmFloat = fsm.FsmVariables.FindFsmFloat("InitialAttachForce");
				if (fsmFloat != null && !WreckMPGlobals.IsHost)
				{
					fsmFloat.Value = float.MaxValue;
				}
				fsm.Initialize();
			}
			catch (Exception ex)
			{
				Console.LogError(string.Format("NetAttachmentManager.SetupPlaymaker(PlaymakerFSM): fsm {0} with hash {1} ({2}) failed with exception {3}", new object[]
				{
					fsm.FsmName,
					hash,
					fsm.transform.name,
					ex
				}), false);
			}
		}

		private static void FixWiringFsm(PlayMakerFSM fsm)
		{
			FsmFloat dist = fsm.FsmVariables.FindFsmFloat("Distance");
			FsmFloat tol = fsm.FsmVariables.FindFsmFloat("Tolerance");
			FsmState state = fsm.GetState("State 1");
			state.Actions[state.Actions.ToList<FsmStateAction>().FindIndex((FsmStateAction a) => a is FloatCompare)] = new PM_Hook(delegate
			{
				if (dist.Value < tol.Value && NetPartManager.wiringMess.transform.root.name == "PLAYER")
				{
					fsm.SendEvent("ASSEMBLE");
				}
			}, true);
		}

		private static void AssembleEvent(int fsmHash)
		{
			try
			{
				PlayMakerFSM playMakerFSM = NetPartManager.assembles[NetPartManager.assemblesHashes.IndexOf(fsmHash)];
				Console.Log(string.Format("Attach event triggered: {0}, {1}", fsmHash, playMakerFSM.transform.name), false);
				SatsumaProfiler.Instance.attached.Add(playMakerFSM.transform.name);
				FsmGameObject fsmGameObject = playMakerFSM.FsmVariables.FindFsmGameObject("Part");
				if (NetPartManager.updatingFsms.Contains(playMakerFSM))
				{
					NetPartManager.updatingFsms.Remove(playMakerFSM);
				}
				else
				{
					NetPartManager.SendAssembleEvent(fsmHash, fsmGameObject);
				}
			}
			catch (Exception ex)
			{
				Console.LogError(string.Format("Error in AssembleEvent: {0}, {1}, {2}", ex.GetType(), ex.Message, ex.StackTrace), false);
			}
		}

		private static void RemoveEvent(int fsmHash)
		{
			try
			{
				int num = NetRigidbodyManager.rigidbodyHashes.IndexOf(fsmHash);
				OwnedRigidbody ownedRigidbody;
				if (num == -1)
				{
					int num2 = NetPartManager.removesHashes.IndexOf(fsmHash);
					PlayMakerFSM playMakerFSM = NetPartManager.removes[num2];
					FsmObject fsmObject = playMakerFSM.FsmVariables.FindFsmObject("Rigidbody");
					ownedRigidbody = NetRigidbodyManager.AddRigidbody(fsmObject.Value as Rigidbody, fsmHash);
					ownedRigidbody.remove = playMakerFSM;
					ownedRigidbody.Removal_Rigidbody = fsmObject;
					num = NetRigidbodyManager.rigidbodyHashes.IndexOf(fsmHash);
				}
				else
				{
					ownedRigidbody = NetRigidbodyManager.ownedRigidbodies[num];
				}
				NetRigidbodyManager.RequestOwnership(ownedRigidbody);
				ownedRigidbody.assemble = null;
				PlayMakerFSM remove = ownedRigidbody.remove;
				SatsumaProfiler.Instance.detached.Add(remove.transform.name);
				if (NetPartManager.updatingFsms.Contains(remove))
				{
					NetPartManager.updatingFsms.Remove(remove);
				}
				else
				{
					NetPartManager.SendRemoveEvent(fsmHash);
				}
			}
			catch (Exception ex)
			{
				Console.LogError(string.Format("Error in RemoveEvent: {0}, {1}, {2}", ex.GetType(), ex.Message, ex.StackTrace), false);
			}
		}

		private static void BatteryOnChargerEvent(int fsmHash, FsmGameObject battery)
		{
			try
			{
				Console.Log(string.Format("Battery attached to charger: {0}", battery.Value.transform.GetPath().GetHashCode()), false);
				PlayMakerFSM playMakerFSM = NetPartManager.assembles[NetPartManager.assemblesHashes.IndexOf(fsmHash)];
				if (NetPartManager.updatingFsms.Contains(playMakerFSM))
				{
					NetPartManager.updatingFsms.Remove(playMakerFSM);
				}
				else
				{
					NetPartManager.SendAssembleEvent(fsmHash, battery);
				}
			}
			catch (Exception ex)
			{
				Console.LogError(string.Format("Error in AssembleEvent: {0}, {1}, {2}", ex.GetType(), ex.Message, ex.StackTrace), false);
			}
		}

		private static void BatteryToRadioOrFlashlightEvent(int fsmHash, FsmGameObject battery)
		{
			try
			{
				PlayMakerFSM playMakerFSM = NetPartManager.assembles[NetPartManager.assemblesHashes.IndexOf(fsmHash)];
				Console.Log(string.Format("Battery added to radio or flashlight: {0}, {1}, batt: {2}", fsmHash, playMakerFSM.transform.name, battery.Value.transform.GetPath().GetHashCode()), false);
				if (NetPartManager.updatingFsms.Contains(playMakerFSM))
				{
					NetPartManager.updatingFsms.Remove(playMakerFSM);
				}
				else
				{
					NetPartManager.SendAssembleEvent(fsmHash, battery);
				}
			}
			catch (Exception ex)
			{
				Console.LogError(string.Format("Error in AssembleEvent: {0}, {1}, {2}", ex.GetType(), ex.Message, ex.StackTrace), false);
			}
		}

		private void Assemble(int fsmHash, int partHash)
		{
			int num = NetPartManager.assemblesHashes.IndexOf(fsmHash);
			if (num == -1)
			{
				return;
			}
			PlayMakerFSM playMakerFSM = NetPartManager.assembles[num];
			NetPartManager.updatingFsms.Add(playMakerFSM);
			if (partHash == 0)
			{
				if (!playMakerFSM.FsmGlobalTransitions.Any((FsmTransition t) => t.EventName == "RESETWIRING"))
				{
					Console.LogError(string.Format("Received assemble event for fsm {0} ({1}) but the part hash is 0 and the fsm doesn't look like a wiring fsm", fsmHash, playMakerFSM.transform.name), false);
				}
				playMakerFSM.Fsm.Event(playMakerFSM.FsmEvents.First((FsmEvent e) => e.Name == "MP_ASSEMBLE"));
				return;
			}
			num = NetRigidbodyManager.rigidbodyHashes.IndexOf(partHash);
			if (num == -1)
			{
				return;
			}
			OwnedRigidbody ownedRigidbody = NetRigidbodyManager.ownedRigidbodies[num];
			ownedRigidbody.assemble = playMakerFSM;
			GameObject gameObject = ownedRigidbody.Rigidbody.gameObject;
			if (playMakerFSM.transform.name == "TriggerCharger")
			{
				playMakerFSM.FsmVariables.FindFsmGameObject("Battery").Value = gameObject;
				playMakerFSM.Fsm.Event(playMakerFSM.FsmEvents.First((FsmEvent e) => e.Name == "PLACEBATT"));
				return;
			}
			playMakerFSM.FsmVariables.FindFsmGameObject("Part").Value = gameObject;
			playMakerFSM.Fsm.Event(playMakerFSM.FsmEvents.First((FsmEvent e) => e.Name == "MP_ASSEMBLE"));
		}

		private void Remove(int fsmHash)
		{
			int num = NetRigidbodyManager.rigidbodyHashes.IndexOf(fsmHash);
			OwnedRigidbody ownedRigidbody;
			if (num == -1)
			{
				int num2 = NetPartManager.removesHashes.IndexOf(fsmHash);
				PlayMakerFSM playMakerFSM = NetPartManager.removes[num2];
				ownedRigidbody = NetRigidbodyManager.AddRigidbody(playMakerFSM.GetComponent<Rigidbody>(), fsmHash);
				ownedRigidbody.remove = playMakerFSM;
				ownedRigidbody.Removal_Rigidbody = playMakerFSM.FsmVariables.FindFsmObject("Rigidbody");
				num = NetRigidbodyManager.rigidbodyHashes.IndexOf(fsmHash);
			}
			else
			{
				ownedRigidbody = NetRigidbodyManager.ownedRigidbodies[num];
			}
			PlayMakerFSM remove = ownedRigidbody.remove;
			remove.Fsm.Event(remove.FsmEvents.First((FsmEvent e) => e.Name == "MP_REMOVE"));
			if (num == -1)
			{
				return;
			}
			NetRigidbodyManager.RequestOwnership(ownedRigidbody);
			ownedRigidbody.assemble = null;
			NetPartManager.updatingFsms.Add(remove);
		}

		private static void TightnessChangeEvent(int boltHash, byte stage)
		{
			using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter("Screw"))
			{
				gameEventWriter.Write(boltHash);
				gameEventWriter.Write(stage);
				GameEvent<NetPartManager>.Send("Screw", gameEventWriter, 0UL, true);
			}
		}

		private void OnTightnessChange(ulong sender, GameEventReader packet)
		{
			int boltHash = packet.ReadInt32();
			byte b2 = packet.ReadByte();
			NetPartManager.Bolt bolt = NetPartManager.bolts.FirstOrDefault((NetPartManager.Bolt b) => b.hash == boltHash);
			if (bolt == null)
			{
				Console.LogError(string.Format("The bolt with hash {0} could not be found", boltHash), false);
				return;
			}
			bolt.SetTightness(b2);
		}

		private void InitSyncBolts(ulong target)
		{
			if (!WreckMPGlobals.IsHost)
			{
				return;
			}
			using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter("InitScrew"))
			{
				for (int i = 0; i < NetPartManager.bolts.Count; i++)
				{
					gameEventWriter.Write(NetPartManager.bolts[i].hash);
					gameEventWriter.Write(NetPartManager.bolts[i].ScrewSyncStage);
				}
				GameEvent<NetPartManager>.Send("InitScrew", gameEventWriter, target, true);
			}
		}

		private void OnInitBolts(ulong sender, GameEventReader p)
		{
			while (p.UnreadLength() > 0)
			{
				int boltHash = p.ReadInt32();
				byte b2 = p.ReadByte();
				NetPartManager.Bolt bolt = NetPartManager.bolts.FirstOrDefault((NetPartManager.Bolt b) => b.hash == boltHash);
				if (bolt == null)
				{
					Console.LogError(string.Format("The bolt with hash {0} could not be found", boltHash), false);
				}
				else
				{
					bolt.SetTightness(b2);
				}
			}
		}

		internal static List<int> assemblesHashes = new List<int>();

		internal static List<int> removesHashes = new List<int>();

		internal static List<PlayMakerFSM> removes = new List<PlayMakerFSM>();

		internal static List<PlayMakerFSM> assembles = new List<PlayMakerFSM>();

		internal static List<PlayMakerFSM> updatingFsms = new List<PlayMakerFSM>();

		internal static List<NetPartManager.Bolt> bolts = new List<NetPartManager.Bolt>();

		internal static Transform wiringMess;

		private const string assembleEvent = "PartAssemble";

		private const string screwEvent = "Screw";

		private const string camshaftGearEvent = "CamGear";

		private static GameEvent removeEvent;

		internal class Bolt
		{
			public byte ScrewSyncStage
			{
				get
				{
					if (this.isTuneBolt)
					{
						return (byte)Mathf.RoundToInt(this.alignment.Value / this.maxAlignment * 255f);
					}
					if (this.isScrewableLid)
					{
						return (byte)Mathf.RoundToInt(this.rot.Value / 360f * 255f);
					}
					if (this.stage == null)
					{
						return (byte)this.floatstage.Value;
					}
					return (byte)this.stage.Value;
				}
			}

			public Bolt(PlayMakerFSM screw, int hash, Action<int, byte> onTightnessChange)
			{
				if (NetPartManager.Bolt.raycastBolt == null)
				{
					NetPartManager.Bolt.raycastBolt = GameObject.Find("PLAYER").transform.Find("Pivot/AnimPivot/Camera/FPSCamera/2Spanner/Raycast").GetPlayMaker("Raycast").FsmVariables.FindFsmGameObject("Bolt");
				}
				screw.Initialize();
				this.screw = screw;
				this.stage = screw.FsmVariables.IntVariables.FirstOrDefault((FsmInt i) => i.Name == "Stage");
				if (this.stage == null)
				{
					this.floatstage = screw.FsmVariables.FindFsmFloat("Tightness");
				}
				this.hash = hash;
				this.onTightnessChange = onTightnessChange;
				if (screw.HasState("Wait") && !screw.HasState("Wait 2") && !screw.HasState("Wait 3") && !screw.HasState("Wait 4"))
				{
					this.rotAmount = screw.FsmVariables.FindFsmFloat("ScrewAmount").Value;
					this.rot = screw.FsmVariables.FindFsmFloat("Rot");
					FsmEvent fsmEvent = screw.AddEvent("MP_UNSCREW");
					FsmEvent fsmEvent2 = screw.AddEvent("MP_SCREW");
					screw.AddGlobalTransition(fsmEvent, "Unscrew");
					screw.AddGlobalTransition(fsmEvent2, "Screw");
					screw.InsertAction("Wait", new Action(this.OnTightness), 0, false);
					this.isScrewableLid = true;
				}
				else if (screw.HasState("Wait") || screw.HasState("Wait 2") || screw.HasState("Wait 3") || screw.HasState("Wait 4"))
				{
					if (screw.HasState("Wait 3") || screw.HasState("Wait"))
					{
						screw.InsertAction(screw.HasState("Wait 3") ? "Wait 3" : "Wait", new Action(this.OnTightness), 0, false);
					}
					screw.InsertAction(screw.HasState("Wait 4") ? "Wait 4" : "Wait 2", new Action(this.OnTightness), 0, false);
				}
				else if (screw.HasState("Setup"))
				{
					this.alignment = screw.FsmVariables.FindFsmFloat("Alignment");
					this.maxAlignment = screw.FsmVariables.FindFsmFloat("Max").Value;
					screw.InsertAction("Setup", new Action(this.OnTightness), 0, false);
					FsmEvent fsmEvent3 = screw.AddEvent("MP_SETUP");
					screw.AddGlobalTransition(fsmEvent3, "Setup");
					this.isTuneBolt = true;
				}
				if (screw.gameObject.name.StartsWith("oil filter"))
				{
					FsmEvent fsmEvent4 = screw.FsmEvents.FirstOrDefault((FsmEvent e) => e.Name == "TIGHTEN");
					FsmEvent fsmEvent5 = screw.FsmEvents.FirstOrDefault((FsmEvent e) => e.Name == "UNTIGHTEN");
					if (fsmEvent4 == null || fsmEvent5 == null)
					{
						Console.LogError(string.Format("Init bolt with name oil filter but occured null: {0} {1}", fsmEvent4 == null, fsmEvent5 == null), false);
						return;
					}
					screw.AddGlobalTransition(fsmEvent4, "Screw");
					screw.AddGlobalTransition(fsmEvent5, "Unscrew");
				}
				if (screw.gameObject.name == "Pin")
				{
					this.isPin = true;
					FsmEvent fsmEvent6 = screw.FsmEvents.FirstOrDefault((FsmEvent e) => e.Name == "TIGHTEN");
					FsmEvent fsmEvent7 = screw.FsmEvents.FirstOrDefault((FsmEvent e) => e.Name == "UNTIGHTEN");
					if (fsmEvent6 == null || fsmEvent7 == null)
					{
						Console.LogError(string.Format("Init bolt with name Pin but occured null: {0} {1}", fsmEvent6 == null, fsmEvent7 == null), false);
						return;
					}
					screw.AddGlobalTransition(fsmEvent6, "1");
					screw.AddGlobalTransition(fsmEvent7, "0");
				}
				if (screw.transform.parent != null && screw.transform.parent.name == "MaskedCamshaftGear")
				{
					this.isCamshaftGear = true;
					screw.InsertAction("Rotate", new Action(NetItemsManager.CamshaftGearAdjustEvent), -1, false);
				}
				screw.transform.name.Contains("oil filter");
				screw.Initialize();
			}

			private void OnTightness()
			{
				if (this.doSync && ((NetPartManager.Bolt.raycastBolt != null && NetPartManager.Bolt.raycastBolt.Value == this.screw.gameObject) || this.isPin || (!this.screw.HasState("Wait 3") && !this.screw.HasState("Wait 4"))) && this.ScrewSyncStage != this.lastScrewSyncStage)
				{
					this.lastScrewSyncStage = this.ScrewSyncStage;
					this.onTightnessChange(this.hash, this.ScrewSyncStage);
				}
				this.doSync = true;
			}

			public void SetTightness(byte stage)
			{
				if (this.stage != null && (int)stage == this.stage.Value)
				{
					return;
				}
				this.doSync = false;
				if (this.isTuneBolt)
				{
					float num = (float)stage / 255f * this.maxAlignment;
					this.alignment.Value = num;
					this.screw.SendEvent("MP_SETUP");
					return;
				}
				if (this.isScrewableLid)
				{
					float num2 = (float)stage / 255f * 360f;
					bool flag = num2 > this.rot.Value;
					this.rot.Value = (flag ? (num2 - this.rotAmount) : (num2 + this.rotAmount));
					if (!flag && num2 < 5f)
					{
						this.rot.Value = this.rotAmount;
					}
					this.screw.SendEvent(flag ? "MP_SCREW" : "MP_UNSCREW");
					return;
				}
				bool flag2 = (int)stage > this.stage.Value;
				this.stage.Value = (int)(flag2 ? (stage - 1) : (stage + 1));
				this.screw.SendEvent(flag2 ? "TIGHTEN" : "UNTIGHTEN");
			}

			internal PlayMakerFSM screw;

			internal FsmInt stage;

			internal FsmFloat floatstage;

			internal int hash;

			private bool isPin;

			private bool isTuneBolt;

			private bool isScrewableLid;

			private bool isCamshaftGear;

			private Action<int, byte> onTightnessChange;

			internal FsmFloat alignment;

			internal FsmFloat rot;

			private float maxAlignment;

			private float rotAmount;

			private byte lastScrewSyncStage;

			private bool doSync = true;

			private static FsmGameObject raycastBolt;
		}
	}
}
