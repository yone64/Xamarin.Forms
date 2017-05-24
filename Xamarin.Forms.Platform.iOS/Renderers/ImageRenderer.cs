using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using UIKit;
using Xamarin.Forms.Internals;
using RectangleF = CoreGraphics.CGRect;

namespace Xamarin.Forms.Platform.iOS
{
	public static class ImageExtensions
	{
		public static UIViewContentMode ToUIViewContentMode(this Aspect aspect)
		{
			switch (aspect)
			{
				case Aspect.AspectFill:
					return UIViewContentMode.ScaleAspectFill;
				case Aspect.Fill:
					return UIViewContentMode.ScaleToFill;
				case Aspect.AspectFit:
				default:
					return UIViewContentMode.ScaleAspectFit;
			}
		}
	}

	public class ImageRenderer : ViewRenderer<Image, UIImageView>
	{
		bool _isDisposed;

		protected override void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			if (disposing)
			{
				UIImage oldUIImage;
				if (Control != null && (oldUIImage = Control.Image) != null)
				{
					oldUIImage.Dispose();
				}
			}

			_isDisposed = true;

			base.Dispose(disposing);
		}

		protected override async void OnElementChanged(ElementChangedEventArgs<Image> e)
		{
			if (Control == null)
			{
				var imageView = new UIImageView(RectangleF.Empty);
				imageView.ContentMode = UIViewContentMode.ScaleAspectFit;
				imageView.ClipsToBounds = true;
				SetNativeControl(imageView);
			}

			if (e.NewElement != null)
			{
				SetAspect();
				await TrySetImage(e.OldElement);
				SetOpacity();
			}

			base.OnElementChanged(e);
		}

