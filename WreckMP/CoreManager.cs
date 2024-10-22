using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Steamworks;
using UnityEngine;
using UnityEngine.EventSystems;
using WreckMP.Properties;

namespace WreckMP
{
	internal class CoreManager : MonoBehaviour
	{
		public static Dictionary<ulong, Player> Players
		{
			get
			{
				return CoreManager.players;
			}
		}

		private static List<string> MscloaderLoadAssetsAssetNames
		{
			get
			{
				FieldInfo fieldInfo = CoreManager.f_MscloaderLoadAssetsAssetNames;
				return ((fieldInfo != null) ? fieldInfo.GetValue(null) : null) as List<string>;
			}
		}

		public static void AddWreckMPSyncs()
		{
			CoreManager.SystemContainer = new GameObject("WreckMP_CoreManagers");
			try
			{
				Type[] array = (from type in Assembly.GetExecutingAssembly().GetTypes()
					where type.IsSubclassOf(typeof(NetManager))
					orderby type.Name
					select type).ToArray<Type>();
				int i = 0;
				int num = array.Length;
				while (i < num)
				{
					CoreManager.SystemContainer.AddComponent(array[i]);
					Console.Log("Created NetManager: " + array[i].Name, false);
					i++;
				}
			}
			catch (Exception ex)
			{
				Console.LogError(ex, false);
			}
		}

		internal static void SendSavegame(CSteamID player)
		{
			using (GameEventWriter gameEventWriter = CoreManager.savegameEvent.Writer())
			{
				string text = Application.persistentDataPath + "/defaultES2File.txt";
				string text2 = Application.persistentDataPath + "/items.txt";
				bool flag = File.Exists(text) && File.Exists(text2);
				gameEventWriter.Write(flag);
				if (flag)
				{
					byte[] array = File.ReadAllBytes(text);
					gameEventWriter.Write(array.Length);
					gameEventWriter.Write(array);
					array = File.ReadAllBytes(text2);
					gameEventWriter.Write(array.Length);
					gameEventWriter.Write(array);
				}
				CoreManager.savegameEvent.Send(gameEventWriter, player.m_SteamID, true, default(GameEvent.RecordingProperties));
			}
		}

		internal static void RecieveSavegame(GameEventReader packet)
		{
			if (CoreManager.savegameRecieved)
			{
				return;
			}
			CoreManager.savegameRecieved = true;
			string text = Application.persistentDataPath + "/defaultES2File.txt";
			string text2 = Application.persistentDataPath + "/items.txt";
			bool flag = (CoreManager.hadSavegame = File.Exists(text) && File.Exists(text2));
			bool flag2 = packet.ReadBoolean();
			if (flag)
			{
				File.Copy(text, Application.persistentDataPath + "/defaultES2File.bak", true);
				File.Copy(text2, Application.persistentDataPath + "/items.bak", true);
			}
			if (flag2)
			{
				File.WriteAllBytes(text, packet.ReadBytes(packet.ReadInt32()));
				File.WriteAllBytes(text2, packet.ReadBytes(packet.ReadInt32()));
			}
			else
			{
				File.Delete(text);
				File.Delete(text2);
			}
			SceneLoader.LoadScene(GameScene.GAME);
		}

		internal static void OnLobbyCreate()
		{
			CoreManager.IsHost = true;
			CoreManager.IsConnected = true;
			CoreManager.HostID = (CoreManager.UserID = SteamUser.GetSteamID());
			CoreManager.players.Clear();
			CoreManager.uiManager.UpdatePlayerlist(SteamNet.currentLobby.m_LobbyCode, CoreManager.maxPlayers - 1, new string[] { SteamFriends.GetPersonaName() });
			SceneLoader.LoadScene(GameScene.GAME);
		}

		internal static void OnMemberConnect(CSteamID userId)
		{
			CoreManager.CreateUserObjects(userId.m_SteamID, true);
			if (CoreManager.IsHost)
			{
				CoreManager.SendSavegame(userId);
			}
		}

