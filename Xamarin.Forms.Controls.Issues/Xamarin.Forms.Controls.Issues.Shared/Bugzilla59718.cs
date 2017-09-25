using Xamarin.Forms.CustomAttributes;
using Xamarin.Forms.Internals;
using System.Linq;
using System;

#if UITEST
using Xamarin.UITest;
using NUnit.Framework;
#endif

namespace Xamarin.Forms.Controls.Issues
{
	[Preserve(AllMembers = true)]
	[Issue(IssueTracker.Bugzilla, 59718, "Multiple issues with listview and navigation in UWP", PlatformAffected.UWP)]
	public class Bugzilla59718 : TestContentPage
	{
		Label _ItemSelectedLabel;
		Label _ItemTappedLabel;
		ListView _list;
		Label _tappedLabel;

		protected override void Init()
		{
			_tappedLabel = new Label { AutomationId = "_tappedLabel", TextColor = Color.Red };
			_ItemTappedLabel = new Label { AutomationId = "_itemTappedLabel", TextColor = Color.Purple };
			_ItemSelectedLabel = new Label { AutomationId = "_ItemSelectedLabel", TextColor = Color.Blue };

			_list = new ListView
			{
				ItemTemplate = new DataTemplate(() =>
				{
					var label = new Label { Text = "Tap me" };
					var tap = new TapGestureRecognizer();
					label.GestureRecognizers.Add(tap);
					tap.Tapped += TapGestureRecognizer_Tapped;
					var view = new ViewCell { View = label };
					return view;
				})
			};
			_list.ItemTapped += ListView_ItemTapped;
			_list.ItemSelected += ListView_ItemSelected;

			Content = new StackLayout { Children = { _tappedLabel, _ItemTappedLabel, _ItemSelectedLabel, _list } };
		}

		protected override void OnAppearing()
		{
			_list.ItemsSource = Enumerable.Range(0, 100);

			base.OnAppearing();
		}

		private void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
		{
			_ItemSelectedLabel.Text += $"ListView_ItemSelected: {e.SelectedItem}\r\n";
		}

		async void ListView_ItemTapped(object sender, ItemTappedEventArgs e)
		{
			_ItemTappedLabel.Text += $"_ItemTappedLabel: {e.Item}\r\n";

			await Navigation.PushAsync(new NextPage(_ItemTappedLabel.Text));
			((ListView)sender).SelectedItem = null;
		}

		void TapGestureRecognizer_Tapped(object sender, EventArgs e)
		{
			_tappedLabel.Text = "TapGestureRecognizer_Tapped: Tapped!";
		}

		class NextPage : ContentPage
		{
			public NextPage(string log)
			{
				Content = new Label { Text = log };
			}
		}

#if UITEST
		[Test]
		public void Bugzilla59718Test()
		{
			RunningApp.Screenshot("I am at Issue 1");
			RunningApp.WaitForElement(q => q.Marked("IssuePageLabel"));
			RunningApp.Screenshot("I see the Label");
		}
#endif
	}
}