using System;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	internal class NetCarpartsOrderManager : NetManager
	{
		private void Start()
		{
			_ObjectsLoader.gameLoaded.Add(delegate
			{
				this.products = new string[64];
				Transform transform = GameObject.Find("Sheets").transform.Find("Magazine");
				this.orderList = transform.parent.Find("OrderList").GetComponents<PlayMakerArrayListProxy>()[0];
				int i = 0;
				int num = 0;
				while (i < transform.childCount)
				{
					Transform child = transform.GetChild(i);
					if (child.name == "ButtonOrder")
					{
						PlayMakerFSM component = child.GetComponent<PlayMakerFSM>();
						component.InsertAction("State 3", delegate
						{
							if (this.spawningEnvelope)
							{
								this.spawningEnvelope = false;
								return;
							}
							ulong num2 = 0UL;
							for (int k = 0; k < this.products.Length; k++)
							{
								if (this.orderList.arrayList.Contains(this.products[k]) && !string.IsNullOrEmpty(this.products[k]))
								{
									num2 |= 1UL << k;
								}
							}
							using (GameEventWriter gameEventWriter = this.spawnEnvelope.Writer())
							{
								gameEventWriter.Write(num2);
								this.spawnEnvelope.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
							}
						}, -1, false);
						this.spawnEnvelopeEvent = component.AddEvent("MP_ENVELOPE");
						component.AddGlobalTransition(this.spawnEnvelopeEvent, "State 3");
						this.spawnEnvelopeFsm = component;
					}
					else if (child.name.StartsWith("Page"))
					{
						for (int j = 0; j < child.childCount; j++)
						{
							PlayMakerFSM component2 = child.GetChild(j).GetComponent<PlayMakerFSM>();
							if (!(component2 == null))
							{
								FsmGameObject fsmGameObject = component2.FsmVariables.FindFsmGameObject("Product");
								if (fsmGameObject != null)
								{
									this.products[num++] = fsmGameObject.Value.name;
								}
							}
						}
					}
					i++;
				}
				this.orderPayFsm = GameObject.Find("STORE").transform.Find("LOD/ActivateStore/PostOffice/PostOrderBuy").GetPlayMaker("Use");
				this.orderPayEvent = this.orderPayFsm.AddEvent("MP_PAY");
				this.orderPayFsm.AddGlobalTransition(this.orderPayEvent, "State 1");
				this.orderPayFsm.InsertAction("State 1", delegate
				{
					if (this.payingOrder)
					{
						this.payingOrder = false;
						return;
					}
					this.payOrder.SendEmpty(0UL, true);
				}, -1, false);
				this.spawnEnvelope = new GameEvent("SpawnEnvelopeCarpartsOrder", delegate(GameEventReader p)
				{
					ulong num3 = p.ReadUInt64();
					this.spawnEnvelopeFsm.Fsm.Event(this.spawnEnvelopeEvent);
					int l = 0;
					int num4 = 1;
					while (l < this.products.Length)
					{
						if ((num3 & (1UL << l)) > 0UL)
						{
							this.orderList.arrayList[num4++] = this.products[l];
						}
						l++;
					}
				}, GameScene.GAME);
				this.payOrder = new GameEvent("PayCarpartsOrder", delegate(GameEventReader p)
				{
					this.payingOrder = true;
					this.orderPayFsm.Fsm.Event(this.orderPayEvent);
				}, GameScene.GAME);
				return "CarpartsOrder";
			});
		}

		private PlayMakerFSM spawnEnvelopeFsm;

		private PlayMakerFSM orderPayFsm;

		private FsmEvent spawnEnvelopeEvent;

		private FsmEvent orderPayEvent;

		private PlayMakerArrayListProxy orderList;

		private string[] products;

		private bool spawningEnvelope;

		private bool payingOrder;

		private GameEvent spawnEnvelope;

		private GameEvent payOrder;
	}
}
