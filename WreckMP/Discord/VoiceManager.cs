using System;
using System.Runtime.InteropServices;

namespace Discord
{
	public class VoiceManager
	{
		private VoiceManager.FFIMethods Methods
		{
			get
			{
				if (this.MethodsStructure == null)
				{
					this.MethodsStructure = Marshal.PtrToStructure(this.MethodsPtr, typeof(VoiceManager.FFIMethods));
				}
				return (VoiceManager.FFIMethods)this.MethodsStructure;
			}
		}

		public event VoiceManager.SettingsUpdateHandler OnSettingsUpdate;

		internal VoiceManager(IntPtr ptr, IntPtr eventsPtr, ref VoiceManager.FFIEvents events)
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

		private void InitEvents(IntPtr eventsPtr, ref VoiceManager.FFIEvents events)
		{
			events.OnSettingsUpdate = new VoiceManager.FFIEvents.SettingsUpdateHandler(VoiceManager.OnSettingsUpdateImpl);
			Marshal.StructureToPtr(events, eventsPtr, false);
		}

		public InputMode GetInputMode()
		{
			InputMode inputMode = default(InputMode);
			Result result = this.Methods.GetInputMode(this.MethodsPtr, ref inputMode);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return inputMode;
		}

		[MonoPInvokeCallback]
		private static void SetInputModeCallbackImpl(IntPtr ptr, Result result)
		{
			GCHandle gchandle = GCHandle.FromIntPtr(ptr);
			VoiceManager.SetInputModeHandler setInputModeHandler = (VoiceManager.SetInputModeHandler)gchandle.Target;
			gchandle.Free();
			setInputModeHandler(result);
		}

		public void SetInputMode(InputMode inputMode, VoiceManager.SetInputModeHandler callback)
		{
			GCHandle gchandle = GCHandle.Alloc(callback);
			this.Methods.SetInputMode(this.MethodsPtr, inputMode, GCHandle.ToIntPtr(gchandle), new VoiceManager.FFIMethods.SetInputModeCallback(VoiceManager.SetInputModeCallbackImpl));
		}

		public bool IsSelfMute()
		{
			bool flag = false;
			Result result = this.Methods.IsSelfMute(this.MethodsPtr, ref flag);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return flag;
		}

		public void SetSelfMute(bool mute)
		{
			Result result = this.Methods.SetSelfMute(this.MethodsPtr, mute);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
		}

		public bool IsSelfDeaf()
		{
			bool flag = false;
			Result result = this.Methods.IsSelfDeaf(this.MethodsPtr, ref flag);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return flag;
		}

		public void SetSelfDeaf(bool deaf)
		{
			Result result = this.Methods.SetSelfDeaf(this.MethodsPtr, deaf);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
		}

		public bool IsLocalMute(long userId)
		{
			bool flag = false;
			Result result = this.Methods.IsLocalMute(this.MethodsPtr, userId, ref flag);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return flag;
		}

		public void SetLocalMute(long userId, bool mute)
		{
			Result result = this.Methods.SetLocalMute(this.MethodsPtr, userId, mute);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
		}

		public byte GetLocalVolume(long userId)
		{
			byte b = 0;
			Result result = this.Methods.GetLocalVolume(this.MethodsPtr, userId, ref b);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return b;
		}

		public void SetLocalVolume(long userId, byte volume)
		{
			Result result = this.Methods.SetLocalVolume(this.MethodsPtr, userId, volume);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
		}

		[MonoPInvokeCallback]
		private static void OnSettingsUpdateImpl(IntPtr ptr)
		{
			Discord discord = (Discord)GCHandle.FromIntPtr(ptr).Target;
			if (discord.VoiceManagerInstance.OnSettingsUpdate != null)
			{
				discord.VoiceManagerInstance.OnSettingsUpdate();
			}
		}

		private IntPtr MethodsPtr;

		private object MethodsStructure;

		internal struct FFIEvents
		{
			internal VoiceManager.FFIEvents.SettingsUpdateHandler OnSettingsUpdate;

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void SettingsUpdateHandler(IntPtr ptr);
		}

		internal struct FFIMethods
		{
			internal VoiceManager.FFIMethods.GetInputModeMethod GetInputMode;

			internal VoiceManager.FFIMethods.SetInputModeMethod SetInputMode;

			internal VoiceManager.FFIMethods.IsSelfMuteMethod IsSelfMute;

			internal VoiceManager.FFIMethods.SetSelfMuteMethod SetSelfMute;

			internal VoiceManager.FFIMethods.IsSelfDeafMethod IsSelfDeaf;

			internal VoiceManager.FFIMethods.SetSelfDeafMethod SetSelfDeaf;

			internal VoiceManager.FFIMethods.IsLocalMuteMethod IsLocalMute;

			internal VoiceManager.FFIMethods.SetLocalMuteMethod SetLocalMute;

			internal VoiceManager.FFIMethods.GetLocalVolumeMethod GetLocalVolume;

			internal VoiceManager.FFIMethods.SetLocalVolumeMethod SetLocalVolume;

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result GetInputModeMethod(IntPtr methodsPtr, ref InputMode inputMode);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void SetInputModeCallback(IntPtr ptr, Result result);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void SetInputModeMethod(IntPtr methodsPtr, InputMode inputMode, IntPtr callbackData, VoiceManager.FFIMethods.SetInputModeCallback callback);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result IsSelfMuteMethod(IntPtr methodsPtr, ref bool mute);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result SetSelfMuteMethod(IntPtr methodsPtr, bool mute);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result IsSelfDeafMethod(IntPtr methodsPtr, ref bool deaf);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result SetSelfDeafMethod(IntPtr methodsPtr, bool deaf);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result IsLocalMuteMethod(IntPtr methodsPtr, long userId, ref bool mute);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result SetLocalMuteMethod(IntPtr methodsPtr, long userId, bool mute);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result GetLocalVolumeMethod(IntPtr methodsPtr, long userId, ref byte volume);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result SetLocalVolumeMethod(IntPtr methodsPtr, long userId, byte volume);
		}

		public delegate void SetInputModeHandler(Result result);

		public delegate void SettingsUpdateHandler();
	}
}
