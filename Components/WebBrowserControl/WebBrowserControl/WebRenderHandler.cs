// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using Xilium.CefGlue;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Engine.UISystem
{
	class WebRenderHandler : CefRenderHandler
	{
		private readonly WebBrowserControl owner;

		public WebRenderHandler( WebBrowserControl owner )
		{
			if( owner == null )
				throw new ArgumentNullException( "owner" );

			this.owner = owner;
		}

		protected override bool GetRootScreenRect( CefBrowser browser, ref CefRectangle rect )
		{
			return owner.GetViewRect( ref rect );
		}

		protected override bool GetViewRect( CefBrowser browser, ref CefRectangle rect )
		{
			return owner.GetViewRect( ref rect );
		}

		protected override bool GetScreenPoint( CefBrowser browser, int viewX, int viewY, ref int screenX, ref int screenY )
		{
			owner.GetScreenPoint( viewX, viewY, ref screenX, ref screenY );
			return true;
		}

		protected override bool GetScreenInfo( CefBrowser browser, CefScreenInfo screenInfo )
		{
			return false;
		}

		protected override void OnPopupShow( CefBrowser browser, bool show )
		{
			//owner.OnPopupShow(show);
		}

		protected override void OnPopupSize( CefBrowser browser, CefRectangle rect )
		{
			//owner.OnPopupSize(rect);
		}

		protected override void OnPaint( CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, IntPtr buffer, int width, int height )
		{
			//_logger.Debug("Type: {0} Buffer: {1:X8} Width: {2} Height: {3}", type, buffer, width, height);
			//foreach (var rect in dirtyRects)
			//{
			//    _logger.Debug("   DirtyRect: X={0} Y={1} W={2} H={3}", rect.X, rect.Y, rect.Width, rect.Height);
			//}

			if( type == CefPaintElementType.View )
			{
				owner.HandleViewPaint( browser, type, dirtyRects, buffer, width, height );
			}
			else if( type == CefPaintElementType.Popup )
			{
				owner.HandlePopupPaint( width, height, dirtyRects, buffer );
			}
		}

		protected override void OnCursorChange( CefBrowser browser, IntPtr cursorHandle )
		{
			//_uiHelper.PerformInUiThread(() =>
			//{
			//    Cursor cursor = CursorInteropHelper.Create(new SafeFileHandle(cursorHandle, false));
			//    _owner.Cursor = cursor;
			//});
		}

		protected override void OnScrollOffsetChanged( CefBrowser browser )
		{
		}
	}
}
