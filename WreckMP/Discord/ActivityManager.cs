using System;
using System.Runtime.InteropServices;

namespace Discord
{
	public class ActivityManager
	{
		public void RegisterCommand()
		{
			this.RegisterCommand(null);
		}

		private ActivityManager.FFIMethods Methods
		{
			get
			{
				if (this.MethodsStructure == null)
				{
					this.MethodsStructure = Marshal.PtrToStructure(this.MethodsPtr, typeof(ActivityManager.FFIMethods));
				}
				return (ActivityManager.FFIMethods)this.MethodsStructure;
			}
		}

		public event ActivityManager.ActivityJoinHandler OnActivityJoin;

		public event ActivityManager.ActivitySpectateHandler OnActivitySpectate;

		public event ActivityManager.ActivityJoinRequestHandler OnActivityJoinRequest;

		public event ActivityManager.ActivityInviteHandler OnActivityInvite;

		internal ActivityManager(IntPtr ptr, IntPtr eventsPtr, ref ActivityManager.FFIEvents events)
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

		private void InitEvents(IntPtr eventsPtr, ref ActivityManager.FFIEvents events)
		{
			events.OnActivityJoin = new ActivityManager.FFIEvents.ActivityJoinHandler(ActivityManager.OnActivityJoinImpl);
			events.OnActivitySpectate = new ActivityManager.FFIEvents.ActivitySpectateHandler(ActivityManager.OnActivitySpectateImpl);
			events.OnActivityJoinRequest = new ActivityManager.FFIEvents.ActivityJoinRequestHandler(ActivityManager.OnActivityJoinRequestImpl);
			events.OnActivityInvite = new ActivityManager.FFIEvents.ActivityInviteHandler(ActivityManager.OnActivityInviteImpl);
			Marshal.StructureToPtr(events, eventsPtr, false);
		}

		public void RegisterCommand(string command)
		{
			Result result = this.Methods.RegisterCommand(this.MethodsPtr, command);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
		}

		public void RegisterSteam(uint steamId)
		{
			Result result = this.Methods.RegisterSteam(this.MethodsPtr, steamId);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
		}

		[MonoPInvokeCallback]
		private static void UpdateActivityCallbackImpl(IntPtr ptr, Result result)
		{
			GCHandle gchandle = GCHandle.FromIntPtr(ptr);
			ActivityManager.UpdateActivityHandler updateActivityHandler = (ActivityManager.UpdateActivityHandler)gchandle.Target;
			gchandle.Free();
			updateActivityHandler(result);
		}

		public void UpdateActivity(Activity activity, ActivityManager.UpdateActivityHandler callback)
		{
			GCHandle gchandle = GCHandle.Alloc(callback);
			this.Methods.UpdateActivity(this.MethodsPtr, ref activity, GCHandle.ToIntPtr(gchandle), new ActivityManager.FFIMethods.UpdateActivityCallback(ActivityManager.UpdateActivityCallbackImpl));
		}

		[MonoPInvokeCallback]
		private static void ClearActivityCallbackImpl(IntPtr ptr, Result result)
		{
			GCHandle gchandle = GCHandle.FromIntPtr(ptr);
			ActivityManager.ClearActivityHandler clearActivityHandler = (ActivityManager.ClearActivityHandler)gchandle.Target;
			gchandle.Free();
			clearActivityHandler(result);
		}

		public void ClearActivity(ActivityManager.ClearActivityHandler callback)
		{
			GCHandle gchandle = GCHandle.Alloc(callback);
			this.Methods.ClearActivity(this.MethodsPtr, GCHandle.ToIntPtr(gchandle), new ActivityManager.FFIMethods.ClearActivityCallback(ActivityManager.ClearActivityCallbackImpl));
		}

		[MonoPInvokeCallback]
		private static void SendRequestReplyCallbackImpl(IntPtr ptr, Result result)
		{
			GCHandle gchandle = GCHandle.FromIntPtr(ptr);
			ActivityManager.SendRequestReplyHandler sendRequestReplyHandler = (ActivityManager.SendRequestReplyHandler)gchandle.Target;
			gchandle.Free();
			sendRequestReplyHandler(result);
		}

		public void SendRequestReply(long userId, ActivityJoinRequestReply reply, ActivityManager.SendRequestReplyHandler callback)
		{
			GCHandle gchandle = GCHandle.Alloc(callback);
			this.Methods.SendRequestReply(this.MethodsPtr, userId, reply, GCHandle.ToIntPtr(gchandle), new ActivityManager.FFIMethods.SendRequestReplyCallback(ActivityManager.SendRequestReplyCallbackImpl));
		}

		[MonoPInvokeCallback]
		private static void SendInviteCallbackImpl(IntPtr ptr, Result result)
		{
			GCHandle gchandle = GCHandle.FromIntPtr(ptr);
			ActivityManager.SendInviteHandler sendInviteHandler = (ActivityManager.SendInviteHandler)gchandle.Target;
			gchandle.Free();
			sendInviteHandler(result);
		}

		public void SendInvite(long userId, ActivityActionType type, string content, ActivityManager.SendInviteHandler callback)
		{
			GCHandle gchandle = GCHandle.Alloc(callback);
			this.Methods.SendInvite(this.MethodsPtr, userId, type, content, GCHandle.ToIntPtr(gchandle), new ActivityManager.FFIMethods.SendInviteCallback(ActivityManager.SendInviteCallbackImpl));
		}

		[MonoPInvokeCallback]
		private static void AcceptInviteCallbackImpl(IntPtr ptr, Result result)
		{
			GCHandle gchandle = GCHandle.FromIntPtr(ptr);
			ActivityManager.AcceptInviteHandler acceptInviteHandler = (ActivityManager.AcceptInviteHandler)gchandle.Target;
			gchandle.Free();
			acceptInviteHandler(result);
		}

