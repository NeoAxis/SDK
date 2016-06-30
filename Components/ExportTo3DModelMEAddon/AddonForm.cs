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

namespace ExportTo3DModelMEAddon
{
	public partial class AddonForm : EditorBase.Theme.EditorForm
	{
		[Config( "ExportTo3DModelMEAddonForm", "lastOutputMapName" )]
		static string lastOutputMapName = "C:\\MapExport.fbx";

		///////////////////////////////////////////

		public class CursorKeeper : IDisposable
		{
			Cursor originalCursor;
			bool isDisposed;

			public CursorKeeper( Cursor newCursor )
			{
				originalCursor = Cursor.Current;
				Cursor.Current = newCursor;
			}

			protected virtual void Dispose( bool disposing )
			{
				if( !isDisposed )
				{
					if( disposing )
					{
						Cursor.Current = originalCursor;
					}
				}
				isDisposed = true;
			}

			public void Dispose()
			{
				Dispose( true );
				GC.SuppressFinalize( this );
			}
		}

		///////////////////////////////////////////

		public AddonForm()
		{
			InitializeComponent();

			Font = MapEditorInterface.Instance.GetFont( MapEditorInterface.FontNames.Form, Font );

			EngineApp.Instance.Config.RegisterClassParameters( typeof( AddonForm ) );
		}

		private void ExportTo3DModelMEAddonForm_Load( object sender, EventArgs e )
		{
			Translate();

			List<Entity> selectedEntities = MapEditorInterface.Instance.GetSelectedEntities();
			checkBoxExportSelectedObjectsOnly.Checked = selectedEntities.Count != 0;
			checkBoxExportSelectedObjectsOnly.Enabled = selectedEntities.Count != 0;

			textBoxOutputFileName.Text = lastOutputMapName;
		}

		private void buttonSelectOutputFileName_Click( object sender, EventArgs e )
		{
			SaveFileDialog dialog = new SaveFileDialog();
			dialog.FileName = textBoxOutputFileName.Text;
			dialog.Filter = Translate( "3D Model formats (*.FBX;*.DAE)|*.FBX;*.DAE|All files (*.*)|*.*" );
			try
			{
				dialog.InitialDirectory = Path.GetDirectoryName( textBoxOutputFileName.Text );
			}
			catch { }
			dialog.RestoreDirectory = true;

			if( dialog.ShowDialog() == DialogResult.OK )
				textBoxOutputFileName.Text = dialog.FileName;
		}

		string GetUniqueName( Set<string> names, Entity entity )
		{
			string name = entity.Name;
			if( string.IsNullOrEmpty( name ) )
				name = entity.ToString();
			string uniqueName;
			for( int n = 1; ; n++ )
			{
				uniqueName = name;
				if( n != 1 )
					uniqueName += n.ToString();
				if( !names.Contains( uniqueName ) )
					break;
			}
			return uniqueName;
		}

