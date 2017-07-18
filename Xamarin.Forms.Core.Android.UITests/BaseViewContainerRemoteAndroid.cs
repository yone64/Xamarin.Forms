using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xamarin.Forms.Core.UITests
{
	internal abstract partial class BaseViewContainerRemote
	{
		bool TryConvertViewScale<T>(BindableProperty formProperty, string query, out T result)
		{
			result = default(T);

			if (formProperty == View.ScaleProperty) {

				Tuple<string[], bool> property = formProperty.GetPlatformPropertyQuery();
				string[] propertyPath = property.Item1;

				var matrix = new Matrix ();
				matrix.M00 = App.Query (q => q.Raw (query).Invoke (propertyPath[0]).Value<float> ()).First ();
				matrix.M11 = App.Query (q => q.Raw (query).Invoke (propertyPath[1]).Value<float> ()).First ();
				matrix.M22 = 0.5f;
				matrix.M33 = 1.0f;
				result = (T)((object)matrix);
				return true;
			}

			return false;
		}
	}
}
