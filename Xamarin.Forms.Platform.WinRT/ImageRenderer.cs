using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Xamarin.Forms.Internals;

#if WINDOWS_UWP

namespace Xamarin.Forms.Platform.UWP
#else

namespace Xamarin.Forms.Platform.WinRT
#endif
{
	public class ImageRenderer : ViewRenderer<Image, Windows.UI.Xaml.Controls.Image>
	{
		bool _measured;
		bool _disposed;

		public override SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
		{
			if (Control.Source == null)
				return new SizeRequest();

			_measured = true;

			var result = new Size { Width = ((BitmapSource)Control.Source).PixelWidth, Height = ((BitmapSource)Control.Source).PixelHeight };

			return new SizeRequest(result);
		}

		protected override void Dispose(bool disposing)
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			if (disposing)
			{
				if (Control != null)
				{
					Control.ImageOpened -= OnImageOpened;
					Control.ImageFailed -= OnImageFailed;
					Control.SizeChanged -= ImageOnSizeChanged;
				}
			}

			base.Dispose(disposing);
		}

		protected override async void OnElementChanged(ElementChangedEventArgs<Image> e)
		{
			base.OnElementChanged(e);

			if (e.NewElement != null)
			{
				if (Control == null)
				{
					var image = new Windows.UI.Xaml.Controls.Image();
					image.ImageOpened += OnImageOpened;
					image.ImageFailed += OnImageFailed;
					image.SizeChanged += ImageOnSizeChanged;
					SetNativeControl(image);
				}

				await TryUpdateSource();
				UpdateAspect();
			}
		}

		void ImageOnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
		{
			Debug.WriteLine($">>>>> ImageRenderer ImageOnSizeChanged 79: Control.ActualHeight = {Control.ActualHeight}, Control.ActualWidth = {Control.ActualWidth}");

			ShrinkToAspect();
		}

		protected override async void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == Image.SourceProperty.PropertyName)
				await TryUpdateSource();
			else if (e.PropertyName == Image.AspectProperty.PropertyName)
				UpdateAspect();
		}

		static Stretch GetStretch(Aspect aspect)
		{
			switch (aspect)
			{
				case Aspect.Fill:
					return Stretch.Fill;
				case Aspect.AspectFill:
					return Stretch.UniformToFill;
				default:
				case Aspect.AspectFit:
					return Stretch.Uniform;
			}
		}

		void OnImageOpened(object sender, RoutedEventArgs routedEventArgs)
		{
			if (_measured)
			{
				RefreshImage();
			}

			Debug.WriteLine($">>>>> ImageRenderer OnImageOpened 106: Control.ActualHeight = {Control.ActualHeight}, Control.ActualWidth = {Control.ActualWidth}");

			Element?.SetIsLoading(false);
		}

		protected virtual void OnImageFailed(object sender, ExceptionRoutedEventArgs exceptionRoutedEventArgs)
		{
			Log.Warning("Image Loading", $"Image failed to load: {exceptionRoutedEventArgs.ErrorMessage}" );
			Element?.SetIsLoading(false);
		}

		void RefreshImage()
		{
			((IVisualElementController)Element)?.InvalidateMeasure(InvalidationTrigger.RendererReady);
		}

		void UpdateAspect()
		{
			if (_disposed || Element == null || Control == null)
			{
				return;
			}

			Control.Stretch = GetStretch(Element.Aspect);
			if (Element.Aspect == Aspect.AspectFill) // Then Center Crop
			{
				Control.HorizontalAlignment = HorizontalAlignment.Center;
				Control.VerticalAlignment = VerticalAlignment.Center;
			}
			else // Default
			{
				Control.HorizontalAlignment = HorizontalAlignment.Left;
				Control.VerticalAlignment = VerticalAlignment.Top;
			}
		}

		protected virtual async Task TryUpdateSource()
		{
			// By default we'll just catch and log any exceptions thrown by UpdateSource so we don't bring down
			// the application; a custom renderer can override this method and handle exceptions from
			// UpdateSource differently if it wants to

			try
			{
				await UpdateSource().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Log.Warning(nameof(ImageRenderer), "Error loading image: {0}", ex);
			}
			finally
			{
				((IImageController)Element)?.SetIsLoading(false);
			}
		}

		protected async Task UpdateSource()
		{
			if (_disposed || Element == null || Control == null)
			{
				return;
			}

			Element.SetIsLoading(true);

			ImageSource source = Element.Source;
			IImageSourceHandler handler;
			if (source != null && (handler = Registrar.Registered.GetHandler<IImageSourceHandler>(source.GetType())) != null)
			{
				Windows.UI.Xaml.Media.ImageSource imagesource;

				try
				{
					imagesource = await handler.LoadImageAsync(source);
				}
				catch (OperationCanceledException)
				{
					imagesource = null;
				}

				// In the time it takes to await the imagesource, some zippy little app
				// might have disposed of this Image already.
				if (Control != null)
				{
					Control.Source = imagesource;
				}

				RefreshImage();
			}
			else
			{
				Control.Source = null;
				Element.SetIsLoading(false);
			}
		}

		// TODO hartez 2017/05/26 12:57:36 Fix the int casts	
		// TODO hartez 2017/05/26 12:57:45 This should return a DidINvalidate bool so we know not to bother with Refresh	
		void ShrinkToAspect()
		{
			if (Element == null || Element.Aspect != Aspect.AspectFit
				|| Element.HeightRequest > -1 || Element.WidthRequest > -1
				|| Element.Height < 1 || Element.Width < 1)
			{
				return;
			}

			if (Control == null || Control.ActualHeight == 0 || Control.ActualWidth == 0)
			{
				return;
			}

			var size = new Tuple<int, int>((int)Control.ActualWidth, (int)Control.ActualHeight);

			if (size.Item1 <= 0 || size.Item2 <= 0)
			{
				return;
			}

			//Debug.WriteLine($">>>>> ImageRenderer Shrink 185: Width = {size.Item1}, Height = {size.Item2}");

			//Debug.WriteLine($">>>>> ImageRenderer Shrink 196: _element.Width = {_element.Width}, _element.Height = {_element.Height}");
			//Debug.WriteLine($">>>>> ImageRenderer Shrink 197: _element.WidthRequest = {_element.WidthRequest}, _element.HeightRequest = {_element.HeightRequest}");

			//	Debug.WriteLine($">>>>> ImageRenderer Shrink 201: (ContextFromPixels version): Width = {sizeViaContextFromPixels.Item1}, Height = {sizeViaContextFromPixels.Item2}");

			const double tolerance = 2;

			var heightDelta = Element.Height - size.Item2;

			if (heightDelta > tolerance)
			{
				Debug.WriteLine($">>>>> ImageRenderer ShrinkToAspect 211: Resizing because of height delta");
				Element.HeightRequest = size.Item2;
				Element.InvalidateMeasureNonVirtual(InvalidationTrigger.SizeRequestChanged);
				return;
			}

			var widthDelta = Element.Width - size.Item1;

			if (widthDelta > tolerance)
			{
				Debug.WriteLine($">>>>> ImageRenderer ShrinkToAspect 220: Resizing because of width delta");
				Element.WidthRequest = size.Item1;
				Element.InvalidateMeasureNonVirtual(InvalidationTrigger.SizeRequestChanged);
			}
		}
	}
}
