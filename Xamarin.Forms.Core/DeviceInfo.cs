﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Xamarin.Forms.Internals
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public abstract class DeviceInfo : INotifyPropertyChanged, IDisposable
	{
		FlowDirection _currentFlowDirection;
		DeviceOrientation _currentOrientation;
		bool _disposed;

		public DeviceOrientation CurrentOrientation
		{
			get { return _currentOrientation; }
			set
			{
				if (Equals(_currentOrientation, value))
					return;
				_currentOrientation = value;
				OnPropertyChanged();
			}
		}

		public FlowDirection CurrentFlowDirection
		{
			get { return _currentFlowDirection; }
			set
			{
				if (Equals(_currentFlowDirection, value))
					return;
				_currentFlowDirection = value;
				OnPropertyChanged();
			}
		}

		public abstract Size PixelScreenSize { get; }

		public abstract Size ScaledScreenSize { get; }

		public abstract double ScalingFactor { get; }

		public void Dispose()
		{
			Dispose(true);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed)
				return;
			_disposed = true;
		}

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}