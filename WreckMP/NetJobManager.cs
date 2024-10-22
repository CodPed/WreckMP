using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace WreckMP
{
	internal class NetJobManager : NetManager
	{
		internal static bool inspectionOngoing
		{
			get
			{
				return !(NetJobManager.inspectionOrder == null) && !NetJobManager.inspectionOrder.gameObject.activeSelf && (NetJobManager.satsuma.position - NetJobManager.inspectionSatsumaCheckpoint.position).sqrMagnitude <= 9f;
			}
		}

		private void Start()
		{
			Transform transform = GameObject.Find("STORE").transform.Find("AdvertSpawn/teimo advert pile(itemx)");
			if (transform == null)
			{
				transform = GameObject.Find("teimo advert pile(itemx)").transform;
			}
			NetJobManager.teimoAdvertPile = transform.GetComponent<PlayMakerFSM>();
			Func<string> func = delegate
			{
				NetJobManager.logwall = GameObject.Find("YARD").transform.Find("MachineHall/Logging/Logwall").GetComponent<PlayMakerFSM>();
				this.currentLog = NetJobManager.logwall.FsmVariables.FindFsmGameObject("Log");
				NetJobManager.logwall.Initialize();
				this.logwallEvent = NetJobManager.logwall.AddEvent("MP_SPAWN");
				NetJobManager.logwall.AddGlobalTransition(this.logwallEvent, "Create log");
				NetJobManager.logwall.InsertAction("Create log", new Action(this.LogSpawned), -1, false);
				this.spawnLog = new GameEvent("SpawnLog", new Action<GameEventReader>(this.OnLogSpawn), GameScene.GAME);
				this.cutLog = new GameEvent("CutLog", new Action<GameEventReader>(this.OnLogCut), GameScene.GAME);
				return "Netjobmanager1";
			};
			if (_ObjectsLoader.IsGameLoaded)
			{
				func();
			}
			else
			{
				_ObjectsLoader.gameLoaded.Add(func);
			}
			func = delegate
			{
				NetJobManager.inspectionOrder = GameObject.Find("INSPECTION").transform.Find("Functions/Order").GetComponent<PlayMakerFSM>();
				this.inspectionOrderEvent = NetJobManager.inspectionOrder.AddEvent("MP_TRIGGER");
				NetJobManager.inspectionOrder.AddGlobalTransition(this.inspectionOrderEvent, "Pay");
				NetJobManager.inspectionOrder.InsertAction("Pay", new Action(this.InspectionTriggered), 0, false);
				this.triggerInspection = new GameEvent("Inspection", new Action<GameEventReader>(this.OnInspection), GameScene.GAME);
				GetDistance getDistance = NetJobManager.inspectionOrder.GetState("Check car").Actions[0] as GetDistance;
				NetJobManager.satsuma = getDistance.gameObject.GameObject.Value.transform;
				NetJobManager.inspectionSatsumaCheckpoint = getDistance.target.Value.transform;
				return "Netjobmanager2";
			};
			if (_ObjectsLoader.IsGameLoaded)
			{
				func();
			}
			else
			{
				_ObjectsLoader.gameLoaded.Add(func);
			}
			func = delegate
			{
				this.spawnAdvertSheetEvent = NetJobManager.teimoAdvertPile.AddEvent("MP_SPAWNSHEET");
				NetJobManager.teimoAdvertPile.AddGlobalTransition(this.spawnAdvertSheetEvent, "Open");
				NetJobManager.teimoAdvertPile.InsertAction("Open", new Action(this.SpawnAdvertSheet), -1, false);
				this.newSheet = new FsmGameObject("MP_NewSheet");
				List<FsmGameObject> list = NetJobManager.teimoAdvertPile.FsmVariables.GameObjectVariables.ToList<FsmGameObject>();
				list.Add(this.newSheet);
				NetJobManager.teimoAdvertPile.FsmVariables.GameObjectVariables = list.ToArray();
				(NetJobManager.teimoAdvertPile.GetState("Open").Actions[1] as CreateObject).storeObject = this.newSheet;
				this.advertUpdate = new GameEvent("SpawnSheet", new Action<GameEventReader>(this.OnSpawnAdvertSheet), GameScene.GAME);
				return "Netjobmanager3";
			};
			if (_ObjectsLoader.IsGameLoaded)
			{
				func();
			}
			else
			{
				_ObjectsLoader.gameLoaded.Add(func);
			}
			func = delegate
			{
				for (int i = 0; i < _ObjectsLoader.ObjectsInGame.Length; i++)
				{
					GameObject gameObject = _ObjectsLoader.ObjectsInGame[i];
					if (gameObject.name == "BoxHatch")
					{
						PlayMakerFSM component = gameObject.GetComponent<PlayMakerFSM>();
						if (!(component.FsmName != "WaitAd"))
						{
							this.mailboxes.Add(component);
							this.mailboxAdvertVariables.Add(component.FsmVariables.FindFsmGameObject("AdvertObject"));
							this.mailboxesDropping.Add(false);
							FsmEvent fsmEvent = component.AddEvent("MP_OPEN");
							this.mailboxDropAdvertEvents.Add(fsmEvent);
							component.AddGlobalTransition(fsmEvent, "Open");
							int id = this.mailboxAdvertVariables.Count - 1;
							component.InsertAction("Open", delegate
							{
								this.AdvertInMailbox(id);
							}, 0, false);
						}
					}
					else if (gameObject.name.Contains("RALLYCAR"))
					{
						Rigidbody component2 = gameObject.GetComponent<Rigidbody>();
						if (component2 != null)
						{
							NetRigidbodyManager.AddRigidbody(component2, gameObject.transform.GetGameobjectHashString().GetHashCode());
							Console.Log("Registered " + gameObject.transform.GetGameobjectHashString(), false);
						}
					}
				}
				this.advertInMailbox = new GameEvent("Advert", new Action<GameEventReader>(this.OnAdvertInMailbox), GameScene.GAME);
				return "Netjobmanager4";
			};
			if (_ObjectsLoader.IsGameLoaded)
			{
				func();
			}
			else
			{
				_ObjectsLoader.gameLoaded.Add(func);
			}
			func = delegate
			{
				if (!WreckMPGlobals.IsHost)
				{
					PlayMakerFSM component3 = GameObject.Find("YARD").transform.Find("Building/BEDROOM1/LOD_bedroom1/Sleep/SleepTrigger").GetComponent<PlayMakerFSM>();
					component3.Initialize();
					(component3.GetState("Does call?").Actions[2] as BoolTest).isTrue = component3.FsmEvents.FirstOrDefault((FsmEvent e) => e.Name == "NOCALL");
				}
				return "Netjobmanager5";
			};
			if (_ObjectsLoader.IsGameLoaded)
			{
				func();
			}
			else
			{
				_ObjectsLoader.gameLoaded.Add(func);
			}
			func = delegate
			{
				NetJobManager.waterFacilityCashRegister = GameObject.Find("WATERFACILITY").transform.Find("LOD/Desk/FacilityCashRegister/Register").GetPlayMaker("Data");
				this.waterFacilityCalcPriceEvent = NetJobManager.waterFacilityCashRegister.AddEvent("MP_CALCPRICE");
				NetJobManager.waterFacilityCashRegister.AddGlobalTransition(this.waterFacilityCalcPriceEvent, "Calculate price");
				NetJobManager.waterFacilityCashRegister.InsertAction("Calculate price", delegate
				{
					this.WaterFacilityCashRegisterUpdate(false);
				}, 0, false);
				this.waterFacilityPayEvent = NetJobManager.waterFacilityCashRegister.AddEvent("MP_PAY");
				NetJobManager.waterFacilityCashRegister.AddGlobalTransition(this.waterFacilityPayEvent, "Check money");
				NetJobManager.waterFacilityCashRegister.InsertAction("Check money", delegate
				{
					this.WaterFacilityCashRegisterUpdate(true);
				}, 0, false);
				this.waterFacilityUpdateEvent = new GameEvent("WaterFacility", new Action<GameEventReader>(this.OnWaterFacilityCashRegisterUpdate), GameScene.GAME);
				return "Netjobmanager6";
			};
			if (_ObjectsLoader.IsGameLoaded)
			{
				func();
				return;
			}
			_ObjectsLoader.gameLoaded.Add(func);
		}

		private void OnWaterFacilityCashRegisterUpdate(GameEventReader packet)
		{
			bool flag = packet.ReadBoolean();
			this.updatingWaterFacility = true;
			NetJobManager.waterFacilityCashRegister.Fsm.Event(flag ? this.waterFacilityPayEvent : this.waterFacilityCalcPriceEvent);
		}

		private void WaterFacilityCashRegisterUpdate(bool openGate)
		{
			if (this.updatingWaterFacility)
			{
				this.updatingWaterFacility = false;
				return;
			}
			using (GameEventWriter gameEventWriter = this.waterFacilityUpdateEvent.Writer())
			{
				gameEventWriter.Write(openGate);
				this.waterFacilityUpdateEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnAdvertInMailbox(GameEventReader packet)
		{
			int num = (int)packet.ReadByte();
			int num2 = (int)packet.ReadByte();
			this.mailboxAdvertVariables[num].Value = this.advertSheets[num2];
			this.mailboxes[num].Fsm.Event(this.mailboxDropAdvertEvents[num]);
			this.mailboxesDropping[num] = true;
		}

		private void AdvertInMailbox(int i)
		{
			if (this.mailboxesDropping[i])
			{
				this.mailboxesDropping[i] = false;
				return;
			}
			using (GameEventWriter gameEventWriter = this.advertInMailbox.Writer())
			{
				gameEventWriter.Write((byte)i);
				gameEventWriter.Write((byte)this.advertSheets.IndexOf(this.mailboxAdvertVariables[i].Value));
				this.advertInMailbox.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnSpawnAdvertSheet(GameEventReader packet)
		{
			this.spawningSheet = true;
			NetJobManager.teimoAdvertPile.Fsm.Event(this.spawnAdvertSheetEvent);
		}

		private void SpawnAdvertSheet()
		{
			Rigidbody component = this.newSheet.Value.GetComponent<Rigidbody>();
			int hashCode = (component.transform.GetGameobjectHashString() + this.advertSheets.Count.ToString()).GetHashCode();
			this.advertSheets.Add(component.gameObject);
			NetRigidbodyManager.AddRigidbody(component, hashCode);
			if (this.spawningSheet)
			{
				this.spawningSheet = false;
				return;
			}
			using (GameEventWriter gameEventWriter = this.advertUpdate.Writer())
			{
				this.advertUpdate.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnInspection(GameEventReader packet)
		{
			this.triggeringInspection = true;
			NetJobManager.inspectionOrder.Fsm.Event(this.inspectionOrderEvent);
		}

		private void InspectionTriggered()
		{
			if (this.triggeringInspection)
			{
				this.triggeringInspection = false;
				return;
			}
			using (GameEventWriter gameEventWriter = this.triggerInspection.Writer())
			{
				this.triggerInspection.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnLogSpawn(GameEventReader packet)
		{
			this.updatingLogs = true;
			NetJobManager.logwall.Fsm.Event(this.logwallEvent);
		}

		private void LogSpawned()
		{
			GameObject value = this.currentLog.Value;
			int hashCode = (value.transform.GetGameobjectHashString() + NetJobManager.logsCount++.ToString()).GetHashCode();
			NetRigidbodyManager.AddRigidbody(value.GetComponent<Rigidbody>(), hashCode);
			int hash2 = (value.transform.GetChild(0).GetGameobjectHashString() + NetJobManager.logsCount++.ToString()).GetHashCode();
			NetRigidbodyManager.AddRigidbody(value.transform.GetChild(0).GetComponent<Rigidbody>(), hash2);
			this.logs.Add(hash2, value.transform.GetChild(0).GetComponent<FixedJoint>());
			value.transform.GetChild(0).GetComponent<PlayMakerFSM>().InsertAction("State 2", delegate
			{
				this.CutLog(hash2);
			}, 0, false);
			if (this.updatingLogs)
			{
				this.updatingLogs = false;
				return;
			}
			using (GameEventWriter gameEventWriter = this.spawnLog.Writer())
			{
				this.spawnLog.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void CutLog(int hash2)
		{
			using (GameEventWriter gameEventWriter = this.cutLog.Writer())
			{
				gameEventWriter.Write(hash2);
				this.cutLog.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnLogCut(GameEventReader packet)
		{
			int num = packet.ReadInt32();
			if (this.logs.ContainsKey(num))
			{
				Object.Destroy(this.logs[num]);
				this.logs.Remove(num);
			}
		}

		private static PlayMakerFSM logwall;

		private static PlayMakerFSM inspectionOrder;

		private static PlayMakerFSM teimoAdvertPile;

		private static PlayMakerFSM waterFacilityCashRegister;

		private static Transform satsuma;

		private static Transform inspectionSatsumaCheckpoint;

		private List<PlayMakerFSM> mailboxes = new List<PlayMakerFSM>();

		private List<FsmGameObject> mailboxAdvertVariables = new List<FsmGameObject>();

		private FsmGameObject currentLog;

		private FsmGameObject newSheet;

		private FsmEvent logwallEvent;

		private FsmEvent inspectionOrderEvent;

		private FsmEvent spawnAdvertSheetEvent;

		private FsmEvent waterFacilityCalcPriceEvent;

		private FsmEvent waterFacilityPayEvent;

		private List<FsmEvent> mailboxDropAdvertEvents = new List<FsmEvent>();

		private Dictionary<int, FixedJoint> logs = new Dictionary<int, FixedJoint>();

		private List<GameObject> advertSheets = new List<GameObject>();

		private List<bool> mailboxesDropping = new List<bool>();

		private bool updatingLogs;

		private bool triggeringInspection;

		private bool spawningSheet;

		private bool updatingWaterFacility;

		internal static int logsCount;

		private GameEvent spawnLog;

		private GameEvent cutLog;

		private GameEvent triggerInspection;

		private GameEvent advertUpdate;

		private GameEvent advertInMailbox;

		private GameEvent waterFacilityUpdateEvent;
	}
}
