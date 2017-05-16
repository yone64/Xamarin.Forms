using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Graphics.Drawables;
using AImageView = Android.Widget.ImageView;
using AView = Android.Views.View;
using Android.Views;
using Xamarin.Forms.Internals;

namespace Xamarin.Forms.Platform.Android.FastRenderers
{
	public class ImageRenderer : AImageView, IVisualElementRenderer, IImageRendererController
	{
		bool _disposed;
		Image _element;
		bool _skipInvalidate;
		int? _defaultLabelFor;
		VisualElementTracker _visualElementTracker;
		VisualElementRenderer _visualElementRenderer;

		protected override void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			_disposed = true;

			if (disposing)
			{
				if (_visualElementTracker != null)
				{
					_visualElementTracker.Dispose();
					_visualElementTracker = null;
				}

				if (_visualElementRenderer != null)
				{
					_visualElementRenderer.Dispose();
					_visualElementRenderer = null;
				}

				if (_element != null)
				{
					_element.PropertyChanged -= OnElementPropertyChanged;
				}
			}

			base.Dispose(disposing);
		}

		public override void Invalidate()
		{
			if (_skipInvalidate)
			{
				_skipInvalidate = false;
				return;
			}

			base.Invalidate();
		}

		protected virtual async void OnElementChanged(ElementChangedEventArgs<Image> e)
		{
			await TryUpdateBitmap(e.OldElement);
			UpdateAspect();

			ElementChanged?.Invoke(this, new VisualElementChangedEventArgs(e.OldElement, e.NewElement));
		}

        public override bool OnTouchEvent(MotionEvent e)
        {
            bool handled;
            var result = _visualElementRenderer.OnTouchEvent(e, Parent, out handled);

            return handled ? result : base.OnTouchEvent(e);
        }

		protected virtual Size MinimumSize()
		{
			return new Size();
		}

		SizeRequest IVisualElementRenderer.GetDesiredSize(int widthConstraint, int heightConstraint)
		{
			if (_disposed)
			{
				return new SizeRequest();
			}

			Measure(widthConstraint, heightConstraint);
			return new SizeRequest(new Size(MeasuredWidth, MeasuredHeight), MinimumSize());
		}

		void IVisualElementRenderer.SetElement(VisualElement element)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			var image = element as Image;
			if (image == null)
				throw new ArgumentException("Element is not of type " + typeof(Image), nameof(element));

			Image oldElement = _element;
			_element = image;

			Internals.Performance.Start();

			if (oldElement != null)
				oldElement.PropertyChanged -= OnElementPropertyChanged;

			element.PropertyChanged += OnElementPropertyChanged;

			if (_visualElementTracker == null)
				_visualElementTracker = new VisualElementTracker(this);

			if (_visualElementRenderer == null)
			{
				_visualElementRenderer = new VisualElementRenderer(this);
			}

			Internals.Performance.Stop();

			OnElementChanged(new ElementChangedEventArgs<Image>(oldElement, _element));

