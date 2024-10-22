using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WreckMP
{
	public class NetVehicleManager : NetManager
	{
		public static void RegisterNetVehicle(NetVehicle netVeh)
		{
			NetVehicleManager.vehicles.Add(netVeh);
			Console.Log(string.Format("registered NetVehicle with hash {0} ({1})", netVeh.Hash, netVeh.Transform.name), false);
		}

		public static void CreateTowHookTrigger(GameObject pivot)
		{
			NetTowHookManager.CreateTowHookTrigger(pivot);
		}

		private void Start()
		{
			NetVehicleManager.vehicles.Clear();
			Func<string> func = delegate
			{
				NetVehicleManager.InitialSyncEvent = new GameEvent<NetVehicleManager>("InitialSync", new Action<ulong, GameEventReader>(this.OnInitialSync), GameScene.GAME);
				NetVehicleManager.SoundUpdateEvent = new GameEvent<NetVehicleManager>("SoundUpdate", new Action<ulong, GameEventReader>(this.OnSoundUpdate), GameScene.GAME);
				NetVehicleManager.DrivingModeEvent = new GameEvent<NetVehicleManager>("DrivingMode", new Action<ulong, GameEventReader>(this.OnDrivingMode), GameScene.GAME);
				NetVehicleManager.PassengerModeEvent = new GameEvent<NetVehicleManager>("PassengerMode", new Action<ulong, GameEventReader>(this.OnPassengerMode), GameScene.GAME);
				NetVehicleManager.FlatbedPassengerModeEvent = new GameEvent<NetVehicleManager>("FlatbedPassengerMode", new Action<ulong, GameEventReader>(this.OnFlatbedPassengerMode), GameScene.GAME);
				NetVehicleManager.InputUpdateEvent = new GameEvent<NetVehicleManager>("InputUpdate", new Action<ulong, GameEventReader>(this.OnInputUpdate), GameScene.GAME);
				NetVehicleManager.DrivetrainUpdateEvent = new GameEvent<NetVehicleManager>("DrivetrainUpdate", new Action<ulong, GameEventReader>(this.OnDrivetrainUpdate), GameScene.GAME);
				PlayMakerFSM[] array = (from x in Resources.FindObjectsOfTypeAll<PlayMakerFSM>()
					where x.FsmName == "GearIndicator"
					select x).ToArray<PlayMakerFSM>();
				for (int i = 0; i < array.Length; i++)
				{
					if (!(array[i].transform.GetComponent<Drivetrain>() == null))
					{
						FsmNetVehicle fsmNetVehicle = new FsmNetVehicle(array[i].transform);
						NetVehicleManager.vanillaVehicles.Add(fsmNetVehicle);
					}
				}
				FsmNetVehicle.DoFlatbedPassengerSeats(out this.flatbed, out this.flatbedHash, new Action<bool>(this.EnterFlatbedPassenger));
				WreckMPGlobals.OnMemberReady.Add(new Action<ulong>(this.OnMemberReady));
				WreckMPGlobals.OnMemberExit = (Action<ulong>)Delegate.Combine(WreckMPGlobals.OnMemberExit, new Action<ulong>(this.OnMemberExit));
				return "VehicleManager";
			};
			if (_ObjectsLoader.IsGameLoaded)
			{
				func();
				return;
			}
			_ObjectsLoader.gameLoaded.Add(func);
		}

		private void OnFlatbedPassengerMode(ulong sender, GameEventReader packet)
		{
			bool flag = packet.ReadBoolean();
			CoreManager.Players[sender].SetPassengerMode(flag, this.flatbed, false);
		}

		private void EnterFlatbedPassenger(bool enter)
		{
			using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter(""))
			{
				gameEventWriter.Write(enter);
				LocalPlayer.Instance.inCar = enter;
				NetVehicleManager.FlatbedPassengerModeEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnInitialSync(ulong sender, GameEventReader packet)
		{
			int hash = packet.ReadInt32();
			NetVehicle netVehicle = NetVehicleManager.vehicles.FirstOrDefault((NetVehicle x) => x.Hash == hash);
			if (netVehicle == null)
			{
				Console.LogError(string.Format("InitialSync: vehicle with hash {0} not found.", hash), true);
				return;
			}
			netVehicle.OnInitialSync(packet);
		}

		private void OnDrivetrainUpdate(ulong sender, GameEventReader packet)
		{
			while (packet.UnreadLength() > 0)
			{
				int hash = packet.ReadInt32();
				float num = packet.ReadSingle();
				int num2 = packet.ReadInt32();
				NetVehicle netVehicle = NetVehicleManager.vehicles.FirstOrDefault((NetVehicle x) => x.Hash == hash);
				if (netVehicle == null)
				{
					Console.LogError(string.Format("Received input update packet for unknown vehicle with hash {0}", hash), false);
					return;
				}
				if (netVehicle.Owner == sender && netVehicle.AxisCarController != null)
				{
					netVehicle.SetDrivetrain(num, num2);
				}
			}
		}

		private void OnMemberExit(ulong player)
		{
			for (int i = 0; i < NetVehicleManager.vehicles.Count; i++)
			{
				NetVehicle netVehicle = NetVehicleManager.vehicles[i];
				if (netVehicle != null && netVehicle.Driver == player)
				{
					netVehicle.Driver = 0UL;
					netVehicle.Owner = WreckMPGlobals.HostID;
					if (WreckMPGlobals.IsHost)
					{
						NetRigidbodyManager.RequestOwnership(netVehicle.Rigidbody);
					}
				}
				int num = 0;
				for (;;)
				{
					int num2 = num;
					int? num3 = ((netVehicle != null) ? new int?(netVehicle.seatsUsed.Count) : null);
					if (!((num2 < num3.GetValueOrDefault()) & (num3 != null)))
					{
						break;
					}
					if (netVehicle.seatsUsed[num] == player)
					{
						netVehicle.seatsUsed[num] = 0UL;
					}
					num++;
				}
			}
		}

		private void OnMemberReady(ulong player)
		{
			using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter(""))
			{
				for (int i = 0; i < NetVehicleManager.vehicles.Count; i++)
				{
					NetVehicle netVehicle = NetVehicleManager.vehicles[i];
					ulong? num = ((netVehicle != null) ? new ulong?(netVehicle.Owner) : null);
					ulong userID = WreckMPGlobals.UserID;
					if ((num.GetValueOrDefault() == userID) & (num != null))
					{
						if (netVehicle.audioController != null)
						{
							netVehicle.audioController.WriteUpdate(gameEventWriter, netVehicle.Hash, true);
						}
						netVehicle.SendInitialSync(player);
					}
				}
				NetVehicleManager.SoundUpdateEvent.Send(gameEventWriter, player, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnInputUpdate(ulong sender, GameEventReader packet)
		{
			while (packet.UnreadLength() > 0)
			{
				int hash = packet.ReadInt32();
				float num = packet.ReadSingle();
				float num2 = packet.ReadSingle();
				float num3 = packet.ReadSingle();
				float num4 = packet.ReadSingle();
				float num5 = packet.ReadSingle();
				NetVehicle netVehicle = NetVehicleManager.vehicles.FirstOrDefault((NetVehicle x) => x.Hash == hash);
				if (netVehicle == null)
				{
					Console.LogError(string.Format("Received input update packet for unknown vehicle with hash {0}", hash), false);
					return;
				}
				if (netVehicle.Owner == sender && netVehicle.AxisCarController != null)
				{
					netVehicle.SetAxisController(num, num2, num3, num4, num5);
				}
			}
		}

		private void LateUpdate()
		{
			using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter(""))
			{
				using (GameEventWriter gameEventWriter2 = GameEvent.EmptyWriter(""))
				{
					using (GameEventWriter gameEventWriter3 = GameEvent.EmptyWriter(""))
					{
						bool flag = false;
						bool flag2 = false;
						bool flag3 = false;
						for (int i = 0; i < NetVehicleManager.vehicles.Count; i++)
						{
							NetVehicle netVehicle = NetVehicleManager.vehicles[i];
							netVehicle.Update();
							if (netVehicle.audioController != null)
							{
								netVehicle.audioController.Update();
							}
							if (netVehicle.Owner == WreckMPGlobals.UserID)
							{
								if (netVehicle.audioController != null)
								{
									flag |= netVehicle.audioController.WriteUpdate(gameEventWriter, netVehicle.Hash, false);
								}
								if (netVehicle.AxisCarController != null)
								{
									flag2 = true;
									netVehicle.WriteAxisControllerUpdate(gameEventWriter2);
								}
								if (netVehicle.Drivetrain != null)
								{
									flag3 = true;
									netVehicle.WriteDrivetrainUpdate(gameEventWriter3);
								}
							}
						}
						if (flag)
						{
							NetVehicleManager.SoundUpdateEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
						}
						if (flag2)
						{
							NetVehicleManager.InputUpdateEvent.Send(gameEventWriter2, 0UL, true, default(GameEvent.RecordingProperties));
						}
						if (flag3)
						{
							NetVehicleManager.DrivetrainUpdateEvent.Send(gameEventWriter3, 0UL, true, default(GameEvent.RecordingProperties));
						}
						for (int j = 0; j < NetVehicleManager.vanillaVehicles.Count; j++)
						{
							for (int k = 0; k < NetVehicleManager.vanillaVehicles[j].dashboardLevers.Count; k++)
							{
								NetVehicleManager.vanillaVehicles[j].dashboardLevers[k].Update();
							}
						}
					}
				}
			}
		}

		private void FixedUpdate()
		{
			for (int i = 0; i < NetVehicleManager.vanillaVehicles.Count; i++)
			{
				for (int j = 0; j < NetVehicleManager.vanillaVehicles[i].vehicleDoors.Count; j++)
				{
					NetVehicleManager.vanillaVehicles[i].vehicleDoors[j].FixedUpdate();
				}
			}
		}

		private void OnPassengerMode(ulong sender, GameEventReader packet)
		{
			int hash = packet.ReadInt32();
			int num = packet.ReadInt32();
			bool flag = packet.ReadBoolean();
			if (hash == this.flatbedHash)
			{
				CoreManager.Players[sender].SetPassengerMode(flag, this.flatbed, false);
				return;
			}
			NetVehicle netVehicle = NetVehicleManager.vehicles.FirstOrDefault((NetVehicle x) => x.Hash == hash);
			if (netVehicle == null)
			{
				Console.LogError(string.Format("Received passenger mode packet for unknown vehicle with hash {0}", hash), false);
				return;
			}
			netVehicle.seatsUsed[num] = (flag ? sender : 0UL);
			CoreManager.Players[sender].SetPassengerMode(flag, netVehicle.Transform, true);
		}

		private void OnDrivingMode(ulong sender, GameEventReader packet)
		{
			int hash = packet.ReadInt32();
			bool flag = packet.ReadBoolean();
			NetVehicle netVehicle = NetVehicleManager.vehicles.FirstOrDefault((NetVehicle x) => x.Hash == hash);
			if (netVehicle == null)
			{
				Console.LogError(string.Format("Received driving mode packet for unknown vehicle with hash {0}", hash), false);
				return;
			}
			netVehicle.DrivingMode(sender, flag);
		}

		private void OnSoundUpdate(ulong sender, GameEventReader packet)
		{
			while (packet.UnreadLength() > 0)
			{
				if (packet.ReadByte() != 7)
				{
					this.SoundUpdateReadError(0);
					return;
				}
				int hash = packet.ReadInt32();
				NetVehicle netVehicle = NetVehicleManager.vehicles.FirstOrDefault((NetVehicle x) => x.Hash == hash);
				if (netVehicle == null)
				{
					Console.LogError(string.Format("OnSoundUpdate can't find car with hash {0}", hash), false);
					return;
				}
				byte b = packet.ReadByte();
				while (b == 15)
				{
					int num = packet.ReadInt32();
					bool? flag = null;
					float? num2 = null;
					float? num3 = null;
					float? num4 = null;
					byte b2 = packet.ReadByte();
					if (b2 == 31)
					{
						flag = new bool?(packet.ReadBoolean());
						if (flag.Value)
						{
							num4 = new float?(packet.ReadSingle());
						}
						b2 = packet.ReadByte();
					}
					if (b2 == 47)
					{
						num2 = new float?(packet.ReadSingle());
						b2 = packet.ReadByte();
					}
					if (b2 == 63)
					{
						num3 = new float?(packet.ReadSingle());
						b2 = packet.ReadByte();
					}
					if (b2 != 255)
					{
						this.SoundUpdateReadError(1);
						return;
					}
					b = packet.ReadByte();
					netVehicle.audioController.sources[num].OnUpdate(flag, num4, num2, num3);
				}
				if (b != 247)
				{
					this.SoundUpdateReadError(2);
					return;
				}
			}
		}

		private void SoundUpdateReadError(int code)
		{
			Console.LogError("Error code " + code.ToString() + " when reading OnSoundUpdate packet", false);
		}

		internal static List<NetVehicle> vehicles = new List<NetVehicle>();

		internal static List<FsmNetVehicle> vanillaVehicles = new List<FsmNetVehicle>();

		internal static GameEvent<NetVehicleManager> InitialSyncEvent;

		internal static GameEvent<NetVehicleManager> SoundUpdateEvent;

		internal static GameEvent<NetVehicleManager> DrivingModeEvent;

		internal static GameEvent<NetVehicleManager> PassengerModeEvent;

		internal static GameEvent<NetVehicleManager> FlatbedPassengerModeEvent;

		internal static GameEvent<NetVehicleManager> InputUpdateEvent;

		internal static GameEvent<NetVehicleManager> DrivetrainUpdateEvent;

		private Transform flatbed;

		internal int flatbedHash;
	}
}
