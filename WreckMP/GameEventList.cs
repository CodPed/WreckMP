using System;
using System.Collections.Generic;
using System.Linq;

namespace WreckMP
{
	internal class GameEventList : List<GameEvent>
	{
		public new void Add(GameEvent item)
		{
			if (base.Count == 0)
			{
				base.Add(item);
				return;
			}
			if (item.Hash < base[0].Hash)
			{
				base.Insert(0, item);
				return;
			}
			for (int i = 1; i < base.Count; i++)
			{
				if (base[i - 1].Hash < item.Hash && item.Hash < base[i].Hash)
				{
					base.Insert(i, item);
					return;
				}
			}
			base.Add(item);
		}

		public GameEvent Find(int Hash)
		{
			GameEvent gameEvent;
			try
			{
				if (base.Count == 1 && base[0].Hash == Hash)
				{
					gameEvent = base[0];
				}
				else
				{
					int num = base.Count / 2;
					int num2 = num;
					Func<GameEvent, bool> <>9__1;
					while (num > 0 && (base[num2 - 1].Hash >= Hash || Hash >= base[num2].Hash))
					{
						num /= 2;
						if (base[num2].Hash == Hash)
						{
							return base[num2];
						}
						if (num == 0)
						{
							int i = num2;
							while (i < base.Count)
							{
								if (i < 0)
								{
									break;
								}
								if (base[i].Hash == Hash)
								{
									return base[i];
								}
								if (i > 0 && base[i].Hash > Hash && base[i - 1].Hash < Hash)
								{
									Func<GameEvent, bool> func;
									if ((func = <>9__1) == null)
									{
										func = (<>9__1 = (GameEvent e) => e.Hash == Hash);
									}
									return this.FirstOrDefault(func);
								}
								if (base[i].Hash > Hash)
								{
									i--;
								}
								else
								{
									i++;
								}
							}
						}
						else if (base[num2].Hash > Hash)
						{
							num2 -= num;
						}
						else
						{
							num2 += num;
						}
					}
					gameEvent = this.FirstOrDefault((GameEvent e) => e.Hash == Hash);
				}
			}
			catch (Exception ex)
			{
				if (!this.Any((GameEvent e) => e.Hash == Hash))
				{
					gameEvent = null;
				}
				else
				{
					Console.Log(string.Format("Gamevent.find errored with: {0}", ex), true);
					Console.Log(string.Format("Target hash: {0}", Hash), true);
					gameEvent = null;
				}
			}
			return gameEvent;
		}
	}
}
