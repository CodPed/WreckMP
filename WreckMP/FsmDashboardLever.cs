using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Steamworks;
using UnityEngine;

namespace WreckMP
{
	internal class FsmDashboardLever
	{
		public FsmDashboardLever(PlayMakerFSM fsm)
		{
			this.hash = fsm.transform.GetGameobjectHashString().GetHashCode();
			this.fsm = fsm;
			this.SetupFSM();
			this.updateEvent = new GameEvent("Update" + this.hash.ToString(), new Action<GameEventReader>(this.OnLeverUpdate), GameScene.GAME);
			if (FsmDashboardLever.initSync == null)
			{
				FsmDashboardLever.initSync = new GameEvent("InitSync", delegate(GameEventReader p)
				{
					while (p.UnreadLength() > 0)
					{
						int hash = p.ReadInt32();
						ulong num = (ulong)p.ReadInt64();
						float num2 = p.ReadSingle();
						FsmDashboardLever fsmDashboardLever = FsmDashboardLever.levers.FirstOrDefault((FsmDashboardLever l) => l.hash == hash);
						if (fsmDashboardLever == null)
						{
							Console.LogError(string.Format("Received dashboard lever init sync from {0} but the hash {1} cannot be found", CoreManager.playerNames[(CSteamID)p.sender], hash), false);
							return;
						}
						fsmDashboardLever.owner = num;
						fsmDashboardLever.SetKnobPos(num2);
					}
				}, GameScene.GAME);
				WreckMPGlobals.OnMemberReady.Add(delegate(ulong u)
				{
					if (!WreckMPGlobals.IsHost)
					{
						return;
					}
					using (GameEventWriter gameEventWriter = FsmDashboardLever.initSync.Writer())
					{
						for (int i = 0; i < FsmDashboardLever.levers.Count; i++)
						{
							gameEventWriter.Write(FsmDashboardLever.levers[i].hash);
							gameEventWriter.Write((long)FsmDashboardLever.levers[i].owner);
							gameEventWriter.Write(FsmDashboardLever.levers[i].knobPos.Value);
						}
						FsmDashboardLever.initSync.Send(gameEventWriter, u, true, default(GameEvent.RecordingProperties));
					}
				});
			}
			WreckMPGlobals.OnMemberExit = (Action<ulong>)Delegate.Combine(WreckMPGlobals.OnMemberExit, new Action<ulong>(delegate(ulong user)
			{
				if (this.owner == user)
				{
					this.StopMoving(null);
				}
			}));
			FsmDashboardLever.levers.Add(this);
			CoreManager.sceneLoaded = (Action<GameScene>)Delegate.Combine(CoreManager.sceneLoaded, new Action<GameScene>(delegate(GameScene a)
			{
				if (FsmDashboardLever.levers.Contains(this))
				{
					FsmDashboardLever.levers.Remove(this);
				}
			}));
		}

		private void OnLeverUpdate(GameEventReader packet)
		{
			bool flag = packet.ReadBoolean();
			bool flag2 = packet.ReadBoolean();
			float num = packet.ReadSingle();
			if (flag)
			{
				this.StartMoving(flag2, packet.sender);
				return;
			}
			this.StopMoving(new float?(num));
		}

		public void Update()
		{
			if (this.owner == WreckMPGlobals.UserID && !Input.GetMouseButton(0) && !Input.GetMouseButton(1))
			{
				this.Move(null);
			}
		}

		private void StartMoving(bool direction, ulong newOwner)
		{
			this.owner = newOwner;
			this.moveDirection = direction;
			this.fsm.Fsm.Event(direction ? this.increaseEvent : this.decreaseEvent);
		}

		private void StopMoving(float? knobPos)
		{
			this.owner = 0UL;
			if (knobPos != null)
			{
				float value = knobPos.Value;
				this.SetKnobPos(value);
			}
		}

		private void SetKnobPos(float value)
		{
			this.knobPos.Value = value;
			this.stopMoving = true;
			this.fsm.Fsm.Event(this.increaseEvent);
		}

