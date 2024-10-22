using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	internal class NetItemsManager : NetManager
	{
		private void Start()
		{
			Func<string> func = delegate
			{
				Transform transform = GameObject.Find("ITEMS").transform;
				NetItemsManager.spannerSetOpenEvent = new GameEvent("SpannerTop", new Action<GameEventReader>(this.OnSpannerSetOpen), GameScene.GAME);
				NetItemsManager.camshaftGearEvent = new GameEvent("CamGear", new Action<GameEventReader>(NetItemsManager.OnCamshaftGearAdjust), GameScene.GAME);
				NetItemsManager.beercaseDrinkEvent = new GameEvent("BeercaseD", new Action<GameEventReader>(this.OnBeercaseSubtractBottle), GameScene.GAME);
				NetItemsManager.jerryCanSyncEvent = new GameEvent("JerryCan", new Action<GameEventReader>(this.OnJerryCanSync), GameScene.GAME);
				NetItemsManager.jerryCanLidEvent = new GameEvent("JCanLid", new Action<GameEventReader>(this.OnJerryCanLid), GameScene.GAME);
				NetItemsManager.distributorRotateEvent = new GameEvent("Distributor", new Action<GameEventReader>(this.OnDistributorHandRotate), GameScene.GAME);
				NetItemsManager.alternatorRotateEvent = new GameEvent("AlternatorTune", new Action<GameEventReader>(this.OnAlternatorHandRotate), GameScene.GAME);
				NetItemsManager.carFluidsSync = new GameEvent("CarFluids", new Action<GameEventReader>(FsmNetVehicle.OnCarFluidsAndFields), GameScene.GAME);
				NetItemsManager.trailerDetachEvent = new GameEvent("TrailerDetach", new Action<GameEventReader>(this.OnTrailerDetach), GameScene.GAME);
				NetItemsManager.woodCarrierEvent = new GameEvent("WoodCar", new Action<GameEventReader>(this.OnWoodCarrierSpawnLog), GameScene.GAME);
				GameObject gameObject = GameObject.Find("ITEMS").transform.Find("lantern(itemx)").gameObject;
				int num = 10;
				NetItemsManager.lanternSync = new bool[num];
				for (int i = 0; i < num; i++)
				{
					int _i = i;
					GameObject gameObject2 = ((i == num - 1) ? gameObject : Object.Instantiate<GameObject>(gameObject));
					gameObject2.name = "lantern(itemx" + i.ToString();
					PlayMakerFSM fsm = gameObject2.GetPlayMaker("Use");
					NetItemsManager.lanternSync[i] = true;
					FsmEvent on = fsm.AddEvent("MP_ON");
					fsm.AddGlobalTransition(on, "ON");
					FsmEvent off = fsm.AddEvent("MP_OFF");
					fsm.AddGlobalTransition(off, "OFF");
					GameEvent toggleEvent = new GameEvent("LanternToggle" + _i.ToString(), delegate(GameEventReader p)
					{
						NetItemsManager.lanternSync[_i] = false;
						bool flag = p.ReadBoolean();
						fsm.Fsm.Event(flag ? on : off);
					}, GameScene.GAME);
					Action<bool> toggle = delegate(bool on)
					{
						if (NetItemsManager.lanternSync[_i])
						{
							using (GameEventWriter gameEventWriter = toggleEvent.Writer())
							{
								gameEventWriter.Write(on);
								toggleEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
							}
						}
						NetItemsManager.lanternSync[_i] = true;
					};
					fsm.InsertAction("ON", delegate
					{
						toggle(true);
					}, 0, false);
					fsm.InsertAction("OFF", delegate
					{
						toggle(false);
					}, 0, false);
				}
				return "Netitems0";
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
				NetItemsManager.spannerSetTop = GameObject.Find("ITEMS").transform.Find("spanner set(itemx)/Pivot/top").GetComponent<PlayMakerFSM>();
				this.InjectSpannerSetTop(NetItemsManager.spannerSetTop, delegate
				{
					this.SpannerSetOpen(false);
				}, out NetItemsManager.spannerSetOpen);
				NetItemsManager.ratchetSetTop = NetItemsManager.GetDatabaseObject("Database/DatabaseOrders/Ratchet Set").transform.Find("Hinge/Pivot/top").GetComponent<PlayMakerFSM>();
				this.InjectSpannerSetTop(NetItemsManager.ratchetSetTop, delegate
				{
					this.SpannerSetOpen(true);
				}, out NetItemsManager.ratchetSetOpen);
				return "Netitems1";
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
				NetItemsManager.camshaftGear = NetItemsManager.GetDatabaseObject("Database/DatabaseMotor/CamshaftGear").GetPlayMaker("BoltCheck");
				NetItemsManager.camshaftGearMesh = NetItemsManager.camshaftGear.transform.Find("camshaft_gear_mesh");
				NetItemsManager.camshaftGearAngle = NetItemsManager.camshaftGear.FsmVariables.FindFsmFloat("Angle");
				NetItemsManager.camshaftGearRotateAmount = NetItemsManager.camshaftGear.FsmVariables.FindFsmFloat("RotateAmount");
				return "Netitems2";
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
				Transform transform2 = GameObject.Find("ITEMS").transform;
				Transform transform3 = transform2.Find("gasoline(itemx)");
				NetItemsManager.gasolineCanFluid = transform3.Find("FluidTrigger").GetPlayMaker("Data").FsmVariables.FindFsmFloat("Fluid");
				this.SetupJerryCanLidFsm(transform3.Find("Open"), false);
				Transform transform4 = transform2.Find("diesel(itemx)");
				NetItemsManager.dieselCanFluid = transform4.Find("FluidTrigger").GetPlayMaker("Data").FsmVariables.FindFsmFloat("Fluid");
				this.SetupJerryCanLidFsm(transform4.Find("Open"), true);
				return "Netitems3";
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
				NetItemsManager.distributorHandRotate = NetItemsManager.GetDatabaseObject("Database/DatabaseMotor/Distributor").GetPlayMaker("HandRotate");
				NetItemsManager.distributorRotation = NetItemsManager.distributorHandRotate.FsmVariables.FindFsmFloat("Rotation");
				NetItemsManager.distributorRotationPivot = NetItemsManager.distributorHandRotate.transform.Find("Pivot");
				NetItemsManager.distributorHandRotate.InsertAction("Wait", new Action(this.DistributorHandRotate), 0, false);
				return "Netitems4";
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
				NetItemsManager.alternatorHandRotate = NetItemsManager.GetDatabaseObject("Database/DatabaseMotor/Alternator").transform.Find("Pivot").GetPlayMaker("HandRotate");
				NetItemsManager.alternatorRotation = NetItemsManager.alternatorHandRotate.FsmVariables.FindFsmFloat("Rotation");
				NetItemsManager.alternatorHandRotate.InsertAction("Wait", new Action(this.AlternatorHandRotate), 0, false);
				return "Netitems5";
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
				NetItemsManager.trailerDetach = GameObject.Find("KEKMET(350-400psi)").transform.Find("Trailer/Remove").GetComponent<PlayMakerFSM>();
				FsmEvent fsmEvent = NetItemsManager.trailerDetach.AddEvent("MP_DETACH");
				NetItemsManager.trailerDetach.AddGlobalTransition(fsmEvent, "Close door");
				NetItemsManager.trailerDetach.InsertAction("Close door", new Action(this.SendTrailerDetached), 0, false);
				return "Netitems6";
			};
			if (_ObjectsLoader.IsGameLoaded)
			{
				func();
				return;
			}
			_ObjectsLoader.gameLoaded.Add(func);
		}

		private void OnWoodCarrierSpawnLog(GameEventReader packet)
		{
			int num = packet.ReadInt32();
			if (!NetItemsManager.woodCarriers.ContainsKey(num))
			{
				return;
			}
			NetItemsManager.woodCarriers[num].SendEvent("PICKWOOD");
		}

		private void SendTrailerDetached()
		{
			using (GameEventWriter gameEventWriter = NetItemsManager.trailerDetachEvent.Writer())
			{
				NetItemsManager.trailerDetachEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnTrailerDetach(GameEventReader packet)
		{
			NetItemsManager.trailerDetach.Fsm.Event(NetItemsManager.trailerDetach.FsmEvents.FirstOrDefault((FsmEvent e) => e.Name == "MP_DETACH"));
		}

		public static GameObject GetDatabaseObject(string databasePath)
		{
			GameObject gameObject = GameObject.Find(databasePath);
			if (gameObject == null)
			{
				Console.Log("Database '" + databasePath + "' could not be found", true);
				return null;
			}
			PlayMakerFSM component = gameObject.GetComponent<PlayMakerFSM>();
			if (component == null)
			{
				Console.Log("Database '" + databasePath + "' doesn't have an fsm", true);
				return null;
			}
			FsmGameObject fsmGameObject = component.FsmVariables.FindFsmGameObject("ThisPart");
			if (fsmGameObject == null)
			{
				Console.Log("Database '" + databasePath + "' doesn't have a this part variable", true);
				return null;
			}
			return fsmGameObject.Value;
		}

		private void Update()
		{
			this.DoRegularSync(ref this.jerryCanSyncTime, new Action(this.SyncJerryCans), 30f, true);
			this.DoRegularSync(ref this.carFluidsSyncTime, new Action(FsmNetVehicle.SendCarFluidsAndFields), 30f, true);
		}

		private void DoRegularSync(ref float time, Action doSync, float resetTime = 30f, bool onlyHost = true)
		{
			if (onlyHost && !WreckMPGlobals.IsHost)
			{
				return;
			}
			time -= Time.deltaTime;
			if (time < 0f)
			{
				time = resetTime;
				doSync();
			}
		}

		private void SpannerSetOpen(bool isRatchet)
		{
			using (GameEventWriter gameEventWriter = NetItemsManager.spannerSetOpenEvent.Writer())
			{
				gameEventWriter.Write(isRatchet);
				gameEventWriter.Write(isRatchet ? NetItemsManager.ratchetSetOpen.Value : NetItemsManager.spannerSetOpen.Value);
				NetItemsManager.spannerSetOpenEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnSpannerSetOpen(GameEventReader packet)
		{
			if (packet.sender == WreckMPGlobals.UserID)
			{
				return;
			}
			bool flag = packet.ReadBoolean();
			if ((flag ? NetItemsManager.ratchetSetOpen : NetItemsManager.spannerSetOpen).Value != packet.ReadBoolean())
			{
				return;
			}
			(flag ? NetItemsManager.ratchetSetTop : NetItemsManager.spannerSetTop).SendEvent("MP_TOGGLE");
		}

		private void InjectSpannerSetTop(PlayMakerFSM fsm, Action topToggled, out FsmBool isOpen)
		{
			FsmEvent fsmEvent = fsm.AddEvent("MP_TOGGLE");
			fsm.AddGlobalTransition(fsmEvent, "Bool test");
			isOpen = fsm.FsmVariables.FindFsmBool("Open");
			fsm.InsertAction("Bool test", topToggled, 0, false);
		}

		internal static void CamshaftGearAdjustEvent()
		{
			using (GameEventWriter gameEventWriter = NetItemsManager.camshaftGearEvent.Writer())
			{
				gameEventWriter.Write(NetItemsManager.camshaftGearAngle.Value);
				NetItemsManager.camshaftGearEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private static void OnCamshaftGearAdjust(GameEventReader p)
		{
			float num = p.ReadSingle();
			NetItemsManager.camshaftGearAngle.Value = num - NetItemsManager.camshaftGearRotateAmount.Value;
			NetItemsManager.camshaftGearMesh.localEulerAngles = Vector3.right * NetItemsManager.camshaftGearAngle.Value;
			NetItemsManager.camshaftGear.SendEvent("ADJUST");
		}

		public static void SetupBeercaseFSM(PlayMakerFSM fsm, NetCreateItemsManager.Item item)
		{
			int hash = item.ID.GetHashCode();
			NetItemsManager.beercases.Add(item);
			fsm.InsertAction("Remove bottle", delegate
			{
				NetItemsManager.BeercaseSubtractBottleEvent(hash);
			}, 0, false);
		}

		private static void BeercaseSubtractBottleEvent(int hash)
		{
			using (GameEventWriter gameEventWriter = NetItemsManager.beercaseDrinkEvent.Writer())
			{
				gameEventWriter.Write(hash);
				NetItemsManager.beercaseDrinkEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnBeercaseSubtractBottle(GameEventReader packet)
		{
			CoreManager.Players[packet.sender].playerAnimationManager.TriggerGesture(7);
			int hash = packet.ReadInt32();
			NetCreateItemsManager.Item item = NetItemsManager.beercases.FirstOrDefault((NetCreateItemsManager.Item i) => i.ID.GetHashCode() == hash);
			if (item == null)
			{
				Console.LogError(string.Format("Beercase of hash {0} does not exist", hash), false);
				return;
			}
			item.fsm.SendEvent("SUSKI");
		}

		private void SyncJerryCans()
		{
			using (GameEventWriter gameEventWriter = NetItemsManager.jerryCanSyncEvent.Writer())
			{
				gameEventWriter.Write(NetItemsManager.gasolineCanFluid.Value);
				gameEventWriter.Write(NetItemsManager.dieselCanFluid.Value);
				NetItemsManager.jerryCanSyncEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnJerryCanSync(GameEventReader packet)
		{
			NetItemsManager.gasolineCanFluid.Value = packet.ReadSingle();
			NetItemsManager.dieselCanFluid.Value = packet.ReadSingle();
		}

		private void SetupJerryCanLidFsm(Transform lid, bool isDiesel)
		{
			PlayMakerFSM playMaker = lid.GetPlayMaker("Use");
			playMaker.Initialize();
			if (isDiesel)
			{
				NetItemsManager.dieselCanLid = playMaker;
			}
			else
			{
				NetItemsManager.gasolineCanLid = playMaker;
			}
			FsmBool fsmBool = playMaker.FsmVariables.FindFsmBool("Open");
			if (isDiesel)
			{
				NetItemsManager.dieselCanOpen = fsmBool;
			}
			else
			{
				NetItemsManager.gasolineCanOpen = fsmBool;
			}
			FsmBool fsmBool2 = playMaker.FsmVariables.FindFsmBool("Closed");
			if (isDiesel)
			{
				NetItemsManager.dieselCanClose = fsmBool2;
			}
			else
			{
				NetItemsManager.gasolineCanClose = fsmBool2;
			}
			playMaker.InsertAction("State 2", delegate
			{
				this.JerryCanLidToggle(isDiesel);
			}, 0, false);
			FsmEvent fsmEvent = playMaker.AddEvent("MP_TOGGLE");
			playMaker.AddGlobalTransition(fsmEvent, "State 2");
			playMaker.Initialize();
		}

		private void JerryCanLidToggle(bool isDiesel)
		{
			PlayMakerFSM playMakerFSM = (isDiesel ? NetItemsManager.dieselCanLid : NetItemsManager.gasolineCanLid);
			if (this.updatingFsms.Contains(playMakerFSM))
			{
				this.updatingFsms.Remove(playMakerFSM);
				return;
			}
			using (GameEventWriter gameEventWriter = NetItemsManager.jerryCanSyncEvent.Writer())
			{
				gameEventWriter.Write(isDiesel);
				gameEventWriter.Write(!(isDiesel ? NetItemsManager.dieselCanOpen : NetItemsManager.gasolineCanOpen).Value);
				NetItemsManager.jerryCanLidEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnJerryCanLid(GameEventReader packet)
		{
			bool flag = packet.ReadBoolean();
			bool flag2 = packet.ReadBoolean();
			(flag ? NetItemsManager.dieselCanOpen : NetItemsManager.gasolineCanOpen).Value = !flag2;
			(flag ? NetItemsManager.dieselCanClose : NetItemsManager.gasolineCanClose).Value = flag2;
			PlayMakerFSM playMakerFSM = (flag ? NetItemsManager.dieselCanLid : NetItemsManager.gasolineCanLid);
			this.updatingFsms.Add(playMakerFSM);
			playMakerFSM.SendEvent("MP_TOGGLE");
		}

		private void DistributorHandRotate()
		{
			using (GameEventWriter gameEventWriter = NetItemsManager.distributorRotateEvent.Writer())
			{
				gameEventWriter.Write(NetItemsManager.distributorRotationPivot.localEulerAngles.z);
				NetItemsManager.distributorRotateEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnDistributorHandRotate(GameEventReader p)
		{
			float num = p.ReadSingle();
			NetItemsManager.distributorRotationPivot.localEulerAngles = Vector3.forward * num;
			NetItemsManager.distributorRotation.Value = num;
		}

		private void AlternatorHandRotate()
		{
			using (GameEventWriter gameEventWriter = NetItemsManager.alternatorRotateEvent.Writer())
			{
				gameEventWriter.Write(NetItemsManager.alternatorHandRotate.transform.localEulerAngles.x);
				NetItemsManager.alternatorRotateEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnAlternatorHandRotate(GameEventReader p)
		{
			float num = p.ReadSingle();
			NetItemsManager.alternatorHandRotate.transform.localEulerAngles = Vector3.right * num;
			NetItemsManager.alternatorRotation.Value = num;
		}

		private static FsmBool spannerSetOpen;

		private static FsmBool ratchetSetOpen;

		private static FsmBool gasolineCanOpen;

		private static FsmBool dieselCanOpen;

		private static FsmBool gasolineCanClose;

		private static FsmBool dieselCanClose;

		private static PlayMakerFSM spannerSetTop;

		private static PlayMakerFSM ratchetSetTop;

		private static PlayMakerFSM camshaftGear;

		private static PlayMakerFSM dieselCanLid;

		private static PlayMakerFSM gasolineCanLid;

		private static PlayMakerFSM distributorHandRotate;

		private static PlayMakerFSM alternatorHandRotate;

		private static PlayMakerFSM trailerDetach;

		private static bool[] lanternSync;

		private static List<NetCreateItemsManager.Item> beercases = new List<NetCreateItemsManager.Item>();

		private static FsmFloat camshaftGearAngle;

		private static FsmFloat camshaftGearRotateAmount;

		private static FsmFloat gasolineCanFluid;

		private static FsmFloat dieselCanFluid;

		private static FsmFloat distributorRotation;

		private static FsmFloat alternatorRotation;

		private static Transform camshaftGearMesh;

		private static Transform distributorRotationPivot;

		private static Dictionary<int, PlayMakerFSM> woodCarriers;

		private List<PlayMakerFSM> updatingFsms = new List<PlayMakerFSM>();

		public float jerryCanSyncTime = 31f;

		public float carFluidsSyncTime = 32f;

		internal static GameEvent spannerSetOpenEvent;

		internal static GameEvent camshaftGearEvent;

		internal static GameEvent beercaseDrinkEvent;

		internal static GameEvent jerryCanSyncEvent;

		internal static GameEvent jerryCanLidEvent;

		internal static GameEvent distributorRotateEvent;

		internal static GameEvent alternatorRotateEvent;

		internal static GameEvent trailerDetachEvent;

		internal static GameEvent woodCarrierEvent;

		internal static GameEvent carFluidsSync;
	}
}
