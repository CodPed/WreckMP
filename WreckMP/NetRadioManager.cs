using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace WreckMP
{
	internal class NetRadioManager : NetManager
	{
		private void Start()
		{
			_ObjectsLoader.gameLoaded.Add(delegate
			{
				for (int i = 0; i < _ObjectsLoader.ObjectsInGame.Length; i++)
				{
					GameObject gameObject = _ObjectsLoader.ObjectsInGame[i];
					if (!(gameObject.name != "RadioChannels"))
					{
						Transform transform = GameObject.Find("RADIO").transform.Find("Paikallisradio");
						transform.gameObject.SetActive(false);
						this.InitChannel1(gameObject.transform, transform);
						this.InitFolk(gameObject.transform);
						NetRadioManager.radioLoaded = true;
						this.newTrackEvent = new GameEvent("NewTrack", new Action<GameEventReader>(this.OnNewTrackSelected), GameScene.GAME);
						break;
					}
				}
				return "Radio";
			});
			WreckMPGlobals.OnMemberReady.Add(delegate(ulong user)
			{
				if (!WreckMPGlobals.IsHost)
				{
					return;
				}
				for (int j = 0; j < this.radios.Count; j++)
				{
					this.NewTrackSelected(j, true, new ulong?(user));
				}
			});
		}

		private void InitFolk(Transform radio)
		{
			PlayMakerFSM component = radio.Find("Folk").GetComponent<PlayMakerFSM>();
			component.Initialize();
			PlayMakerFSM playMaker = (component.GetState("State 1").Actions[1] as SendEvent).eventTarget.gameObject.GameObject.Value.GetPlayMaker("Kansanradio");
			playMaker.Initialize();
			this.radios.Add(component);
			this.radioSources.Add(component.GetComponent<AudioSource>());
			FsmState state = playMaker.GetState("Play advert 1");
			FsmEvent fsmEvent = component.AddEvent("MP_NEXTRACK");
			this.radioPlayNextTrack.Add(fsmEvent);
			component.AddGlobalTransition(fsmEvent, "State 1");
			FsmInt trackID = new FsmInt("MP_NextTrack");
			List<FsmInt> list = playMaker.FsmVariables.IntVariables.ToList<FsmInt>();
			list.Add(trackID);
			this.radioNextTrackIndex.Add(trackID);
			playMaker.FsmVariables.IntVariables = list.ToArray();
			playMaker.FsmGlobalTransitions[0].ToState = state.Name;
			if (WreckMPGlobals.IsHost)
			{
				(state.Actions[0] as ArrayListGetRandom).randomIndex = trackID;
				playMaker.InsertAction(state.Name, delegate
				{
					this.NewTrackSelected(1, false, null);
				}, 2, false);
			}
			else
			{
				FsmStateAction[] actions = state.Actions;
				ArrayListGetRandom oldAction = actions[0] as ArrayListGetRandom;
				PlayMakerArrayListProxy arrayList = playMaker.GetComponents<PlayMakerArrayListProxy>().FirstOrDefault((PlayMakerArrayListProxy al) => al.referenceName == oldAction.reference.Value);
				FsmVar targetVar = oldAction.randomItem;
				actions[0] = new PM_Hook(delegate
				{
					object obj = arrayList.arrayList[trackID.Value];
					targetVar.SetValue(obj);
				}, false);
				actions[1].Enabled = false;
				state.Actions = actions;
				component.InsertAction("Play radio", delegate
				{
					this.SetSRCtime(1);
				}, 1, false);
			}
			this.audioTimes.Add(0f);
		}

		private void InitChannel1(Transform radio, Transform pakaliradioidk)
		{
			PlayMakerFSM component = radio.Find("Channel1").GetComponent<PlayMakerFSM>();
			component.Initialize();
			this.radios.Add(component);
			this.radioSources.Add(component.GetComponent<AudioSource>());
			FsmState state = component.GetState("Channel1");
			FsmEvent fsmEvent = component.AddEvent("MP_NEXTRACK");
			this.radioPlayNextTrack.Add(fsmEvent);
			component.AddGlobalTransition(fsmEvent, state.Name);
			component.Fsm.Event(fsmEvent);
			FsmInt trackID = new FsmInt("MP_NextTrack");
			List<FsmInt> list = component.FsmVariables.IntVariables.ToList<FsmInt>();
			list.Add(trackID);
			this.radioNextTrackIndex.Add(trackID);
			component.FsmVariables.IntVariables = list.ToArray();
			if (WreckMPGlobals.IsHost)
			{
				(state.Actions[0] as ArrayListGetRandom).randomIndex = trackID;
				component.InsertAction(state.Name, delegate
				{
					this.NewTrackSelected(0, false, null);
				}, 2, false);
			}
			else
			{
				FsmStateAction[] actions = state.Actions;
				ArrayListGetRandom oldAction = actions[0] as ArrayListGetRandom;
				PlayMakerArrayListProxy arrayList = pakaliradioidk.GetComponents<PlayMakerArrayListProxy>().FirstOrDefault((PlayMakerArrayListProxy al) => al.referenceName == oldAction.reference.Value);
				FsmVar targetVar = oldAction.randomItem;
				actions[0] = new PM_Hook(delegate
				{
					object obj = arrayList.arrayList[trackID.Value];
					targetVar.SetValue(obj);
				}, false);
				actions[1].Enabled = false;
				state.Actions = actions;
				component.InsertAction("Play radio 2", delegate
				{
					this.SetSRCtime(0);
				}, 2, false);
			}
			this.audioTimes.Add(0f);
		}

		private void SetSRCtime(int index)
		{
			this.radioSources[index].time = this.audioTimes[index];
		}

		private void OnNewTrackSelected(GameEventReader p)
		{
			int num = p.ReadInt32();
			int num2 = p.ReadInt32();
			float num3 = p.ReadSingle();
			this.audioTimes[num] = ((num3 < 0f) ? 0f : num3);
			this.radioNextTrackIndex[num].Value = num2;
			this.radios[num].Fsm.Event(this.radioPlayNextTrack[num]);
		}

		private void NewTrackSelected(int index, bool includeTime = false, ulong? target = null)
		{
			using (GameEventWriter gameEventWriter = this.newTrackEvent.Writer())
			{
				gameEventWriter.Write(index);
				gameEventWriter.Write(this.radioNextTrackIndex[index].Value);
				gameEventWriter.Write(includeTime ? this.radioSources[index].time : (-1f));
				if (target == null)
				{
					this.newTrackEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
				}
				else
				{
					this.newTrackEvent.Send(gameEventWriter, target.Value, true, default(GameEvent.RecordingProperties));
				}
			}
		}

		private List<AudioSource> radioSources = new List<AudioSource>();

		private List<PlayMakerFSM> radios = new List<PlayMakerFSM>();

		private List<FsmEvent> radioPlayNextTrack = new List<FsmEvent>();

		private List<FsmInt> radioNextTrackIndex = new List<FsmInt>();

		private List<float> audioTimes = new List<float>();

		private GameEvent newTrackEvent;

		internal static bool radioLoaded;
	}
}
