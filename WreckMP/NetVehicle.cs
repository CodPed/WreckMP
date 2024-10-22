using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Steamworks;
using UnityEngine;

namespace WreckMP
{
	public class NetVehicle
	{
		public int Hash { get; internal set; }

		public Transform Transform { get; set; }

		public Rigidbody Rigidbody { get; set; }

		public AxisCarController AxisCarController { get; set; }

		public Drivetrain Drivetrain { get; set; }

		public ulong Owner
		{
			get
			{
				return this._owner;
			}
			internal set
			{
				this._owner = value;
				if (this.audioController != null)
				{
					this.audioController.IsDrivenBySoundController = value == WreckMPGlobals.UserID;
				}
			}
		}

		public ulong Driver { get; internal set; }

		public NetVehicle(Transform transform)
		{
			this.Rigidbody = transform.GetComponent<Rigidbody>();
			this.allRBS = transform.GetComponentsInChildren<Rigidbody>(true);
			this.Transform = transform;
			this.Hash = transform.GetGameobjectHashString().GetHashCode();
			this.AxisCarController = transform.GetComponent<AxisCarController>();
			this.Drivetrain = transform.GetComponent<Drivetrain>();
			SoundController component = transform.GetComponent<SoundController>();
			if (component != null)
			{
				this.audioController = new NetVehicleAudio(transform, component);
			}
			this.Owner = WreckMPGlobals.HostID;
			NetVehicleManager.RegisterNetVehicle(this);
			if (NetVehicle.updateIdleThrottle == null)
			{
				NetVehicle.updateIdleThrottle = new GameEvent<NetVehicle>("IThrottle", new Action<ulong, GameEventReader>(NetVehicle.OnIdleThrottleUpdate), GameScene.GAME);
			}
			ConfigurableJoint componentInChildren = transform.GetComponentInChildren<ConfigurableJoint>();
			this.headJoints.Add(componentInChildren);
			NetRigidbodyManager.AddRigidbody(this.Rigidbody, this.Hash);
			NetVehicle.FindFlatbed();
		}

		public bool DriverSeatTaken
		{
			get
			{
				return this.Driver > 0UL;
			}
		}

		private static void FindFlatbed()
		{
			if (NetVehicle.FLATBED == null)
			{
				NetVehicle.FLATBED = GameObject.Find("FLATBED").GetComponent<Rigidbody>();
			}
		}

		private static void OnIdleThrottleUpdate(ulong sender, GameEventReader p)
		{
			int hash = p.ReadInt32();
			NetVehicle netVehicle = NetVehicleManager.vehicles.FirstOrDefault((NetVehicle v) => v.Hash == hash);
			if (netVehicle == null)
			{
				Console.LogError(string.Format("Received idle throttle update for vehicle of hash {0} but it doesn't exist", hash), false);
				return;
			}
			netVehicle.updatingLastIdleThrottle = true;
			netVehicle.Drivetrain.idlethrottle = p.ReadSingle();
		}

		public void SendEnterDrivingMode()
		{
			using (GameEventWriter gameEventWriter = NetVehicleManager.DrivingModeEvent.Writer())
			{
				gameEventWriter.Write(this.Hash);
				gameEventWriter.Write(true);
				this.Driver = (this.Owner = WreckMPGlobals.UserID);
				LocalPlayer.Instance.inCar = true;
				NetVehicleManager.DrivingModeEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
				if (this.Transform.name == "JONNEZ ES(Clone)")
				{
					NetRigidbodyManager.RequestOwnership(this.Rigidbody);
					for (int i = 0; i < this.allRBS.Length; i++)
					{
						NetRigidbodyManager.RequestOwnership(this.allRBS[i]);
					}
				}
			}
		}

