using System;
using System.Collections.Generic;
using UnityEngine;

namespace WreckMP
{
	internal class NetTowHookManager : NetManager
	{
		internal static void CreateTowHookTrigger(GameObject o)
		{
			int hashCode = o.transform.GetGameobjectHashString().GetHashCode();
			TowHookTrigger towHookTrigger = o.AddComponent<TowHookTrigger>();
			towHookTrigger.hash = hashCode;
			NetTowHookManager.towHooks.Add(hashCode, towHookTrigger);
		}

		private void Start()
		{
			_ObjectsLoader.gameLoaded.Add(delegate
			{
				for (int i = 0; i < _ObjectsLoader.ObjectsInGame.Length; i++)
				{
					GameObject gameObject = _ObjectsLoader.ObjectsInGame[i];
					if (gameObject.name == "HookFront" || gameObject.name == "HookRear")
					{
						Object.Destroy(gameObject.GetComponent<PlayMakerFSM>());
						NetTowHookManager.CreateTowHookTrigger(gameObject);
					}
				}
				Transform transform = GameObject.Find("PLAYER").transform.Find("Pivot/AnimPivot/Camera/FPSCamera/TowingRope/Rope");
				Object.Destroy(transform.GetComponent<PlayMakerFSM>());
				NetTowHookManager.towRopePrefab = transform.gameObject;
				NetTowHookManager.createRopeEvent = new GameEvent("CreateTowHookRope", new Action<GameEventReader>(this.CreateRope), GameScene.GAME);
				NetTowHookManager.initSync = new GameEvent("TowHookInitSync", delegate(GameEventReader p)
				{
					List<TowRope> list = new List<TowRope>();
					while (p.UnreadLength() > 0)
					{
						byte b = p.ReadByte();
						int num = ((b > 0) ? p.ReadInt32() : 0);
						int num2 = ((b > 1) ? p.ReadInt32() : 0);
						if (b == 2)
						{
							if (NetTowHookManager.towHooks.ContainsKey(num) && NetTowHookManager.towHooks.ContainsKey(num2))
							{
								Player player = CoreManager.Players[p.sender];
								NetTowHookManager.towHooks[num].CreateRopeMP(player);
								NetTowHookManager.towHooks[num].rope.ConnectB_MP(num2);
							}
						}
						else
						{
							list.Add(NetTowHookManager.GetFreeRope(0, false));
						}
					}
					for (int j = 0; j < list.Count; j++)
					{
						NetTowHookManager.SetFreeRope(list[j]);
					}
				}, GameScene.GAME);
				if (WreckMPGlobals.IsHost)
				{
					WreckMPGlobals.OnMemberReady.Add(delegate(ulong user)
					{
						using (GameEventWriter gameEventWriter = NetTowHookManager.initSync.Writer())
						{
							int num3 = 0;
							int num4 = 0;
							while (num3 < NetTowHookManager.freeRopes.Count || num4 < NetTowHookManager.usedRopes.Count)
							{
								bool flag = num3 < NetTowHookManager.freeRopes.Count && (num4 >= NetTowHookManager.usedRopes.Count || NetTowHookManager.freeRopes[num3].id < NetTowHookManager.usedRopes[num4].id);
								TowRope towRope = (flag ? NetTowHookManager.freeRopes[num3] : NetTowHookManager.usedRopes[num4]);
								if (flag)
								{
									num3++;
								}
								else
								{
									num4++;
								}
								int num5 = 0;
								int num6 = 0;
								byte b2 = 0;
								foreach (KeyValuePair<int, TowHookTrigger> keyValuePair in NetTowHookManager.towHooks)
								{
									if (keyValuePair.Value.transform == towRope.a.parent)
									{
										num5 = keyValuePair.Value.hash;
										b2 = 1;
									}
									else if (keyValuePair.Value.transform == towRope.b.parent)
									{
										num6 = keyValuePair.Value.hash;
									}
									if (num5 != 0 && num6 != 0)
									{
										b2 = 2;
										break;
									}
								}
								gameEventWriter.Write(b2);
								if (num5 != 0)
								{
									gameEventWriter.Write(num5);
								}
								if (num6 != 0)
								{
									gameEventWriter.Write(num6);
								}
							}
							NetTowHookManager.initSync.Send(gameEventWriter, user, true, default(GameEvent.RecordingProperties));
						}
					});
				}
				return "TowHookMgr";
			});
		}

		private void CreateRope(GameEventReader p)
		{
			int num = p.ReadInt32();
			if (!NetTowHookManager.towHooks.ContainsKey(num))
			{
				return;
			}
			Player player = CoreManager.Players[p.sender];
			NetTowHookManager.towHooks[num].CreateRopeMP(player);
		}

		internal static TowRope GetFreeRope(int eventHash, bool sendEvent)
		{
			if (sendEvent)
			{
				using (GameEventWriter gameEventWriter = NetTowHookManager.createRopeEvent.Writer())
				{
					gameEventWriter.Write(eventHash);
					NetTowHookManager.createRopeEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
				}
			}
			if (NetTowHookManager.freeRopes.Count > 0)
			{
				TowRope towRope = NetTowHookManager.freeRopes[0];
				towRope.isValid = true;
				NetTowHookManager.freeRopes.RemoveAt(0);
				towRope.transform.localPosition = Vector3.zero;
				towRope.gameObject.SetActive(true);
				return towRope;
			}
			GameObject gameObject = Object.Instantiate<GameObject>(NetTowHookManager.towRopePrefab);
			gameObject.transform.parent = NetTowHookManager.towRopePrefab.transform.parent;
			gameObject.transform.localPosition = Vector3.zero;
			gameObject.SetActive(true);
			TowRope towRope2 = gameObject.AddComponent<TowRope>();
			TowRope towRope3 = towRope2;
			byte b = NetTowHookManager.freeID;
			NetTowHookManager.freeID = b + 1;
			towRope3.id = b;
			towRope2._Start();
			NetTowHookManager.usedRopes.Add(towRope2);
			return towRope2;
		}

		internal static void SetFreeRope(TowRope rope)
		{
			rope.SetConnect(null, null);
			rope.gameObject.SetActive(false);
			int num = NetTowHookManager.usedRopes.IndexOf(rope);
			if (num == -1)
			{
				return;
			}
			NetTowHookManager.usedRopes.RemoveAt(num);
			NetTowHookManager.freeRopes.Add(rope);
		}

		internal static TowRope ropeInHand;

		internal static Dictionary<int, TowHookTrigger> towHooks = new Dictionary<int, TowHookTrigger>();

		private static GameObject towRopePrefab;

		private static List<TowRope> freeRopes = new List<TowRope>();

		private static List<TowRope> usedRopes = new List<TowRope>();

		internal static byte freeID = 1;

		private static GameEvent createRopeEvent;

		private static GameEvent initSync;
	}
}