			_element?.SendViewInitialized(Control);
		}
		
		void IVisualElementRenderer.SetLabelFor(int? id)
		{
			if (_defaultLabelFor == null)
				_defaultLabelFor = LabelFor;

			LabelFor = (int)(id ?? _defaultLabelFor);
		}

		void IVisualElementRenderer.UpdateLayout() => _visualElementTracker?.UpdateLayout();

		VisualElement IVisualElementRenderer.Element => _element;

		VisualElementTracker IVisualElementRenderer.Tracker => _visualElementTracker;

		AView IVisualElementRenderer.View => this;

		ViewGroup IVisualElementRenderer.ViewGroup => null;

		void IImageRendererController.SkipInvalidate() => _skipInvalidate = true;

		protected AImageView Control => this;

		public event EventHandler<VisualElementChangedEventArgs> ElementChanged;
		public event EventHandler<PropertyChangedEventArgs> ElementPropertyChanged;

		public ImageRenderer() : base(Forms.Context)
		{
			
		}

		protected virtual async void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == Image.SourceProperty.PropertyName)
				await TryUpdateBitmap();
			else if (e.PropertyName == Image.AspectProperty.PropertyName)
				UpdateAspect();

			ElementPropertyChanged?.Invoke(this, e);
		}

		public override void Layout(int l, int t, int r, int b)
		{
			base.Layout(l, t, r, b);
			ShrinkToAspect();
		}

		protected virtual async Task TryUpdateBitmap(Image previous = null)
		{
			// By default we'll just catch and log any exceptions thrown by UpdateBitmap so they don't bring down
			// the application; a custom renderer can override this method and handle exceptions from
			// UpdateBitmap differently if it wants to

			try
			{
				await UpdateBitmap(previous);
			}
			catch (Exception ex)
			{
				Log.Warning(nameof(ImageRenderer), "Error loading image: {0}", ex);
			}
			finally
			{
				((IImageController)_element)?.SetIsLoading(false);
			}
		}

		protected async Task UpdateBitmap(Image previous = null)
		{
			if (_element == null || _disposed)
			{
				return;
			}

			await Control.UpdateBitmap(_element, previous);
		}

		void UpdateAspect()
		{
			if (_element == null || _disposed)
			{
				return;
			}

			ScaleType type = _element.Aspect.ToScaleType();
			SetScaleType(type);
		}

		// TODO hartez 2017/05/16 17:42:14 Clean this up if you can come up with reasonable iOS and Windows implementations	
		void ShrinkToAspect()
		{
			if (_element == null || _element.Aspect != Aspect.AspectFit
				|| _element.HeightRequest > -1 || _element.WidthRequest > -1
				|| _element.Height < 1 || _element.Width < 1)
			{
				return;
			}

			var size = GetBitmapDisplaySize(this);

			if (size.Item1 <= 0 || size.Item2 <= 0)
			{
				return;
			}

			//Debug.WriteLine($">>>>> ImageRenderer Shrink 185: Width = {size.Item1}, Height = {size.Item2}");

			//Debug.WriteLine($">>>>> ImageRenderer Shrink 196: _element.Width = {_element.Width}, _element.Height = {_element.Height}");
			//Debug.WriteLine($">>>>> ImageRenderer Shrink 197: _element.WidthRequest = {_element.WidthRequest}, _element.HeightRequest = {_element.HeightRequest}");

			var sizeViaContextFromPixels = new Tuple<double, double>(this.Context.FromPixels(size.Item1), this.Context.FromPixels(size.Item2));

			//	Debug.WriteLine($">>>>> ImageRenderer Shrink 201: (ContextFromPixels version): Width = {sizeViaContextFromPixels.Item1}, Height = {sizeViaContextFromPixels.Item2}");

			const double tolerance = 2;

			var heightDelta = _element.Height - sizeViaContextFromPixels.Item2;

			if (heightDelta > tolerance)
			{
				Debug.WriteLine($">>>>> ImageRenderer ShrinkToAspect 211: Resizing because of height delta");
				_element.HeightRequest = sizeViaContextFromPixels.Item2;
				_element.InvalidateMeasureNonVirtual(InvalidationTrigger.SizeRequestChanged);
				return;
			}

			var widthDelta = _element.Width - sizeViaContextFromPixels.Item1;

			if (widthDelta > tolerance)
			{
				Debug.WriteLine($">>>>> ImageRenderer ShrinkToAspect 220: Resizing because of width delta");
				_element.WidthRequest = sizeViaContextFromPixels.Item1;
				_element.InvalidateMeasureNonVirtual(InvalidationTrigger.SizeRequestChanged);
			}
		}

		public static Tuple<int, int> GetBitmapDisplaySize(AImageView imageView)
		{
			if (imageView?.Drawable == null)
				return new Tuple<int, int>(0, 0);

			// Get image dimensions
			// Get image matrix values and place them in an array
			float[] f = new float[9];
			imageView.ImageMatrix.GetValues(f);

			// Extract the scale values using the constants (if aspect ratio maintained, scaleX == scaleY)
			float scaleX = f[Matrix.MscaleX];
			float scaleY = f[Matrix.MscaleY];

			// Get the drawable (could also get the bitmap behind the drawable and getWidth/getHeight)
			Drawable d = imageView.Drawable;
			int origW = d.IntrinsicWidth;
			int origH = d.IntrinsicHeight;

			// Calculate the actual dimensions
			int actW = (int)Math.Round(origW * scaleX);
			int actH = (int)Math.Round(origH * scaleY);

			return new Tuple<int, int>(actW, actH);
		}
	}
}
