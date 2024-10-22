using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Discord
{
	public class StorageManager
	{
		private StorageManager.FFIMethods Methods
		{
			get
			{
				if (this.MethodsStructure == null)
				{
					this.MethodsStructure = Marshal.PtrToStructure(this.MethodsPtr, typeof(StorageManager.FFIMethods));
				}
				return (StorageManager.FFIMethods)this.MethodsStructure;
			}
		}

		internal StorageManager(IntPtr ptr, IntPtr eventsPtr, ref StorageManager.FFIEvents events)
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

		private void InitEvents(IntPtr eventsPtr, ref StorageManager.FFIEvents events)
		{
			Marshal.StructureToPtr(events, eventsPtr, false);
		}

		public uint Read(string name, byte[] data)
		{
			uint num = 0U;
			Result result = this.Methods.Read(this.MethodsPtr, name, data, data.Length, ref num);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return num;
		}

		[MonoPInvokeCallback]
		private static void ReadAsyncCallbackImpl(IntPtr ptr, Result result, IntPtr dataPtr, int dataLen)
		{
			GCHandle gchandle = GCHandle.FromIntPtr(ptr);
			StorageManager.ReadAsyncHandler readAsyncHandler = (StorageManager.ReadAsyncHandler)gchandle.Target;
			gchandle.Free();
			byte[] array = new byte[dataLen];
			Marshal.Copy(dataPtr, array, 0, dataLen);
			readAsyncHandler(result, array);
		}

		public void ReadAsync(string name, StorageManager.ReadAsyncHandler callback)
		{
			GCHandle gchandle = GCHandle.Alloc(callback);
			this.Methods.ReadAsync(this.MethodsPtr, name, GCHandle.ToIntPtr(gchandle), new StorageManager.FFIMethods.ReadAsyncCallback(StorageManager.ReadAsyncCallbackImpl));
		}

		[MonoPInvokeCallback]
		private static void ReadAsyncPartialCallbackImpl(IntPtr ptr, Result result, IntPtr dataPtr, int dataLen)
		{
			GCHandle gchandle = GCHandle.FromIntPtr(ptr);
			StorageManager.ReadAsyncPartialHandler readAsyncPartialHandler = (StorageManager.ReadAsyncPartialHandler)gchandle.Target;
			gchandle.Free();
			byte[] array = new byte[dataLen];
			Marshal.Copy(dataPtr, array, 0, dataLen);
			readAsyncPartialHandler(result, array);
		}

		public void ReadAsyncPartial(string name, ulong offset, ulong length, StorageManager.ReadAsyncPartialHandler callback)
		{
			GCHandle gchandle = GCHandle.Alloc(callback);
			this.Methods.ReadAsyncPartial(this.MethodsPtr, name, offset, length, GCHandle.ToIntPtr(gchandle), new StorageManager.FFIMethods.ReadAsyncPartialCallback(StorageManager.ReadAsyncPartialCallbackImpl));
		}

		public void Write(string name, byte[] data)
		{
			Result result = this.Methods.Write(this.MethodsPtr, name, data, data.Length);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
		}

		[MonoPInvokeCallback]
		private static void WriteAsyncCallbackImpl(IntPtr ptr, Result result)
		{
			GCHandle gchandle = GCHandle.FromIntPtr(ptr);
			StorageManager.WriteAsyncHandler writeAsyncHandler = (StorageManager.WriteAsyncHandler)gchandle.Target;
			gchandle.Free();
			writeAsyncHandler(result);
		}

		public void WriteAsync(string name, byte[] data, StorageManager.WriteAsyncHandler callback)
		{
			GCHandle gchandle = GCHandle.Alloc(callback);
			this.Methods.WriteAsync(this.MethodsPtr, name, data, data.Length, GCHandle.ToIntPtr(gchandle), new StorageManager.FFIMethods.WriteAsyncCallback(StorageManager.WriteAsyncCallbackImpl));
		}

		public void Delete(string name)
		{
			Result result = this.Methods.Delete(this.MethodsPtr, name);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
		}

		public bool Exists(string name)
		{
			bool flag = false;
			Result result = this.Methods.Exists(this.MethodsPtr, name, ref flag);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return flag;
		}

		public int Count()
		{
			int num = 0;
			this.Methods.Count(this.MethodsPtr, ref num);
			return num;
		}

		public FileStat Stat(string name)
		{
			FileStat fileStat = default(FileStat);
			Result result = this.Methods.Stat(this.MethodsPtr, name, ref fileStat);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return fileStat;
		}

		public FileStat StatAt(int index)
		{
			FileStat fileStat = default(FileStat);
			Result result = this.Methods.StatAt(this.MethodsPtr, index, ref fileStat);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return fileStat;
		}

		public string GetPath()
		{
			StringBuilder stringBuilder = new StringBuilder(4096);
			Result result = this.Methods.GetPath(this.MethodsPtr, stringBuilder);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return stringBuilder.ToString();
		}

		public IEnumerable<FileStat> Files()
		{
			int num = this.Count();
			List<FileStat> list = new List<FileStat>();
			for (int i = 0; i < num; i++)
			{
				list.Add(this.StatAt(i));
			}
			return list;
		}

		private IntPtr MethodsPtr;

		private object MethodsStructure;

		internal struct FFIEvents
		{
		}

		internal struct FFIMethods
		{
			internal StorageManager.FFIMethods.ReadMethod Read;

			internal StorageManager.FFIMethods.ReadAsyncMethod ReadAsync;

			internal StorageManager.FFIMethods.ReadAsyncPartialMethod ReadAsyncPartial;

			internal StorageManager.FFIMethods.WriteMethod Write;

			internal StorageManager.FFIMethods.WriteAsyncMethod WriteAsync;

			internal StorageManager.FFIMethods.DeleteMethod Delete;

			internal StorageManager.FFIMethods.ExistsMethod Exists;

			internal StorageManager.FFIMethods.CountMethod Count;

			internal StorageManager.FFIMethods.StatMethod Stat;

			internal StorageManager.FFIMethods.StatAtMethod StatAt;

			internal StorageManager.FFIMethods.GetPathMethod GetPath;

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result ReadMethod(IntPtr methodsPtr, [MarshalAs(UnmanagedType.LPStr)] string name, byte[] data, int dataLen, ref uint read);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void ReadAsyncCallback(IntPtr ptr, Result result, IntPtr dataPtr, int dataLen);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void ReadAsyncMethod(IntPtr methodsPtr, [MarshalAs(UnmanagedType.LPStr)] string name, IntPtr callbackData, StorageManager.FFIMethods.ReadAsyncCallback callback);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void ReadAsyncPartialCallback(IntPtr ptr, Result result, IntPtr dataPtr, int dataLen);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void ReadAsyncPartialMethod(IntPtr methodsPtr, [MarshalAs(UnmanagedType.LPStr)] string name, ulong offset, ulong length, IntPtr callbackData, StorageManager.FFIMethods.ReadAsyncPartialCallback callback);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result WriteMethod(IntPtr methodsPtr, [MarshalAs(UnmanagedType.LPStr)] string name, byte[] data, int dataLen);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void WriteAsyncCallback(IntPtr ptr, Result result);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void WriteAsyncMethod(IntPtr methodsPtr, [MarshalAs(UnmanagedType.LPStr)] string name, byte[] data, int dataLen, IntPtr callbackData, StorageManager.FFIMethods.WriteAsyncCallback callback);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result DeleteMethod(IntPtr methodsPtr, [MarshalAs(UnmanagedType.LPStr)] string name);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result ExistsMethod(IntPtr methodsPtr, [MarshalAs(UnmanagedType.LPStr)] string name, ref bool exists);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void CountMethod(IntPtr methodsPtr, ref int count);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result StatMethod(IntPtr methodsPtr, [MarshalAs(UnmanagedType.LPStr)] string name, ref FileStat stat);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result StatAtMethod(IntPtr methodsPtr, int index, ref FileStat stat);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result GetPathMethod(IntPtr methodsPtr, StringBuilder path);
		}

		public delegate void ReadAsyncHandler(Result result, byte[] data);

		public delegate void ReadAsyncPartialHandler(Result result, byte[] data);

		public delegate void WriteAsyncHandler(Result result);
	}
}
