// Copyright (C) 2006-2011 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Drawing;
using System.Runtime.InteropServices;
using Engine.EntitySystem;
using Engine.Utils;
using Engine.MapSystem;
//!!!!!!
//using MapEditor;

namespace Engine.Editor
{
	/*
	[TypeConverter(typeof(CollectionTypeConverter))]
	[Editor("Engine.Editor.RecastGeometriesCollectionEditor, RecastNavigationSystem.Editor", typeof(UITypeEditor))]
	public List<Entity> Geometries
	{
		 get { return geometries; }
	}

	public class RecastGeometriesCollectionEditor : CollectionEditor
	{
		 public RecastGeometriesCollectionEditor() : base(typeof(List<Entity>))
		 {
		 }

		 protected override CollectionEditor.CollectionForm CreateCollectionForm()
		 {
			  CollectionEditor.CollectionForm form = base.CreateCollectionForm();
			  form.FormClosed += new FormClosedEventHandler(form_FormClosed);
			  return form;
		 }

		 protected override object CreateInstance(Type itemType)
		 {
			  Entity entity = null;

			  ChooseEntityForm dialog = new ChooseEntityForm(Map.Instance, null, false, entity);
			  if (dialog.ShowDialog() == DialogResult.OK)
					entity = RecastNavigationSystem.Instance.GeometryVerifier(dialog.Entity, true);

			  //return null throws an illegal array error if the list is empty and we selected bad entity, nothing really bad thou
			  return entity;
		 }

		 void form_FormClosed(object sender, FormClosedEventArgs e)
		 {
			  if ((sender as Form).DialogResult == DialogResult.OK)
			  {
					UndoSystem.Instance.Clear();
					MapEditorInterface.Instance.SetMapModified();
			  }
		 }
	}
    
	[Category("Geometries")]
	[TypeConverter(typeof(CollectionTypeConverter))]
	public ICollection<Entity> Geometries
	{
		 get 
		 {
			  List<Entity> list = new List<Entity>();
			  foreach (Entity entity in geometries)
					list.Add(entity);
			  return list;
		 }
	}
	*/
}
