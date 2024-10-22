using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Steamworks;

namespace WreckMP
{
	internal class SteamNet
	{
		public static CSteamID[] GetMembers()
		{
			LobbyID lobbyID = SteamNet.currentLobby;
			int numLobbyMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
			CSteamID[] array = new CSteamID[numLobbyMembers];
			for (int i = 0; i < numLobbyMembers; i++)
			{
				array[i] = SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, i);
			}
			return array;
		}

		public static void CloseConnections()
		{
			if (SteamNet.p2pConnections == null)
			{
				return;
			}
			for (int i = 0; i < SteamNet.p2pConnections.Count; i++)
			{
				SteamNet.CloseConnection(SteamNet.p2pConnections[i]);
			}
			SteamNet.p2pConnections.Clear();
		}

		public static void CloseConnection(CSteamID user)
		{
			SteamNetworking.CloseP2PSessionWithUser(user);
			SteamNet.p2pConnections.Remove(user);
		}

		private static void OnP2PSessionConnectFail(P2PSessionConnectFail_t cb)
		{
			Console.LogError(string.Format("P2P Connection failed {0}", cb.m_eP2PSessionError), false);
		}

		private static void OnP2PSessionRequest(P2PSessionRequest_t cb)
		{
			bool flag = SteamNet.allowedGhostPlayer == cb.m_steamIDRemote;
			if (SteamNet.p2pConnections.Count + 2 >= CoreManager.maxPlayers && !flag)
			{
				return;
			}
			if (SteamNetworking.AcceptP2PSessionWithUser(cb.m_steamIDRemote))
			{
				CoreManager.OnMemberConnect(cb.m_steamIDRemote);
				if (!SteamNet.p2pConnections.Contains(cb.m_steamIDRemote))
				{
					SteamNet.p2pConnections.Add(cb.m_steamIDRemote);
				}
				if (!flag)
				{
					Console.Log(SteamFriends.GetFriendPersonaName(cb.m_steamIDRemote) + " joined the game.", true);
				}
				SteamNet.SetActivity();
				return;
			}
			if (!flag)
			{
				Console.Log(SteamFriends.GetFriendPersonaName(cb.m_steamIDRemote) + " failed to join.", true);
			}
		}

