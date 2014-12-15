// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using Xilium.CefGlue;
using System;

namespace Engine.UISystem
{
	class WebDisplayHandler : CefDisplayHandler
	{
		private readonly WebBrowserControl owner;

		public WebDisplayHandler( WebBrowserControl owner )
		{
			if( owner == null )
				throw new ArgumentNullException( "owner" );

			this.owner = owner;
		}

		protected override void OnTitleChange( CefBrowser browser, string title )
		{
			owner.OnTitleChanged( title );
		}

		protected override void OnAddressChange( CefBrowser browser, CefFrame frame, string url )
		{
			if( frame.IsMain )
			{
				owner.OnAddressChanged( url );
			}
		}

		protected override void OnStatusMessage( CefBrowser browser, string value )
		{
			owner.OnTargetUrlChanged( value );
		}

		protected override bool OnTooltip( CefBrowser browser, string text )
		{
			return owner.OnTooltip( text );
		}
	}
}
