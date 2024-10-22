using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace WreckMP.Properties
{
	[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
	[DebuggerNonUserCode]
	[CompilerGenerated]
	internal class Resources
	{
		internal Resources()
		{
		}

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static ResourceManager ResourceManager
		{
			get
			{
				if (Resources.resourceMan == null)
				{
					Resources.resourceMan = new ResourceManager("WreckMP.Properties.Resources", typeof(Resources).Assembly);
				}
				return Resources.resourceMan;
			}
		}

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static CultureInfo Culture
		{
			get
			{
				return Resources.resourceCulture;
			}
			set
			{
				Resources.resourceCulture = value;
			}
		}

		internal static string clientID
		{
			get
			{
				return Resources.ResourceManager.GetString("clientID", Resources.resourceCulture);
			}
		}

		internal static byte[] clothes
		{
			get
			{
				return (byte[])Resources.ResourceManager.GetObject("clothes", Resources.resourceCulture);
			}
		}

		internal static byte[] wreckmp
		{
			get
			{
				return (byte[])Resources.ResourceManager.GetObject("wreckmp", Resources.resourceCulture);
			}
		}

		private static ResourceManager resourceMan;

		private static CultureInfo resourceCulture;
	}
}
