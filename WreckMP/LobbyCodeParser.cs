using System;

namespace WreckMP
{
	internal static class LobbyCodeParser
	{
		internal static string GetString(ulong lobbyCode)
		{
			string text = "";
			int num = 0;
			while (num < 11 && lobbyCode != 0UL)
			{
				text += LobbyCodeParser.Get6BitChar((int)lobbyCode & 63).ToString();
				lobbyCode >>= 6;
				num++;
			}
			return text;
		}

		internal static ulong GetUlong(string lobbyCode)
		{
			ulong num = 0UL;
			for (int i = 0; i < lobbyCode.Length; i++)
			{
				num |= (ulong)LobbyCodeParser.GetByte(lobbyCode[i]) << i * 6;
			}
			return num;
		}

		private static char Get6BitChar(int b)
		{
			int num;
			if (b < 10)
			{
				num = 48 + b;
			}
			else if (b < 36)
			{
				num = 65 + (b - 10);
			}
			else if (b == 36)
			{
				num = 95;
			}
			else if (b < 63)
			{
				num = 97 + (b - 37);
			}
			else
			{
				num = 45;
			}
			return Convert.ToChar((byte)num);
		}

		private static byte GetByte(char b)
		{
			int num = 0;
			if (b == '-')
			{
				num = 63;
			}
			else if (b < ':')
			{
				num = (int)(b - '0');
			}
			else if (b < '[')
			{
				num = (int)(b - 'A' + '\n');
			}
			else if (b == '_')
			{
				num = 36;
			}
			else if (b < '{')
			{
				num = (int)(b - 'a' + '%');
			}
			return (byte)num;
		}
	}
}
