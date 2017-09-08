using Android.Views;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.ControlGallery.Android;
using Xamarin.Forms.Controls;
using Xamarin.Forms.Platform.Android;
using static Android.Views.ViewTreeObserver;

[assembly: ExportRenderer(typeof(PerformanceTracker), typeof(PerformanceTrackerRenderer))]

namespace Xamarin.Forms.ControlGallery.Android
{
	public class PerformanceTrackerRenderer : ViewRenderer, IOnDrawListener
	{
		public PerformanceTrackerRenderer()
		{
			ViewTreeObserver.AddOnDrawListener(this);
		}

		PerformanceTracker PerformanceTracker => Element as PerformanceTracker;

		void IOnDrawListener.OnDraw()
		{
			PerformanceTracker.Watcher.BeginTest(cleanup: () => UnsubscribeChildrenToDraw(this, this));
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == PerformanceTracker.ContentProperty.PropertyName)
			{
				PerformanceTracker.Watcher.ResetTest();

				SubscribeChildrenToDraw(this, this);
			}
		}

		static void SubscribeChildrenToDraw(ViewGroup viewGroup, IOnDrawListener observer)
		{
			if (viewGroup == null)
				return;

			for (int i = 0; i < viewGroup.ChildCount; i++)
			{
				var child = viewGroup.GetChildAt(i);
				child.ViewTreeObserver.AddOnDrawListener(observer);

				SubscribeChildrenToDraw(child as ViewGroup, observer);
			}
		}

		static void UnsubscribeChildrenToDraw(ViewGroup viewGroup, IOnDrawListener observer)
		{
			if (viewGroup == null)
				return;

			for (int i = 0; i < viewGroup.ChildCount; i++)
			{
				var child = viewGroup.GetChildAt(i);
				child.ViewTreeObserver.RemoveOnDrawListener(observer);

				UnsubscribeChildrenToDraw(child as ViewGroup, observer);
			}
		}
	}
}