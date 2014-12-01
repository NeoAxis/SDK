// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using Engine;
using Engine.Utils;
using Engine.MapSystem;
using EditorBase;

namespace ExportTo3DModelMEAddon
{
	public class MapEditorAddonImpl : MapEditorAddon
	{
		public override bool OnInit( out string mainMenuItemText, out Image mainMenuItemIcon )
		{
			mainMenuItemText = ToolsLocalization.Translate( "Addons", "Exporting To 3D Model" );//(FBX, COLLADA...
			mainMenuItemIcon = Properties.Resources.Addon_16;
			return true;
		}

		public override void OnMainMenuItemClick()
		{
			base.OnMainMenuItemClick();

			if( Map.Instance != null )
			{
				AddonForm form = new AddonForm();
				form.ShowDialog( Application.OpenForms[ 0 ] );
			}
		}
	}
}