		private void buttonExport_Click( object sender, EventArgs e )
		{
			lastOutputMapName = textBoxOutputFileName.Text;

			string fileName = textBoxOutputFileName.Text.Trim();

			bool rooted;
			try
			{
				rooted = Path.IsPathRooted( fileName );
			}
			catch
			{
				rooted = false;
			}
			if( !rooted )
			{
				Log.Warning( Translate( "Invalid file name." ) );
				return;
			}

			string caption = Translate( "Export To 3D Model Add-on" );

			if( File.Exists( fileName ) )
			{
				string template = Translate( "The file with the name \"{0}\" is already exists. Overwrite?" );
				string text = string.Format( template, fileName );
				if( MessageBox.Show( text, caption, MessageBoxButtons.OKCancel,
					MessageBoxIcon.Question ) != DialogResult.OK )
					return;
			}

			try
			{
				using( new CursorKeeper( Cursors.WaitCursor ) )
				{
					//get selected entities

					List<Entity> selectedEntities;
					if( checkBoxExportSelectedObjectsOnly.Checked )
						selectedEntities = MapEditorInterface.Instance.GetSelectedEntities();
					else
						selectedEntities = new List<Entity>();

					Set<Entity> selectedEntitiesSet = new Set<Entity>();
					foreach( Entity entity in selectedEntities )
						selectedEntitiesSet.AddWithCheckAlreadyContained( entity );

					string extension = Path.GetExtension( fileName );
					ModelImportLoader loader = MeshManager.Instance.GetModeImportLoaderByExtension( extension );
					if( loader == null )
					{
						Log.Warning( Translate( "File extension \"{0}\" is not supported." ), extension );
						return;
					}

					List<ModelImportLoader.SaveGeometryItem> geometry =
						new List<ModelImportLoader.SaveGeometryItem>();
					Set<string> names = new Set<string>();

					//SceneNodes
					foreach( SceneNode sceneNode in SceneManager.Instance.SceneNodes )
					{
						Entity entity = sceneNode._InternalUserData as Entity;
						if( entity != null )
						{
							if( selectedEntities.Count == 0 || selectedEntitiesSet.Contains( entity ) )
							{
								foreach( MovableObject movableObject in sceneNode.MovableObjects )
								{
									MeshObject meshObject = movableObject as MeshObject;
									if( meshObject != null )
									{
										foreach( SubMesh subMesh in meshObject.Mesh.SubMeshes )
										{
											string uniqueName = GetUniqueName( names, entity );

											VertexData vertexData = subMesh.UseSharedVertices ?
												subMesh.Parent.SharedVertexData : subMesh.VertexData;
											IndexData indexData = subMesh.IndexData;

											ModelImportLoader.SaveGeometryItem item =
												new ModelImportLoader.SaveGeometryItem(
												vertexData, indexData, sceneNode.Position, sceneNode.Rotation,
												sceneNode.Scale, uniqueName );
											geometry.Add( item );
											names.Add( uniqueName );
										}
									}
								}
							}
						}
					}

					foreach( Entity entity in Map.Instance.Children )
					{
						if( selectedEntities.Count == 0 || selectedEntitiesSet.Contains( entity ) )
						{
							//StaticMesh
							StaticMesh staticMesh = entity as StaticMesh;
							if( staticMesh != null )
							{
								Mesh mesh = MeshManager.Instance.Load( staticMesh.MeshName );
								if( mesh != null )
								{
									foreach( SubMesh subMesh in mesh.SubMeshes )
									{
										string uniqueName = GetUniqueName( names, entity );

										VertexData vertexData = subMesh.UseSharedVertices ?
											subMesh.Parent.SharedVertexData : subMesh.VertexData;
										IndexData indexData = subMesh.IndexData;

										ModelImportLoader.SaveGeometryItem item =
											new ModelImportLoader.SaveGeometryItem( vertexData, indexData,
											staticMesh.Position, staticMesh.Rotation, staticMesh.Scale, uniqueName );
										geometry.Add( item );
										names.Add( uniqueName );
									}
								}
							}

							//HeightmapTerrain
							if( entity.Type.Name == "HeightmapTerrain" )
							{
								try
								{
									MethodInfo method = entity.GetType().GetMethod( "GetBodies" );
									Body[] bodies = (Body[])method.Invoke( entity, new object[ 0 ] );
									foreach( Body body in bodies )
									{
										foreach( Shape shape in body.Shapes )
										{
											//MeshShape
											MeshShape meshShape = shape as MeshShape;
											if( meshShape != null )
											{
												Vec3[] vertices;
												int[] indices;
												if( meshShape.GetData( out vertices, out indices ) )
												{
													ModelImportLoader.SaveGeometryItem.Vertex[] vertices2 =
														new ModelImportLoader.SaveGeometryItem.Vertex[ vertices.Length ];
													for( int n = 0; n < vertices.Length; n++ )
														vertices2[ n ] = new ModelImportLoader.SaveGeometryItem.Vertex( vertices[ n ] );

													string uniqueName = GetUniqueName( names, entity );

													ModelImportLoader.SaveGeometryItem item =
														new ModelImportLoader.SaveGeometryItem( vertices2, indices, false,
														body.Position, body.Rotation, new Vec3( 1, 1, 1 ), uniqueName );
													if( item != null )
													{
														geometry.Add( item );
														names.Add( uniqueName );
													}
												}
											}

											//HeightFieldShape
											HeightFieldShape heightFieldShape = shape as HeightFieldShape;
											if( heightFieldShape != null )
											{
												Vec3[] vertices;
												int[] indices;
												heightFieldShape.GetVerticesAndIndices( false, false, out vertices,
													out indices );

												ModelImportLoader.SaveGeometryItem.Vertex[] vertices2 =
													new ModelImportLoader.SaveGeometryItem.Vertex[ vertices.Length ];
												for( int n = 0; n < vertices.Length; n++ )
													vertices2[ n ] = new ModelImportLoader.SaveGeometryItem.Vertex( vertices[ n ] );

												string uniqueName = GetUniqueName( names, entity );

												ModelImportLoader.SaveGeometryItem item =
													new ModelImportLoader.SaveGeometryItem( vertices2, indices, false,
													body.Position, body.Rotation, new Vec3( 1, 1, 1 ),
													uniqueName );
												if( item != null )
												{
													geometry.Add( item );
													names.Add( uniqueName );
												}
											}

										}
									}
								}
								catch { }
							}

						}
					}

					////StaticMeshObjects
					//foreach( StaticMeshObject staticMeshObject in SceneManager.Instance.StaticMeshObjects )
					//{
					//   Entity entity = staticMeshObject._InternalUserData as Entity;
					//   if( entity != null )
					//   {
					//      if( selectedEntities.Count == 0 || selectedEntitiesSet.Contains( entity ) )
					//      {
					//         string name = entity.Name;
					//         if( string.IsNullOrEmpty( name ) )
					//            name = entity.ToString();
					//         string uniqueName;
					//         for( int n = 1; ; n++ )
					//         {
					//            uniqueName = name;
					//            if( n != 1 )
					//               uniqueName += n.ToString();
					//            if( !names.Contains( uniqueName ) )
					//               break;
					//         }

					//         ModelImportLoader.SaveGeometryItem item = new ModelImportLoader.SaveGeometryItem(
					//            staticMeshObject.VertexData, staticMeshObject.IndexData, staticMeshObject.Position,
					//            staticMeshObject.Rotation, staticMeshObject.Scale, uniqueName );
					//         geometry.Add( item );
					//         names.Add( uniqueName );
					//      }
					//   }
					//}

					if( geometry.Count == 0 )
					{
						Log.Warning( Translate( "No data to export." ) );
						return;
					}

					if( !loader.Save( geometry, fileName ) )
						return;
				}
			}
			catch( Exception ex )
			{
				Log.Warning( Translate( "Error." ) + "\n\n" + ex.Message );
				return;
			}

			MessageBox.Show( Translate( "The geometry successfully exported!" ), caption );
		}

		string Translate( string text )
		{
			return ToolsLocalization.Translate( "ExportTo3DModelMEAddonForm", text );
		}

		void Translate()
		{
			Text = Translate( Text );

			foreach( Control control in Controls )
			{
				if( control is Label || control is Button || control is CheckBox )
					control.Text = Translate( control.Text );
			}
		}

	}
}
