using System;
using System.Collections;

namespace WreckMP
{
	internal class Hashtable<T> : Hashtable
	{
		public void Add(int hash, T value)
		{
			this.Add(hash, value);
		}

		public T this[int hash]
		{
			get
			{
				return (T)((object)this[hash]);
			}
			set
			{
				this[hash] = value;
			}
		}
	}
}
