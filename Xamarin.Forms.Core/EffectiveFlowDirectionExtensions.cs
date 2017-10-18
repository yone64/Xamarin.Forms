using System;
using System.Linq;

namespace Xamarin.Forms
{
	static class EffectiveFlowDirectionExtensions
	{
		public static EffectiveFlowDirection ToEffectiveFlowDirection(this FlowDirection self, EffectiveFlowDirection mode)
		{
			switch (self)
			{
				case FlowDirection.MatchParent:
					return EffectiveFlowDirection.LeftToRight | EffectiveFlowDirection.Implicit;
				case FlowDirection.LeftToRight:
					return EffectiveFlowDirection.LeftToRight | mode;
				case FlowDirection.RightToLeft:
					return EffectiveFlowDirection.RightToLeft | mode;
			}

			throw new InvalidOperationException($"Cannot convert {self} to {nameof(EffectiveFlowDirection)}.");
		}

		public static FlowDirection ToFlowDirection(this EffectiveFlowDirection self)
		{
			if (self.HasFlag(EffectiveFlowDirection.LeftToRight) && !self.HasFlag(EffectiveFlowDirection.RightToLeft))
				return FlowDirection.LeftToRight;
			else if (self.HasFlag(EffectiveFlowDirection.RightToLeft) && !self.HasFlag(EffectiveFlowDirection.LeftToRight))
				return FlowDirection.RightToLeft;

			throw new InvalidOperationException($"Cannot convert {self} to {nameof(FlowDirection)}.");
		}
	}
}