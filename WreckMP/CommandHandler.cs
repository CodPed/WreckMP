using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Steamworks;
using UnityEngine;

namespace WreckMP
{
	internal static class CommandHandler
	{
		public static void Execute(string command, params string[] args)
		{
			int num = -1;
			for (int i = 0; i < CommandHandler.commands.Length; i++)
			{
				if (CommandHandler.commands[i].name == command)
				{
					num = i;
					break;
				}
			}
			if (num == -1)
			{
				Console.LogError("Unknown command '" + command + "'", true);
				return;
			}
			if (args.Length < CommandHandler.commands[num].argCountMin || args.Length > CommandHandler.commands[num].argCountMax)
			{
				Console.LogError("Invalid syntax. Please use: " + CommandHandler.commands[num].usage, true);
				return;
			}
			if (CommandHandler.commands[num].isHostOnly && !WreckMPGlobals.IsHost)
			{
				Console.LogError("This command can only be executed by the lobby host!", true);
				return;
			}
			Func<string[], bool> handler = CommandHandler.commands[num].handler;
			if (handler != null && !handler(args))
			{
				Console.LogError("Invalid syntax. Please use: " + CommandHandler.commands[num].usage, true);
			}
		}

		private static CommandHandler.Command[] commands = new CommandHandler.Command[]
		{
			new CommandHandler.Command("/tp", "/tp <player>", 1, false, delegate(string[] args)
			{
				int num;
				if (!int.TryParse(args[0], out num))
				{
					Console.LogError("The player argument is not an integer!", true);
					return;
				}
				if (num < 0 || num >= CoreManager.Players.Count)
				{
					Console.LogError("The player argument is out of range!", true);
					return;
				}
				Player value = CoreManager.Players.ToArray<KeyValuePair<ulong, Player>>()[num].Value;
				Transform transform = value.player.transform;
				LocalPlayer.Instance.player.Value.transform.position = transform.position;
				Console.Log("Teleported to " + value.PlayerName, true);
			}),
			new CommandHandler.Command("/plist", "/plist", 0, false, delegate(string[] args)
			{
				KeyValuePair<ulong, Player>[] array = CoreManager.Players.ToArray<KeyValuePair<ulong, Player>>();
				for (int i = 0; i < array.Length; i++)
				{
					Console.Log(string.Format("{0}: {1}", i, array[i].Value.PlayerName), true);
				}
			}),
			new CommandHandler.Command("/ssg", "/ssg <player>", 1, false, delegate(string[] args)
			{
				int num2;
				if (!int.TryParse(args[0], out num2))
				{
					Console.LogError("The player argument is not an integer!", true);
					return;
				}
				if (num2 < 0 || num2 >= CoreManager.Players.Count)
				{
					Console.LogError("The player argument is out of range!", true);
					return;
				}
				KeyValuePair<ulong, Player> keyValuePair = CoreManager.Players.ToArray<KeyValuePair<ulong, Player>>()[num2];
				CoreManager.SendSavegame((CSteamID)keyValuePair.Key);
			}),
			new CommandHandler.Command("/rbhash", "/rbhash <hash>", 1, false, delegate(string[] args)
			{
				int num3;
				if (int.TryParse(args[0], out num3))
				{
					OwnedRigidbody ownedRigidbody = NetRigidbodyManager.GetOwnedRigidbody(num3);
					Console.Log(string.Format("The object of hash {0} is {1}, full path: {2}. DEBUG: {3}, {4}, {5}, {6}, {7}, {8}", new object[]
					{
						num3,
						ownedRigidbody.transform.name,
						ownedRigidbody.transform.GetGameobjectHashString(),
						ownedRigidbody.remove == null,
						ownedRigidbody.Removal_Part == null,
						ownedRigidbody.Removal_Rigidbody == null,
						ownedRigidbody.rigidbody == null,
						ownedRigidbody.rigidbodyPart == null,
						ownedRigidbody.transform == null
					}), true);
					return;
				}
				Console.LogError("The argument is not a number!", true);
			}),
			new CommandHandler.Command("/triggest", "/triggest <player> <trigger true|false> <gesture index>", 3, false, delegate(string[] args)
			{
				int num4;
				if (!int.TryParse(args[0], out num4))
				{
					Console.LogError("The player argument is not an integer!", true);
					return;
				}
				if (num4 < 0 || num4 >= CoreManager.Players.Count)
				{
					Console.LogError("The player argument is out of range!", true);
					return;
				}
				bool flag;
				if (!bool.TryParse(args[1], out flag))
				{
					Console.LogError("The trigger type argument is not a bool!", true);
					return;
				}
				int num5;
				if (!int.TryParse(args[2], out num5))
				{
					Console.LogError("The value argument is not an integer!", true);
					return;
				}
				Player value2 = CoreManager.Players.ToArray<KeyValuePair<ulong, Player>>()[num4].Value;
				PlayerAnimationManager playerAnimationManager = value2.playerAnimationManager;
				if (flag)
				{
					playerAnimationManager.TriggerGesture(num5);
				}
				else
				{
					playerAnimationManager.StopGesture();
				}
				Console.Log(string.Format("{0} gesture {1} of {2}", flag ? "Triggered" : "Stopped", num5, value2.PlayerName), true);
			}),
			new CommandHandler.Command("/resetgrab", "/resetgrab", 0, false, delegate(string[] args)
			{
				PlayerGrabbingManager.handFSM.SendEvent("FINISHED");
				Console.Log("Successfully attempted to reset grabbing fsm", true);
			}),
			new CommandHandler.Command("/satprof", "/satprof", 0, false, delegate(string[] args)
			{
				SatsumaProfiler.Instance.PrintToFile();
				Console.Log("Saved last 10 seconds of satsuma behaviour to satsuma_profiler.txt", true);
			}),
			new CommandHandler.Command("/vehlist", "/vehlist", 0, false, delegate(string[] args)
			{
				for (int j = 0; j < NetVehicleManager.vehicles.Count; j++)
				{
					Console.Log(string.Format("{0} = {1}", j, NetVehicleManager.vehicles[j].Transform.name), true);
				}
			}),
			new CommandHandler.Command("/resetdm", "/resetdm <vehicle index>", 1, false, delegate(string[] args)
			{
				int num6 = int.Parse(args[0]);
				NetVehicleManager.vehicles[num6].Driver = 0UL;
				Console.Log("Successfully resetted " + NetVehicleManager.vehicles[num6].Transform.name + " driving mode", true);
			}),
			new CommandHandler.Command("/fpt", "/fpt <safe|unsafe|default>", 1, false, delegate(string[] args)
			{
				string text = args[0];
				if (!(text == "safe"))
				{
					if (!(text == "unsafe"))
					{
						if (!(text == "default"))
						{
							return false;
						}
						GameEventRouter.overrideSafeSend = null;
					}
					else
					{
						GameEventRouter.overrideSafeSend = new bool?(false);
					}
				}
				else
				{
					GameEventRouter.overrideSafeSend = new bool?(true);
				}
				Console.Log("Set override packet transfer protocol to " + args[0], true);
				return true;
			}),
			new CommandHandler.Command("/togmov", "/togmov <true|false>", 1, false, delegate(string[] args)
			{
				string text2 = args[0];
				bool flag2;
				if (!(text2 == "true"))
				{
					if (!(text2 == "false"))
					{
						return false;
					}
					flag2 = false;
				}
				else
				{
					flag2 = true;
				}
				LocalPlayer.Instance.characterMotor.canControl = flag2;
				Console.Log("Set player canControl to " + args[0], true);
				return true;
			}),
			new CommandHandler.Command("/rec", "/rec <name>", 1, false, delegate(string[] args)
			{
				string text3 = Path.Combine(Application.persistentDataPath, "../MCinematics");
				if (!Directory.Exists(text3))
				{
					Directory.CreateDirectory(text3);
				}
				args[0] = Path.Combine(text3, args[0]);
				GameEventRouter.StartRecording(args[0]);
				Console.Log("Started recording packets...", true);
			}),
			new CommandHandler.Command("/stoprec", "/stoprec", 0, false, delegate(string[] args)
			{
				GameEventRouter.StopRecording();
				Console.Log("Stopped recording packets...", true);
			}),
			new CommandHandler.Command("/play", "/play <name>", 1, false, delegate(string[] args)
			{
				if (NetReplayManager.playbackOngoing)
				{
					Console.LogError("Playback is already ongoing!", true);
					return;
				}
				args[0] = Path.Combine(Application.persistentDataPath, "../MCinematics/" + args[0]);
				if (!File.Exists(args[0]))
				{
					Console.LogError("The file does not exist!", true);
					return;
				}
				NetReplayManager.StartPlayback(args[0]);
			}),
			new CommandHandler.Command("/stopplay", "/stopplay", 0, false, delegate(string[] args)
			{
				NetReplayManager.StopPlayback();
			}),
			new CommandHandler.Command("/togconsole", "/togconsole", 0, false, delegate(string[] args)
			{
				UIManager.allowConsoleInterrupt = !UIManager.allowConsoleInterrupt;
				Console.Log("Successfully " + (UIManager.allowConsoleInterrupt ? "enabled" : "disabled") + " console interrupt", true);
			})
		};

