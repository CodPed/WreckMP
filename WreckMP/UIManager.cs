using System;
using System.IO;
using System.Linq;
using Steamworks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WreckMP
{
	internal class UIManager : MonoBehaviour
	{
		internal void _Start()
		{
			Object.DontDestroyOnLoad(base.transform.root.gameObject);
			Font[] array = Resources.FindObjectsOfTypeAll<Font>();
			Font font = null;
			foreach (Font font2 in array)
			{
				if (font2.name == "FugazOne-Regular" && font2.characterInfo.Length == 0)
				{
					font = font2;
					break;
				}
				if (font2.name.Contains("Cour10Bd"))
				{
					font = font2;
				}
			}
			if (font == null)
			{
				Console.LogError("Didn't find Fugaz font. Available fonts: " + array.Length.ToString(), false);
				foreach (Font font3 in array)
				{
					Console.Log(string.Format("{0} ({1})", font3.name, font3.characterInfo.Length), true);
				}
			}
			else
			{
				Text[] componentsInChildren = base.GetComponentsInChildren<Text>(true);
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					componentsInChildren[i].font = font;
				}
			}
			base.transform.Find("Watermark").GetComponent<Text>().text = "WreckMP " + WreckMP.version + " | intcost Development\nThis is Public Test Branch of WreckMP, some features may be missing or broken.";
			this.playerList = base.transform.Find("PlayerList").GetComponent<RectTransform>();
			this.playerList.anchoredPosition = new Vector2(-this.playerList.sizeDelta.x, 0f);
			this.playerListPlayerCount = base.transform.Find("PlayerList/Header/PlayerCount").GetComponent<Text>();
			this.playerListLobbycode = base.transform.Find("PlayerList/Header/LobbyCode").GetComponent<Text>();
			this.playerPrefab = base.transform.Find("PlayerList/Player_PR").gameObject;
			this.console = base.transform.Find("Console").GetComponent<CanvasGroup>();
			this.console.alpha = 0f;
			this.chatInputField = this.console.transform.Find("Footer/InputField").GetComponent<InputField>();
			this.consoleContent = this.console.transform.Find("Mask/Content").GetComponent<Text>();
			this.consoleContent.text = "";
			CoreManager.uiManager = this;
			this.chatMessage = new GameEvent("ChatMessage", delegate(GameEventReader p)
			{
				string text = p.ReadString();
				string playerName = CoreManager.Players[p.sender].PlayerName;
				this.LogConsolePlayerMessage(text, playerName, p.sender == WreckMPGlobals.HostID);
			}, GameScene.GAME);
			this.playMenu = base.transform.Find("JoinLobby").gameObject;
			this.playMenu.SetActive(false);
			this.joinLobbyCode = this.playMenu.transform.Find("JoinLobby/InputField 1").GetComponent<InputField>();
			this.openLobbyMaxPlayers = this.playMenu.transform.Find("OpenLobby 1/InputField 1").GetComponent<InputField>();
			this.openLobbyMaxPlayers.onEndEdit.AddListener(delegate(string val)
			{
				int num;
				if (int.TryParse(val, out num))
				{
					if (num < 2)
					{
						this.openLobbyMaxPlayers.text = 2.ToString();
						return;
					}
					if (num > 200)
					{
						this.openLobbyMaxPlayers.text = 200.ToString();
						return;
					}
				}
				else
				{
					this.openLobbyMaxPlayers.text = 2.ToString();
				}
			});
			this.openLobbyUseSavegame = this.playMenu.transform.Find("OpenLobby 2/GameObject/Toggle").GetComponent<Toggle>();
			this.playMenu.transform.Find("OpenLobbyBtn 1/Button").GetComponent<Button>().onClick.AddListener(new UnityAction(this.OpenlobbyClick));
			this.playMenu.transform.Find("JoinLobbyBtn/Button").GetComponent<Button>().onClick.AddListener(new UnityAction(this.JoinlobbyClick));
			Console.Log("UI initialized!", true);
			if (font == null)
			{
				Console.LogError("Failed to find Fugaz font", false);
			}
		}

		private void JoinlobbyClick()
		{
			if (this.joinLobbyCode.text.Length != 10)
			{
				Console.Log("Invalid lobby code!", true);
				return;
			}
			this.TogglePlayMenu();
			SteamNet.JoinLobby(this.joinLobbyCode.text);
		}

		private void OpenlobbyClick()
		{
			int num = int.Parse(this.openLobbyMaxPlayers.text) + 1;
			if (!this.openLobbyUseSavegame.isOn)
			{
				if (File.Exists(Application.persistentDataPath + "/defaultES2File.txt"))
				{
					File.Delete(Application.persistentDataPath + "/defaultES2File.txt");
				}
				if (File.Exists(Application.persistentDataPath + "/items.txt"))
				{
					File.Delete(Application.persistentDataPath + "/items.txt");
				}
			}
			this.TogglePlayMenu();
			SteamNet.CreateLobby(SteamUser.GetSteamID(), num, 2);
		}

		public void TogglePlayMenu()
		{
			this.playMenu.SetActive(!this.playMenu.activeSelf);
			if (this.playMenu.activeSelf)
			{
				this.joinLobbyCode.text = "";
				this.openLobbyMaxPlayers.text = "2";
				this.openLobbyUseSavegame.isOn = true;
			}
		}

		public void LogConsoleSystemMessage(string msg)
		{
			this.LogConsole("<color=#2dceff>System:</color> " + msg);
		}

		public void LogConsolePlayerMessage(string msg, string name, bool isHost)
		{
			this.LogConsole(string.Concat(new string[]
			{
				"<color=",
				isHost ? "#ff882d" : "#2dff2d",
				">",
				name,
				":</color> ",
				msg
			}));
		}

		private void LogConsole(string msg)
		{
			if (UIManager.allowConsoleInterrupt)
			{
				this.consoleVisibleTime = 6f;
				this.console.alpha = 1f;
			}
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < this.consoleContent.text.Length; i++)
			{
				if (this.consoleContent.text[i] == '\n')
				{
					num++;
					if (num2 == 0)
					{
						num2 = i;
					}
				}
			}
			if (num > 12)
			{
				this.consoleContent.text = this.consoleContent.text.Substring(num2 + 1);
			}
			Text text = this.consoleContent;
			text.text = text.text + "\n" + msg;
		}

		public void UpdatePlayerlist(string lobbyCode, int maxPlayers, string[] players)
		{
			this.playerListPlayerCount.text = string.Format("{0} / {1}", players.Length, maxPlayers);
			this.playerListLobbycode.text = lobbyCode;
			for (int i = 2; i < this.playerList.childCount; i++)
			{
				Object.Destroy(this.playerList.GetChild(i).gameObject);
			}
			this.playerList.sizeDelta = new Vector2(this.playerList.sizeDelta.x, 42f * (float)(players.Length + 1));
			for (int j = 0; j < players.Length; j++)
			{
				Transform transform = Object.Instantiate<GameObject>(this.playerPrefab).transform;
				transform.parent = this.playerList;
				transform.localScale = Vector3.one;
				transform.gameObject.SetActive(true);
				transform.Find("Player").GetComponent<Text>().text = players[j];
				Text component = transform.Find("Rank").GetComponent<Text>();
				component.text = ((j == 0) ? "Host" : "Client");
				component.color = ((j == 0) ? new Color32(byte.MaxValue, 136, 45, byte.MaxValue) : new Color32(45, byte.MaxValue, 45, byte.MaxValue));
			}
		}

		private void Update()
		{
			this.PlayerlistUpdate();
			this.ConsoleUpdate();
			this.pressedEnter = Input.GetKeyDown(13);
		}

		private void PlayerlistUpdate()
		{
			float num = ((Input.GetKey(9) && CoreManager.IsConnected) ? 0f : 1f);
			if (this.playerListPosition != num)
			{
				this.playerListPosition += ((num > this.playerListPosition) ? 1f : (-1f)) * Time.deltaTime * 5f;
				this.playerListPosition = Mathf.Clamp01(this.playerListPosition);
			}
			this.playerList.anchoredPosition = new Vector2(-this.playerList.sizeDelta.x * this.playerListPosition, 0f);
		}

		private void ConsoleUpdate()
		{
			if (this.lastChatboxFocused != this.chatInputField.isFocused)
			{
				this.lastChatboxFocused = this.chatInputField.isFocused;
				_ObjectsLoader.ToggleKeyboardInput(!this.lastChatboxFocused);
			}
			if (Input.GetKeyDown(116))
			{
				this.consoleVisibleTime = 6f;
				this.console.alpha = 1f;
				this.chatInputField.ActivateInputField();
				this.forceChatboxOn = true;
			}
			if (this.forceChatboxOn && this.chatInputField.text.Length > 0)
			{
				this.forceChatboxOn = false;
			}
			if (!this.forceChatboxOn && this.chatInputField.text.Length == 0)
			{
				this.chatInputField.DeactivateInputField();
			}
			if (this.pressedEnter && this.consoleVisibleTime > 1f && this.chatInputField.text.Length > 1)
			{
				string text = this.chatInputField.text;
				if (text.StartsWith("/"))
				{
					if (text.Contains(' '))
					{
						string[] array = text.Split(new char[] { ' ' });
						string[] array2 = new string[array.Length - 1];
						for (int i = 0; i < array2.Length; i++)
						{
							array2[i] = array[i + 1];
						}
						CommandHandler.Execute(array[0], array2);
					}
					else
					{
						CommandHandler.Execute(text, new string[0]);
					}
				}
				else
				{
					this.LogConsolePlayerMessage(text, SteamFriends.GetPersonaName(), WreckMPGlobals.IsHost);
					using (GameEventWriter gameEventWriter = this.chatMessage.Writer())
					{
						gameEventWriter.Write(text);
						this.chatMessage.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
					}
				}
				this.chatInputField.text = "";
			}
			if (this.consoleVisibleTime > 0f && !this.chatInputField.isFocused)
			{
				this.consoleVisibleTime -= 2f * Time.deltaTime;
				if (this.consoleVisibleTime < 1f)
				{
					this.console.alpha = this.consoleVisibleTime;
				}
			}
			if (Input.GetKey(284) && Input.GetKeyDown(100))
			{
				this.consoleContent.text = "";
				this.consoleVisibleTime = 0f;
				this.console.alpha = 0f;
				this.forceChatboxOn = false;
				this.chatInputField.DeactivateInputField();
			}
		}

		private RectTransform playerList;

		private GameObject playMenu;

		private GameObject playerPrefab;

		private InputField joinLobbyCode;

		private InputField openLobbyMaxPlayers;

		private InputField chatInputField;

		private Toggle openLobbyUseSavegame;

		private CanvasGroup console;

		private Text consoleContent;

		private Text playerListPlayerCount;

		private Text playerListLobbycode;

		private const int minPlayers = 2;

		private const int maxPlayers = 200;

		private float consoleVisibleTime;

		private float playerListPosition = 1f;

		private bool forceChatboxOn;

		private bool lastChatboxFocused;

		public static bool allowConsoleInterrupt = true;

		private GameEvent chatMessage;

		private bool pressedEnter;
	}
}
