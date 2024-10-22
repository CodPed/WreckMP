using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	internal class NetNpcManager : NetManager
	{
		private void Start()
		{
			_ObjectsLoader.gameLoaded.Add(delegate
			{
				Transform transform = GameObject.Find("HUMANS/Randomizer/Walkers").transform;
				for (int i = 0; i < transform.childCount; i++)
				{
					Transform child = transform.GetChild(i);
					int hashCode = child.GetGameobjectHashString().GetHashCode();
					NetNpcManager.walkers.Add(hashCode, child);
				}
				Transform transform2 = GameObject.Find("KILJUGUY").transform.Find("HikerPivot");
				NetNpcManager.walkers.Add(transform2.GetGameobjectHashString().GetHashCode(), transform2);
				this.drunkHiker2Fsm = transform2.Find("JokkeHiker2").GetPlayMaker("Logic");
				this.drunkHiker2Fsm.Initialize();
				this.drunkHiker2CarEvents = new FsmEvent[NetNpcManager.jokkeHiker_eventNames.Length];
				for (int j = 0; j < NetNpcManager.jokkeHiker_eventNames.Length; j++)
				{
					this.drunkHiker2CarEvents[j] = this.drunkHiker2Fsm.AddEvent(NetNpcManager.jokkeHiker_eventNames[j]);
					this.drunkHiker2Fsm.AddGlobalTransition(this.drunkHiker2CarEvents[j], NetNpcManager.jokkeHiker_stateNames[j]);
					int _i = j;
					this.drunkHiker2Fsm.InsertAction(NetNpcManager.jokkeHiker_stateNames[j], delegate
					{
						this.JokkeEnterCar(_i);
					}, 0, false);
				}
				this.drunkHiker2EnterCar = new GameEvent<NetNpcManager>("DH2Car", new Action<ulong, GameEventReader>(this.OnJokkeEnterCar), GameScene.GAME);
				Transform transform3 = GameObject.Find("TRAFFIC").transform;
				this.vehiclesHighway = transform3.Find("VehiclesHighway").gameObject;
				this.policeFsm = transform3.Find("Police").GetComponent<PlayMakerFSM>();
				if (WreckMPGlobals.IsHost)
				{
					this.policeFsm.InsertAction("State 3", delegate
					{
						this.PoliceSpawn(0);
					}, 0, false);
					this.policeFsm.InsertAction("State 4", delegate
					{
						this.PoliceSpawn(1);
					}, 0, false);
					this.policeFsm.InsertAction("State 5", delegate
					{
						this.PoliceSpawn(2);
					}, 0, false);
					this.policeFsm.InsertAction("State 6", delegate
					{
						this.PoliceSpawn(3);
					}, 0, false);
				}
				else
				{
					this.policeEvents = new FsmEvent[]
					{
						this.policeFsm.AddEvent("MP_SPAWN0"),
						this.policeFsm.AddEvent("MP_SPAWN1"),
						this.policeFsm.AddEvent("MP_SPAWN2"),
						this.policeFsm.AddEvent("MP_SPAWN3")
					};
					this.policeFsm.AddGlobalTransition(this.policeEvents[0], "State 3");
					this.policeFsm.AddGlobalTransition(this.policeEvents[1], "State 4");
					this.policeFsm.AddGlobalTransition(this.policeEvents[2], "State 5");
					this.policeFsm.AddGlobalTransition(this.policeEvents[3], "State 6");
					this.policeFsm.GetState("Cop1").Actions[2].Enabled = false;
					this.policeFsm.GetState("State 1").Actions[0].Enabled = false;
				}
				this.walkerSync = new GameEvent("Walk", new Action<GameEventReader>(this.OnWalkerNPCSync), GameScene.GAME);
				this.highwayUpdateEvent = new GameEvent<NetNpcManager>("HighwayUpdate", new Action<ulong, GameEventReader>(this.OnHighwayUpdate), GameScene.GAME);
				this.policeUpdateEvent = new GameEvent<NetNpcManager>("PoliceUpdate", new Action<ulong, GameEventReader>(this.OnPoliceUpdate), GameScene.GAME);
				return "NetNpcManager";
			});
		}

		private void OnJokkeEnterCar(ulong s, GameEventReader p)
		{
			int num = (int)p.ReadByte();
			this.receivedDrunkHiker2 = num;
			this.drunkHiker2Fsm.Fsm.Event(this.drunkHiker2CarEvents[num]);
		}

		private void JokkeEnterCar(int index)
		{
			if (this.receivedDrunkHiker2 != -1)
			{
				this.receivedDrunkHiker2 = -1;
				return;
			}
			using (GameEventWriter gameEventWriter = this.drunkHiker2EnterCar.Writer())
			{
				gameEventWriter.Write((byte)index);
				this.drunkHiker2EnterCar.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnPoliceUpdate(ulong s, GameEventReader p)
		{
			int num = (int)p.ReadByte();
			this.policeFsm.Fsm.Event(this.policeEvents[num]);
		}

		private void PoliceSpawn(int index)
		{
			using (GameEventWriter gameEventWriter = this.policeUpdateEvent.Writer())
			{
				gameEventWriter.Write((byte)index);
				this.policeUpdateEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnHighwayUpdate(ulong s, GameEventReader p)
		{
			bool flag = p.ReadBoolean();
			this.highwayOn = flag;
		}

		private void CheckHighway()
		{
			if (WreckMPGlobals.IsHost)
			{
				if (this.highwayOn == this.vehiclesHighway.activeSelf)
				{
					return;
				}
				this.highwayOn = this.vehiclesHighway.activeSelf;
				using (GameEventWriter gameEventWriter = this.highwayUpdateEvent.Writer())
				{
					gameEventWriter.Write(this.highwayOn);
					this.highwayUpdateEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
					return;
				}
			}
			if (this.highwayOn != this.vehiclesHighway.activeSelf)
			{
				this.vehiclesHighway.SetActive(this.highwayOn);
			}
		}

		private void OnWalkerNPCSync(GameEventReader p)
		{
			while (p.UnreadLength() > 0)
			{
				int num = p.ReadInt32();
				Vector3 vector = p.ReadVector3();
				Vector3 vector2 = p.ReadVector3();
				if (NetNpcManager.walkers.ContainsKey(num))
				{
					NetNpcManager.walkers[num].position = vector;
					NetNpcManager.walkers[num].eulerAngles = vector2;
				}
			}
		}

		private void UpdateWalkers()
		{
			if (!WreckMPGlobals.IsHost || NetNpcManager.walkers == null)
			{
				return;
			}
			using (GameEventWriter gameEventWriter = this.walkerSync.Writer())
			{
				foreach (KeyValuePair<int, Transform> keyValuePair in NetNpcManager.walkers)
				{
					if (!(keyValuePair.Value == null))
					{
						gameEventWriter.Write(keyValuePair.Key);
						gameEventWriter.Write(keyValuePair.Value.position);
						gameEventWriter.Write(keyValuePair.Value.eulerAngles);
					}
				}
				this.walkerSync.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void Update()
		{
			if (!_ObjectsLoader.IsGameLoaded)
			{
				return;
			}
			this.UpdateWalkers();
			this.CheckHighway();
		}

		private static Dictionary<int, Transform> walkers = new Dictionary<int, Transform>();

		private GameObject vehiclesHighway;

		private PlayMakerFSM policeFsm;

		private PlayMakerFSM drunkHiker2Fsm;

		private FsmEvent[] policeEvents;

		private FsmEvent[] drunkHiker2CarEvents;

		private bool highwayOn;

		private int policeIndex = -1;

		private int receivedDrunkHiker2 = -1;

		private static readonly string[] jokkeHiker_eventNames = new string[] { "MP_SATSUMA", "MP_MUSCLE", "MP_VAN", "MP_RUSCKO" };

		private static readonly string[] jokkeHiker_stateNames = new string[] { "Satsuma", "Muscle", "Van", "Ruscko" };

		private GameEvent walkerSync;

		private GameEvent highwayUpdateEvent;

		private GameEvent policeUpdateEvent;

		private GameEvent drunkHiker2EnterCar;
	}
}
