// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using WeifenLuo.WinFormsUI.Docking;
using Engine;
using Engine.Utils;

namespace WinFormsMultiViewAppExample
{
	public partial class PropertiesForm : DockContent
	{
		public PropertiesForm()
		{
			InitializeComponent();
		}

		public void OnClose()
		{
		}

		public object[] GetSelectedObjects()
		{
			return propertyGrid1.SelectedObjects;
		}

		public void SelectObjects( object[] objects )
		{
			if( objects != null && objects.Length != 0 )
				propertyGrid1.SelectedObjects = objects;
			else
				propertyGrid1.SelectedObject = null;
		}

		public PropertyGrid GetPropertyGrid()
		{
			return propertyGrid1;
		}
	}
}
