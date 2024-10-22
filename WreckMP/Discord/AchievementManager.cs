using System;
using System.Runtime.InteropServices;

namespace Discord
{
	public class AchievementManager
	{
		private AchievementManager.FFIMethods Methods
		{
			get
			{
				if (this.MethodsStructure == null)
				{
					this.MethodsStructure = Marshal.PtrToStructure(this.MethodsPtr, typeof(AchievementManager.FFIMethods));
				}
				return (AchievementManager.FFIMethods)this.MethodsStructure;
			}
		}

		public event AchievementManager.UserAchievementUpdateHandler OnUserAchievementUpdate;

		internal AchievementManager(IntPtr ptr, IntPtr eventsPtr, ref AchievementManager.FFIEvents events)
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

		private void InitEvents(IntPtr eventsPtr, ref AchievementManager.FFIEvents events)
		{
			events.OnUserAchievementUpdate = new AchievementManager.FFIEvents.UserAchievementUpdateHandler(AchievementManager.OnUserAchievementUpdateImpl);
			Marshal.StructureToPtr(events, eventsPtr, false);
		}

		[MonoPInvokeCallback]
		private static void SetUserAchievementCallbackImpl(IntPtr ptr, Result result)
		{
			GCHandle gchandle = GCHandle.FromIntPtr(ptr);
			AchievementManager.SetUserAchievementHandler setUserAchievementHandler = (AchievementManager.SetUserAchievementHandler)gchandle.Target;
			gchandle.Free();
			setUserAchievementHandler(result);
		}

		public void SetUserAchievement(long achievementId, byte percentComplete, AchievementManager.SetUserAchievementHandler callback)
		{
			GCHandle gchandle = GCHandle.Alloc(callback);
			this.Methods.SetUserAchievement(this.MethodsPtr, achievementId, percentComplete, GCHandle.ToIntPtr(gchandle), new AchievementManager.FFIMethods.SetUserAchievementCallback(AchievementManager.SetUserAchievementCallbackImpl));
		}

		[MonoPInvokeCallback]
		private static void FetchUserAchievementsCallbackImpl(IntPtr ptr, Result result)
		{
			GCHandle gchandle = GCHandle.FromIntPtr(ptr);
			AchievementManager.FetchUserAchievementsHandler fetchUserAchievementsHandler = (AchievementManager.FetchUserAchievementsHandler)gchandle.Target;
			gchandle.Free();
			fetchUserAchievementsHandler(result);
		}

		public void FetchUserAchievements(AchievementManager.FetchUserAchievementsHandler callback)
		{
			GCHandle gchandle = GCHandle.Alloc(callback);
			this.Methods.FetchUserAchievements(this.MethodsPtr, GCHandle.ToIntPtr(gchandle), new AchievementManager.FFIMethods.FetchUserAchievementsCallback(AchievementManager.FetchUserAchievementsCallbackImpl));
		}

		public int CountUserAchievements()
		{
			int num = 0;
			this.Methods.CountUserAchievements(this.MethodsPtr, ref num);
			return num;
		}

		public UserAchievement GetUserAchievement(long userAchievementId)
		{
			UserAchievement userAchievement = default(UserAchievement);
			Result result = this.Methods.GetUserAchievement(this.MethodsPtr, userAchievementId, ref userAchievement);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return userAchievement;
		}

		public UserAchievement GetUserAchievementAt(int index)
		{
			UserAchievement userAchievement = default(UserAchievement);
			Result result = this.Methods.GetUserAchievementAt(this.MethodsPtr, index, ref userAchievement);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return userAchievement;
		}

		[MonoPInvokeCallback]
		private static void OnUserAchievementUpdateImpl(IntPtr ptr, ref UserAchievement userAchievement)
		{
			Discord discord = (Discord)GCHandle.FromIntPtr(ptr).Target;
			if (discord.AchievementManagerInstance.OnUserAchievementUpdate != null)
			{
				discord.AchievementManagerInstance.OnUserAchievementUpdate(ref userAchievement);
			}
		}

		private IntPtr MethodsPtr;

		private object MethodsStructure;

		internal struct FFIEvents
		{
			internal AchievementManager.FFIEvents.UserAchievementUpdateHandler OnUserAchievementUpdate;

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void UserAchievementUpdateHandler(IntPtr ptr, ref UserAchievement userAchievement);
		}

		internal struct FFIMethods
		{
			internal AchievementManager.FFIMethods.SetUserAchievementMethod SetUserAchievement;

			internal AchievementManager.FFIMethods.FetchUserAchievementsMethod FetchUserAchievements;

			internal AchievementManager.FFIMethods.CountUserAchievementsMethod CountUserAchievements;

			internal AchievementManager.FFIMethods.GetUserAchievementMethod GetUserAchievement;

			internal AchievementManager.FFIMethods.GetUserAchievementAtMethod GetUserAchievementAt;

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void SetUserAchievementCallback(IntPtr ptr, Result result);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void SetUserAchievementMethod(IntPtr methodsPtr, long achievementId, byte percentComplete, IntPtr callbackData, AchievementManager.FFIMethods.SetUserAchievementCallback callback);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void FetchUserAchievementsCallback(IntPtr ptr, Result result);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void FetchUserAchievementsMethod(IntPtr methodsPtr, IntPtr callbackData, AchievementManager.FFIMethods.FetchUserAchievementsCallback callback);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void CountUserAchievementsMethod(IntPtr methodsPtr, ref int count);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result GetUserAchievementMethod(IntPtr methodsPtr, long userAchievementId, ref UserAchievement userAchievement);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result GetUserAchievementAtMethod(IntPtr methodsPtr, int index, ref UserAchievement userAchievement);
		}

		public delegate void SetUserAchievementHandler(Result result);

		public delegate void FetchUserAchievementsHandler(Result result);

		public delegate void UserAchievementUpdateHandler(ref UserAchievement userAchievement);
	}
}
