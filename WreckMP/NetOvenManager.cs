using System;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	internal class NetOvenManager : NetManager
	{
		private void Start()
		{
			new GameEvent<NetOvenManager>("KnobTurn", new Action<ulong, GameEventReader>(this.OnKnobTurn), GameScene.GAME);
			new GameEvent<NetOvenManager>("SimSync", new Action<ulong, GameEventReader>(this.OnSimSync), GameScene.GAME);
			this.knobData = new FsmFloat[4];
			this.knobRot = new FsmFloat[4];
			this.hotplateTemps = new FsmFloat[4];
			this.knobMesh = new Transform[4];
			Action<ulong>[] knobSyncs = new Action<ulong>[4];
			Transform transform = GameObject.Find("YARD").transform.Find("Building/KITCHEN/OvenStove");
			for (int i = 0; i < 4; i++)
			{
				PlayMakerFSM playMaker = transform.Find("KnobPower" + (i + 1).ToString()).GetPlayMaker("Screw");
				FsmFloat data = playMaker.FsmVariables.FindFsmFloat("Data");
				FsmFloat fsmFloat = playMaker.FsmVariables.FindFsmFloat("Rot");
				Transform mesh = playMaker.FsmVariables.FindFsmGameObject("Mesh").Value.transform;
				this.knobData[i] = data;
				this.knobRot[i] = fsmFloat;
				this.knobMesh[i] = mesh;
				Action<ulong> a = delegate(ulong target)
				{
					using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter("KnobTurn"))
					{
						int num = 0;
						for (int k = 0; k < 4; k++)
						{
							if (this.knobMesh[k] == mesh)
							{
								num = k;
								break;
							}
						}
						gameEventWriter.Write(num);
						gameEventWriter.Write(data.Value);
						if (target == 0UL)
						{
							GameEvent<NetOvenManager>.Send("KnobTurn", gameEventWriter, 0UL, true);
						}
						else
						{
							GameEvent<NetOvenManager>.Send("KnobTurn", gameEventWriter, target, true);
						}
					}
				};
				playMaker.InsertAction("State 1", delegate
				{
					a(0UL);
				}, -1, false);
				knobSyncs[i] = a;
			}
			PlayMakerFSM playMaker2 = GameObject.Find("YARD").transform.Find("Building/KITCHEN/OvenStove/Simulation").GetPlayMaker("Data");
			for (int j = 0; j < 4; j++)
			{
				this.hotplateTemps[j] = playMaker2.FsmVariables.FindFsmFloat(string.Format("HotPlate{0}Heat", j + 1));
			}
			WreckMPGlobals.OnMemberReady.Add(delegate(ulong user)
			{
				if (WreckMPGlobals.IsHost)
				{
					for (int l = 0; l < 4; l++)
					{
						knobSyncs[l](user);
					}
					this.SyncSim(user);
				}
			});
		}

		private void Update()
		{
			if (!WreckMPGlobals.IsHost)
			{
				return;
			}
			this.stoveSimulationSyncTime += Time.deltaTime;
			if (this.stoveSimulationSyncTime >= 10f)
			{
				this.stoveSimulationSyncTime = 0f;
				this.SyncSim(0UL);
			}
		}

		private void SyncSim(ulong target = 0UL)
		{
			using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter("SimSync"))
			{
				for (int i = 0; i < 4; i++)
				{
					gameEventWriter.Write(this.hotplateTemps[i].Value);
				}
				if (target == 0UL)
				{
					GameEvent<NetOvenManager>.Send("SimSync", gameEventWriter, 0UL, true);
				}
				else
				{
					GameEvent<NetOvenManager>.Send("SimSync", gameEventWriter, target, true);
				}
			}
		}

		private void OnSimSync(ulong sender, GameEventReader packet)
		{
			for (int i = 0; i < this.hotplateTemps.Length; i++)
			{
				this.hotplateTemps[i].Value = packet.ReadSingle();
			}
		}

		private void OnKnobTurn(ulong sender, GameEventReader packet)
		{
			int num = packet.ReadInt32();
			float num2 = packet.ReadSingle();
			if (num > 4)
			{
				return;
			}
			this.knobData[num].Value = (this.knobRot[num].Value = num2);
			this.knobMesh[num].localEulerAngles = Vector3.up * num2;
		}

		private FsmFloat[] knobData;

		private FsmFloat[] knobRot;

		private FsmFloat[] hotplateTemps;

		private Transform[] knobMesh;

		private const string KnobTurnEvent = "KnobTurn";

		private const string SimSyncEvent = "SimSync";

		private float stoveSimulationSyncTime = 10f;
	}
}
