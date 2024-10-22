using System;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	internal class NetSwitchManager : NetManager
	{
		private void Start()
		{
			NetSwitchManager.Instance = this;
			GameObject[] array = (from x in Resources.FindObjectsOfTypeAll<GameObject>()
				where x.name.StartsWith("switch")
				select x).ToArray<GameObject>();
			for (int i = 0; i < array.Length; i++)
			{
				GameObject gameObject = array[i];
				if (gameObject.name.StartsWith("switch"))
				{
					PlayMakerFSM fsm = gameObject.GetPlayMaker("Use");
					if (!(fsm == null))
					{
						fsm.Initialize();
						if (fsm.FsmStates.Any((FsmState x) => x.Name == "Switch"))
						{
							if (fsm.FsmStates.Any((FsmState x) => x.Name == "Position"))
							{
								int hashCode = gameObject.transform.GetGameobjectHashString().GetHashCode();
								FsmEvent fsmEvent = fsm.AddEvent("MP_SWITCH");
								bool flag = !fsm.FsmStates.Any((FsmState x) => x.Name == "Switch");
								FsmBool switchOn = fsm.FsmVariables.FindFsmBool(flag ? "SwitchOn" : "Switch");
								bool doSync = true;
								GameEvent ToggleSwitch = new GameEvent(string.Format("Togglelightswitch{0}", hashCode), delegate(GameEventReader p)
								{
									if (switchOn.Value == p.ReadBoolean())
									{
										return;
									}
									doSync = false;
									fsm.Fsm.Event(fsmEvent);
								}, GameScene.GAME);
								Action<ulong> sync = delegate(ulong target)
								{
									using (GameEventWriter gameEventWriter = ToggleSwitch.Writer())
									{
										gameEventWriter.Write(switchOn.Value != (target == 0UL));
										ToggleSwitch.Send(gameEventWriter, target, true, default(GameEvent.RecordingProperties));
									}
								};
								Action action = delegate
								{
									if (doSync)
									{
										sync(0UL);
									}
									doSync = true;
								};
								WreckMPGlobals.OnMemberReady.Add(sync);
								if (flag)
								{
									fsm.AddGlobalTransition(fsmEvent, "Position");
									fsm.InsertAction("Position", action, 0, false);
								}
								else
								{
									fsm.AddGlobalTransition(fsmEvent, "Switch");
									fsm.InsertAction("Switch", action, 0, false);
								}
							}
						}
					}
				}
			}
		}

		public static NetSwitchManager Instance;
	}
}
