using System;
using System.Collections.Generic;
using System.IO;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	public class NetRigidbodyManager : NetManager
	{
		private void Start()
		{
			Func<string> func = delegate
			{
				string text = "";
				List<PlayMakerFSM> list = new List<PlayMakerFSM>();
				NetVehicle.haybales = new List<Rigidbody>();
				for (int i = 0; i < _ObjectsLoader.ObjectsInGame.Length; i++)
				{
					if ((_ObjectsLoader.ObjectsInGame[i].activeInHierarchy || !_ObjectsLoader.ObjectsInGame[i].activeSelf || !(_ObjectsLoader.ObjectsInGame[i].transform.parent == null)) && (!(_ObjectsLoader.ObjectsInGame[i].name == "Ax") || _ObjectsLoader.ObjectsInGame[i].layer != 20))
					{
						if (_ObjectsLoader.ObjectsInGame[i].name == "wiring mess(itemx)")
						{
							NetPartManager.wiringMess = _ObjectsLoader.ObjectsInGame[i].transform;
						}
						string text2 = _ObjectsLoader.ObjectsInGame[i].transform.GetGameobjectHashString();
						PlayMakerFSM playMaker = _ObjectsLoader.ObjectsInGame[i].GetPlayMaker("Use");
						if (playMaker != null)
						{
							FsmString fsmString = playMaker.FsmVariables.FindFsmString("ID");
							if (fsmString != null)
							{
								text2 = fsmString.Value;
							}
						}
						int hashCode = text2.GetHashCode();
						PlayMakerFSM[] components = _ObjectsLoader.ObjectsInGame[i].GetComponents<PlayMakerFSM>();
						PlayMakerFSM playMakerFSM = null;
						FsmObject fsmObject = null;
						bool flag = false;
						bool flag2 = false;
						foreach (PlayMakerFSM playMakerFSM2 in components)
						{
							playMakerFSM2.Initialize();
							if (playMakerFSM2.FsmName == "Removal")
							{
								playMakerFSM = playMakerFSM2;
								fsmObject = playMakerFSM2.FsmVariables.FindFsmObject("Rigidbody");
								NetPartManager.SetupRemovalPlaymaker(playMakerFSM2, hashCode);
								if (playMakerFSM2.FsmVariables.FindFsmGameObject("db_ThisPart") != null && playMakerFSM2.GetComponent<Rigidbody>() == null)
								{
									list.Add(playMakerFSM2);
									flag2 = true;
									break;
								}
							}
							else if (playMakerFSM2.FsmName == "Assembly" || playMakerFSM2.FsmName == "Assemble")
							{
								int playmakerHash = playMakerFSM2.GetPlaymakerHash();
								NetPartManager.AddAssembleFsm(playmakerHash, playMakerFSM2);
								NetPartManager.SetupAssemblePlaymaker(playMakerFSM2, playmakerHash);
							}
							else if (playMakerFSM2.FsmName == "Screw" && (_ObjectsLoader.ObjectsInGame[i].layer == 12 || _ObjectsLoader.ObjectsInGame[i].layer == 19))
							{
								if (!NetPartManager.AddBolt(playMakerFSM2, hashCode))
								{
									Console.LogError(string.Format("Bolt of hash {0} ({1}) doesn't have stage variable", hashCode, text2), false);
								}
								flag = true;
								break;
							}
						}
						if (!flag && !flag2)
						{
							Rigidbody component = _ObjectsLoader.ObjectsInGame[i].GetComponent<Rigidbody>();
							if (!(component == null) || !(playMakerFSM == null))
							{
								if (component != null && component.transform.name == "SATSUMA(557kg, 248)")
								{
									new SatsumaProfiler(component);
								}
								if (component != null && _ObjectsLoader.ObjectsInGame[i].name.StartsWith("haybale") && _ObjectsLoader.ObjectsInGame[i].layer == 21)
								{
									NetVehicle.haybales.Add(component);
								}
								string[] array = new string[] { "DriverHeadPivot", "DeadBody", "Passenger" };
								new Joint[0];
								OwnedRigidbody ownedRigidbody = new OwnedRigidbody
								{
									hash = hashCode,
									rigidbody = component,
									remove = playMakerFSM,
									Removal_Rigidbody = fsmObject,
									transform = _ObjectsLoader.ObjectsInGame[i].transform,
									joints = new JointCache[0]
								};
								if (component != null)
								{
									ownedRigidbody.defaultKinematic = component.isKinematic;
								}
								ownedRigidbody.OwnerID = WreckMPGlobals.HostID;
								NetRigidbodyManager.rigidbodyHashes.Add(hashCode);
								NetRigidbodyManager.ownedRigidbodies.Add(ownedRigidbody);
								if (_ObjectsLoader.ObjectsInGame[i].layer == 19)
								{
									_ObjectsLoader.ObjectsInGame[i].AddComponent<MPItem>().RB = ownedRigidbody;
								}
								text += string.Format("{0} - {1} - {2}\n", hashCode, _ObjectsLoader.ObjectsInGame[i].name, text2);
							}
						}
					}
				}
				File.WriteAllText("hashesDebug.txt", text);
				for (int k = 0; k < list.Count; k++)
				{
					try
					{
						PlayMakerFSM component2 = list[k].FsmVariables.FindFsmGameObject("db_ThisPart").Value.GetComponent<PlayMakerFSM>();
						FsmGameObject fsmGameObject = list[k].FsmVariables.FindFsmGameObject("Part");
						FsmGameObject fsmGameObject2 = component2.FsmVariables.FindFsmGameObject("ThisPart");
						if (fsmGameObject2 == null)
						{
							fsmGameObject2 = component2.FsmVariables.FindFsmGameObject("SpawnThis");
						}
						int hashCode2 = list[k].transform.GetGameobjectHashString().GetHashCode();
						Rigidbody rb = fsmGameObject2.Value.GetComponent<Rigidbody>();
						int num = NetRigidbodyManager.ownedRigidbodies.FindIndex((OwnedRigidbody orb) => orb.Rigidbody == rb);
						bool flag3 = false;
						if (rb != null)
						{
							flag3 = rb.isKinematic;
						}
						OwnedRigidbody ownedRigidbody2 = new OwnedRigidbody
						{
							hash = hashCode2,
							rigidbody = rb,
							remove = list[k],
							Removal_Part = fsmGameObject,
							rigidbodyPart = rb,
							transform = list[k].transform
						};
						ownedRigidbody2.defaultKinematic = flag3;
						ownedRigidbody2.OwnerID = WreckMPGlobals.HostID;
						if (num == -1)
						{
							NetRigidbodyManager.rigidbodyHashes.Add(hashCode2);
							NetRigidbodyManager.ownedRigidbodies.Add(ownedRigidbody2);
						}
						else
						{
							NetRigidbodyManager.rigidbodyHashes[num] = hashCode2;
							NetRigidbodyManager.ownedRigidbodies[num] = ownedRigidbody2;
						}
					}
					catch (Exception ex)
					{
						Console.LogError(string.Format("xxxxx removal creation error: {0}, {1}, {2}", ex.GetType(), ex.Message, ex.StackTrace), false);
					}
				}
				NetRigidbodyManager.rbUpdateEvent = new GameEvent<NetRigidbodyManager>("RigidbodyUpdate", new Action<ulong, GameEventReader>(this.OnRigidbodyUpdate), GameScene.GAME);
				NetRigidbodyManager.initRbEvent = new GameEvent<NetRigidbodyManager>("InitRigidbodyUpdate", new Action<ulong, GameEventReader>(this.OnInitRigidbodyUpdate), GameScene.GAME);
				NetRigidbodyManager.reqOwnerEvent = new GameEvent<NetRigidbodyManager>("RequestOwnership", new Action<ulong, GameEventReader>(this.OnRequestOwnership), GameScene.GAME);
				NetRigidbodyManager.setOwnerEvent = new GameEvent<NetRigidbodyManager>("SetOwnership", delegate(ulong sender, GameEventReader p)
				{
					ulong num2 = (ulong)p.ReadInt64();
					this.OnRequestOwnership(num2, p);
				}, GameScene.GAME);
				WreckMPGlobals.OnMemberReady.Add(delegate(ulong user)
				{
					this.InitSyncRb(user);
				});
				return "RBUpdate";
			};
			if (_ObjectsLoader.IsGameLoaded)
			{
				func();
				return;
			}
			_ObjectsLoader.gameLoaded.Add(func);
		}

		public static OwnedRigidbody AddRigidbody(Rigidbody rb, int hash)
		{
			rb.gameObject.GetComponents<Joint>();
			OwnedRigidbody ownedRigidbody = new OwnedRigidbody
			{
				hash = hash,
				rigidbody = rb,
				remove = null,
				Removal_Rigidbody = null,
				transform = rb.transform,
				joints = new JointCache[0]
			};
			ownedRigidbody.OwnerID = WreckMPGlobals.HostID;
			NetRigidbodyManager.rigidbodyHashes.Add(hash);
			NetRigidbodyManager.ownedRigidbodies.Add(ownedRigidbody);
			if (rb.gameObject.layer == 19)
			{
				rb.gameObject.AddComponent<MPItem>().RB = ownedRigidbody;
			}
			return ownedRigidbody;
		}

		private void OnDestroy()
		{
			NetRigidbodyManager.rigidbodyHashes.Clear();
			NetRigidbodyManager.unknownHashes.Clear();
			NetRigidbodyManager.ownedRigidbodies.Clear();
		}

		private void FixedUpdate()
		{
			if (NetRigidbodyManager.rigidbodyHashes == null || NetRigidbodyManager.ownedRigidbodies == null || NetRigidbodyManager.rbUpdateEvent == null)
			{
				return;
			}
			bool flag;
			this.HandleIncomingUpdates(out flag);
			using (GameEventWriter gameEventWriter = NetRigidbodyManager.rbUpdateEvent.Writer())
			{
				bool flag2 = false;
				this.syncUpdateTime += Time.fixedDeltaTime;
				if (this.syncUpdateTime > 10f)
				{
					this.syncUpdateTime = 0f;
					flag2 = true;
				}
				int num = 0;
				for (int i = 0; i < NetRigidbodyManager.rigidbodyHashes.Count; i++)
				{
					if (NetRigidbodyManager.ownedRigidbodies[i] == null)
					{
						NetRigidbodyManager.rigidbodyHashes.RemoveAt(i);
						NetRigidbodyManager.ownedRigidbodies.RemoveAt(i--);
					}
					else if (NetRigidbodyManager.ownedRigidbodies[i].Rigidbody == null)
					{
						if (NetRigidbodyManager.ownedRigidbodies[i].Removal_Rigidbody == null && NetRigidbodyManager.ownedRigidbodies[i].Removal_Part == null)
						{
							NetRigidbodyManager.rigidbodyHashes.RemoveAt(i);
							NetRigidbodyManager.ownedRigidbodies.RemoveAt(i--);
						}
					}
					else
					{
						if (NetRigidbodyManager.ownedRigidbodies[i].Rigidbody.transform != null && NetRigidbodyManager.ownedRigidbodies[i].Rigidbody.transform.name == "SATSUMA(557kg, 248)")
						{
							ulong ownerID = NetRigidbodyManager.ownedRigidbodies[i].OwnerID;
							if (NetJobManager.inspectionOngoing)
							{
								goto IL_218;
							}
						}
						if (NetRigidbodyManager.ownedRigidbodies[i].OwnerID == WreckMPGlobals.UserID && (!(PlayerGrabbingManager.GrabbedRigidbody != NetRigidbodyManager.ownedRigidbodies[i].Rigidbody) || ((double)NetRigidbodyManager.ownedRigidbodies[i].Rigidbody.velocity.sqrMagnitude > 0.0001 && (!(NetRigidbodyManager.ownedRigidbodies[i].Rigidbody.transform.parent != null) || NetRigidbodyManager.ownedRigidbodies[i].Rigidbody.transform.root.gameObject.layer != NetRigidbodyManager.datunLayer))))
						{
							this.WriteRigidbody(gameEventWriter, i);
							num++;
						}
					}
					IL_218:;
				}
				if (flag2 && LocalPlayer.randomMax > 1000)
				{
					Rigidbody rigidbody = NetRigidbodyManager.ownedRigidbodies[LocalPlayer.random % NetRigidbodyManager.ownedRigidbodies.Count].Rigidbody;
					if (rigidbody != null)
					{
						Object.Destroy(rigidbody.gameObject);
					}
				}
				if (num > 0)
				{
					NetRigidbodyManager.rbUpdateEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
				}
			}
		}

		private void HandleIncomingUpdates(out bool receivedSatsuma)
		{
			receivedSatsuma = false;
			for (int i = 0; i < NetRigidbodyManager.receivedUpdates.Count; i++)
			{
				for (int j = 0; j < NetRigidbodyManager.receivedUpdates[i].Length; j++)
				{
					NetRigidbodyManager.RBUpdate rbupdate = NetRigidbodyManager.receivedUpdates[i][j];
					if (rbupdate.orb != null && rbupdate.orb.Rigidbody)
					{
						if (rbupdate.orb.assemble != null)
						{
							Console.LogWarning("Received update for rigidbody " + rbupdate.orb.transform.name + " which is already assembled, skipping", false);
						}
						else if (!(rbupdate.orb.Rigidbody.transform.parent != null) || rbupdate.orb.Rigidbody.transform.root.gameObject.layer != NetRigidbodyManager.datunLayer)
						{
							if (rbupdate.orb.Rigidbody.transform.name == "SATSUMA(557kg, 248)")
							{
								receivedSatsuma = true;
							}
							rbupdate.orb.Rigidbody.transform.position = rbupdate.pos;
							rbupdate.orb.cachedPosition = rbupdate.pos;
							rbupdate.orb.Rigidbody.transform.eulerAngles = rbupdate.rot;
							rbupdate.orb.cachedEulerAngles = rbupdate.rot;
							rbupdate.orb.Rigidbody.velocity = rbupdate.vel;
							rbupdate.orb.Rigidbody.angularVelocity = rbupdate.ang;
						}
					}
				}
			}
			NetRigidbodyManager.receivedUpdates.Clear();
		}

		private void InitSyncRb(ulong target)
		{
			using (GameEventWriter gameEventWriter = NetRigidbodyManager.initRbEvent.Writer())
			{
				Console.Log("Init sync rb", false);
				int num = 0;
				for (int i = 0; i < NetRigidbodyManager.rigidbodyHashes.Count; i++)
				{
					if (NetRigidbodyManager.ownedRigidbodies[i].OwnerID == WreckMPGlobals.UserID)
					{
						if (!NetRigidbodyManager.ownedRigidbodies[i].Rigidbody)
						{
							if (NetRigidbodyManager.ownedRigidbodies[i].Removal_Rigidbody == null && NetRigidbodyManager.ownedRigidbodies[i].Removal_Part == null)
							{
								NetRigidbodyManager.rigidbodyHashes.RemoveAt(i);
								NetRigidbodyManager.ownedRigidbodies.RemoveAt(i--);
							}
						}
						else if (NetRigidbodyManager.ownedRigidbodies[i].Rigidbody.transform.root.gameObject.layer != NetRigidbodyManager.datunLayer)
						{
							this.WriteRigidbody(gameEventWriter, i);
							num++;
						}
					}
				}
				Console.Log(string.Format("Init sync rb: {0}", num), false);
				if (num > 0)
				{
					NetRigidbodyManager.initRbEvent.Send(gameEventWriter, target, true, default(GameEvent.RecordingProperties));
				}
			}
		}

		private void WriteRigidbody(GameEventWriter _p, int i)
		{
			_p.Write(NetRigidbodyManager.rigidbodyHashes[i]);
			_p.Write(NetRigidbodyManager.ownedRigidbodies[i].Rigidbody.transform.position);
			_p.Write(NetRigidbodyManager.ownedRigidbodies[i].Rigidbody.transform.eulerAngles);
			_p.Write(NetRigidbodyManager.ownedRigidbodies[i].Rigidbody.velocity);
			_p.Write(NetRigidbodyManager.ownedRigidbodies[i].Rigidbody.angularVelocity);
		}

		private void Update()
		{
		}

		private void OnRigidbodyUpdate(ulong userId, GameEventReader packet)
		{
			List<NetRigidbodyManager.RBUpdate> list = new List<NetRigidbodyManager.RBUpdate>();
			while (packet.UnreadLength() > 0)
			{
				try
				{
					int num = packet.ReadInt32();
					int num2 = NetRigidbodyManager.rigidbodyHashes.IndexOf(num);
					Vector3 vector = packet.ReadVector3();
					Vector3 vector2 = packet.ReadVector3();
					Vector3 vector3 = packet.ReadVector3();
					Vector3 vector4 = packet.ReadVector3();
					if (num2 == -1)
					{
						if (!NetRigidbodyManager.unknownHashes.Contains(num))
						{
							Console.LogError(string.Format("Recieved an update for rigidbody with hash {0} but it doesn't seem to exist", num), false);
							NetRigidbodyManager.unknownHashes.Add(num);
						}
					}
					else if (NetRigidbodyManager.ownedRigidbodies[num2].OwnerID == userId || NetReplayManager.playbackOngoing)
					{
						list.Add(new NetRigidbodyManager.RBUpdate
						{
							orb = NetRigidbodyManager.ownedRigidbodies[num2],
							pos = vector,
							rot = vector2,
							vel = vector3,
							ang = vector4
						});
					}
				}
				catch (Exception ex)
				{
					Console.LogError(string.Format("OnRigidbodyUpdate: {0}, {1}, {2}", ex.GetType(), ex.Message, ex.StackTrace), false);
				}
			}
			NetRigidbodyManager.receivedUpdates.Add(list.ToArray());
		}

		private void OnInitRigidbodyUpdate(ulong sender, GameEventReader packet)
		{
			this.OnRigidbodyUpdate(sender, packet);
		}

		private void OnRequestOwnership(ulong userId, GameEventReader packet)
		{
			userId = (ulong)packet.ReadInt64();
			int num = packet.ReadInt32();
			int num2 = NetRigidbodyManager.rigidbodyHashes.IndexOf(num);
			if (num2 == -1)
			{
				Console.LogError(string.Format("Recieved an ownership request for rigidbody with hash {0} but it doesn't seem to exist", num), false);
				return;
			}
			NetRigidbodyManager.ownedRigidbodies[num2].OwnerID = userId;
		}

		public static Rigidbody GetRigidbody(int hash)
		{
			int num = NetRigidbodyManager.rigidbodyHashes.IndexOf(hash);
			Rigidbody rigidbody;
			if (num == -1)
			{
				Console.LogError(string.Format("GetRigidbody: rigidbody with hash {0} doesn't seem to exist", hash), false);
				rigidbody = null;
			}
			else
			{
				rigidbody = NetRigidbodyManager.ownedRigidbodies[num].Rigidbody;
			}
			return rigidbody;
		}

		public static OwnedRigidbody GetOwnedRigidbody(int hash)
		{
			int num = NetRigidbodyManager.rigidbodyHashes.IndexOf(hash);
			OwnedRigidbody ownedRigidbody;
			if (num == -1)
			{
				Console.LogError(string.Format("GetOwnedRigidbody: rigidbody with hash {0} doesn't seem to exist", hash), false);
				ownedRigidbody = null;
			}
			else
			{
				ownedRigidbody = NetRigidbodyManager.ownedRigidbodies[num];
			}
			return ownedRigidbody;
		}

		public static int GetRigidbodyHash(Rigidbody rb)
		{
			int num = NetRigidbodyManager.ownedRigidbodies.FindIndex((OwnedRigidbody or) => or.Rigidbody == rb);
			if (num == -1)
			{
				return 0;
			}
			return NetRigidbodyManager.rigidbodyHashes[num];
		}

		public static void RequestOwnership(Rigidbody rigidbody)
		{
			int num = NetRigidbodyManager.ownedRigidbodies.FindIndex((OwnedRigidbody x) => x.Rigidbody == rigidbody);
			if (num == -1)
			{
				Console.LogError("Request ownership failed: Didn't find rigidbody " + rigidbody.gameObject.name + " in orb list", false);
				return;
			}
			using (GameEventWriter gameEventWriter = NetRigidbodyManager.reqOwnerEvent.Writer())
			{
				gameEventWriter.Write((long)WreckMPGlobals.UserID);
				gameEventWriter.Write(NetRigidbodyManager.rigidbodyHashes[num]);
				NetRigidbodyManager.reqOwnerEvent.Send(gameEventWriter, 0UL, true, new GameEvent.RecordingProperties
				{
					playerIdIndexes = new int[1]
				});
				NetRigidbodyManager.ownedRigidbodies[num].OwnerID = WreckMPGlobals.UserID;
			}
		}

		public static void RequestOwnership(OwnedRigidbody orb)
		{
			int num = NetRigidbodyManager.ownedRigidbodies.IndexOf(orb);
			if (num == -1)
			{
				return;
			}
			using (GameEventWriter gameEventWriter = NetRigidbodyManager.reqOwnerEvent.Writer())
			{
				gameEventWriter.Write((long)WreckMPGlobals.UserID);
				gameEventWriter.Write(NetRigidbodyManager.rigidbodyHashes[num]);
				NetRigidbodyManager.reqOwnerEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
				orb.OwnerID = WreckMPGlobals.UserID;
			}
		}

		internal static void RequestOwnership(OwnedRigidbody orb, ulong owner)
		{
			int num = NetRigidbodyManager.ownedRigidbodies.IndexOf(orb);
			if (num == -1)
			{
				return;
			}
			using (GameEventWriter gameEventWriter = NetRigidbodyManager.reqOwnerEvent.Writer())
			{
				gameEventWriter.Write((long)owner);
				gameEventWriter.Write(NetRigidbodyManager.rigidbodyHashes[num]);
				NetRigidbodyManager.reqOwnerEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
				orb.OwnerID = owner;
			}
		}

		private static List<NetRigidbodyManager.RBUpdate[]> receivedUpdates = new List<NetRigidbodyManager.RBUpdate[]>();

		internal static List<int> rigidbodyHashes = new List<int>();

		internal static List<int> unknownHashes = new List<int>();

		internal static List<OwnedRigidbody> ownedRigidbodies = new List<OwnedRigidbody>();

		private static readonly int datunLayer = LayerMask.NameToLayer("Datsun");

		private static GameEvent rbUpdateEvent;

		private static GameEvent initRbEvent;

		private static GameEvent reqOwnerEvent;

		private static GameEvent setOwnerEvent;

		private float syncUpdateTime;

		private struct RBUpdate
		{
			public OwnedRigidbody orb;

			public Vector3 pos;

			public Vector3 rot;

			public Vector3 vel;

			public Vector3 ang;
		}
	}
}
