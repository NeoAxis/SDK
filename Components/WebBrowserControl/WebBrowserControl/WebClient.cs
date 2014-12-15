// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xilium.CefGlue;

namespace Engine.UISystem
{
	public class EmptyCefContextMenuHandlerImpl : CefContextMenuHandler
	{
		protected override void OnBeforeContextMenu( CefBrowser browser, CefFrame frame, CefContextMenuParams state, CefMenuModel model )
		{
			model.Clear();
		}
	}

	class WebClient : CefClient
	{
		private readonly WebBrowserControl owner;
		private readonly WebLifeSpanHandler lifeSpanHandler;
		private readonly WebDisplayHandler displayHandler;
		private readonly WebRenderHandler renderHandler;
		private readonly WebLoadHandler loadHandler;

		public WebClient( WebBrowserControl owner )
		{
			if( owner == null )
				throw new ArgumentNullException( "owner" );

			this.owner = owner;
			this.lifeSpanHandler = new WebLifeSpanHandler( this.owner );
			this.displayHandler = new WebDisplayHandler( this.owner );
			this.renderHandler = new WebRenderHandler( this.owner );
			this.loadHandler = new WebLoadHandler( this.owner );
		}

		protected override CefLifeSpanHandler GetLifeSpanHandler()
		{
			return this.lifeSpanHandler;
		}

		protected override CefDisplayHandler GetDisplayHandler()
		{
			return this.displayHandler;
		}

		protected override CefRenderHandler GetRenderHandler()
		{
			return this.renderHandler;
		}

		protected override CefLoadHandler GetLoadHandler()
		{
			return this.loadHandler;
		}

		protected override CefContextMenuHandler GetContextMenuHandler()
		{
			return new EmptyCefContextMenuHandlerImpl();
		}
	}
}
