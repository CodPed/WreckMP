using System;
using System.Reflection;

namespace WreckMP
{
	internal class FieldCacher
	{
		public FieldCacher(object src, Type t, bool publicOnly)
		{
			BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;
			if (!publicOnly)
			{
				bindingFlags |= BindingFlags.NonPublic;
			}
			this.fields = t.GetFields(bindingFlags);
			this.properties = t.GetProperties(bindingFlags);
			this.f_values = new object[this.fields.Length];
			this.p_values = new object[this.properties.Length];
			for (int i = 0; i < this.fields.Length; i++)
			{
				this.f_values[i] = this.fields[i].GetValue(src);
			}
			for (int j = 0; j < this.properties.Length; j++)
			{
				this.p_values[j] = this.properties[j].GetValue(src, null);
			}
		}

		public void Apply(object target)
		{
			for (int i = 0; i < this.fields.Length; i++)
			{
				this.fields[i].SetValue(target, this.f_values[i]);
			}
			for (int j = 0; j < this.properties.Length; j++)
			{
				if (this.properties[j].CanWrite)
				{
					this.properties[j].SetValue(target, this.p_values[j], null);
				}
			}
		}

		private FieldInfo[] fields;

		private PropertyInfo[] properties;

		private object[] f_values;

		private object[] p_values;
	}
}
