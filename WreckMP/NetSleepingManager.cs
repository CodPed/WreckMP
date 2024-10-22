using System;
using System.Collections.Generic;
using UnityEngine;

namespace WreckMP
{
	public class NetSleepingManager : NetManager
	{
		internal static int EmptyBags
		{
			get
			{
				return SteamNet.p2pConnections.Count + 1 - NetSleepingManager.occupiedBags;
			}
		}

		public NetSleepingManager()
		{
			NetSleepingManager.Instance = this;
		}

		private void CreateSB(Vector3 position)
		{
			GameObject gameObject = Object.Instantiate<GameObject>(NetSleepingManager.sleepingBagPrefab);
			gameObject.transform.position = position;
			gameObject.tag = "PART";
			gameObject.layer = 19;
			SleepingBag sleepingBag = gameObject.AddComponent<SleepingBag>();
			this.sleepingBags.Add(sleepingBag);
		}

		private void Start()
		{
			this.sleepingBags.Clear();
			_ObjectsLoader.gameLoaded.Add(delegate
			{
				this.CreateSB(LocalPlayer.Instance.player.Value.transform.position);
				for (int i = 0; i < SteamNet.p2pConnections.Count; i++)
				{
					this.CreateSB(CoreManager.Players[SteamNet.p2pConnections[i].m_SteamID].player.transform.position);
				}
				WreckMPGlobals.OnMemberReady.Add(delegate(ulong u)
				{
					if (SteamNet.p2pConnections.Count + 1 > this.sleepingBags.Count)
					{
						this.CreateSB(CoreManager.Players[u].player.transform.position);
					}
				});
				this.sleepEvent = new GameEvent("EveryoneSleepNow!", delegate(GameEventReader p)
				{
					this.SendSleepToBags();
				}, GameScene.GAME);
				int num = 0;
				for (int j = 0; j < _ObjectsLoader.ObjectsInGame.Length; j++)
				{
					Transform transform = _ObjectsLoader.ObjectsInGame[j].transform;
					if (!(transform == null) && !(transform.gameObject == null))
					{
						for (int k = 0; k < NetSleepingManager.vanillaBedsPaths.Length; k++)
						{
							int num2 = NetSleepingManager.vanillaBedsPaths[k].IndexOf('/');
							if (NetSleepingManager.vanillaBedsPaths[k].StartsWith("GIFU"))
							{
								num2 = NetSleepingManager.vanillaBedsPaths[k].Substring(num2 + 1).IndexOf('/') + num2 + 1;
							}
							if (!(NetSleepingManager.vanillaBedsPaths[k].Substring(0, num2) != transform.gameObject.name) && (num >> k) % 2 != 1)
							{
								Transform transform2 = transform.Find(NetSleepingManager.vanillaBedsPaths[k].Substring(num2 + 1));
								if (!(transform2 == null))
								{
									PlayMakerFSM component = transform2.GetComponent<PlayMakerFSM>();
									if (!(component == null))
									{
										component.enabled = false;
										NetSleepingManager.CreateSleepTrigger(false, null, transform2.gameObject, NetSleepingManager.vanillaBedsPivots[k, 0], NetSleepingManager.vanillaBedsPivots[k, 1]);
										num |= 1 << k;
									}
								}
							}
						}
					}
				}
				return "SleepingBags";
			});
		}

		public static void CreateSleepTrigger(bool makeCollider, GameObject gameObject, GameObject sleepTriggerObj, Vector3 pivotPos, Vector3 pivotRot)
		{
			byte b;
			SleepTrigger sleepTrigger;
			GameEvent gameEvent;
			SleepingBag.CreateSleepTrigger(makeCollider, out b, gameObject, sleepTriggerObj, out sleepTrigger, out gameEvent, pivotPos, pivotRot);
			NetSleepingManager.Instance.vanillaSleepTriggers.Add(sleepTrigger);
		}

		private void Update()
		{
			if (WreckMPGlobals.IsHost && NetSleepingManager.occupiedBags >= SteamNet.p2pConnections.Count + 1 && !NetSleepingManager.sleeping)
			{
				NetSleepingManager.sleeping = true;
				this.sleepEvent.SendEmpty(0UL, true);
				this.SendSleepToBags();
			}
		}

		private void SendSleepToBags()
		{
			for (int i = 0; i < this.sleepingBags.Count; i++)
			{
				this.sleepingBags[i].Sleep();
			}
			for (int j = 0; j < this.vanillaSleepTriggers.Count; j++)
			{
				this.vanillaSleepTriggers[j].TriggerSleep();
			}
		}

		// Note: this type is marked as 'beforefieldinit'.
		static NetSleepingManager()
		{
			Vector3[,] array = new Vector3[8, 2];
			array[0, 0] = new Vector3(0.016f, 0.446f, -0.007f);
			array[0, 1] = new Vector3(0f, 90f, 180f);
			array[1, 0] = new Vector3(0.31f, 0.4f, -0.1f);
			array[1, 1] = new Vector3(0f, 90f, 180f);
			array[2, 0] = new Vector3(0f, 0.69f, -0.09f);
			array[2, 1] = new Vector3(0f, 90f, 180f);
			array[3, 0] = new Vector3(0f, 0.59f, 0.01f);
			array[3, 1] = new Vector3(0f, 90f, 180f);
			array[4, 0] = new Vector3(0.016f, -0.554f, -0.037f);
			array[4, 1] = new Vector3(0f, 0f, 0f);
			array[5, 0] = new Vector3(0.016f, -0.554f, -0.037f);
			array[5, 1] = new Vector3(0f, 0f, 0f);
			array[6, 0] = new Vector3(0.016f, -0.554f, -0.037f);
			array[6, 1] = new Vector3(0f, 0f, 0f);
			array[7, 0] = new Vector3(0f, -0.21f, -0.14f);
			array[7, 1] = new Vector3(0f, 90f, 180f);
			NetSleepingManager.vanillaBedsPivots = array;
			NetSleepingManager.occupiedBags = 0;
			NetSleepingManager.sleeping = false;
		}

		private static readonly string[] vanillaBedsPaths = new string[] { "YARD/Building/BEDROOM1/LOD_bedroom1/Sleep/SleepTrigger", "ITEMS/sofa(itemx)/Sleep/SleepTrigger", "seat rear(Clone)/Sleep/SleepTrigger", "RCO_RUSCKO12(270)/LOD/Sleep/SleepTrigger", "CABIN/LOD/Sleep/SleepTrigger/PivotSleep", "COTTAGE/LOD/Sleep/SleepTrigger", "JAIL/Sleep/SleepTrigger", "GIFU(750/450psi)/LOD/Sleep/SleepTrigger" };

		private static readonly Vector3[,] vanillaBedsPivots;

		internal static GameObject sleepingBagPrefab;

		private List<SleepingBag> sleepingBags = new List<SleepingBag>();

		private List<SleepTrigger> vanillaSleepTriggers = new List<SleepTrigger>();

		internal static int occupiedBags;

		internal static bool sleeping;

		private GameEvent sleepEvent;

		private static NetSleepingManager Instance;
	}
}