		protected override async void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);
			if (e.PropertyName == Image.SourceProperty.PropertyName)
				await TrySetImage();
			else if (e.PropertyName == Image.IsOpaqueProperty.PropertyName)
				SetOpacity();
			else if (e.PropertyName == Image.AspectProperty.PropertyName)
				SetAspect();
		}

		void SetAspect()
		{
			if (_isDisposed || Element == null || Control == null)
			{
				return;
			}

			Control.ContentMode = Element.Aspect.ToUIViewContentMode();
		}

		protected virtual async Task TrySetImage(Image previous = null)
		{
			// By default we'll just catch and log any exceptions thrown by SetImage so they don't bring down
			// the application; a custom renderer can override this method and handle exceptions from
			// SetImage differently if it wants to

			try
			{
				await SetImage(previous).ConfigureAwait(false);
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

		protected async Task SetImage(Image oldElement = null)
		{
			if (_isDisposed || Element == null || Control == null)
			{
				return;
			}

			var source = Element.Source;

			if (oldElement != null)
			{
				var oldSource = oldElement.Source;
				if (Equals(oldSource, source))
					return;

				if (oldSource is FileImageSource && source is FileImageSource &&
				    ((FileImageSource)oldSource).File == ((FileImageSource)source).File)
					return;

				Control.Image = null;
			}

			IImageSourceHandler handler;

			Element.SetIsLoading(true);

			if (source != null &&
			    (handler = Internals.Registrar.Registered.GetHandler<IImageSourceHandler>(source.GetType())) != null)
			{
				UIImage uiimage;
				try
				{
					uiimage = await handler.LoadImageAsync(source, scale: (float)UIScreen.MainScreen.Scale);
				}
				catch (OperationCanceledException)
				{
					uiimage = null;
				}

				if (_isDisposed)
					return;

				var imageView = Control;
				if (imageView != null)
					imageView.Image = uiimage;

				((IVisualElementController)Element).NativeSizeChanged();
			}
			else
			{
				Control.Image = null;
			}

			Element.SetIsLoading(false);
		}

		void SetOpacity()
		{
			if (_isDisposed || Element == null || Control == null)
			{
				return;
			}

			Control.Opaque = Element.IsOpaque;
		}

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();
			ShrinkToAspect();
		}

		void ShrinkToAspect()
		{
			if (Element == null || Control == null || Element.Aspect != Aspect.AspectFit
			    || Element.HeightRequest > -1 || Element.WidthRequest > -1
			    || Element.Height < 1 || Element.Width < 1)
			{
				return;
			}

			var size = GetBitmapDisplaySize(Control);

			if (size.Item1 <= 0 || size.Item2 <= 0)
			{
				return;
			}

			//Debug.WriteLine($">>>>> ImageRenderer Shrink 185: Width = {size.Item1}, Height = {size.Item2}");

			//Debug.WriteLine($">>>>> ImageRenderer Shrink 196: _element.Width = {_element.Width}, _element.Height = {_element.Height}");
			//Debug.WriteLine($">>>>> ImageRenderer Shrink 197: _element.WidthRequest = {_element.WidthRequest}, _element.HeightRequest = {_element.HeightRequest}");

			//	var sizeViaContextFromPixels = new Tuple<double, double>(this.Context.FromPixels(size.Item1), this.Context.FromPixels(size.Item2));

			//	Debug.WriteLine($">>>>> ImageRenderer Shrink 201: (ContextFromPixels version): Width = {sizeViaContextFromPixels.Item1}, Height = {sizeViaContextFromPixels.Item2}");

			const double tolerance = 2;

			var heightDelta = Element.Height - size.Item2;

			if (heightDelta > tolerance)
			{

				Element.HeightRequest = size.Item2;
				Element.InvalidateMeasureNonVirtual(InvalidationTrigger.SizeRequestChanged);
				return;
			}

			var widthDelta = Element.Width - size.Item1;

			if (widthDelta > tolerance)
			{

				Element.WidthRequest = size.Item1;
				Element.InvalidateMeasureNonVirtual(InvalidationTrigger.SizeRequestChanged);
			}

		}

		public static Tuple<int, int> GetBitmapDisplaySize(UIImageView imageView)
		{
			CGSize sizeInView = AVFoundation.AVUtilities.WithAspectRatio(imageView.Bounds, imageView.Image.Size).Size;
			return new Tuple<int, int>((int)sizeInView.Width, (int)sizeInView.Height);
		}
	}

	public interface IImageSourceHandler : IRegisterable
	{
		Task<UIImage> LoadImageAsync(ImageSource imagesource, CancellationToken cancelationToken = default(CancellationToken), float scale = 1);
	}

	public sealed class FileImageSourceHandler : IImageSourceHandler
	{
		public Task<UIImage> LoadImageAsync(ImageSource imagesource, CancellationToken cancelationToken = default(CancellationToken), float scale = 1f)
		{
			UIImage image = null;
			var filesource = imagesource as FileImageSource;
			var file = filesource?.File;
			if (!string.IsNullOrEmpty(file))
				image = File.Exists(file) ? new UIImage(file) : UIImage.FromBundle(file);

			if (image == null)
			{
				Log.Warning(nameof(FileImageSourceHandler), "Could not find image: {0}", imagesource);
			}

			return Task.FromResult(image);
		}
	}

	public sealed class StreamImagesourceHandler : IImageSourceHandler
	{
		public async Task<UIImage> LoadImageAsync(ImageSource imagesource, CancellationToken cancelationToken = default(CancellationToken), float scale = 1f)
		{
			UIImage image = null;
			var streamsource = imagesource as StreamImageSource;
			if (streamsource?.Stream != null)
			{
				using (var streamImage = await ((IStreamImageSource)streamsource).GetStreamAsync(cancelationToken).ConfigureAwait(false))
				{
					if (streamImage != null)
						image = UIImage.LoadFromData(NSData.FromStream(streamImage), scale);
				}
			}

			if (image == null)
			{
				Log.Warning(nameof(StreamImagesourceHandler), "Could not load image: {0}", streamsource);
			}

			return image;
		}
	}

	public sealed class ImageLoaderSourceHandler : IImageSourceHandler
	{
		public async Task<UIImage> LoadImageAsync(ImageSource imagesource, CancellationToken cancelationToken = default(CancellationToken), float scale = 1f)
		{
			UIImage image = null;
			var imageLoader = imagesource as UriImageSource;
			if (imageLoader?.Uri != null)
			{
				using (var streamImage = await imageLoader.GetStreamAsync(cancelationToken).ConfigureAwait(false))
				{
					if (streamImage != null)
						image = UIImage.LoadFromData(NSData.FromStream(streamImage), scale);
				}
			}

			if (image == null)
			{
				Log.Warning(nameof(ImageLoaderSourceHandler), "Could not load image: {0}", imageLoader);
			}

			return image;
		}
	}
}