		public void AcceptInvite(long userId, ActivityManager.AcceptInviteHandler callback)
		{
			GCHandle gchandle = GCHandle.Alloc(callback);
			this.Methods.AcceptInvite(this.MethodsPtr, userId, GCHandle.ToIntPtr(gchandle), new ActivityManager.FFIMethods.AcceptInviteCallback(ActivityManager.AcceptInviteCallbackImpl));
		}

		[MonoPInvokeCallback]
		private static void OnActivityJoinImpl(IntPtr ptr, string secret)
		{
			Discord discord = (Discord)GCHandle.FromIntPtr(ptr).Target;
			if (discord.ActivityManagerInstance.OnActivityJoin != null)
			{
				discord.ActivityManagerInstance.OnActivityJoin(secret);
			}
		}

		[MonoPInvokeCallback]
		private static void OnActivitySpectateImpl(IntPtr ptr, string secret)
		{
			Discord discord = (Discord)GCHandle.FromIntPtr(ptr).Target;
			if (discord.ActivityManagerInstance.OnActivitySpectate != null)
			{
				discord.ActivityManagerInstance.OnActivitySpectate(secret);
			}
		}

		[MonoPInvokeCallback]
		private static void OnActivityJoinRequestImpl(IntPtr ptr, ref User user)
		{
			Discord discord = (Discord)GCHandle.FromIntPtr(ptr).Target;
			if (discord.ActivityManagerInstance.OnActivityJoinRequest != null)
			{
				discord.ActivityManagerInstance.OnActivityJoinRequest(ref user);
			}
		}

		[MonoPInvokeCallback]
		private static void OnActivityInviteImpl(IntPtr ptr, ActivityActionType type, ref User user, ref Activity activity)
		{
			Discord discord = (Discord)GCHandle.FromIntPtr(ptr).Target;
			if (discord.ActivityManagerInstance.OnActivityInvite != null)
			{
				discord.ActivityManagerInstance.OnActivityInvite(type, ref user, ref activity);
			}
		}

		private IntPtr MethodsPtr;

		private object MethodsStructure;

		internal struct FFIEvents
		{
			internal ActivityManager.FFIEvents.ActivityJoinHandler OnActivityJoin;

			internal ActivityManager.FFIEvents.ActivitySpectateHandler OnActivitySpectate;

			internal ActivityManager.FFIEvents.ActivityJoinRequestHandler OnActivityJoinRequest;

			internal ActivityManager.FFIEvents.ActivityInviteHandler OnActivityInvite;

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void ActivityJoinHandler(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string secret);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void ActivitySpectateHandler(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string secret);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void ActivityJoinRequestHandler(IntPtr ptr, ref User user);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void ActivityInviteHandler(IntPtr ptr, ActivityActionType type, ref User user, ref Activity activity);
		}

		internal struct FFIMethods
		{
			internal ActivityManager.FFIMethods.RegisterCommandMethod RegisterCommand;

			internal ActivityManager.FFIMethods.RegisterSteamMethod RegisterSteam;

			internal ActivityManager.FFIMethods.UpdateActivityMethod UpdateActivity;

			internal ActivityManager.FFIMethods.ClearActivityMethod ClearActivity;

			internal ActivityManager.FFIMethods.SendRequestReplyMethod SendRequestReply;

			internal ActivityManager.FFIMethods.SendInviteMethod SendInvite;

			internal ActivityManager.FFIMethods.AcceptInviteMethod AcceptInvite;

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result RegisterCommandMethod(IntPtr methodsPtr, [MarshalAs(UnmanagedType.LPStr)] string command);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result RegisterSteamMethod(IntPtr methodsPtr, uint steamId);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void UpdateActivityCallback(IntPtr ptr, Result result);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void UpdateActivityMethod(IntPtr methodsPtr, ref Activity activity, IntPtr callbackData, ActivityManager.FFIMethods.UpdateActivityCallback callback);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void ClearActivityCallback(IntPtr ptr, Result result);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void ClearActivityMethod(IntPtr methodsPtr, IntPtr callbackData, ActivityManager.FFIMethods.ClearActivityCallback callback);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void SendRequestReplyCallback(IntPtr ptr, Result result);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void SendRequestReplyMethod(IntPtr methodsPtr, long userId, ActivityJoinRequestReply reply, IntPtr callbackData, ActivityManager.FFIMethods.SendRequestReplyCallback callback);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void SendInviteCallback(IntPtr ptr, Result result);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void SendInviteMethod(IntPtr methodsPtr, long userId, ActivityActionType type, [MarshalAs(UnmanagedType.LPStr)] string content, IntPtr callbackData, ActivityManager.FFIMethods.SendInviteCallback callback);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void AcceptInviteCallback(IntPtr ptr, Result result);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void AcceptInviteMethod(IntPtr methodsPtr, long userId, IntPtr callbackData, ActivityManager.FFIMethods.AcceptInviteCallback callback);
		}

		public delegate void UpdateActivityHandler(Result result);

		public delegate void ClearActivityHandler(Result result);

		public delegate void SendRequestReplyHandler(Result result);

		public delegate void SendInviteHandler(Result result);

		public delegate void AcceptInviteHandler(Result result);

		public delegate void ActivityJoinHandler(string secret);

		public delegate void ActivitySpectateHandler(string secret);

		public delegate void ActivityJoinRequestHandler(ref User user);

		public delegate void ActivityInviteHandler(ActivityActionType type, ref User user, ref Activity activity);
	}
}
