using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	internal class NetWaterSourceManager : NetManager
	{
		private void Start()
		{
			new GameEvent<NetWaterSourceManager>("Tap", new Action<ulong, GameEventReader>(this.OnWaterTap), GameScene.GAME);
			new GameEvent<NetWaterSourceManager>("Shower", new Action<ulong, GameEventReader>(this.OnShower), GameScene.GAME);
			new GameEvent<NetWaterSourceManager>("Well", new Action<ulong, GameEventReader>(this.OnWaterWell), GameScene.GAME);
			List<PlayMakerFSM> list = (from x in Resources.FindObjectsOfTypeAll<PlayMakerFSM>()
				where x.FsmName == "Use"
				select x).ToList<PlayMakerFSM>();
			for (int i = 0; i < list.Count; i++)
			{
				PlayMakerFSM fsm = list[i];
				if (!(fsm.transform.parent == null))
				{
					if (fsm.transform.parent.name == "KitchenWaterTap")
					{
						FsmBool tapOn2 = fsm.FsmVariables.FindFsmBool("SwitchOn");
						FsmEvent fsmEvent = fsm.AddEvent("MP_ON");
						fsm.AddGlobalTransition(fsmEvent, "ON");
						FsmEvent fsmEvent2 = fsm.AddEvent("MP_OFF");
						fsm.AddGlobalTransition(fsmEvent2, "OFF");
						fsm.InsertAction("Position", delegate
						{
							using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter(""))
							{
								gameEventWriter.Write(fsm.transform.position.GetHashCode());
								gameEventWriter.Write(!tapOn2.Value);
								GameEvent<NetWaterSourceManager>.Send("Tap", gameEventWriter, 0UL, true);
							}
						}, 0, false);
						this.waterTaps.Add(new NetWaterSourceManager.WaterTap
						{
							fsm = fsm,
							tapOn = tapOn2
						});
					}
					else if (fsm.transform.parent.name == "Shower")
					{
						PlayMakerFSM playMaker = fsm.transform.parent.Find("Valve").GetPlayMaker("Switch");
						FsmBool showerSwitch = fsm.FsmVariables.FindFsmBool("ShowerSwitch");
						FsmEvent fsmEvent3 = fsm.AddEvent("MP_ON");
						fsm.AddGlobalTransition(fsmEvent3, "Shower");
						FsmEvent fsmEvent4 = fsm.AddEvent("MP_OFF");
						fsm.AddGlobalTransition(fsmEvent4, "State 1");
						fsm.InsertAction("Position", delegate
						{
							using (GameEventWriter gameEventWriter2 = GameEvent.EmptyWriter(""))
							{
								gameEventWriter2.Write(fsm.transform.position.GetHashCode());
								gameEventWriter2.Write(true);
								gameEventWriter2.Write(!showerSwitch.Value);
								GameEvent<NetWaterSourceManager>.Send("Shower", gameEventWriter2, 0UL, true);
							}
						}, 0, false);
						FsmBool tapOn = playMaker.FsmVariables.FindFsmBool("Valve");
						fsmEvent3 = playMaker.AddEvent("MP_ON");
						playMaker.AddGlobalTransition(fsmEvent3, "ON");
						fsmEvent4 = playMaker.AddEvent("MP_OFF");
						playMaker.AddGlobalTransition(fsmEvent4, "OFF");
						playMaker.InsertAction("Position", delegate
						{
							using (GameEventWriter gameEventWriter3 = GameEvent.EmptyWriter(""))
							{
								gameEventWriter3.Write(fsm.transform.position.GetHashCode());
								gameEventWriter3.Write(!tapOn.Value);
								gameEventWriter3.Write(false);
								GameEvent<NetWaterSourceManager>.Send("Shower", gameEventWriter3, 0UL, true);
							}
						}, 0, false);
						this.showers.Add(new NetWaterSourceManager.Shower
						{
							valve = playMaker,
							showerSwitch = fsm,
							tapOn = tapOn,
							showerOn = showerSwitch
						});
					}
					else if (fsm.transform.name == "Trigger")
					{
						bool flag = false;
						Transform transform = fsm.transform;
						while (transform.parent != null)
						{
							if (transform.name == "WaterWell")
							{
								flag = true;
								break;
							}
							transform = transform.parent;
						}
						if (flag)
						{
							FsmEvent fsmEvent5 = fsm.AddEvent("MP_USE");
							NetWaterSourceManager.WaterWell well = new NetWaterSourceManager.WaterWell
							{
								fsm = fsm
							};
							fsm.AddGlobalTransition(fsmEvent5, "Move lever");
							fsm.InsertAction("Move lever", delegate
							{
								if (well.receivedWellEvent)
								{
									well.receivedWellEvent = false;
									return;
								}
								using (GameEventWriter gameEventWriter4 = GameEvent.EmptyWriter(""))
								{
									gameEventWriter4.Write(fsm.transform.position.GetHashCode());
									GameEvent<NetWaterSourceManager>.Send("Well", gameEventWriter4, 0UL, true);
								}
							}, 0, false);
							this.wells.Add(well);
						}
					}
				}
			}
			WreckMPGlobals.OnMemberReady.Add(delegate(ulong user)
			{
				if (!WreckMPGlobals.IsHost)
				{
					return;
				}
				for (int j = 0; j < this.waterTaps.Count; j++)
				{
					using (GameEventWriter gameEventWriter5 = GameEvent.EmptyWriter(""))
					{
						gameEventWriter5.Write(this.waterTaps[j].fsm.transform.position.GetHashCode());
						gameEventWriter5.Write(this.waterTaps[j].tapOn.Value);
						GameEvent<NetWaterSourceManager>.Send("Tap", gameEventWriter5, 0UL, true);
					}
				}
				for (int k = 0; k < this.showers.Count; k++)
				{
					using (GameEventWriter gameEventWriter6 = GameEvent.EmptyWriter(""))
					{
						gameEventWriter6.Write(this.showers[k].showerSwitch.transform.position.GetHashCode());
						gameEventWriter6.Write(this.showers[k].tapOn.Value);
						gameEventWriter6.Write(this.showers[k].showerOn.Value);
						GameEvent<NetWaterSourceManager>.Send("Shower", gameEventWriter6, 0UL, true);
					}
				}
			});
		}

		private void OnWaterTap(ulong sender, GameEventReader packet)
		{
			int hash = packet.ReadInt32();
			bool flag = packet.ReadBoolean();
			NetWaterSourceManager.WaterTap waterTap = this.waterTaps.FirstOrDefault((NetWaterSourceManager.WaterTap t) => t.fsm.transform.position.GetHashCode() == hash);
			if (waterTap != null)
			{
				bool flag2 = waterTap.tapOn.Value != flag;
				waterTap.tapOn.Value = flag;
				if (flag2)
				{
					waterTap.fsm.SendEvent(flag ? "MP_ON" : "MP_OFF");
				}
			}
		}

		private void OnShower(ulong sender, GameEventReader packet)
		{
			int hash = packet.ReadInt32();
			bool flag = packet.ReadBoolean();
			bool flag2 = packet.ReadBoolean();
			NetWaterSourceManager.Shower shower = this.showers.FirstOrDefault((NetWaterSourceManager.Shower t) => t.showerSwitch.transform.position.GetHashCode() == hash);
			if (shower != null)
			{
				bool flag3 = shower.tapOn.Value != flag;
				shower.tapOn.Value = flag;
				if (flag3)
				{
					shower.valve.SendEvent(flag ? "MP_ON" : "MP_OFF");
				}
				bool flag4 = shower.showerOn.Value != flag2;
				shower.showerOn.Value = flag2;
				if (flag4)
				{
					shower.showerSwitch.SendEvent(flag2 ? "MP_ON" : "MP_OFF");
				}
			}
		}

		private void OnWaterWell(ulong sender, GameEventReader packet)
		{
			int hash = packet.ReadInt32();
			NetWaterSourceManager.WaterWell waterWell = this.wells.FirstOrDefault((NetWaterSourceManager.WaterWell f) => f.fsm.transform.position.GetHashCode() == hash);
			if (waterWell != null)
			{
				waterWell.receivedWellEvent = true;
				waterWell.fsm.SendEvent("MP_USE");
			}
		}

		private List<NetWaterSourceManager.WaterTap> waterTaps = new List<NetWaterSourceManager.WaterTap>();

		private List<NetWaterSourceManager.Shower> showers = new List<NetWaterSourceManager.Shower>();

		private List<NetWaterSourceManager.WaterWell> wells = new List<NetWaterSourceManager.WaterWell>();

		private class WaterTap
		{
			public PlayMakerFSM fsm;

			public FsmBool tapOn;
		}

		private class Shower
		{
			public PlayMakerFSM valve;

			public PlayMakerFSM showerSwitch;

			public FsmBool tapOn;

			public FsmBool showerOn;
		}

		private class WaterWell
		{
			public PlayMakerFSM fsm;

			public bool receivedWellEvent;
		}
	}
}