		internal static void OnMemberDisconnect(CSteamID userId)
		{
			CoreManager.DeleteUserObjects(userId.m_SteamID);
			if (SteamNet.allowedGhostPlayer != userId)
			{
				Console.Log(SteamFriends.GetFriendPersonaName(userId) + " left the game.", true);
			}
			Action<ulong> onMemberExit = WreckMPGlobals.OnMemberExit;
			if (onMemberExit == null)
			{
				return;
			}
			onMemberExit(userId.m_SteamID);
		}

		internal static void OnConnect()
		{
			CoreManager.IsConnected = true;
			CoreManager.UserID = SteamUser.GetSteamID();
			CoreManager.HostID = SteamMatchmaking.GetLobbyOwner(SteamNet.currentLobby);
		}

		internal static void OnReceive(ulong sender, byte[] data, int length)
		{
			length = Mathf.Min(length, data.Length);
			byte[] array = new byte[length];
			for (int i = 0; i < length; i++)
			{
				array[i] = data[i];
			}
			GameEventRouter.ReceivePacket(array, sender, false);
		}

		private void OnLevelWasLoaded(int levelId)
		{
			string loadedLevelName = Application.loadedLevelName;
			if (!(loadedLevelName == "MainMenu"))
			{
				if (loadedLevelName == "GAME")
				{
					CoreManager.currentScene = GameScene.GAME;
					if (CoreManager.IsConnected && !CoreManager.init)
					{
						CoreManager.init = true;
						for (int i = 0; i < SteamNet.p2pConnections.Count; i++)
						{
							if (SteamNet.p2pConnections[i].m_SteamID != WreckMPGlobals.UserID)
							{
								CoreManager.CreateUserObjects(SteamNet.p2pConnections[i].m_SteamID, true);
							}
						}
						AssetBundle assetBundle = AssetBundle.CreateFromMemoryImmediate(Resources.wreckmp);
						NetSleepingManager.sleepingBagPrefab = assetBundle.LoadAsset<GameObject>("Sleeping bag.prefab");
						assetBundle.Unload(false);
						CoreManager.AddWreckMPSyncs();
						if (!WreckMPGlobals.IsHost)
						{
							CoreManager.DisableSaveFSM();
						}
						CoreManager.SystemContainer.AddComponent<LocalPlayer>();
					}
				}
			}
			else
			{
				CoreManager.currentScene = GameScene.MainMenu;
				CoreManager.init = false;
				this.StartMenu();
			}
			Action<GameScene> action = CoreManager.sceneLoaded;
			if (action == null)
			{
				return;
			}
			action(CoreManager.currentScene);
		}

		private static void DisableSaveFSM()
		{
			_ObjectsLoader.gameLoaded.Add(delegate
			{
				for (int i = 0; i < _ObjectsLoader.ObjectsInGame.Length; i++)
				{
					if (!(_ObjectsLoader.ObjectsInGame[i].name != "SAVEGAME"))
					{
						PlayMakerFSM component = _ObjectsLoader.ObjectsInGame[i].GetComponent<PlayMakerFSM>();
						if (!(component == null) && !(component.FsmName != "Button"))
						{
							component.Initialize();
							FsmState state = component.GetState("Wait for click");
							if (state == null)
							{
								Console.LogError("Wait for click is null on " + component.transform.GetGameobjectHashString(), false);
							}
							else
							{
								SetStringValue setStringValue = state.Actions.First((FsmStateAction a) => a is SetStringValue) as SetStringValue;
								if (setStringValue == null)
								{
									Console.LogError("Wait for click string is null on " + component.transform.GetGameobjectHashString(), false);
								}
								else
								{
									setStringValue.stringValue = "DISCONNECT";
									FsmState state2 = component.GetState("Mute audio");
									if (state2 == null)
									{
										Console.LogError("Mute audio is null on " + component.transform.GetGameobjectHashString(), false);
									}
									else
									{
										SetStringValue setStringValue2 = state2.Actions.First((FsmStateAction a) => a is SetStringValue) as SetStringValue;
										if (setStringValue2 == null)
										{
											Console.LogError("Mute audio string is null on " + component.transform.GetGameobjectHashString(), false);
										}
										else
										{
											setStringValue2.stringValue = "DISCONNECTING...";
											state2.Transitions[0].ToState = "Load menu";
											component.Initialize();
										}
									}
								}
							}
						}
					}
				}
				return "Coreman";
			});
		}