		private void SetupFSM()
		{
			string text = (this.fsm.HasState("INCREASE") ? "INCREASE" : "INCREASE 2");
			string text2 = (this.fsm.HasState("DECREASE") ? "DECREASE" : "DECREASE 2");
			this.fsm.InsertAction(text, delegate
			{
				if (!this.stopMoving)
				{
					this.Move(new bool?(true));
				}
			}, 0, false);
			this.fsm.InsertAction(text2, delegate
			{
				if (!this.stopMoving)
				{
					this.Move(new bool?(false));
				}
			}, 0, false);
			this.increaseEvent = this.fsm.AddEvent("MP_INCREASE");
			this.decreaseEvent = this.fsm.AddEvent("MP_DECREASE");
			this.fsm.AddGlobalTransition(this.increaseEvent, text);
			this.fsm.AddGlobalTransition(this.decreaseEvent, text2);
			try
			{
				for (int i = 0; i < 2; i++)
				{
					string text3 = ((i == 0) ? text : text2);
					FsmState state = this.fsm.GetState(text3);
					FsmStateAction[] actions = state.Actions;
					List<FsmStateAction> list = new List<FsmStateAction>();
					if (i == 0)
					{
						FloatAdd floatAdd = actions.First((FsmStateAction f) => f is FloatAdd) as FloatAdd;
						this.knobPos = floatAdd.floatVariable;
					}
					bool domousePick = false;
					int layerMask = 0;
					float rayDistance = 0f;
					FsmEvent mouseOff = null;
					bool dombd = false;
					MouseButton mbd_btn = 0;
					FsmEvent mbd_event = null;
					bool dombu = false;
					MouseButton mbu_btn = 0;
					FsmEvent mbu_event = null;
					for (int j = 0; j < actions.Length; j++)
					{
						MousePickEvent mousePickEvent = actions[j] as MousePickEvent;
						if (mousePickEvent != null)
						{
							domousePick = true;
							layerMask = ActionHelpers.LayerArrayToLayerMask(mousePickEvent.layerMask, mousePickEvent.invertMask.Value);
							rayDistance = mousePickEvent.rayDistance.Value;
							mouseOff = mousePickEvent.mouseOff;
						}
						else
						{
							GetMouseButtonDown getMouseButtonDown = actions[j] as GetMouseButtonDown;
							if (getMouseButtonDown != null)
							{
								dombd = true;
								mbd_btn = getMouseButtonDown.button;
								mbd_event = getMouseButtonDown.sendEvent;
							}
							else
							{
								GetMouseButtonUp getMouseButtonUp = actions[j] as GetMouseButtonUp;
								if (getMouseButtonUp != null)
								{
									dombu = true;
									mbu_btn = getMouseButtonUp.button;
									mbu_event = getMouseButtonUp.sendEvent;
								}
								else
								{
									list.Add(actions[j]);
								}
							}
						}
					}
					list.Add(new PM_Hook(delegate
					{
						if (this.stopMoving)
						{
							this.stopMoving = false;
							this.fsm.Fsm.Event(mbu_event);
							return;
						}
						if (this.owner == 0UL || this.owner == WreckMPGlobals.UserID)
						{
							if (domousePick)
							{
								bool flag = ActionHelpers.IsMouseOver(this.fsm.gameObject, rayDistance, layerMask);
								this.fsm.Fsm.RaycastHitInfo = ActionHelpers.mousePickInfo;
								if (!flag)
								{
									this.fsm.Fsm.Event(mouseOff);
									return;
								}
							}
							if (dombd && ((mbd_btn == null && Input.GetMouseButtonDown(0)) || (mbd_btn == 1 && Input.GetMouseButtonDown(1))))
							{
								this.fsm.Fsm.Event(mbd_event);
								return;
							}
							if (dombu && ((mbu_btn == null && Input.GetMouseButtonUp(0)) || (mbu_btn == 1 && Input.GetMouseButtonUp(1))))
							{
								this.fsm.Fsm.Event(mbu_event);
								return;
							}
						}
					}, true));
					state.Actions = list.ToArray();
				}
			}
			catch (Exception ex)
			{
				Console.LogError(string.Format("Failed to setup dashboard lever {0} ({1}): {2}, {3}, {4}", new object[]
				{
					this.hash,
					this.fsm.transform.GetGameobjectHashString(),
					ex.GetType(),
					ex.Message,
					ex.StackTrace
				}), false);
			}
		}

		private void Move(bool? direction)
		{
			if (this.owner != 0UL && this.owner != WreckMPGlobals.UserID)
			{
				return;
			}
			using (GameEventWriter gameEventWriter = this.updateEvent.Writer())
			{
				gameEventWriter.Write(direction != null);
				gameEventWriter.Write(direction != null && direction.Value);
				gameEventWriter.Write(this.knobPos.Value);
				this.owner = ((direction != null) ? WreckMPGlobals.UserID : 0UL);
				this.updateEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private PlayMakerFSM fsm;

		private FsmFloat knobPos;

		private FsmFloat multiplyResult;

		private FsmEvent increaseEvent;

		private FsmEvent decreaseEvent;

		private Transform mesh;

		private int axis;

		private int hash;

		private ulong owner;

		private bool stopMoving;

		private float moveRate;

		private float minMove;

		private float maxMove;

		private bool movePerSecond;

		private bool moveDirection;

		private float multiplyRate;

		private float minmultiply;

		private float maxmultiply;

		private GameEvent updateEvent;

		private static GameEvent initSync;

		private static List<FsmDashboardLever> levers = new List<FsmDashboardLever>();
	}
}
