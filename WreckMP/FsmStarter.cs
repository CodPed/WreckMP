using System;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace WreckMP
{
	internal class FsmStarter
	{
		public FsmStarter(FsmIgnition ignition, PlayMakerFSM fsm)
		{
			FsmStarter <>4__this = this;
			this.fsm = fsm;
			bool flag = fsm.transform.root.name.Contains("SATSUMA");
			this.owner = ignition;
			this.acc = fsm.FsmVariables.FindFsmBool("ACC");
			this.shutOff = fsm.FsmVariables.FindFsmBool("ShutOff");
			this.starting = fsm.FsmVariables.FindFsmBool("Starting");
			this.plugHeat = fsm.FsmVariables.FindFsmFloat("PlugHeat");
			this.engineTemp = fsm.FsmVariables.FindFsmFloat("EngineTemp");
			string text = (flag ? "Turn key" : "Starting engine");
			this.triggerStarting = fsm.AddEvent("MP_START");
			fsm.AddGlobalTransition(this.triggerStarting, text);
			fsm.InsertAction("Wait for start", delegate
			{
				<>4__this.startingMP = false;
			}, -1, false);
			FsmState state = fsm.GetState(text);
			FsmStateAction[] actions = state.Actions;
			for (int i = 0; i < actions.Length; i++)
			{
				GetMouseButtonUp lmbup = actions[i] as GetMouseButtonUp;
				if (lmbup != null)
				{
					actions[i] = new PM_Hook(delegate
					{
						if (!<>4__this.startingMP && Input.GetMouseButtonUp(0))
						{
							fsm.Fsm.Event(lmbup.sendEvent);
						}
					}, true);
					break;
				}
			}
			state.Actions = actions;
			fsm.InsertAction("Start engine", delegate
			{
				<>4__this.owner.starting = false;
				<>4__this.owner.updatingMP = true;
			}, -1, false);
			Console.Log("Init starter for " + fsm.transform.root.name, false);
		}

		public void Start(bool val, bool acc)
		{
			if (val)
			{
				this.startingMP = true;
				this.fsm.Fsm.Event(this.triggerStarting);
			}
			this.starting.Value = val;
			this.acc.Value = acc;
			this.shutOff.Value = !val;
		}

		private PlayMakerFSM fsm;

		private FsmBool acc;

		private FsmBool shutOff;

		private FsmBool starting;

		internal FsmFloat plugHeat;

		internal FsmFloat engineTemp;

		private FsmEvent triggerStarting;

		private FsmIgnition owner;

		private bool startingMP;
	}
}
