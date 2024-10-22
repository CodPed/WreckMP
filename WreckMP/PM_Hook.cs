using System;
using HutongGames.PlayMaker;

namespace WreckMP
{
	internal class PM_Hook : FsmStateAction
	{
		public PM_Hook(Action action, bool everyFrame = false)
		{
			this.action = action;
			this.everyFrame = everyFrame;
		}

		public override void OnEnter()
		{
			if (!this.everyFrame)
			{
				Action action = this.action;
				if (action != null)
				{
					action();
				}
				base.Finish();
			}
		}

		public override void OnUpdate()
		{
			if (this.everyFrame)
			{
				Action action = this.action;
				if (action == null)
				{
					return;
				}
				action();
			}
		}

		public Action action;

		public bool everyFrame;
	}
}
