using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Steamworks;

namespace WreckMP
{
	public class WreckMPGlobals
	{
		public static bool IsHost
		{
			get
			{
				return !NetReplayManager.playbackOngoing && CoreManager.IsHost;
			}
		}

		public static ulong HostID
		{
			get
			{
				if (NetReplayManager.playbackOngoing)
				{
					return 1UL;
				}
				if (!(CoreManager.HostID != default(CSteamID)))
				{
					return 0UL;
				}
				return CoreManager.HostID.m_SteamID;
			}
		}

		public static ulong UserID
		{
			get
			{
				if (!(CoreManager.UserID != default(CSteamID)))
				{
					return 0UL;
				}
				return CoreManager.UserID.m_SteamID;
			}
		}

		public static Dictionary<ulong, Player> Players
		{
			get
			{
				return CoreManager.Players;
			}
		}

		public static Action<ulong> OnMemberJoin;

		public static List<Action<ulong>> OnMemberReady = new List<Action<ulong>>();

		public static Action<ulong> OnMemberExit;

		internal static bool ModLoaderInstalled = File.Exists("mysummercar_Data\\Managed\\MSCLoader.dll") && File.Exists("mysummercar_Data\\Managed\\MSCLoader.Preloader.dll") && !Environment.GetCommandLineArgs().Any((string x) => x.Contains("-mscloader-disable"));

		internal static Assembly mscloader = (WreckMPGlobals.ModLoaderInstalled ? Assembly.LoadFile("mysummercar_Data\\Managed\\MSCLoader.dll") : null);
	}
}
