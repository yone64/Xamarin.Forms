using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.CustomAttributes;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;

namespace Xamarin.Forms.Controls.Issues
{
	[Preserve(AllMembers = true)]
	[Issue(IssueTracker.Bugzilla, 945153, "ScrollView with 2x3 Grid of images doesn't fit screen well", PlatformAffected.All)]
	public partial class Bugzilla45153 :  TestContentPage
	{
		public Bugzilla45153 ()
		{
#if APP
			InitializeComponent ();

			//var s = Device.Info.ScaledScreenSize;

			//var widthPadding = ImageGrid.Padding.Left + ImageGrid.Padding.Right;

			//var cols = ImageGrid.ColumnDefinitions.Count;

			//var totalSpacing = ImageGrid.ColumnSpacing * (cols - 1);

			//var colTotalWidth = s.Width - totalSpacing - widthPadding;

			//var colSize = colTotalWidth / cols;

			//foreach (var coldef in ImageGrid.ColumnDefinitions)
			//{
			//	coldef.Width = new GridLength(colSize, GridUnitType.Absolute);
			//}

			//foreach (var rowdef in ImageGrid.RowDefinitions)
			//{
			//	rowdef.Height = new GridLength(colSize / 0.72, GridUnitType.Absolute);
			//}
#endif
		}

		protected override void Init()
		{
			
		}
	}
}
