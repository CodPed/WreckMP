using System;
using System.Runtime.InteropServices;

namespace Discord
{
	public class ImageManager
	{
		private ImageManager.FFIMethods Methods
		{
			get
			{
				if (this.MethodsStructure == null)
				{
					this.MethodsStructure = Marshal.PtrToStructure(this.MethodsPtr, typeof(ImageManager.FFIMethods));
				}
				return (ImageManager.FFIMethods)this.MethodsStructure;
			}
		}

		internal ImageManager(IntPtr ptr, IntPtr eventsPtr, ref ImageManager.FFIEvents events)
		{
			if (eventsPtr == IntPtr.Zero)
			{
				throw new ResultException(Result.InternalError);
			}
			this.InitEvents(eventsPtr, ref events);
			this.MethodsPtr = ptr;
			if (this.MethodsPtr == IntPtr.Zero)
			{
				throw new ResultException(Result.InternalError);
			}
		}

		private void InitEvents(IntPtr eventsPtr, ref ImageManager.FFIEvents events)
		{
			Marshal.StructureToPtr(events, eventsPtr, false);
		}

		[MonoPInvokeCallback]
		private static void FetchCallbackImpl(IntPtr ptr, Result result, ImageHandle handleResult)
		{
			GCHandle gchandle = GCHandle.FromIntPtr(ptr);
			ImageManager.FetchHandler fetchHandler = (ImageManager.FetchHandler)gchandle.Target;
			gchandle.Free();
			fetchHandler(result, handleResult);
		}

		public void Fetch(ImageHandle handle, bool refresh, ImageManager.FetchHandler callback)
		{
			GCHandle gchandle = GCHandle.Alloc(callback);
			this.Methods.Fetch(this.MethodsPtr, handle, refresh, GCHandle.ToIntPtr(gchandle), new ImageManager.FFIMethods.FetchCallback(ImageManager.FetchCallbackImpl));
		}

		public ImageDimensions GetDimensions(ImageHandle handle)
		{
			ImageDimensions imageDimensions = default(ImageDimensions);
			Result result = this.Methods.GetDimensions(this.MethodsPtr, handle, ref imageDimensions);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return imageDimensions;
		}

		public void GetData(ImageHandle handle, byte[] data)
		{
			Result result = this.Methods.GetData(this.MethodsPtr, handle, data, data.Length);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
		}

		public void Fetch(ImageHandle handle, ImageManager.FetchHandler callback)
		{
			this.Fetch(handle, false, callback);
		}

		public byte[] GetData(ImageHandle handle)
		{
			ImageDimensions dimensions = this.GetDimensions(handle);
			byte[] array = new byte[dimensions.Width * dimensions.Height * 4U];
			this.GetData(handle, array);
			return array;
		}

		private IntPtr MethodsPtr;

		private object MethodsStructure;

		internal struct FFIEvents
		{
		}

		internal struct FFIMethods
		{
			internal ImageManager.FFIMethods.FetchMethod Fetch;

			internal ImageManager.FFIMethods.GetDimensionsMethod GetDimensions;

			internal ImageManager.FFIMethods.GetDataMethod GetData;

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void FetchCallback(IntPtr ptr, Result result, ImageHandle handleResult);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void FetchMethod(IntPtr methodsPtr, ImageHandle handle, bool refresh, IntPtr callbackData, ImageManager.FFIMethods.FetchCallback callback);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result GetDimensionsMethod(IntPtr methodsPtr, ImageHandle handle, ref ImageDimensions dimensions);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result GetDataMethod(IntPtr methodsPtr, ImageHandle handle, byte[] data, int dataLen);
		}

		public delegate void FetchHandler(Result result, ImageHandle handleResult);
	}
}
