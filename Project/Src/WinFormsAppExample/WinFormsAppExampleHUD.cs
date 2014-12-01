// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.UISystem;
using Engine.MathEx;

namespace WinFormsAppExample
{
	public class WinFormsAppExampleHUD : Control
	{
		public WinFormsAppExampleHUD()
		{
		}

		protected override void OnAttach()
		{
			base.OnAttach();

			Control window = ControlDeclarationManager.Instance.CreateControl(
				"Gui\\WindowsAppExampleHUD.gui" );
			Controls.Add( window );

			( (Button)window.Controls[ "Close" ] ).Click += CloseButton_Click;
		}

		void CloseButton_Click( Button sender )
		{
			SetShouldDetach();
		}
	}
}
