using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Xamarin.Forms.CustomAttributes;
using Xamarin.Forms.Internals;

#if UITEST
using Xamarin.UITest;
using NUnit.Framework;
using Xamarin.Forms.Core.UITests;
#endif

namespace Xamarin.Forms.Controls.Issues
{
#if UITEST
	//[Category(UITestCategories.)]
#endif

	[Preserve(AllMembers = true)]
	[Issue(IssueTracker.Bugzilla, 44998, "Back on NavigationPage causes System.ObjectDisposedException", PlatformAffected.All)]
	public class Bugzilla44998 : TestNavigationPage 
	{
		protected override void Init()
		{
			s_dataCollection = new ObservableCollection<_44998Data>();

			for (int i = 0; i < 10; i++)
			{
				_44998Data d = new _44998Data
				{
					Text = "TEST" + i,
					Visible = true
				};
				s_dataCollection.Add(d);
			}

			PushAsync(Root());
		}

		static ObservableCollection<_44998Data> s_dataCollection;

		[Preserve(AllMembers = true)]
		public class _44998Data : INotifyPropertyChanged
		{
			string _text;
			bool _visible;

			public string Text
			{
				get { return _text; }
				set
				{
					_text = value;
					OnPropertyChanged();
				}
			}

			public bool Visible
			{
				get { return _visible; }
				set
				{
					_visible = value;
					OnPropertyChanged();
				}
			}

			public event PropertyChangedEventHandler PropertyChanged;

			protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		ContentPage Root()
		{
			var button = new Button { Text = "Go" };

			button.Clicked += (sender, args) =>
			{
				GC.Collect();

				var r = new Random(DateTime.Now.Millisecond);

				foreach (_44998Data item in s_dataCollection)
				{
					item.Text = r.NextDouble().ToString(CultureInfo.InvariantCulture);
					// item.Visible = !item.Visible;
				}

				Navigation.PushAsync(ListPage());
			};

			return new ContentPage { Content = button };
		}

		[Preserve(AllMembers = true)]
		class ListItem : ViewCell
		{
			public ListItem()
			{
				var title = new Label
				{
					FontSize = 35
				};

				title.SetBinding(Label.TextProperty, "Text");
				title.SetBinding(IsVisibleProperty, "Visible");

				View = title;
			}
		}

		static ContentPage ListPage()
		{
			var lv = new ListView
			{
				ItemTemplate = new DataTemplate(typeof(ListItem)),
				ItemsSource = s_dataCollection
			};

			var layout = new StackLayout();

			layout.Children.Add(lv);

			return new ContentPage { Content = layout };
		}

//#if UITEST
//		[Test]
//		public void _44998Test()
//		{
//			//RunningApp.WaitForElement(q => q.Marked(""));
//		}
//#endif
	}
}