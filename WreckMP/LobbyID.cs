using System;
using Steamworks;

namespace WreckMP
{
	internal struct LobbyID
	{
		public LobbyID(CSteamID cSteamID, string lobbyCode)
		{
			this.m_SteamID = cSteamID;
			this.m_LobbyCode = lobbyCode;
		}

		public LobbyID(CSteamID cSteamID)
		{
			this.m_SteamID = cSteamID;
			this.m_LobbyCode = LobbyCodeParser.GetString(cSteamID.m_SteamID);
		}

		public LobbyID(string lobbyCode)
		{
			this.m_SteamID = new CSteamID(LobbyCodeParser.GetUlong(lobbyCode));
			this.m_LobbyCode = lobbyCode;
		}

		public static explicit operator LobbyID(string lobbyCode)
		{
			return new LobbyID(lobbyCode);
		}

		public static explicit operator LobbyID(CSteamID cSteamID)
		{
			return new LobbyID(cSteamID);
		}

		public static implicit operator CSteamID(LobbyID lobbyID)
		{
			return lobbyID.m_SteamID;
		}

		public static implicit operator string(LobbyID lobbyID)
		{
			return lobbyID.m_LobbyCode;
		}

		public override string ToString()
		{
			return this.m_LobbyCode;
		}

		public override int GetHashCode()
		{
			return this.m_SteamID.m_SteamID.GetHashCode();
		}

		public CSteamID m_SteamID;

		public string m_LobbyCode;
	}
}
