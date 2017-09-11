using System;
using System.ComponentModel;
using System.Linq;
using Android.Support.V4.View;
using Android.Views;

namespace Xamarin.Forms.Platform.Android
{
	internal class GestureManager : IDisposable
	{
		IVisualElementRenderer _renderer;
		readonly Lazy<GestureDetector> _tapAndPanDetector;
		readonly Lazy<ScaleGestureDetector> _scaleDetector;

		bool _disposed;
		bool _inputTransparent;
	    bool _isEnabled;

		VisualElement Element => _renderer?.Element;

		View View => _renderer?.Element as View;

		global::Android.Views.View Control => _renderer?.View;

		GestureDetector InitializeTapAndPanDetector()
		{
			var listener = new InnerGestureListener(new TapGestureHandler(() => View),
				new PanGestureHandler(() => View, Control.Context.FromPixels));

			return new GestureDetector(listener);
		}

		ScaleGestureDetector InitializeScaleDetector()
		{
			var listener = new InnerScaleListener(new PinchGestureHandler(() => View));
			var detector = new ScaleGestureDetector(Control.Context, listener, Control.Handler);
			ScaleGestureDetectorCompat.SetQuickScaleEnabled(detector, true);

			return detector;
		}

		public GestureManager(IVisualElementRenderer renderer)
		{
			_renderer = renderer;
			_renderer.ElementChanged += OnElementChanged;

			_tapAndPanDetector = new Lazy<GestureDetector>(InitializeTapAndPanDetector);
			_scaleDetector = new Lazy<ScaleGestureDetector>(InitializeScaleDetector);
		}

		bool ViewHasPinchGestures()
		{
			return View != null && View.GestureRecognizers.OfType<PinchGestureRecognizer>().Any();
		}

		public bool OnTouchEvent(MotionEvent e)
		{
			if (Control == null)
			{
				return false;
			}

			if (!_isEnabled || _inputTransparent)
			{
				return false;
			}

			var eventConsumed = false;
			if (ViewHasPinchGestures())
			{
				eventConsumed = _scaleDetector.Value.OnTouchEvent(e);
			}

			eventConsumed = _tapAndPanDetector.Value.OnTouchEvent(e) || eventConsumed;

			return eventConsumed;
		}

		void OnElementChanged(object sender, VisualElementChangedEventArgs e)
		{
			if (e.OldElement != null)
			{
				e.OldElement.PropertyChanged -= OnElementPropertyChanged;
			}

			if (e.NewElement != null)
			{
                e.NewElement.PropertyChanged += OnElementPropertyChanged;
			}

			UpdateInputTransparent();
            UpdateIsEnabled();
		}

		void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == VisualElement.InputTransparentProperty.PropertyName)
				UpdateInputTransparent();
            else if (e.PropertyName == VisualElement.IsEnabledProperty.PropertyName)
                UpdateIsEnabled();
        }

		protected void Dispose(bool disposing)
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			if (disposing)
			{
				if (Element != null)
				{
					Element.PropertyChanged -= OnElementPropertyChanged;
				}

				_renderer = null;
			}
		}

		void UpdateInputTransparent()
		{
			if (Element == null)
			{
				return;
			}

			_inputTransparent = Element.InputTransparent;
		}

        void UpdateIsEnabled()
        {
            if (Element == null)
            {
                return;
            }

            _isEnabled = Element.IsEnabled;
        }

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}