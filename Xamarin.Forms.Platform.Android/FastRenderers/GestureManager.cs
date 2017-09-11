using System;
using System.ComponentModel;
using Android.Support.V4.View;
using Android.Views;
using Object = Java.Lang.Object;

namespace Xamarin.Forms.Platform.Android.FastRenderers
{
	internal class GestureManager : Object
	{
		IVisualElementRenderer _renderer;
		readonly Lazy<GestureDetector> _tapAndPanDetector;
		readonly PanGestureHandler _panGestureHandler;
		readonly PinchGestureHandler _pinchGestureHandler;
		readonly Lazy<ScaleGestureDetector> _scaleDetector;
		readonly TapGestureHandler _tapGestureHandler;
        InnerGestureListener _gestureListener;

		bool _disposed;
		bool _inputTransparent;
	    bool _isEnabled;

		VisualElement Element => _renderer?.Element;

		View View => _renderer?.Element as View;

		global::Android.Views.View Control => _renderer?.View;

		public GestureManager(IVisualElementRenderer renderer)
		{
			_renderer = renderer;
			_renderer.ElementChanged += OnElementChanged;

			_tapGestureHandler = new TapGestureHandler(() => View);
			_panGestureHandler = new PanGestureHandler(() => View, Control.Context.FromPixels);
			_pinchGestureHandler = new PinchGestureHandler(() => View);
			_tapAndPanDetector =
				new Lazy<GestureDetector>(
					() =>
						new GestureDetector(
							_gestureListener =
								new InnerGestureListener(_tapGestureHandler, _panGestureHandler)));

			_scaleDetector =
				new Lazy<ScaleGestureDetector>(
					() =>
						new ScaleGestureDetector(Control.Context,
							new InnerScaleListener(_pinchGestureHandler.OnPinch, _pinchGestureHandler.OnPinchStarted,
								_pinchGestureHandler.OnPinchEnded), Control.Handler));
		}

		public bool OnTouchEvent(MotionEvent e)
		{
			if (!_isEnabled || _inputTransparent)
			{
				return false;
			}

			bool eventConsumed = false;
			if (_pinchGestureHandler.IsPinchSupported)
			{
				if (!_scaleDetector.IsValueCreated)
					ScaleGestureDetectorCompat.SetQuickScaleEnabled(_scaleDetector.Value, true);
				eventConsumed = _scaleDetector.Value.OnTouchEvent(e);
			}

			eventConsumed = _tapAndPanDetector.Value.OnTouchEvent(e) || eventConsumed;

			if (eventConsumed)
			{
				return true;
			}

			return false;
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

		protected override void Dispose(bool disposing)
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

				if (_gestureListener != null)
				{
					_gestureListener.Dispose();
					_gestureListener = null;
				}

				_renderer = null;
			}

			base.Dispose(disposing);
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
    }
}