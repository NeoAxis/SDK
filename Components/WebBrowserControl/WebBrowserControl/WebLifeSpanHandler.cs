// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using Xilium.CefGlue;
using System;

namespace Engine.UISystem
{
	class WebLifeSpanHandler : CefLifeSpanHandler
	{
		private readonly WebBrowserControl owner;

		public WebLifeSpanHandler( WebBrowserControl owner )
		{
			if( owner == null )
				throw new ArgumentNullException( "owner" );

			this.owner = owner;
		}

		protected override void OnAfterCreated( CefBrowser browser )
		{
			base.OnAfterCreated( browser );

			this.owner.HandleAfterCreated( browser );
		}

		protected override bool DoClose( CefBrowser browser )
		{
			// TODO: ... dispose owner
			return false;
		}

		protected override bool OnBeforePopup( CefBrowser browser, CefFrame frame, string targetUrl, string targetFrameName, CefPopupFeatures popupFeatures, CefWindowInfo windowInfo, ref CefClient client, CefBrowserSettings settings, ref bool noJavascriptAccess )
		{
			var e = new BeforePopupEventArgs( frame, targetUrl, targetFrameName, popupFeatures, windowInfo, client, settings,
					 noJavascriptAccess );

			this.owner.OnBeforePopup( e );

			client = e.Client;
			noJavascriptAccess = e.NoJavascriptAccess;

			return e.Handled;
		}
	}
}
