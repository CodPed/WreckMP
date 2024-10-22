using System;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace WreckMP
{
	internal class NetGameWorldManager : NetManager
	{
		private void OnMemberReady(ulong userId)
		{
			if (!WreckMPGlobals.IsHost)
			{
				return;
			}
			this.SendTimeUpdate(userId);
			this.SendWeatherUpdate(userId);
		}

		private void Start()
		{
			GameObject gameObject = GameObject.Find("MAP/SUN/Pivot/SUN");
			this.sunFSM = gameObject.GetPlayMaker("Color");
			this.day = FsmVariables.GlobalVariables.FindFsmInt("GlobalDay");
			this.time = this.sunFSM.FsmVariables.FindFsmInt("Time");
			this.minutes = this.sunFSM.FsmVariables.FindFsmFloat("Minutes");
			this.sunFSM.InsertAction("State 3", new PM_Hook(delegate
			{
				if (this.IsLocal)
				{
					this.SendTimeUpdate(0UL);
				}
				this.IsLocal = true;
			}, false), 0);
			PlayMakerFSM playMaker = GameObject.Find("YARD").transform.Find("Building/Dynamics/SuomiClock/Clock").GetPlayMaker("Time");
			playMaker.Initialize();
			FsmState state = playMaker.GetState("Set time");
			FsmStateAction[] actions = state.Actions;
			SetRotation setRotation = actions[1] as SetRotation;
			GameObject needle = setRotation.gameObject.GameObject.Value;
			actions[1] = new PM_Hook(delegate
			{
				float num2 = this.minutes.Value % 60f;
				num2 = 60f - num2;
				num2 = num2 / 60f * 360f;
				needle.transform.localEulerAngles = Vector3.up * num2;
			}, false);
			state.Actions = actions;
			Action <>9__3;
			for (int i = 0; i < 12; i++)
			{
				FsmState fsmState = this.sunFSM.FsmStates[i];
				FsmStateAction[] actions2 = fsmState.Actions;
				for (int j = 0; j < actions2.Length; j++)
				{
					SetFloatValue setFloatValue = actions2[j] as SetFloatValue;
					if (setFloatValue != null && setFloatValue.floatVariable == this.minutes)
					{
						FsmStateAction[] array = actions2;
						int num = j;
						Action action;
						if ((action = <>9__3) == null)
						{
							action = (<>9__3 = delegate
							{
								if (!this.UpdatingMinutes)
								{
									this.minutes.Value = 0f;
								}
								this.UpdatingMinutes = false;
							});
						}
						array[num] = new PM_Hook(action, false);
					}
				}
				fsmState.Actions = actions2;
			}
			GameObject gameObject2 = GameObject.Find("MAP/CloudSystem/Clouds");
			this.weatherFSM = gameObject2.GetPlayMaker("Weather");
			this.offset = this.weatherFSM.FsmVariables.FindFsmFloat("Offset");
			this.posX = this.weatherFSM.FsmVariables.FindFsmFloat("PosX");
			this.posZ = this.weatherFSM.FsmVariables.FindFsmFloat("PosZ");
			this.rotation = this.weatherFSM.FsmVariables.FindFsmFloat("Rotation");
			this.x = this.weatherFSM.FsmVariables.FindFsmFloat("X");
			this.z = this.weatherFSM.FsmVariables.FindFsmFloat("Z");
			this.weatherCloudID = this.weatherFSM.FsmVariables.FindFsmInt("WeatherCloudID");
			this.weatherType = this.weatherFSM.FsmVariables.FindFsmInt("WeatherType");
			this.rain = this.weatherFSM.FsmVariables.FindFsmBool("Rain");
			this.updateWeather = this.weatherFSM.AddEvent("MP_UpdateWeather");
			this.weatherFSM.AddGlobalTransition(this.updateWeather, "Set cloud");
			if (WreckMPGlobals.IsHost)
			{
				this.weatherFSM.InsertAction("Move clouds", new PM_Hook(delegate
				{
					this.SendWeatherUpdate(0UL);
				}, false), 0);
			}
			WreckMPGlobals.OnMemberReady.Add(new Action<ulong>(this.OnMemberReady));
			this.TimeChange = new GameEvent("TimeChange", new Action<GameEventReader>(this.OnTimeChange), GameScene.GAME);
			this.WeatherChange = new GameEvent("WeatherChange", new Action<GameEventReader>(this.OnWeatherChange), GameScene.GAME);
			if (!WreckMPGlobals.IsHost)
			{
				FsmStateAction[] actions3 = this.weatherFSM.GetState("Load game").Actions;
				for (int k = 0; k < actions3.Length; k++)
				{
					actions3[k].Enabled = false;
				}
				FsmStateAction[] actions4 = this.sunFSM.GetState("Load").Actions;
				for (int l = 0; l < actions4.Length; l++)
				{
					actions4[l].Enabled = false;
				}
			}
		}

		private void OnTimeChange(GameEventReader packet)
		{
			int num = packet.ReadInt32();
			int num2 = packet.ReadInt32();
			float num3 = packet.ReadSingle();
			Console.Log(string.Format("Time update: {0}, {1}:{2}", num, num2, num3), true);
			this.day.Value = num;
			this.time.Value = num2;
			this.minutes.Value = num3;
			this.UpdatingMinutes = true;
			this.IsLocal = false;
			this.sunFSM.SendEvent("WAKEUP");
		}

		private void SendTimeUpdate(ulong target = 0UL)
		{
			using (GameEventWriter gameEventWriter = this.TimeChange.Writer())
			{
				gameEventWriter.Write(this.day.Value);
				gameEventWriter.Write(this.time.Value);
				gameEventWriter.Write(this.minutes.Value);
				this.TimeChange.Send(gameEventWriter, target, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnWeatherChange(GameEventReader packet)
		{
			float num = packet.ReadSingle();
			float num2 = packet.ReadSingle();
			float num3 = packet.ReadSingle();
			float num4 = packet.ReadSingle();
			float num5 = packet.ReadSingle();
			float num6 = packet.ReadSingle();
			this.weatherCloudID.Value = packet.ReadInt32();
			this.weatherType.Value = packet.ReadInt32();
			this.weatherFSM.Fsm.Event(this.updateWeather);
			this.offset.Value = num;
			this.posX.Value = num2;
			this.posZ.Value = num3;
			this.rotation.Value = num4;
			this.weatherFSM.transform.eulerAngles = Vector3.up * num4;
			this.x.Value = num5;
			this.z.Value = num6;
		}

		private void SendWeatherUpdate(ulong target = 0UL)
		{
			using (GameEventWriter gameEventWriter = this.WeatherChange.Writer())
			{
				gameEventWriter.Write(this.offset.Value);
				gameEventWriter.Write(this.posX.Value);
				gameEventWriter.Write(this.posZ.Value);
				gameEventWriter.Write(this.rotation.Value);
				gameEventWriter.Write(this.x.Value);
				gameEventWriter.Write(this.z.Value);
				gameEventWriter.Write(this.weatherCloudID.Value);
				gameEventWriter.Write(this.weatherType.Value);
				this.WeatherChange.Send(gameEventWriter, target, true, default(GameEvent.RecordingProperties));
			}
		}

		private PlayMakerFSM sunFSM;

		private FsmInt day;

		private FsmInt time;

		private FsmFloat minutes;

		private PlayMakerFSM weatherFSM;

		private FsmFloat offset;

		private FsmFloat posX;

		private FsmFloat posZ;

		private FsmFloat rotation;

		private FsmFloat x;

		private FsmFloat z;

		private FsmInt weatherCloudID;

		private FsmInt weatherType;

		private FsmBool rain;

		private FsmEvent updateWeather;

		private bool IsLocal = true;

		private bool UpdatingMinutes;

		private GameEvent TimeChange;

		private GameEvent WeatherChange;
	}
}
