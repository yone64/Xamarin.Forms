using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Xamarin.Forms.StyleSheets
{
	sealed class StyleSheet : IStyle
	{
		StyleSheet()
		{
		}

		public IDictionary<Selector, Style> Styles { get; set; } = new Dictionary<Selector, Style>();

		public static StyleSheet Parse(TextReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException(nameof(reader));

			return Parse(new CssReader(reader));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static StyleSheet Parse(CssReader reader)
		{
			var sheet = new StyleSheet();

			Style style = null;
			var selector = Selector.All;

			int p;
			bool inStyle = false;
			reader.SkipWhiteSpaces();
			while ((p = reader.Peek()) > 0) {
				switch ((char)p) {
				case '@':
					throw new NotSupportedException("AT-rules not supported");
				case '{':
					reader.Read();
					style = Style.Parse(reader, '}');
					inStyle = true;
					break;
				case '}':
					reader.Read();
					if (!inStyle)
						throw new Exception();
					inStyle = false;
					sheet.Styles.Add(selector, style);
					style = null;
					selector = Selector.All;
					break;
				default:
					selector = Selector.Parse(reader, '{');
					break;
				}
			}
			return sheet;
		}

		public Type TargetType => typeof(VisualElement);

		public void Apply(BindableObject bindable)
		{
			var styleable = bindable as VisualElement;
			if (styleable == null)
				return;
			Apply(styleable);
		}

		void Apply(VisualElement styleable)
		{
			ApplyCore(styleable);
			foreach (var child in styleable.LogicalChildrenInternal)
				Apply(child);
		}

		void ApplyCore(VisualElement styleable)
		{
			foreach (var kvp in Styles) {
				var selector = kvp.Key;
				var style = kvp.Value;
				if (!selector.Matches(styleable))
					continue;
				style.Apply(styleable);
			}
		}

		public void UnApply(BindableObject bindable)
		{
			throw new NotImplementedException();
		}
	}
}