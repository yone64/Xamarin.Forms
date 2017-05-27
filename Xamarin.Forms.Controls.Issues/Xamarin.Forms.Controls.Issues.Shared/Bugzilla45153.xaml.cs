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
#endif
		}

		protected override void Init()
		{
			
		}
	}
}
