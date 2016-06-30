// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Engine.MapSystem.Editor;
using Engine.EntitySystem;
using Engine.FileSystem;
using Engine.MapSystem;
using Engine.Utils;
using System.IO;

namespace Engine.Editor
{
	public class RecastNavigationSystemExtendedFunctionalityDescriptor : MapGeneralObjectExtendedFunctionalityDescriptor
	{
		RecastNavigationSystem recastSystem;
		Timer timer;
		Button buttonClear;
		Button buttonTest;

		//

		public RecastNavigationSystemExtendedFunctionalityDescriptor( Control parentControl, object owner )
			: base( parentControl, owner )
		{
			recastSystem = (RecastNavigationSystem)owner;

			Button button;

			int posY = 8;

			//Geometries toolbox button
			button = new EditorBase.Theme.EditorButton();
			parentControl.Controls.Add( button );
			button.Location = new System.Drawing.Point( 8, posY );
			button.Size = new System.Drawing.Size( 140, 32 );
			button.Text = Translate( "Collision" );
			button.UseVisualStyleBackColor = true;
			button.Click += new EventHandler( geometriesButton_Click );

			//Build toolbox button
			button = new EditorBase.Theme.EditorButton();
			parentControl.Controls.Add( button );
			button.Location = new System.Drawing.Point( 140 + 8 * 2, posY );
			button.Size = new System.Drawing.Size( 140, 32 );
			button.Text = Translate( "Rebuild" );
			button.UseVisualStyleBackColor = true;
			button.Click += new EventHandler( buildButton_Click );

			posY += 36;

			//Destroy toolbox button
			button = new EditorBase.Theme.EditorButton();
			buttonClear = button;
			parentControl.Controls.Add( button );
			button.Location = new System.Drawing.Point( 8, posY );
			button.Size = new System.Drawing.Size( 140, 32 );
			button.Text = Translate( "Clear" );
			button.UseVisualStyleBackColor = true;
			button.Click += new EventHandler( clearButton_Click );

			//Test NavMesh toolbox button
			button = new EditorBase.Theme.EditorButton();
			buttonTest = button;
			parentControl.Controls.Add( button );
			button.Location = new System.Drawing.Point( 140 + 8 * 2, posY );
			button.Size = new System.Drawing.Size( 140, 32 );
			button.Text = Translate( "Test" );
			button.UseVisualStyleBackColor = true;
			button.Click += new EventHandler( testButton_Click );

			timer = new Timer();
			timer.Interval = 50;
			timer.Start();
			timer.Tick += new EventHandler( timer_Tick );

			UpdateControls();
		}

		public override void Dispose()
		{
			timer.Dispose();
			base.Dispose();
		}

		void UpdateControls()
		{
			buttonClear.Enabled = recastSystem.IsInitialized;
			buttonTest.Enabled = recastSystem.IsInitialized;
		}

		void timer_Tick( object sender, EventArgs e )
		{
			if( buttonClear != null && !recastSystem.IsSetForDeletion )
				UpdateControls();
		}

		void buildButton_Click( object sender, EventArgs e )
		{
			DialogResult result = MessageBox.Show( Translate( "Build navigation mesh?" ), Translate( "Recast Navigation System" ),
				MessageBoxButtons.YesNo, MessageBoxIcon.Question );
			if( result != DialogResult.Yes )
				return;

			if( recastSystem.Geometries.Count == 0 )
			{
				MessageBox.Show( Translate( "Collision objects are not configured." ), Translate( "Recast Navigation System" ),
					MessageBoxButtons.OK, MessageBoxIcon.Warning );
				return;
			}

			//!!!!!!спросить сообщением, если слишком много ячеек.

			recastSystem.DestroyRecastWorld();

			//time begin
			float timeStarted = EngineApp.Instance.Time;
			Log.Info( Translate( "Navigation mesh rebuild calculation started..." ) );
			EngineApp.Instance.RenderScene();

			recastSystem.InitRecastWorld();

			string error;
			bool success = recastSystem.BuildAllTiles( out error );

			if( success )
			{
				float time = EngineApp.Instance.Time - timeStarted;
				Log.Info( Translate( "Build succeeded! Time: {0} seconds." ), time.ToString( "F2" ) );
			}
			else
			{
				Log.Info( Translate( "Error! {0}" ), error );
			}

			MapEditorInterface.Instance.SetMapModified();
			UndoSystem.Instance.Clear();
		}

		void clearButton_Click( object sender, EventArgs e )
		{
			DialogResult result = MessageBox.Show( Translate( "Clear navigation mesh?" ), Translate( "Recast Navigation System" ),
				MessageBoxButtons.YesNo, MessageBoxIcon.Question );
			if( result != DialogResult.Yes )
				return;

			recastSystem.DestroyRecastWorld();

			MapEditorInterface.Instance.SetMapModified();
			UndoSystem.Instance.Clear();
		}

		void geometriesButton_Click( object sender, EventArgs e )
		{
			MapEditorInterface.Instance.SendCustomMessage( (RecastNavigationSystem)Owner, "Geometries", null );
		}

		void testButton_Click( object sender, EventArgs e )
		{
			if( recastSystem.IsInitialized )
				MapEditorInterface.Instance.SendCustomMessage( (RecastNavigationSystem)Owner, "Test", null );
		}

		string Translate( string text )
		{
			return ToolsLocalization.Translate( "RecastNavigationSystemExtendedFunctionalityDescriptor", text );
		}
	}
}
