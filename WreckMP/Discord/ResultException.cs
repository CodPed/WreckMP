using System;

namespace Discord
{
	public class ResultException : Exception
	{
		public ResultException(Result result)
			: base(result.ToString())
		{
		}

		public readonly Result Result;
	}
}
