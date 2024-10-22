using System;

namespace WreckMP
{
	public static class DateTimeExtension
	{
		public static long ToUnixTimestamp(this DateTime value)
		{
			return (long)value.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
		}
	}
}
