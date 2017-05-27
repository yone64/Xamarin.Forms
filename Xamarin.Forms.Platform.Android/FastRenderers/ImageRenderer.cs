using System;
using System.ComponentModel;
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
			ShrinkIfNecessary();
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


		bool IsElementShrinkable()
		{
			return _element != null
				   && _element.Aspect == Aspect.AspectFit
				   && _element.HeightRequest <= -1
				   && _element.WidthRequest <= -1
				   && _element.Height > 1
				   && _element.Width > 1;
		}

		void ShrinkIfNecessary()
		{
			if (!IsElementShrinkable())
			{
				return;
			}

			// The the size of the image on screen
			var size = GetBitmapDisplaySize(this);

			if (size.Width <= 0 || size.Height <= 0)
			{
				return;
			}

			// Figure out the device-independent size
			var independentSize = new Size(Context.FromPixels(size.Width), Context.FromPixels(size.Height));

			const double tolerance = 2;

			var heightDelta = _element.Height - independentSize.Height;

			if (heightDelta > tolerance)
			{
				_element.HeightRequest = independentSize.Height;
				_element.InvalidateMeasureNonVirtual(InvalidationTrigger.SizeRequestChanged);
				return;
			}

			var widthDelta = _element.Width - independentSize.Width;

			if (widthDelta > tolerance)
			{
				_element.WidthRequest = independentSize.Width;
				_element.InvalidateMeasureNonVirtual(InvalidationTrigger.SizeRequestChanged);
			}
		}

		static Size GetBitmapDisplaySize(AImageView imageView)
		{
			if (imageView?.Drawable == null)
			{
				return Size.Zero;
			}

			var matrixValues = new float[9];
			imageView.ImageMatrix.GetValues(matrixValues);

			// Extract the scale values using the constants (if aspect ratio maintained, scaleX == scaleY)
			float scaleX = matrixValues[Matrix.MscaleX];
			float scaleY = matrixValues[Matrix.MscaleY];

			// Get the size of the drawable if it weren't scaled/fit
			Drawable drawable = imageView.Drawable;
			int width = drawable.IntrinsicWidth;
			int height = drawable.IntrinsicHeight;

			// Figure out the current width/height of the drawable
			double currentWidth = Math.Round(width * scaleX);
			double currentHeight = Math.Round(height * scaleY);

			return new Size(currentWidth, currentHeight);
		}
	}
}
