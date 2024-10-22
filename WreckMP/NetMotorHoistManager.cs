using System;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	internal class NetMotorHoistManager : NetManager
	{
		private void Start()
		{
			new GameEvent<NetMotorHoistManager>("Init", new Action<ulong, GameEventReader>(this.OnInitSync), GameScene.GAME);
			new GameEvent<NetMotorHoistManager>("BeginMove", new Action<ulong, GameEventReader>(this.OnBeginMovement), GameScene.GAME);
			new GameEvent<NetMotorHoistManager>("EndMove", new Action<ulong, GameEventReader>(this.OnEndMovement), GameScene.GAME);
			Transform transform = GameObject.Find("ITEMS").transform.Find("motor hoist(itemx)");
			this.motorHoistArm = transform.Find("motorhoist_arm");
			this.handle = transform.Find("Pump/HandlePivot").GetComponent<Animation>();
			this.usageFsm = transform.Find("Pump/Trigger").GetPlayMaker("Usage");
			this.angle = this.usageFsm.FsmVariables.FindFsmFloat("Angle");
			Action<bool> beginMove = delegate(bool isUp)
			{
				using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter("BeginMove"))
				{
					gameEventWriter.Write(isUp);
					gameEventWriter.Write(this.angle.Value);
					GameEvent<NetMotorHoistManager>.Send("BeginMove", gameEventWriter, 0UL, true);
				}
			};
			this.usageFsm.InsertAction("Up", delegate
			{
				beginMove(true);
			}, -1, false);
			this.usageFsm.InsertAction("Down", delegate
			{
				beginMove(false);
			}, -1, false);
			this.usageFsm.InsertAction("State 1", delegate
			{
				using (GameEventWriter gameEventWriter2 = GameEvent.EmptyWriter("EndMove"))
				{
					gameEventWriter2.Write(this.angle.Value);
					GameEvent<NetMotorHoistManager>.Send("EndMove", gameEventWriter2, 0UL, true);
				}
			}, -1, false);
			WreckMPGlobals.OnMemberReady.Add(delegate(ulong user)
			{
				using (GameEventWriter gameEventWriter3 = GameEvent.EmptyWriter("Init"))
				{
					gameEventWriter3.Write(this.angle.Value);
					GameEvent<NetMotorHoistManager>.Send("Init", gameEventWriter3, user, true);
				}
			});
			WreckMPGlobals.OnMemberExit = (Action<ulong>)Delegate.Combine(WreckMPGlobals.OnMemberExit, new Action<ulong>(delegate(ulong user)
			{
				if (this.hoistOwner == user)
				{
					this.OnEndMovement(user, this.angle.Value);
				}
			}));
		}

		private void OnInitSync(ulong sender, GameEventReader packet)
		{
			float num = packet.ReadSingle();
			this.angle.Value = num;
			this.motorHoistArm.localEulerAngles = Vector3.right * num;
		}

		private void OnBeginMovement(ulong sender, GameEventReader packet)
		{
			bool flag = packet.ReadBoolean();
			float num = packet.ReadSingle();
			this.angle.Value = num;
			this.motorHoistArm.localEulerAngles = Vector3.right * num;
			this.handle.Play("motor_hoist_pump_down", 4);
			this.usageFsm.enabled = false;
			this.hoistOwner = sender;
			MasterAudio.PlaySound3DAndForget("HouseFoley", this.usageFsm.transform, false, 1f, null, 0f, "carjack1");
			if (flag)
			{
				this.isHoistMoving = true;
			}
		}

		private void OnEndMovement(ulong sender, GameEventReader packet)
		{
			this.OnEndMovement(sender, packet.ReadSingle());
		}

		private void OnEndMovement(ulong sender, float ang)
		{
			if (sender != this.hoistOwner && this.hoistOwner != 0UL)
			{
				return;
			}
			this.angle.Value = ang;
			this.motorHoistArm.localEulerAngles = Vector3.right * ang;
			this.handle.Play("motor_hoist_pump_up", 4);
			this.usageFsm.enabled = true;
			this.hoistOwner = 0UL;
			this.isHoistMoving = false;
		}

		private void Update()
		{
			if (this.isHoistMoving)
			{
				float num = this.angle.Value + 0.07f;
				this.angle.Value = num;
				this.motorHoistArm.localEulerAngles = Vector3.right * num;
			}
		}

		private FsmFloat angle;

		private Transform motorHoistArm;

		private Animation handle;

		private PlayMakerFSM usageFsm;

		private bool isHoistMoving;

		private ulong hoistOwner;
	}
}
