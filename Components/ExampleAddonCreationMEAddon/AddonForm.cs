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
using Engine.Renderer;
using Engine.Renderer.ModelImporting;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.MathEx;

namespace ExampleAddonCreationMEAddon
{
	public partial class AddonForm : EditorBase.Theme.EditorForm
	{
		MapEditorAddonImpl addon;

		public AddonForm( MapEditorAddonImpl addon )
		{
			this.addon = addon;

			InitializeComponent();

			Font = MapEditorInterface.Instance.GetFont( MapEditorInterface.FontNames.Form, Font );
		}

		private void AddonForm_Load( object sender, EventArgs e )
		{
			checkBoxAddDockingWindow.Checked = addon.dockingForm != null && addon.dockingForm.Visible;

			checkBoxAddItemToMainMenu.Checked = MapEditorAddonImpl.addItemToMainMenu;
			checkBoxAddButtonToToolbar.Checked = MapEditorAddonImpl.addButtonToToolbar;
			checkBoxAddItemToContextMenuOfWorkingArea.Checked = MapEditorAddonImpl.addItemToContextMenuOfWorkingArea;
			checkBoxAddItemToContextMenuOfLayer.Checked = MapEditorAddonImpl.addItemToContextMenuOfLayer;
			checkBoxDrawTextOnScreen.Checked = MapEditorAddonImpl.drawTextOnScreen;
			checkBoxOverrideCameraSettings.Checked = MapEditorAddonImpl.overrideCameraSettings;
			checkBoxAddPageToOptions.Checked = MapEditorAddonImpl.addPageToOptions;
		}

		private void AddonForm_FormClosed( object sender, FormClosedEventArgs e )
		{
			if( DialogResult == DialogResult.OK )
			{
				MapEditorAddonImpl.addItemToMainMenu = checkBoxAddItemToMainMenu.Checked;
				MapEditorAddonImpl.addButtonToToolbar = checkBoxAddButtonToToolbar.Checked;
				MapEditorAddonImpl.addItemToContextMenuOfWorkingArea = checkBoxAddItemToContextMenuOfWorkingArea.Checked;
				MapEditorAddonImpl.addItemToContextMenuOfLayer = checkBoxAddItemToContextMenuOfLayer.Checked;
				MapEditorAddonImpl.drawTextOnScreen = checkBoxDrawTextOnScreen.Checked;
				MapEditorAddonImpl.overrideCameraSettings = checkBoxOverrideCameraSettings.Checked;
				MapEditorAddonImpl.addPageToOptions = checkBoxAddPageToOptions.Checked;
			}
		}

		public bool AddDockingWindow
		{
			get { return checkBoxAddDockingWindow.Checked; }
		}
	}
}