		internal static void CreateUserObjects(ulong steamid, bool useName = true)
		{
			if (CoreManager.players.ContainsKey(steamid))
			{
				Player player = CoreManager.players[steamid];
				player.player.SetActive(!player.isGhost);
				return;
			}
			Player player2 = WreckMP.instance.gameObject.AddComponent<Player>();
			player2.useName = useName;
			player2.SteamID = steamid;
			CoreManager.players[steamid] = player2;
		}

		internal static void DeleteUserObjects(ulong steamID)
		{
			if (CoreManager.players.ContainsKey(steamID))
			{
				CoreManager.players[steamID].Leave();
			}
		}

		public static void SendData(byte[] buffer, CSteamID target, bool safe)
		{
			if (!CoreManager.IsConnected)
			{
				return;
			}
			SteamNetworking.SendP2PPacket(target, buffer, (uint)buffer.Length, safe ? 2 : 0, 0);
		}

		public static void SendData(byte[] buffer, ulong target, bool safe)
		{
			CoreManager.SendData(buffer, (CSteamID)target, safe);
		}

		public static void SendData(byte[] buffer, bool safe, List<CSteamID> targets)
		{
			if (targets == null)
			{
				return;
			}
			for (int i = 0; i < targets.Count; i++)
			{
				if (targets[i].m_SteamID != WreckMPGlobals.UserID)
				{
					CoreManager.SendData(buffer, targets[i], safe);
				}
			}
		}

		private void StartMenu()
		{
			Console.Log("StartMenu", true);
			if (!CoreManager.loadedUI)
			{
				AssetBundle assetBundle = AssetBundle.CreateFromMemoryImmediate(Resources.wreckmp);
				GameObject gameObject = Object.Instantiate<GameObject>(assetBundle.LoadAsset<GameObject>("Canvas.prefab"));
				gameObject.name = "WreckMP_Canvas";
				gameObject.transform.Find("UI").gameObject.AddComponent<UIManager>()._Start();
				if (Resources.FindObjectsOfTypeAll<EventSystem>().Length == 0)
				{
					Object.Instantiate<GameObject>(assetBundle.LoadAsset<GameObject>("EventSystem.prefab")).transform.parent = gameObject.transform;
				}
				assetBundle.Unload(false);
				CoreManager.loadedUI = !WreckMPGlobals.ModLoaderInstalled;
			}
			GameObject gameObject2 = GameObject.Find("Interface/Buttons");
			if (gameObject2 != null)
			{
				gameObject2.GetComponent<PlayMakerFSM>().enabled = false;
				PlayMakerFSM[] array = new PlayMakerFSM[2];
				array[0] = this.InitMenuButton(gameObject2.transform.Find("ButtonContinue"), "PLAY", delegate
				{
					CoreManager.uiManager.TogglePlayMenu();
				});
				array[1] = this.InitMenuButton(gameObject2.transform.Find("ButtonNewgame"), "CONNECT", delegate
				{
					Process.Start(new ProcessStartInfo
					{
						FileName = "https://github.com/intcost/WreckMP",
						UseShellExecute = true
					});
				});
				this.menuButtons = array;
			}
		}

		private PlayMakerFSM InitMenuButton(Transform parent, string name, Action click)
		{
			PlayMakerFSM component = parent.GetComponent<PlayMakerFSM>();
			FsmState fsmState = component.FsmStates.First((FsmState s) => s.Name == "Action");
			string clickStateName = fsmState.Transitions.First((FsmTransition t) => t.EventName == "DOWN").ToState;
			FsmState fsmState2 = component.FsmStates.First((FsmState s) => s.Name == clickStateName);
			FsmState fsmState3 = fsmState2;
			FsmTransition[] array = new FsmTransition[1];
			int num = 0;
			FsmTransition fsmTransition = new FsmTransition();
			fsmTransition.FsmEvent = component.FsmEvents.First((FsmEvent e) => e.Name == "FINISHED");
			fsmTransition.ToState = component.FsmStates[0].Name;
			array[num] = fsmTransition;
			fsmState3.Transitions = array;
			fsmState2.Actions = new FsmStateAction[]
			{
				new PM_Hook(click, false),
				new Wait
				{
					time = 0.2f
				}
			};
			TextMesh component2 = parent.GetChild(0).GetComponent<TextMesh>();
			TextMesh component3 = component2.transform.GetChild(0).GetComponent<TextMesh>();
			component3.text = name;
			component2.text = name;
			return component;
		}

