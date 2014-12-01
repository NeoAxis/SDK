// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.UISystem;
using Engine.MathEx;

namespace Game
{
	/// <summary>
	/// Defines a "MessageBox" window.
	/// </summary>
	public class MessageBoxWindow : Control
	{
		string messageText;
		string caption;
		Button.ClickDelegate clickHandler;

		//

		public MessageBoxWindow( string messageText, string caption, Button.ClickDelegate clickHandler )
		{
			this.messageText = messageText;
			this.caption = caption;
			this.clickHandler = clickHandler;
		}

		protected override void OnAttach()
		{
			base.OnAttach();

			TopMost = true;

			Control window = ControlDeclarationManager.Instance.CreateControl(
				"Gui\\MessageBoxWindow.gui" );
			Controls.Add( window );

			window.Controls[ "MessageText" ].Text = messageText;

			window.Text = caption;

			( (Button)window.Controls[ "OK" ] ).Click += OKButton_Click;

			BackColor = new ColorValue( 0, 0, 0, .5f );

			EngineApp.Instance.RenderScene();
		}

		void OKButton_Click( Button sender )
		{
			if( clickHandler != null )
				clickHandler( sender );

			SetShouldDetach();
		}
	}
}
