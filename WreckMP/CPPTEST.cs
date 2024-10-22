using System;
using UnityEngine;

namespace WreckMP
{
	internal static class CPPTEST
	{
		public static int GetNum(int a)
		{
			return (int)Mathf.Pow((float)a, 2f);
		}
	}
}
