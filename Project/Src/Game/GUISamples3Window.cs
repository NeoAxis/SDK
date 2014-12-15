// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.Renderer;
using Engine.UISystem;
using Engine.MathEx;
using Engine.Utils;

namespace Game
{
	public class GUISamples3Window : Control
	{
		Control window;
		EditBox addressEditBox;
		Engine.UISystem.WebBrowserControl webBrowserControl;

		///////////////////////////////////////////

		protected override void OnAttach()
		{
			base.OnAttach();

			//create window
			window = ControlDeclarationManager.Instance.CreateControl( "Gui\\GUISamples3Window.gui" );
			Controls.Add( window );

			addressEditBox = (EditBox)window.Controls[ "Address" ];
			addressEditBox.PreKeyDown += addressEditBox_PreKeyDown;

			webBrowserControl = (Engine.UISystem.WebBrowserControl)window.Controls[ "WebBrowser" ];

			( (Button)window.Controls[ "Close" ] ).Click += Close_Click;
			( (Button)window.Controls[ "Go" ] ).Click += go_Click;

			BackColor = new ColorValue( 0, 0, 0, .5f );
			MouseCover = true;
		}

		protected override void OnDetach()
		{
			base.OnDetach();
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( base.OnKeyDown( e ) )
				return true;

			if( e.Key == EKeys.Escape )
			{
				Close();
				return true;
			}

			return false;
		}

		void Close()
		{
			SetShouldDetach();
		}

		void addressEditBox_PreKeyDown( KeyEvent e, ref bool handled )
		{
			if( e.Key == EKeys.Return )
			{
				webBrowserControl.LoadURL( addressEditBox.Text );
				webBrowserControl.Focus();
				handled = true;
			}
		}

		void Close_Click( Button sender )
		{
			Close();
		}

		void go_Click( Button sender )
		{
			webBrowserControl.LoadURL( addressEditBox.Text );
		}
	}
}
