using System;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	internal class NetSaunaManager : NetManager
	{
		private void Start()
		{
			new GameEvent<NetSaunaManager>("Knob", new Action<ulong, GameEventReader>(this.OnKnobScrew), GameScene.GAME);
			new GameEvent<NetSaunaManager>("SimSync", new Action<ulong, GameEventReader>(this.OnSimSync), GameScene.GAME);
			new GameEvent<NetSaunaManager>("Steam", new Action<ulong, GameEventReader>(this.OnSteam), GameScene.GAME);
			Transform transform = GameObject.Find("YARD").transform.Find("Building/SAUNA/Sauna");
			PlayMakerFSM playMaker = transform.Find("Simulation").GetPlayMaker("Time");
			this.simMaxHeat = playMaker.FsmVariables.FindFsmFloat("MaxHeat");
			this.simTimer = playMaker.FsmVariables.FindFsmFloat("Time");
			this.simMaxSaunaHeat = playMaker.FsmVariables.FindFsmFloat("MaxSaunaHeat");
			this.simSaunaHeat = playMaker.FsmVariables.FindFsmFloat("SaunaHeat");
			this.simStoveHeat = playMaker.FsmVariables.FindFsmFloat("StoveHeat");
			this.simCoolingSauna = playMaker.FsmVariables.FindFsmFloat("CoolingSauna");
			PlayMakerFSM playMaker2 = transform.Find("Kiuas/ButtonPower").GetPlayMaker("Screw");
			this.powerRot = playMaker2.FsmVariables.FindFsmFloat("Rot");
			this.maxHeat = playMaker2.FsmVariables.FindFsmFloat("MaxHeat");
			this.powerKnobMesh = playMaker2.FsmVariables.FindFsmGameObject("CapMesh").Value.transform;
			Action<ulong> a = delegate(ulong target)
			{
				using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter("Knob"))
				{
					gameEventWriter.Write(false);
					gameEventWriter.Write(this.powerRot.Value);
					if (target == 0UL)
					{
						GameEvent<NetSaunaManager>.Send("Knob", gameEventWriter, 0UL, true);
					}
					else
					{
						GameEvent<NetSaunaManager>.Send("Knob", gameEventWriter, target, true);
					}
				}
			};
			playMaker2.InsertAction("Wait", delegate
			{
				a(0UL);
			}, -1, false);
			PlayMakerFSM playMaker3 = transform.Find("Kiuas/ButtonTime").GetPlayMaker("Screw");
			this.timerRot = playMaker3.FsmVariables.FindFsmFloat("Timer");
			this.timerMath1 = playMaker3.FsmVariables.FindFsmFloat("Math1");
			this.timerKnobMesh = playMaker3.FsmVariables.FindFsmGameObject("CapMesh").Value.transform;
			Action<ulong> b = delegate(ulong target)
			{
				using (GameEventWriter gameEventWriter2 = GameEvent.EmptyWriter("Knob"))
				{
					gameEventWriter2.Write(true);
					gameEventWriter2.Write(this.timerRot.Value);
					if (target == 0UL)
					{
						GameEvent<NetSaunaManager>.Send("Knob", gameEventWriter2, 0UL, true);
					}
					else
					{
						GameEvent<NetSaunaManager>.Send("Knob", gameEventWriter2, target, true);
					}
				}
			};
			playMaker3.InsertAction("Wait", delegate
			{
				b(0UL);
			}, -1, false);
			this.stoveTrigger = transform.Find("Kiuas/StoveTrigger").GetPlayMaker("Steam");
			this.stoveTrigger.InsertAction("Calc blur", delegate
			{
				if (this.receivedSteamEvent)
				{
					this.receivedSteamEvent = false;
					return;
				}
				using (GameEventWriter gameEventWriter3 = GameEvent.EmptyWriter("Steam"))
				{
					GameEvent<NetSaunaManager>.Send("Steam", gameEventWriter3, 0UL, true);
				}
			}, -1, false);
			WreckMPGlobals.OnMemberReady.Add(delegate(ulong user)
			{
				a(user);
				b(user);
				this.SyncSim(user);
			});
		}

		private void Update()
		{
			if (!WreckMPGlobals.IsHost)
			{
				return;
			}
			this.saunaSimSyncTime += Time.deltaTime;
			if (this.saunaSimSyncTime >= 10f)
			{
				this.saunaSimSyncTime = 0f;
				this.SyncSim(0UL);
			}
		}

		private void SyncSim(ulong target = 0UL)
		{
			using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter("SimSync"))
			{
				gameEventWriter.Write(this.simMaxSaunaHeat.Value);
				gameEventWriter.Write(this.simSaunaHeat.Value);
				gameEventWriter.Write(this.simStoveHeat.Value);
				gameEventWriter.Write(this.simCoolingSauna.Value);
				if (target == 0UL)
				{
					GameEvent<NetSaunaManager>.Send("SimSync", gameEventWriter, 0UL, true);
				}
				else
				{
					GameEvent<NetSaunaManager>.Send("SimSync", gameEventWriter, target, true);
				}
			}
		}

		private void OnSteam(ulong sender, GameEventReader packet)
		{
			this.receivedSteamEvent = true;
			this.stoveTrigger.SendEvent("GLOBALEVENT");
		}

		private void OnSimSync(ulong sender, GameEventReader packet)
		{
			this.simMaxSaunaHeat.Value = packet.ReadSingle();
			this.simSaunaHeat.Value = packet.ReadSingle();
			this.simStoveHeat.Value = packet.ReadSingle();
			this.simCoolingSauna.Value = packet.ReadSingle();
		}

		private void OnKnobScrew(ulong sender, GameEventReader packet)
		{
			bool flag = packet.ReadBoolean();
			float num = packet.ReadSingle();
			if (flag)
			{
				this.timerRot.Value = num;
				this.timerMath1.Value = (this.simTimer.Value = num * 6f);
				this.timerKnobMesh.localEulerAngles = Vector3.up * num;
			}
			else
			{
				this.powerRot.Value = num;
				this.maxHeat.Value = (this.simMaxHeat.Value = num / 300f);
				this.powerKnobMesh.localEulerAngles = Vector3.up * num;
			}
			MasterAudio.PlaySound3DAndForget("HouseFoley", flag ? this.timerKnobMesh : this.powerKnobMesh, false, 1f, null, 0f, "sauna_stove_knob");
		}

		private FsmFloat powerRot;

		private FsmFloat maxHeat;

		private FsmFloat timerRot;

		private FsmFloat timerMath1;

		private FsmFloat simMaxHeat;

		private FsmFloat simTimer;

		private FsmFloat simMaxSaunaHeat;

		private FsmFloat simSaunaHeat;

		private FsmFloat simStoveHeat;

		private FsmFloat simCoolingSauna;

		private Transform powerKnobMesh;

		private Transform timerKnobMesh;

		private PlayMakerFSM stoveTrigger;

		private float saunaSimSyncTime = 10f;

		private bool receivedSteamEvent;
	}
}