		public void SendExitDrivingMode()
		{
			using (GameEventWriter gameEventWriter = NetVehicleManager.DrivingModeEvent.Writer())
			{
				gameEventWriter.Write(this.Hash);
				gameEventWriter.Write(false);
				this.Driver = 0UL;
				LocalPlayer.Instance.inCar = false;
				NetVehicleManager.DrivingModeEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		internal void DrivingMode(ulong player, bool enter)
		{
			Console.Log(string.Concat(new string[]
			{
				SteamFriends.GetFriendPersonaName((CSteamID)player),
				" ",
				enter ? "entered" : "exited",
				" ",
				this.Transform.name,
				" driving mode"
			}), false);
			this.Driver = (enter ? player : 0UL);
			if (enter)
			{
				this.Owner = player;
			}
			CoreManager.Players[player].SetInCar(enter, this);
		}

		internal void WriteAxisControllerUpdate(GameEventWriter p)
		{
			p.Write(this.Hash);
			p.Write(this.AxisCarController.brakeInput);
			p.Write(this.AxisCarController.throttleInput);
			p.Write(this.AxisCarController.steerInput);
			p.Write(this.AxisCarController.handbrakeInput);
			p.Write(this.AxisCarController.clutchInput);
		}

		internal void SetAxisController(float brake, float throttle, float steering, float handbrake, float clutch)
		{
			this.brakeInput = brake;
			this.throttleInput = throttle;
			this.steerInput = steering;
			this.handbrakeInput = handbrake;
			this.clutchInput = clutch;
		}

		internal void WriteDrivetrainUpdate(GameEventWriter p)
		{
			p.Write(this.Hash);
			p.Write(this.Drivetrain.rpm);
			p.Write(this.Drivetrain.gear);
		}

		internal void SetDrivetrain(float rpm, int gear)
		{
			this.Drivetrain.gear = gear;
		}

		internal void SendInitialSync(ulong target)
		{
			if (this.Driver == WreckMPGlobals.UserID)
			{
				this.SendEnterDrivingMode();
			}
			using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter(""))
			{
				gameEventWriter.Write(this.Hash);
				gameEventWriter.Write(this.Transform.position);
				gameEventWriter.Write(this.Transform.eulerAngles);
			}
		}

		internal void OnInitialSync(GameEventReader packet)
		{
			Vector3 vector = packet.ReadVector3();
			Vector3 vector2 = packet.ReadVector3();
			this.Transform.position = vector;
			this.Transform.eulerAngles = vector2;
		}

		internal void Update()
		{
			if (this.Owner != WreckMPGlobals.UserID && this.Owner != 0UL && this.AxisCarController != null)
			{
				this.AxisCarController.brakeInput = this.brakeInput;
				this.AxisCarController.throttleInput = this.throttleInput;
				this.AxisCarController.steerInput = this.steerInput;
				this.AxisCarController.handbrakeInput = this.handbrakeInput;
				this.AxisCarController.clutchInput = this.clutchInput;
			}
			for (int i = 0; i < this.headJoints.Count; i++)
			{
				if (!(this.headJoints[i] == null))
				{
					this.headJoints[i].breakForce = float.PositiveInfinity;
					this.headJoints[i].breakTorque = float.PositiveInfinity;
				}
			}
			if (NetVehicle.flatbedPassengerSeat != null)
			{
				NetVehicle.flatbedPassengerSeat.breakForce = float.PositiveInfinity;
				NetVehicle.flatbedPassengerSeat.breakTorque = float.PositiveInfinity;
			}
		}

		public Transform AddPassengerSeat(Vector3 triggerOffset, Vector3 headPivotOffset)
		{
			return NetVehicle.AddPassengerSeat(this, this.Rigidbody, this.Transform, triggerOffset, headPivotOffset);
		}

		internal static Transform AddPassengerSeat(NetVehicle self, Rigidbody rb, Transform parent, Vector3 triggerOffset, Vector3 headPivotOffset)
		{
			GameObject gameObject = GameObject.Find("NPC_CARS").transform.Find("Amikset/KYLAJANI/LOD/PlayerFunctions").gameObject;
			int seatIndex = 0;
			GameObject gameObject2 = Object.Instantiate<GameObject>(gameObject);
			if (self != null)
			{
				seatIndex = self.seatCount;
				self.seatsUsed.Add(0UL);
			}
			gameObject2.name = string.Format("MPPlayerFunctions_{0}", seatIndex);
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
			PlayMakerFSM dtFsm = gameObject2.transform.Find("PlayerTrigger/DriveTrigger").GetComponent<PlayMakerFSM>();
			dtFsm.name = "PassengerTrigger";
			dtFsm.transform.parent.name = "PlayerOffset";
			dtFsm.Initialize();
			dtFsm.GetComponent<CapsuleCollider>().radius = 0.2f;
			int hash = parent.GetGameobjectHashString().GetHashCode();
			if (self != null)
			{
				dtFsm.InsertAction("Press return", delegate
				{
					if (self.seatsUsed[seatIndex] != 0UL)
					{
						dtFsm.SendEvent("FINISHED");
					}
				}, 0, false);
			}
			if (self != null)
			{
				dtFsm.InsertAction("Reset view", delegate
				{
					using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter(""))
					{
						gameEventWriter.Write(hash);
						gameEventWriter.Write(seatIndex);
						gameEventWriter.Write(true);
						LocalPlayer.Instance.inCar = true;
						NetVehicleManager.PassengerModeEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
					}
				}, -1, false);
				dtFsm.InsertAction("Create player", delegate
				{
					using (GameEventWriter gameEventWriter2 = GameEvent.EmptyWriter(""))
					{
						gameEventWriter2.Write(hash);
						gameEventWriter2.Write(seatIndex);
						gameEventWriter2.Write(false);
						LocalPlayer.Instance.inCar = false;
						NetVehicleManager.PassengerModeEvent.Send(gameEventWriter2, 0UL, true, default(GameEvent.RecordingProperties));
					}
				}, -1, false);
			}
			(dtFsm.FsmStates.First((FsmState s) => s.Name == "Check speed").Actions[0] as GetVelocity).gameObject = new FsmOwnerDefault
			{
				GameObject = rb.transform.gameObject,
				OwnerOption = 1
			};
			(dtFsm.FsmStates.First((FsmState s) => s.Name == "Player in car").Actions[3] as SetStringValue).stringValue = "Passenger_" + rb.transform.name;
			gameObject2.SetActive(true);
			gameObject2.transform.GetChild(1).gameObject.SetActive(true);
			dtFsm.gameObject.SetActive(true);
			if (self != null)
			{
				self.seatCount++;
			}
			return gameObject2.transform;
		}

		private ulong _owner;

		public NetVehicleDriverPivots driverPivots;

		internal static Rigidbody FLATBED;

		internal static List<Rigidbody> haybales;

		private Rigidbody[] allRBS;

		private float brakeInput;

		private float throttleInput;

		private float steerInput;

		private float handbrakeInput;

		private float clutchInput;

		internal NetVehicleAudio audioController;

		private int seatCount;

		internal List<ulong> seatsUsed = new List<ulong>();

		private float lastIdleThrottle;

		private float lastIdleThrottleUpdate;

		private bool updatingLastIdleThrottle;

		private static GameEvent<NetVehicle> updateIdleThrottle;

		internal List<ConfigurableJoint> headJoints = new List<ConfigurableJoint>();

		private static ConfigurableJoint flatbedPassengerSeat;

		private int itemMask = LayerMask.GetMask(new string[] { "Parts" });
	}
}
