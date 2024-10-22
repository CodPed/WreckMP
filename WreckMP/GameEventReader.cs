using System;
using System.IO;
using UnityEngine;

namespace WreckMP
{
	public class GameEventReader : BinaryReader
	{
		public int Length
		{
			get
			{
				return this.m_Length;
			}
		}

		internal GameEventReader(ulong sender, byte[] packet)
			: base(new MemoryStream(packet))
		{
			this.sender = sender;
			this.m_Length = packet.Length;
		}

		public Vector3 ReadVector3()
		{
			return new Vector3(this.ReadSingle(), this.ReadSingle(), this.ReadSingle());
		}

		public int UnreadLength()
		{
			MemoryStream memoryStream = this.BaseStream as MemoryStream;
			return (int)((long)this.m_Length - memoryStream.Position);
		}

		public ulong sender;

		private int m_Length;
	}
}
