using System.ComponentModel;
using Android.Content;
using Android.Views;
using Android.OS;

namespace Xamarin.Forms.Platform.Android
{
	public class ImageCellRenderer : TextCellRenderer
	{
		protected override global::Android.Views.View GetCellCore(Cell item, global::Android.Views.View convertView, ViewGroup parent, Context context)
		{
			var result = (BaseCellView)base.GetCellCore(item, convertView, parent, context);

			UpdateImage();
			UpdateLayoutDirection();

			return result;
		}

		protected override void OnCellPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			base.OnCellPropertyChanged(sender, args);
			if (args.PropertyName == ImageCell.ImageSourceProperty.PropertyName)
				UpdateImage();
		}

		void UpdateImage()
		{
			var cell = (ImageCell)Cell;
			if (cell.ImageSource != null)
			{
				View.SetImageVisible(true);
				View.SetImageSource(cell.ImageSource);
			}
			else
				View.SetImageVisible(false);
		}

		void UpdateLayoutDirection()
		{
			if (ViewController == null || (int)Build.VERSION.SdkInt < 17)
				return;

			if (ViewController.EffectiveFlowDirection.HasFlag(EffectiveFlowDirection.RightToLeft))
				View.LayoutDirection = LayoutDirection.Rtl;
			else if (ViewController.EffectiveFlowDirection.HasFlag(EffectiveFlowDirection.LeftToRight))
				View.LayoutDirection = LayoutDirection.Ltr;
		}
	}
}