		private void Update()
		{
			if (CoreManager.currentScene == GameScene.MainMenu && WreckMPGlobals.ModLoaderInstalled)
			{
				if (this.doMenuReset && !Application.isLoadingLevel)
				{
					GameObject[] array = Object.FindObjectsOfType<GameObject>();
					for (int i = 0; i < array.Length; i++)
					{
						if (!(array[i].name == "MSCUnloader") && !(array[i].name == "WreckMP"))
						{
							Object.Destroy(array[i]);
						}
					}
					GameObject[] array2 = (from x in Resources.FindObjectsOfTypeAll<GameObject>()
						where !x.activeInHierarchy && x.transform.parent == null
						select x).ToArray<GameObject>();
					List<string> mscloaderLoadAssetsAssetNames = CoreManager.MscloaderLoadAssetsAssetNames;
					for (int j = 0; j < array.Length; j++)
					{
						if (mscloaderLoadAssetsAssetNames.Contains(array2[j].name.ToLower()))
						{
							Object.Destroy(array2[j]);
						}
					}
					PlayMakerGlobals.Instance.Variables.FindFsmBool("SongImported").Value = false;
					Application.LoadLevel(Application.loadedLevelName);
					this.doMenuReset = false;
				}
				if (this.showingLobbyDialog != 0 && Input.GetKeyDown(27))
				{
					this.showingLobbyDialog = 0;
				}
				for (int k = 0; k < this.menuButtons.Length; k++)
				{
					if (!this.menuButtons[k].enabled)
					{
						this.menuButtons[k].enabled = true;
					}
				}
			}
			if (CoreManager.IsConnected)
			{
				SteamNet.GetNetworkData();
			}
			if (CoreManager.SystemContainer != null && !CoreManager.SystemContainer.activeSelf)
			{
				CoreManager.SystemContainer.SetActive(true);
			}
		}

		private void FixedUpdate()
		{
			if (CoreManager.IsConnected)
			{
				SteamNet.CheckConnections();
			}
		}

		internal void Init()
		{
			SteamNet.Start();
			CoreManager.savegameEvent = new GameEvent("Savegame", new Action<GameEventReader>(CoreManager.RecieveSavegame), GameScene.MainMenu);
			CoreManager.playerLoadedEvent = new GameEvent("PlayerLoaded", delegate(GameEventReader _p)
			{
				Player player = CoreManager.players[_p.sender];
				player.player.SetActive(!player.isGhost);
				for (int i = 0; i < WreckMPGlobals.OnMemberReady.Count; i++)
				{
					try
					{
						WreckMPGlobals.OnMemberReady[i](_p.sender);
					}
					catch (Exception ex)
					{
						Console.LogError(string.Format("An error of type {0} occured when {1} sent Ready event. Please report this error to the developers", ex.GetType(), player.PlayerName), false);
						Console.LogError(string.Format("{0} {1} [{2}]", ex.GetType(), ex.Message, ex.StackTrace), false);
					}
				}
			}, GameScene.GAME);
			if (Application.loadedLevelName == "MainMenu")
			{
				CoreManager.currentScene = GameScene.MainMenu;
			}
		}

