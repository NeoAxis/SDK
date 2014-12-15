// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using Xilium.CefGlue;
using System;

namespace Engine.UISystem
{
	internal sealed class WebLoadHandler : CefLoadHandler
	{
		private readonly WebBrowserControl owner;

		public WebLoadHandler( WebBrowserControl owner )
		{
			if( owner == null )
				throw new ArgumentNullException( "owner" );

			this.owner = owner;
		}

		protected override void OnLoadingStateChange( CefBrowser browser, bool isLoading, bool canGoBack, bool canGoForward )
		{
			owner.OnLoadingStateChange( isLoading, canGoBack, canGoForward );
		}

		protected override void OnLoadError( CefBrowser browser, CefFrame frame, CefErrorCode errorCode, string errorText, string failedUrl )
		{
			owner.OnLoadError( frame, errorCode, errorText, failedUrl );
		}

		protected override void OnLoadStart( CefBrowser browser, CefFrame frame )
		{
			owner.OnLoadStart( frame );
		}

		protected override void OnLoadEnd( CefBrowser browser, CefFrame frame, int httpStatusCode )
		{
			owner.OnLoadEnd( frame, httpStatusCode );
		}
	}
}
