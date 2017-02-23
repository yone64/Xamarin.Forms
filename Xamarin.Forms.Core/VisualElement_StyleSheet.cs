using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xamarin.Forms.Internals;
using Xamarin.Forms.StyleSheets;


namespace Xamarin.Forms
{
	public partial class Element : IStyleSelectable
	{
		IEnumerable<IStyleSelectable> IStyleSelectable.Children => LogicalChildrenInternal;

		IList<string> IStyleSelectable.Classes => null;

		string IStyleSelectable.Id => StyleId;

		string _styleSelectableName;
		string IStyleSelectable.Name => _styleSelectableName ?? (_styleSelectableName = GetType().Name);

		IStyleSelectable IStyleSelectable.Parent => Parent;
	}

	public partial class VisualElement : IStyleSelectable, IStylable
	{
		IList<string> IStyleSelectable.Classes => StyleClass;

		public static readonly BindableProperty StyleSheetProperty =
			BindableProperty.Create("StyleSheet", typeof(string), typeof(VisualElement), default(string),
				propertyChanged: (bp, o, n) => ((VisualElement)bp).OnStyleSheetChanged((string)o, (string)n));

		public string StyleSheet {
			get { return (string)GetValue(StyleSheetProperty); }
			set { SetValue(StyleSheetProperty, value); }
		}

		StyleSheet _sheet;
		void OnStyleSheetChanged(string oldValue, string newValue)
		{
			if (_sheet != null)
				_sheet.UnApply(this);
			_sheet = Xamarin.Forms.StyleSheets.StyleSheet.Parse(new StringReader(newValue));
			if (_sheet != null)
				_sheet.Apply(this);
		}

		BindableProperty IStylable.GetProperty(string key)
		{
			StylePropertyAttribute styleAttribute;
			if (!Xamarin.Forms.Internals.Registrar.StyleProperties.TryGetValue(key, out styleAttribute))
				return null;

			if (!styleAttribute.TargetType.GetTypeInfo().IsAssignableFrom(GetType().GetTypeInfo()))
				return null;

			if (styleAttribute.BindableProperty != null)
				return styleAttribute.BindableProperty;

			var bpField = GetType().GetField(styleAttribute.BindablePropertyName);
			if (bpField == null || !bpField.IsStatic)
				return null;

			return (styleAttribute.BindableProperty = bpField.GetValue(null) as BindableProperty);
		}

		void ApplyStyleSheetOnParentSet()
		{
			var parent = Parent;
			if (parent == null)
				return;
			var sheets = new List<StyleSheet>();
			while (parent != null) {
				var visualParent = parent as VisualElement;
				var sheet = visualParent?._sheet;
				if (sheet != null)
					sheets.Add(sheet);
				parent = parent.Parent;
			}
			for (var i = sheets.Count - 1; i >= 0; i--)
				sheets[i].Apply(this);
		}
	}
}