		internal class Command
		{
			public Command(string name, string usage, int argCount, bool isHostOnly, Action<string[]> handler)
			{
				this.ctor(name, usage, argCount, argCount, isHostOnly, delegate(string[] args)
				{
					Action<string[]> action = handler;
					if (action != null)
					{
						action(args);
					}
					return true;
				});
			}

			public Command(string name, string usage, int argCountMin, int argCountMax, bool isHostOnly, Action<string[]> handler)
			{
				this.ctor(name, usage, argCountMin, argCountMax, isHostOnly, delegate(string[] args)
				{
					Action<string[]> action = handler;
					if (action != null)
					{
						action(args);
					}
					return true;
				});
			}

			public Command(string name, string usage, int argCount, bool isHostOnly, Func<string[], bool> handler)
			{
				this.ctor(name, usage, argCount, argCount, isHostOnly, handler);
			}

			public Command(string name, string usage, int argCountMin, int argCountMax, bool isHostOnly, Func<string[], bool> handler)
			{
				this.ctor(name, usage, argCountMin, argCountMax, isHostOnly, handler);
			}

			private void ctor(string name, string usage, int argCountMin, int argCountMax, bool isHostOnly, Func<string[], bool> handler)
			{
				this.name = name;
				this.usage = usage;
				this.argCountMin = argCountMin;
				this.argCountMax = argCountMax;
				this.isHostOnly = isHostOnly;
				this.handler = handler;
			}

			public string name;

			public string usage;

			public bool isHostOnly;

			public int argCountMin;

			public int argCountMax;

			public Func<string[], bool> handler;
		}
	}
}
