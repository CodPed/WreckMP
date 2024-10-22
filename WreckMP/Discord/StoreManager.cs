using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Discord
{
	public class StoreManager
	{
		private StoreManager.FFIMethods Methods
		{
			get
			{
				if (this.MethodsStructure == null)
				{
					this.MethodsStructure = Marshal.PtrToStructure(this.MethodsPtr, typeof(StoreManager.FFIMethods));
				}
				return (StoreManager.FFIMethods)this.MethodsStructure;
			}
		}

		public event StoreManager.EntitlementCreateHandler OnEntitlementCreate;

		public event StoreManager.EntitlementDeleteHandler OnEntitlementDelete;

		internal StoreManager(IntPtr ptr, IntPtr eventsPtr, ref StoreManager.FFIEvents events)
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

		private void InitEvents(IntPtr eventsPtr, ref StoreManager.FFIEvents events)
		{
			events.OnEntitlementCreate = new StoreManager.FFIEvents.EntitlementCreateHandler(StoreManager.OnEntitlementCreateImpl);
			events.OnEntitlementDelete = new StoreManager.FFIEvents.EntitlementDeleteHandler(StoreManager.OnEntitlementDeleteImpl);
			Marshal.StructureToPtr(events, eventsPtr, false);
		}

		[MonoPInvokeCallback]
		private static void FetchSkusCallbackImpl(IntPtr ptr, Result result)
		{
			GCHandle gchandle = GCHandle.FromIntPtr(ptr);
			StoreManager.FetchSkusHandler fetchSkusHandler = (StoreManager.FetchSkusHandler)gchandle.Target;
			gchandle.Free();
			fetchSkusHandler(result);
		}

		public void FetchSkus(StoreManager.FetchSkusHandler callback)
		{
			GCHandle gchandle = GCHandle.Alloc(callback);
			this.Methods.FetchSkus(this.MethodsPtr, GCHandle.ToIntPtr(gchandle), new StoreManager.FFIMethods.FetchSkusCallback(StoreManager.FetchSkusCallbackImpl));
		}

		public int CountSkus()
		{
			int num = 0;
			this.Methods.CountSkus(this.MethodsPtr, ref num);
			return num;
		}

		public Sku GetSku(long skuId)
		{
			Sku sku = default(Sku);
			Result result = this.Methods.GetSku(this.MethodsPtr, skuId, ref sku);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return sku;
		}

		public Sku GetSkuAt(int index)
		{
			Sku sku = default(Sku);
			Result result = this.Methods.GetSkuAt(this.MethodsPtr, index, ref sku);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return sku;
		}

		[MonoPInvokeCallback]
		private static void FetchEntitlementsCallbackImpl(IntPtr ptr, Result result)
		{
			GCHandle gchandle = GCHandle.FromIntPtr(ptr);
			StoreManager.FetchEntitlementsHandler fetchEntitlementsHandler = (StoreManager.FetchEntitlementsHandler)gchandle.Target;
			gchandle.Free();
			fetchEntitlementsHandler(result);
		}

		public void FetchEntitlements(StoreManager.FetchEntitlementsHandler callback)
		{
			GCHandle gchandle = GCHandle.Alloc(callback);
			this.Methods.FetchEntitlements(this.MethodsPtr, GCHandle.ToIntPtr(gchandle), new StoreManager.FFIMethods.FetchEntitlementsCallback(StoreManager.FetchEntitlementsCallbackImpl));
		}

		public int CountEntitlements()
		{
			int num = 0;
			this.Methods.CountEntitlements(this.MethodsPtr, ref num);
			return num;
		}

		public Entitlement GetEntitlement(long entitlementId)
		{
			Entitlement entitlement = default(Entitlement);
			Result result = this.Methods.GetEntitlement(this.MethodsPtr, entitlementId, ref entitlement);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return entitlement;
		}

		public Entitlement GetEntitlementAt(int index)
		{
			Entitlement entitlement = default(Entitlement);
			Result result = this.Methods.GetEntitlementAt(this.MethodsPtr, index, ref entitlement);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return entitlement;
		}

		public bool HasSkuEntitlement(long skuId)
		{
			bool flag = false;
			Result result = this.Methods.HasSkuEntitlement(this.MethodsPtr, skuId, ref flag);
			if (result != Result.Ok)
			{
				throw new ResultException(result);
			}
			return flag;
		}

		[MonoPInvokeCallback]
		private static void StartPurchaseCallbackImpl(IntPtr ptr, Result result)
		{
			GCHandle gchandle = GCHandle.FromIntPtr(ptr);
			StoreManager.StartPurchaseHandler startPurchaseHandler = (StoreManager.StartPurchaseHandler)gchandle.Target;
			gchandle.Free();
			startPurchaseHandler(result);
		}

		public void StartPurchase(long skuId, StoreManager.StartPurchaseHandler callback)
		{
			GCHandle gchandle = GCHandle.Alloc(callback);
			this.Methods.StartPurchase(this.MethodsPtr, skuId, GCHandle.ToIntPtr(gchandle), new StoreManager.FFIMethods.StartPurchaseCallback(StoreManager.StartPurchaseCallbackImpl));
		}

		[MonoPInvokeCallback]
		private static void OnEntitlementCreateImpl(IntPtr ptr, ref Entitlement entitlement)
		{
			Discord discord = (Discord)GCHandle.FromIntPtr(ptr).Target;
			if (discord.StoreManagerInstance.OnEntitlementCreate != null)
			{
				discord.StoreManagerInstance.OnEntitlementCreate(ref entitlement);
			}
		}

		[MonoPInvokeCallback]
		private static void OnEntitlementDeleteImpl(IntPtr ptr, ref Entitlement entitlement)
		{
			Discord discord = (Discord)GCHandle.FromIntPtr(ptr).Target;
			if (discord.StoreManagerInstance.OnEntitlementDelete != null)
			{
				discord.StoreManagerInstance.OnEntitlementDelete(ref entitlement);
			}
		}

		public IEnumerable<Entitlement> GetEntitlements()
		{
			int num = this.CountEntitlements();
			List<Entitlement> list = new List<Entitlement>();
			for (int i = 0; i < num; i++)
			{
				list.Add(this.GetEntitlementAt(i));
			}
			return list;
		}

		public IEnumerable<Sku> GetSkus()
		{
			int num = this.CountSkus();
			List<Sku> list = new List<Sku>();
			for (int i = 0; i < num; i++)
			{
				list.Add(this.GetSkuAt(i));
			}
			return list;
		}

		private IntPtr MethodsPtr;

		private object MethodsStructure;

		internal struct FFIEvents
		{
			internal StoreManager.FFIEvents.EntitlementCreateHandler OnEntitlementCreate;

			internal StoreManager.FFIEvents.EntitlementDeleteHandler OnEntitlementDelete;

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void EntitlementCreateHandler(IntPtr ptr, ref Entitlement entitlement);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void EntitlementDeleteHandler(IntPtr ptr, ref Entitlement entitlement);
		}

		internal struct FFIMethods
		{
			internal StoreManager.FFIMethods.FetchSkusMethod FetchSkus;

			internal StoreManager.FFIMethods.CountSkusMethod CountSkus;

			internal StoreManager.FFIMethods.GetSkuMethod GetSku;

			internal StoreManager.FFIMethods.GetSkuAtMethod GetSkuAt;

			internal StoreManager.FFIMethods.FetchEntitlementsMethod FetchEntitlements;

			internal StoreManager.FFIMethods.CountEntitlementsMethod CountEntitlements;

			internal StoreManager.FFIMethods.GetEntitlementMethod GetEntitlement;

			internal StoreManager.FFIMethods.GetEntitlementAtMethod GetEntitlementAt;

			internal StoreManager.FFIMethods.HasSkuEntitlementMethod HasSkuEntitlement;

			internal StoreManager.FFIMethods.StartPurchaseMethod StartPurchase;

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void FetchSkusCallback(IntPtr ptr, Result result);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void FetchSkusMethod(IntPtr methodsPtr, IntPtr callbackData, StoreManager.FFIMethods.FetchSkusCallback callback);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void CountSkusMethod(IntPtr methodsPtr, ref int count);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result GetSkuMethod(IntPtr methodsPtr, long skuId, ref Sku sku);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result GetSkuAtMethod(IntPtr methodsPtr, int index, ref Sku sku);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void FetchEntitlementsCallback(IntPtr ptr, Result result);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void FetchEntitlementsMethod(IntPtr methodsPtr, IntPtr callbackData, StoreManager.FFIMethods.FetchEntitlementsCallback callback);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void CountEntitlementsMethod(IntPtr methodsPtr, ref int count);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result GetEntitlementMethod(IntPtr methodsPtr, long entitlementId, ref Entitlement entitlement);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result GetEntitlementAtMethod(IntPtr methodsPtr, int index, ref Entitlement entitlement);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate Result HasSkuEntitlementMethod(IntPtr methodsPtr, long skuId, ref bool hasEntitlement);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void StartPurchaseCallback(IntPtr ptr, Result result);

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate void StartPurchaseMethod(IntPtr methodsPtr, long skuId, IntPtr callbackData, StoreManager.FFIMethods.StartPurchaseCallback callback);
		}

		public delegate void FetchSkusHandler(Result result);

		public delegate void FetchEntitlementsHandler(Result result);

		public delegate void StartPurchaseHandler(Result result);

		public delegate void EntitlementCreateHandler(ref Entitlement entitlement);

		public delegate void EntitlementDeleteHandler(ref Entitlement entitlement);
	}
}
