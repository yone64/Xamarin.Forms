namespace Xamarin.Forms.PlatformConfiguration.AndroidSpecific
{
	using FormsElement = VisualElement;

	public static class Elevation
	{
		public static readonly BindableProperty ElevationProperty =
			BindableProperty.Create("Elevation", typeof(float?),
				typeof(Elevation));

		public static float? GetElevation<T>(T element) where T : FormsElement
		{
			return (float?)element.GetValue(ElevationProperty);
		}

		public static void SetElevation<T>(T element, float? value) where T : FormsElement
		{
			element.SetValue(ElevationProperty, value);
		}

		public static float? GetElevation(this IPlatformElementConfiguration<Android, FormsElement> config)
		{
			return GetElevation(config.Element);
		}

		public static IPlatformElementConfiguration<Android, FormsElement> SetElevation(this IPlatformElementConfiguration<Android, FormsElement> config, float? value) 
		{
			SetElevation(config.Element, value);
			return config;
		}
	}
}