		private static void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t cb)
		{
			SteamMatchmaking.JoinLobby(cb.m_steamIDLobby);
		}

		private static void OnLobbyCreated(LobbyCreated_t cb)
		{
			SteamNet.p2pConnections = new List<CSteamID>();
			ulong ulSteamIDLobby = cb.m_ulSteamIDLobby;
			SteamMatchmaking.SetLobbyData((CSteamID)ulSteamIDLobby, "ver", WreckMP.version);
			SteamMatchmaking.SetLobbyData((CSteamID)ulSteamIDLobby, "gver", SteamApps.GetAppBuildId().ToString());
			SteamNet.currentLobby = new LobbyID(LobbyCodeParser.GetString(ulSteamIDLobby));
			if (cb.m_eResult == 1)
			{
				Console.Log(string.Format("Created Lobby {0}", SteamNet.currentLobby), true);
				Clipboard.text = SteamNet.currentLobby.ToString();
				Console.Log("Copied Lobby ID to Clipboard.", true);
				CoreManager.OnLobbyCreate();
				return;
			}
			if (cb.m_eResult == 16)
			{
				Console.LogError("Creating lobby timed out. Please try again. If the issue remains, Steam servers are most likely not working properly.", true);
				return;
			}
			Console.LogError(string.Format("Creating Lobby failed. {0}", cb.m_eResult), true);
		}

		private static void OnLobbyEnter(LobbyEnter_t cb)
		{
			SteamNet.p2pConnections = new List<CSteamID>();
			if (cb.m_EChatRoomEnterResponse == 1U)
			{
				SteamNet.currentLobby = new LobbyID(LobbyCodeParser.GetString(cb.m_ulSteamIDLobby));
				SteamNet.p2pConnections = SteamNet.GetMembers().ToList<CSteamID>();
				SteamNet.p2pConnections.Remove(SteamUser.GetSteamID());
				CoreManager.OnConnect();
				Console.Log(string.Format("Joined Lobby {0}", SteamNet.currentLobby), true);
				CoreManager.maxPlayers = SteamMatchmaking.GetLobbyMemberLimit((CSteamID)cb.m_ulSteamIDLobby);
				CoreManager.SendData(GameEventRouter.handshakeMessage, true, SteamNet.p2pConnections);
				SteamNet.UpdatePlayerList();
				SteamNet.SetActivity();
				return;
			}
			Console.LogError(string.Format("Joining Lobby failed. {0}", cb.m_EChatRoomEnterResponse), true);
		}

		private static void UpdatePlayerList()
		{
			string[] array = new string[SteamNet.p2pConnections.Count + ((SteamNet.p2pConnections.Contains(SteamNet.allowedGhostPlayer) && SteamNet.allowedGhostPlayer.m_SteamID != WreckMPGlobals.HostID) ? 0 : 1)];
			array[0] = (WreckMPGlobals.IsHost ? SteamFriends.GetPersonaName() : SteamFriends.GetFriendPersonaName((CSteamID)WreckMPGlobals.HostID));
			bool flag = false;
			for (int i = 1; i < array.Length; i++)
			{
				int num = (flag ? i : (i - 1));
				if (SteamNet.p2pConnections[num] == SteamNet.allowedGhostPlayer && SteamNet.allowedGhostPlayer.m_SteamID != WreckMPGlobals.HostID)
				{
					flag = true;
					i--;
				}
				else if (SteamNet.p2pConnections[num].m_SteamID == WreckMPGlobals.HostID)
				{
					array[i] = SteamFriends.GetPersonaName();
				}
				else
				{
					array[i] = SteamFriends.GetFriendPersonaName(SteamNet.p2pConnections[num]);
				}
			}
			CoreManager.uiManager.UpdatePlayerlist(SteamNet.currentLobby.m_LobbyCode, CoreManager.maxPlayers - 1, array);
		}

		public static void Start()
		{
			SteamAPI.Init();
			SteamNet.onP2PSessionConnectFail = Callback<P2PSessionConnectFail_t>.Create(new Callback<P2PSessionConnectFail_t>.DispatchDelegate(SteamNet.OnP2PSessionConnectFail));
			SteamNet.onP2PSessionRequest = Callback<P2PSessionRequest_t>.Create(new Callback<P2PSessionRequest_t>.DispatchDelegate(SteamNet.OnP2PSessionRequest));
			SteamNet.onGameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(new Callback<GameLobbyJoinRequested_t>.DispatchDelegate(SteamNet.OnGameLobbyJoinRequested));
			SteamNet.onLobbyCreated = Callback<LobbyCreated_t>.Create(new Callback<LobbyCreated_t>.DispatchDelegate(SteamNet.OnLobbyCreated));
			SteamNet.onLobbyEnter = Callback<LobbyEnter_t>.Create(new Callback<LobbyEnter_t>.DispatchDelegate(SteamNet.OnLobbyEnter));
			SteamNet.onLobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(new Callback<LobbyChatUpdate_t>.DispatchDelegate(SteamNet.OnLobbyMemberStateUpdate));
			SteamNet.onLobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(new Callback<LobbyDataUpdate_t>.DispatchDelegate(SteamNet.OnLobbyDataUpdate));
			Callback<LobbyMatchList_t>.Create(delegate(LobbyMatchList_t list)
			{
				Console.Log(string.Format("Public lobbies: {0}", list.m_nLobbiesMatching), false);
				int num = 0;
				while ((long)num < (long)((ulong)list.m_nLobbiesMatching))
				{
					Console.Log(LobbyCodeParser.GetString(SteamMatchmaking.GetLobbyByIndex(num).m_SteamID) ?? "", false);
					num++;
				}
			});
			SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);
			SteamMatchmaking.RequestLobbyList();
		}

		private static void OnLobbyDataUpdate(LobbyDataUpdate_t param)
		{
			if (!SteamNet.joiningLobby)
			{
				return;
			}
			SteamNet.joiningLobby = false;
			CSteamID csteamID = (CSteamID)param.m_ulSteamIDLobby;
			string @string = LobbyCodeParser.GetString(param.m_ulSteamIDLobby);
			string lobbyData = SteamMatchmaking.GetLobbyData(csteamID, "gver");
			string lobbyData2 = SteamMatchmaking.GetLobbyData(csteamID, "ver");
			string text = SteamApps.GetAppBuildId().ToString();
			if (string.IsNullOrEmpty(lobbyData))
			{
				Console.LogError("Can't join lobby " + @string + " because it's still loading (ERR: gver null)", true);
				return;
			}
			if (lobbyData != text)
			{
				Console.LogError(string.Concat(new string[] { "Can't join lobby ", @string, " because it's targetting game version ", lobbyData, " (current version is ", text, ")" }), true);
				return;
			}
			if (string.IsNullOrEmpty(lobbyData2))
			{
				Console.LogError("Can't join lobby " + @string + " because it's still loading (ERR: ver null)", true);
				return;
			}
			if (lobbyData2 != WreckMP.version)
			{
				Console.LogError(string.Concat(new string[]
				{
					"Can't join lobby ",
					@string,
					" because it's targetting WreckMP version ",
					lobbyData2,
					" (current version is ",
					WreckMP.version,
					")"
				}), true);
				return;
			}
			SteamMatchmaking.JoinLobby(csteamID);
			Console.Log("Trying to Join Lobby (" + @string + ")...", true);
		}

		private static void OnLobbyMemberStateUpdate(LobbyChatUpdate_t param)
		{
			CSteamID csteamID = (CSteamID)param.m_ulSteamIDUserChanged;
			if (param.m_rgfChatMemberStateChange == 2U)
			{
				if (param.m_ulSteamIDUserChanged == WreckMPGlobals.HostID)
				{
					CoreManager.Disconnect();
					Console.Log("Session closed. Host disconnected.", true);
					return;
				}
				SteamNet.CloseConnection(csteamID);
				CoreManager.OnMemberDisconnect(csteamID);
			}
		}

		public static void JoinLobby(string steamIDLobby)
		{
			if (!SteamMatchmaking.RequestLobbyData((CSteamID)LobbyCodeParser.GetUlong(steamIDLobby)))
			{
				Console.LogError("Can't join lobby " + steamIDLobby + " failed to request lobby data", true);
				return;
			}
			SteamNet.joiningLobby = true;
			Console.Log("Requesting Lobby data ...", true);
		}

		public static void CreateLobby(CSteamID owner, int maxPlayers, ELobbyType lobbyType)
		{
			SteamMatchmaking.CreateLobby(lobbyType, maxPlayers);
		}

		public static void GetNetworkData()
		{
			uint num;
			while (SteamNetworking.IsP2PPacketAvailable(ref num, 0))
			{
				byte[] array = new byte[num];
				uint num2 = 0U;
				CSteamID csteamID;
				if (SteamNetworking.ReadP2PPacket(array, num, ref num2, ref csteamID, 0) && csteamID != SteamUser.GetSteamID())
				{
					CoreManager.OnReceive(csteamID.m_SteamID, array, (int)num2);
				}
			}
		}

		public static void CheckConnections()
		{
			foreach (CSteamID csteamID in SteamNet.p2pConnections)
			{
				P2PSessionState_t p2PSessionState_t;
				SteamNetworking.GetP2PSessionState(csteamID, ref p2PSessionState_t);
				bool flag = Convert.ToBoolean(p2PSessionState_t.m_bConnecting);
				bool flag2 = Convert.ToBoolean(p2PSessionState_t.m_bConnectionActive);
				if (!flag && !flag2)
				{
					if (SteamNet.p2pConnections.Contains(csteamID))
					{
						SteamNet.p2pConnections.Remove(csteamID);
					}
					CoreManager.OnMemberDisconnect(csteamID);
					SteamNet.SetActivity();
				}
			}
		}

		private static void SetActivity()
		{
			SteamNet.activity = new Activity
			{
				State = string.Format("Playing Online ({0} of {1})", SteamNet.p2pConnections.Count, CoreManager.maxPlayers),
				Details = "Roaming the roads of Alivieska",
				Type = ActivityType.Playing,
				Timestamps = new ActivityTimestamps
				{
					Start = DateTime.Now.ToUnixTimestamp()
				},
				Instance = true
			};
			SteamNet.UpdatePlayerList();
			WreckMP.UpdateActivity(SteamNet.activity);
		}

		public static LobbyID currentLobby;

		internal static List<CSteamID> p2pConnections;

		internal static readonly CSteamID allowedGhostPlayer = (CSteamID)76561198990857830UL;

		private static Callback<P2PSessionConnectFail_t> onP2PSessionConnectFail = null;

		private static Callback<P2PSessionRequest_t> onP2PSessionRequest = null;

		private static Callback<GameLobbyJoinRequested_t> onGameLobbyJoinRequested = null;

		private static Callback<LobbyCreated_t> onLobbyCreated = null;

		private static Callback<LobbyEnter_t> onLobbyEnter = null;

		private static Callback<LobbyChatUpdate_t> onLobbyChatUpdate = null;

		private static Callback<LobbyDataUpdate_t> onLobbyDataUpdate = null;

		private static Activity activity;

		private static bool joiningLobby = false;
	}
}