		public static void Disconnect()
		{
			if (CoreManager.savegameRecieved && CoreManager.hadSavegame)
			{
				File.Copy(Application.persistentDataPath + "/defaultES2File.bak", Application.persistentDataPath + "/defaultES2File.txt", true);
				File.Copy(Application.persistentDataPath + "/items.bak", Application.persistentDataPath + "/items.txt", true);
			}
			CoreManager.IsConnected = false;
			SteamNet.CloseConnections();
			CoreManager.init = false;
			if (WreckMPGlobals.ModLoaderInstalled)
			{
				Object[] array = Resources.FindObjectsOfTypeAll(WreckMPGlobals.mscloader.GetType("MSCLoader.MSCUnloader"));
				if (array.Length != 0)
				{
					GameObject gameObject = (array[0] as MonoBehaviour).gameObject;
					gameObject.SetActive(false);
					Object.Destroy(gameObject);
					WreckMP.instance.netman.doMenuReset = true;
				}
			}
			SceneLoader.LoadScene(GameScene.MainMenu);
		}

		private void OnApplicationQuit()
		{
			SteamNet.CloseConnections();
			if (CoreManager.savegameRecieved)
			{
				File.Copy(Application.persistentDataPath + "/defaultES2File.bak", Application.persistentDataPath + "/defaultES2File.txt", true);
				File.Copy(Application.persistentDataPath + "/items.bak", Application.persistentDataPath + "/items.txt", true);
			}
		}

		private void OnGUI()
		{
			if (this.cam == null)
			{
				FsmGameObject fsmGameObject = FsmVariables.GlobalVariables.FindFsmGameObject("POV");
				if (fsmGameObject != null && fsmGameObject.Value != null)
				{
					this.cam = fsmGameObject.Value.GetComponent<Camera>();
				}
			}
			KeyValuePair<ulong, Player>[] array = CoreManager.Players.ToArray<KeyValuePair<ulong, Player>>();
			for (int i = 0; i < array.Length; i++)
			{
				if (!array[i].Value.isGhost && this.cam != null)
				{
					Player value = array[i].Value;
					if (value != null)
					{
						Vector3 headPos = value.HeadPos;
						Vector3 position = this.cam.transform.position;
						if ((this.cam.transform.position + this.cam.transform.forward - headPos).sqrMagnitude < (position - headPos).sqrMagnitude)
						{
							this.DrawUsername(this.cam, headPos, value.PlayerName);
						}
					}
				}
			}
		}

		private void DrawUsername(Camera cam, Vector3 worldPosition, string name)
		{
			Vector3 vector = cam.WorldToScreenPoint(worldPosition + Vector3.up * 0.5f);
			float magnitude = (worldPosition - cam.transform.position).magnitude;
			GUIStyle guistyle = new GUIStyle
			{
				fontSize = Mathf.FloorToInt(36f / magnitude)
			};
			if (guistyle.fontSize == 0)
			{
				return;
			}
			vector.x -= guistyle.CalcSize(new GUIContent(name)).x / 2f;
			GUI.Label(new Rect(vector.x, (float)Screen.height - vector.y, (float)Screen.width, (float)Screen.height), "<color=white>" + name + "</color>", guistyle);
		}

		private static Dictionary<ulong, Player> players = new Dictionary<ulong, Player>();

		public static AssetBundle assetBundle;

		public static GameObject SystemContainer;

		public static GameScene currentScene = GameScene.Unknown;

		public static Action<GameScene> sceneLoaded;

		public static int maxPlayers = 3;

		public static bool IsConnected;

		public static bool IsHost;

		public static CSteamID HostID;

		public static CSteamID UserID;

		private static bool init = false;

		private static bool savegameRecieved;

		private static bool hadSavegame;

		private static readonly FieldInfo f_MscloaderLoadAssetsAssetNames = ((!WreckMPGlobals.ModLoaderInstalled) ? null : WreckMPGlobals.mscloader.GetType("MSCLoader.LoadAssets").GetField("assetNames", BindingFlags.Static | BindingFlags.NonPublic));

		private int showingLobbyDialog;

		private bool doMenuReset;

		private static GameEvent savegameEvent;

		private static GameEvent playerLoadedEvent;

		internal static UIManager uiManager;

		private PlayMakerFSM[] menuButtons;

		private static bool loadedUI = false;

		private Rect windowRect = new Rect((float)Screen.width / 2f - 150f, (float)Screen.height / 2f - 32.5f, 300f, 65f);

		public static Dictionary<CSteamID, string> playerNames = new Dictionary<CSteamID, string>();

		private string lobbyCode = "";

		private Camera cam;
	}
}
