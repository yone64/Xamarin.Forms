using System.ComponentModel;
using Android.Content.Res;
using Android.Text;
using Android.Text.Method;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Java.Lang;

namespace Xamarin.Forms.Platform.Android
{
	public class EntryRenderer : ViewRenderer<Entry, EntryEditText>, ITextWatcher, TextView.IOnEditorActionListener
	{
		TextColorSwitcher _hintColorSwitcher;
		TextColorSwitcher _textColorSwitcher;
		bool _useLegacyColorManagement;
		EntryEditText _textView;

		public EntryRenderer()
		{
			AutoPackage = false;
		}

		bool TextView.IOnEditorActionListener.OnEditorAction(TextView v, ImeAction actionId, KeyEvent e)
		{
			// Fire Completed and dismiss keyboard for hardware / physical keyboards
			if (actionId == ImeAction.Done || (actionId == ImeAction.ImeNull && e.KeyCode == Keycode.Enter))
			{
				Control.ClearFocus();
				v.HideKeyboard();
				((IEntryController)Element).SendCompleted();
			}

			return true;
		}

		void ITextWatcher.AfterTextChanged(IEditable s)
		{
		}

		void ITextWatcher.BeforeTextChanged(ICharSequence s, int start, int count, int after)
		{
		}

		void ITextWatcher.OnTextChanged(ICharSequence s, int start, int before, int count)
		{
			if (string.IsNullOrEmpty(Element.Text) && s.Length() == 0)
				return;

			((IElementController)Element).SetValueFromRenderer(Entry.TextProperty, s.ToString());
		}

		protected override EntryEditText CreateNativeControl()
		{
			return new EntryEditText(Context);
		}

		protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
		{
			base.OnElementChanged(e);

			HandleKeyboardOnFocus = true;

			if (e.OldElement == null)
			{
				_textView = CreateNativeControl();
				_textView.ImeOptions = ImeAction.Done;
				_textView.AddTextChangedListener(this);
				_textView.SetOnEditorActionListener(this);
				_textView.OnKeyboardBackPressed += (sender, args) => _textView.ClearFocus();

				_textColorSwitcher = new TextColorSwitcher(_textView.TextColors);
				_hintColorSwitcher = new TextColorSwitcher(_textView.HintTextColors);

				_useLegacyColorManagement = VisualStateManager.GetVisualStateGroups(e.NewElement) == null;

				SetNativeControl(_textView);
			}

			_textView.Hint = Element.Placeholder;
			_textView.Text = Element.Text;
			UpdateInputType();

			UpdateColor();
			UpdateAlignment();
			UpdateFont();
			UpdatePlaceholderColor();
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == Entry.PlaceholderProperty.PropertyName)
				Control.Hint = Element.Placeholder;
			else if (e.PropertyName == Entry.IsPasswordProperty.PropertyName)
				UpdateInputType();
			else if (e.PropertyName == Entry.TextProperty.PropertyName)
			{
				if (Control.Text != Element.Text)
				{
					Control.Text = Element.Text;
					if (Control.IsFocused)
					{
						Control.SetSelection(Control.Text.Length);
						Control.ShowKeyboard();
					}
				}
			}
			else if (e.PropertyName == Entry.TextColorProperty.PropertyName)
				UpdateColor();
			else if (e.PropertyName == InputView.KeyboardProperty.PropertyName)
				UpdateInputType();
			else if (e.PropertyName == Entry.HorizontalTextAlignmentProperty.PropertyName)
				UpdateAlignment();
			else if (e.PropertyName == Entry.FontAttributesProperty.PropertyName)
				UpdateFont();
			else if (e.PropertyName == Entry.FontFamilyProperty.PropertyName)
				UpdateFont();
			else if (e.PropertyName == Entry.FontSizeProperty.PropertyName)
				UpdateFont();
			else if (e.PropertyName == Entry.PlaceholderColorProperty.PropertyName)
				UpdatePlaceholderColor();

			base.OnElementPropertyChanged(sender, e);
		}

		protected virtual NumberKeyListener GetDigitsKeyListener(InputTypes inputTypes)
		{
			// Override this in a custom renderer to use a different NumberKeyListener 
			// or to filter out input types you don't want to allow 
			// (e.g., inputTypes &= ~InputTypes.NumberFlagSigned to disallow the sign)
			return LocalizedDigitsKeyListener.Create(inputTypes);
		}

		void UpdateAlignment()
		{
			Control.Gravity = Element.HorizontalTextAlignment.ToHorizontalGravityFlags();
		}

		void UpdateColor()
		{
			if (_useLegacyColorManagement)
			{
				_textColorSwitcher.UpdateTextColor(Control, Element.TextColor);
			}
			else
			{
				_textColorSwitcher.UpdateTextColor(Control, Element.TextColor, preserveDisabled: false);
			}
		}

		void UpdateFont()
		{
			Control.Typeface = Element.ToTypeface();
			Control.SetTextSize(ComplexUnitType.Sp, (float)Element.FontSize);
		}

		void UpdateInputType()
		{
			Entry model = Element;
			var keyboard = model.Keyboard;

			_textView.InputType = keyboard.ToInputType();

			if (keyboard == Keyboard.Numeric)
			{
				_textView.KeyListener = GetDigitsKeyListener(_textView.InputType);
			}

			if (model.IsPassword && ((_textView.InputType & InputTypes.ClassText) == InputTypes.ClassText))
				_textView.InputType = _textView.InputType | InputTypes.TextVariationPassword;
			if (model.IsPassword && ((_textView.InputType & InputTypes.ClassNumber) == InputTypes.ClassNumber))
				_textView.InputType = _textView.InputType | InputTypes.NumberVariationPassword;
		}

		void UpdatePlaceholderColor()
		{
			if (_useLegacyColorManagement)
			{
				_hintColorSwitcher.UpdateTextColor(Control, Element.PlaceholderColor, Control.SetHintTextColor);
			}
			else
			{
				_hintColorSwitcher.UpdateTextColor(Control, Element.PlaceholderColor, Control.SetHintTextColor, preserveDisabled: false);
			}
		}
	}
}