// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Reflection;
using Engine;
using Engine.Utils;
using Engine.Utils.Editor;
using Engine.Renderer;
using Engine.Renderer.ModelImporting;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.MathEx;

namespace ExampleAddonCreationREAddon
{
	public partial class AddonForm : Form
	{
		public AddonForm()
		{
			InitializeComponent();

			Font = ResourceEditorInterface.Instance.GetFont( ResourceEditorInterface.FontNames.Form, Font );
		}

		private void AddonForm_Load( object sender, EventArgs e )
		{
			checkBoxAddItemToMainMenu.Checked = ResourceEditorAddonImpl.addItemToMainMenu;
			checkBoxAddButtonToToolbar.Checked = ResourceEditorAddonImpl.addButtonToToolbar;
			checkBoxAddItemToContextMenuOfResourcesTree.Checked = ResourceEditorAddonImpl.addItemToContextMenuOfResourcesTree;
			checkBoxDrawTextOnScreen.Checked = ResourceEditorAddonImpl.drawTextOnScreen;
			checkBoxOverrideCameraSettings.Checked = ResourceEditorAddonImpl.overrideCameraSettings;
			checkBoxAddPageToOptions.Checked = ResourceEditorAddonImpl.addPageToOptions;
		}

		private void AddonForm_FormClosed( object sender, FormClosedEventArgs e )
		{
			if( DialogResult == DialogResult.OK )
			{
				ResourceEditorAddonImpl.addItemToMainMenu = checkBoxAddItemToMainMenu.Checked;
				ResourceEditorAddonImpl.addButtonToToolbar = checkBoxAddButtonToToolbar.Checked;
				ResourceEditorAddonImpl.addItemToContextMenuOfResourcesTree = checkBoxAddItemToContextMenuOfResourcesTree.Checked;
				ResourceEditorAddonImpl.drawTextOnScreen = checkBoxDrawTextOnScreen.Checked;
				ResourceEditorAddonImpl.overrideCameraSettings = checkBoxOverrideCameraSettings.Checked;
				ResourceEditorAddonImpl.addPageToOptions = checkBoxAddPageToOptions.Checked;
			}
		}
	}
}
