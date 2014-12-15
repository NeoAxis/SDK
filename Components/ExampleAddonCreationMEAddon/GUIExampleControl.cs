// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.UISystem;
using Engine.MathEx;

namespace ExampleAddonCreationMEAddon
{
	public class GUIExampleControl : Control
	{
		public GUIExampleControl()
		{
		}

		protected override void OnAttach()
		{
			base.OnAttach();

			Control window = ControlDeclarationManager.Instance.CreateControl( "Gui\\AboutWindow.gui" );
			Controls.Add( window );

			( (Button)window.Controls[ "Quit" ] ).Click += CloseButton_Click;
		}

		void CloseButton_Click( Button sender )
		{
			SetShouldDetach();
		}
	}
}
