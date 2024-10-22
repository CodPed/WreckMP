using System;

namespace Discord
{
	public struct ImageHandle
	{
		public static ImageHandle User(long id)
		{
			return ImageHandle.User(id, 128U);
		}

		public static ImageHandle User(long id, uint size)
		{
			return new ImageHandle
			{
				Type = ImageType.User,
				Id = id,
				Size = size
			};
		}

		public ImageType Type;

		public long Id;

		public uint Size;
	}
}
