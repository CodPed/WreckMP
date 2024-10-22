using System;
using System.IO;
using UnityEngine;

namespace WreckMP
{
	public class GameEventWriter : BinaryWriter
	{
		internal GameEventWriter()
			: base(new MemoryStream())
		{
		}

		public void Write(Vector3 vector3)
		{
			this.Write(vector3.x);
			this.Write(vector3.y);
			this.Write(vector3.z);
		}

		internal byte[] GetPacket()
		{
			return (this.OutStream as MemoryStream).ToArray();
		}

		[Obsolete("Compatibility with old beermp code")]
		internal bool isEmpty;
	}
}
