using System;
using UnityEngine;

namespace WreckMP
{
	internal class FsmHorn : MonoBehaviour
	{
		public FsmHorn()
		{
			this.horn = new GameEvent("hornActivate" + base.transform.root.name, delegate(GameEventReader p)
			{
				bool flag = p.ReadBoolean();
				base.gameObject.SetActive(flag);
				this.isUpdating = true;
			}, GameScene.GAME);
		}

		private void SendPacket(bool b)
		{
			if (this.isUpdating)
			{
				this.isUpdating = false;
				return;
			}
			using (GameEventWriter gameEventWriter = this.horn.Writer())
			{
				gameEventWriter.Write(b);
				this.horn.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnEnable()
		{
			this.SendPacket(true);
		}

		private void OnDisable()
		{
			this.SendPacket(false);
		}

		public GameEvent horn;

		public bool isUpdating;
	}
}
