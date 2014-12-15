// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.Renderer;

namespace Engine.UISystem
{
	public static class PixelBoxExtensions
	{
		public static void WriteDataUnmanaged( this PixelBox pixelBox, IntPtr buffer, int width, int height )
		{
			unsafe
			{
				byte* pointer = (byte*)buffer;
				for( int h = 0; h < height; ++h )
				{
					int offset = h * pixelBox.RowPitch * 4;
					pixelBox.WriteDataUnmanaged( offset, (IntPtr)pointer, width * 4 );
					pointer += width * 4;
				}
			}
		}
	}
}
