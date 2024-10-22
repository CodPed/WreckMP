using System;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace WreckMP
{
	internal class NetBoatManager : NetManager
	{
		private void Start()
		{
			NetBoatManager.instance = this;
			_ObjectsLoader.gameLoaded.Add(delegate
			{
				this.boat = GameObject.Find("BOAT").GetComponent<Rigidbody>();
				this.allRBS = this.boat.GetComponentsInChildren<Rigidbody>(true);
				PlayMakerFSM[] componentsInChildren = this.boat.GetComponentsInChildren<PlayMakerFSM>(true);
				this.RemoveDeath(componentsInChildren);
				this.DoItemCollider();
				PlayMakerFSM drivingMode = componentsInChildren.FirstOrDefault(delegate(PlayMakerFSM fsm)
				{
					if (fsm.FsmName != "PlayerTrigger" || fsm.gameObject.name != "DriveTrigger")
					{
						return false;
					}
					fsm.Initialize();
					FsmState state = fsm.GetState("Press return");
					if (state == null)
					{
						return false;
					}
					SetStringValue setStringValue = state.Actions.FirstOrDefault((FsmStateAction a) => a is SetStringValue) as SetStringValue;
					return setStringValue != null && setStringValue.stringValue.Value.Contains("DRIVING");
				});
				if (drivingMode != null)
				{
					drivingMode.Initialize();
					drivingMode.InsertAction("Press return", delegate
					{
						if (this.driver != 0UL)
						{
							drivingMode.SendEvent("FINISHED");
						}
					}, 0, false);
					drivingMode.InsertAction("Player in car", new Action(this.SendEnterDrivingMode), -1, false);
					drivingMode.InsertAction("Create player", new Action(this.SendExitDrivingMode), -1, false);
					Console.Log("Init driving mode for BOAT", false);
				}
				BoxCollider component = this.boat.transform.Find("GFX/Triggers/PlayerTrigger").GetComponent<BoxCollider>();
				component.center = Vector3.forward * -1.38f;
				component.size = new Vector3(0.8f, 0.4f, 3.7f);
				this.AddPassengerSeat(this.boat, this.boat.transform, new Vector3(0f, 0f, 0.1f), new Vector3(1f, 0f, 0f));
				this.AddPassengerSeat(this.boat, this.boat.transform, new Vector3(0f, 0f, -1.1f), new Vector3(1f, 0f, 0f));
				this.drivingModeEvent = new GameEvent<NetBoatManager>("DrivingMode", delegate(ulong s, GameEventReader p)
				{
					bool flag = p.ReadBoolean();
					this.DrivingMode(s, flag);
				}, GameScene.GAME);
				this.passengerModeEvent = new GameEvent<NetBoatManager>("PassengerMode", delegate(ulong s, GameEventReader p)
				{
					bool flag2 = p.ReadBoolean();
					CoreManager.Players[s].SetPassengerMode(flag2, this.boat.transform, false);
				}, GameScene.GAME);
				return "BoatMgr";
			});
		}

		private void DoItemCollider()
		{
			SphereCollider sphereCollider = new GameObject("ItemCollider")
			{
				transform = 
				{
					parent = this.boat.transform,
					localPosition = default(Vector3)
				}
			}.AddComponent<SphereCollider>();
			sphereCollider.isTrigger = true;
			sphereCollider.radius = 3f;
			this.itemCollider = sphereCollider;
		}

		private void RemoveDeath(PlayMakerFSM[] fsms)
		{
			for (int i = 0; i < fsms.Length; i++)
			{
				if (fsms[i].FsmName == "Death" && fsms[i].gameObject.name == "DriverHeadPivot")
				{
					Transform transform = fsms[i].transform;
					transform.GetComponent<Rigidbody>().isKinematic = true;
					transform.transform.localPosition = Vector3.zero;
					transform.transform.localEulerAngles = Vector3.zero;
					Object.Destroy(fsms[i]);
					Object.Destroy(transform.parent.GetComponentInChildren<ConfigurableJoint>());
					Console.Log("Successfully removed death fsm from driving mode of " + base.transform.name, false);
					return;
				}
			}
		}

		public void SendEnterDrivingMode()
		{
			using (GameEventWriter gameEventWriter = this.drivingModeEvent.Writer())
			{
				gameEventWriter.Write(true);
				this.driver = (this.owner = WreckMPGlobals.UserID);
				LocalPlayer.Instance.inCar = true;
				this.drivingModeEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
				NetRigidbodyManager.RequestOwnership(this.boat);
				for (int i = 0; i < this.allRBS.Length; i++)
				{
					NetRigidbodyManager.RequestOwnership(this.allRBS[i]);
				}
			}
		}

		public void SendExitDrivingMode()
		{
			using (GameEventWriter gameEventWriter = this.drivingModeEvent.Writer())
			{
				gameEventWriter.Write(false);
				this.driver = 0UL;
				LocalPlayer.Instance.inCar = false;
				this.drivingModeEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		internal void DrivingMode(ulong player, bool enter)
		{
			this.driver = (enter ? player : 0UL);
			if (enter)
			{
				this.owner = player;
			}
			CoreManager.Players[player].SetPassengerMode(enter, this.boat.transform, false);
		}

		private void AddPassengerSeat(Rigidbody rb, Transform parent, Vector3 triggerOffset, Vector3 headPivotOffset)
		{
			GameObject gameObject = GameObject.Find("NPC_CARS").transform.Find("Amikset/KYLAJANI/LOD/PlayerFunctions").gameObject;
			int num = 0;
			GameObject gameObject2 = Object.Instantiate<GameObject>(gameObject);
			gameObject2.name = string.Format("MPPlayerFunctions_{0}", num);
			Transform transform = gameObject2.transform.Find("DriverHeadPivot");
			transform.GetComponent<Rigidbody>().isKinematic = true;
			transform.transform.localPosition = Vector3.zero;
			Object.Destroy(transform.GetPlayMaker("Death"));
			Transform child = gameObject2.transform.GetChild(1);
			child.gameObject.SetActive(false);
			child.transform.localPosition = headPivotOffset;
			Object.Destroy(child.GetComponent<ConfigurableJoint>());
			child.gameObject.SetActive(true);
			Object.Destroy(gameObject2.transform.GetChild(0).gameObject);
			gameObject2.transform.SetParent(parent, false);
			gameObject2.transform.Find("PlayerTrigger/DriveTrigger").localPosition = triggerOffset;
			Transform transform2 = gameObject2.transform.Find("PlayerTrigger");
			transform2.localPosition = Vector3.zero;
			Object.Destroy(transform2.GetComponent<PlayMakerFSM>());
			Object.Destroy(transform2.GetComponent<BoxCollider>());
			PlayMakerFSM component = gameObject2.transform.Find("PlayerTrigger/DriveTrigger").GetComponent<PlayMakerFSM>();
			component.name = "PassengerTrigger";
			component.transform.parent.name = "PlayerOffset";
			component.Initialize();
			component.GetComponent<CapsuleCollider>().radius = 0.2f;
			component.InsertAction("Reset view", delegate
			{
				using (GameEventWriter gameEventWriter = this.passengerModeEvent.Writer())
				{
					gameEventWriter.Write(true);
					LocalPlayer.Instance.inCar = true;
					this.passengerModeEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
				}
			}, -1, false);
			component.InsertAction("Create player", delegate
			{
				using (GameEventWriter gameEventWriter2 = this.passengerModeEvent.Writer())
				{
					gameEventWriter2.Write(false);
					LocalPlayer.Instance.inCar = false;
					this.passengerModeEvent.Send(gameEventWriter2, 0UL, true, default(GameEvent.RecordingProperties));
				}
			}, -1, false);
			(component.FsmStates.First((FsmState s) => s.Name == "Check speed").Actions[0] as GetVelocity).gameObject = new FsmOwnerDefault
			{
				GameObject = rb.transform.gameObject,
				OwnerOption = 1
			};
			(component.FsmStates.First((FsmState s) => s.Name == "Player in car").Actions[3] as SetStringValue).stringValue = "Passenger_" + rb.transform.name;
			gameObject2.SetActive(true);
			gameObject2.transform.GetChild(1).gameObject.SetActive(true);
			component.gameObject.SetActive(true);
			Transform transform3 = gameObject2.transform;
		}

		private void Update()
		{
		}

		internal static NetBoatManager instance;

		internal SphereCollider itemCollider;

		internal Rigidbody boat;

		private Rigidbody[] allRBS;

		private ConfigurableJoint[] passengerModes;

		private GameEvent<NetBoatManager> drivingModeEvent;

		private GameEvent<NetBoatManager> passengerModeEvent;

		public ulong driver;

		public ulong owner = WreckMPGlobals.HostID;
	}
}
