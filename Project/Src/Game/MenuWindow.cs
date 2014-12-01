// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.UISystem;
using Engine.MapSystem;
using Engine.EntitySystem;
using Engine.MathEx;
using ProjectCommon;
using ProjectEntities;

namespace Game
{
	/// <summary>
	/// Defines a system game menu.
	/// </summary>
	public class MenuWindow : Control
	{
		protected override void OnAttach()
		{
			base.OnAttach();

			Control window = ControlDeclarationManager.Instance.CreateControl( "Gui\\MenuWindow.gui" );
			Controls.Add( window );

			( (Button)window.Controls[ "Maps" ] ).Click += mapsButton_Click;
			( (Button)window.Controls[ "LoadSave" ] ).Click += loadSaveButton_Click;
			( (Button)window.Controls[ "Options" ] ).Click += optionsButton_Click;
			( (Button)window.Controls[ "ProfilingTool" ] ).Click += ProfilingToolButton_Click;
			( (Button)window.Controls[ "About" ] ).Click += aboutButton_Click;
			( (Button)window.Controls[ "ExitToMainMenu" ] ).Click += exitToMainMenuButton_Click;
			( (Button)window.Controls[ "Exit" ] ).Click += exitButton_Click;
			( (Button)window.Controls[ "Resume" ] ).Click += resumeButton_Click;

			if( GameWindow.Instance == null )
				window.Controls[ "ExitToMainMenu" ].Enable = false;

			if( GameNetworkClient.Instance != null )
				window.Controls[ "Maps" ].Enable = false;

			if( GameNetworkServer.Instance != null || GameNetworkClient.Instance != null )
				window.Controls[ "LoadSave" ].Enable = false;

			MouseCover = true;

			BackColor = new ColorValue( 0, 0, 0, .5f );
		}

		void mapsButton_Click( object sender )
		{
			foreach( Control control in Controls )
				control.Visible = false;
			Controls.Add( new MapsWindow() );
		}

		void loadSaveButton_Click( object sender )
		{
			foreach( Control control in Controls )
				control.Visible = false;
			Controls.Add( new WorldLoadSaveWindow() );
		}

		void optionsButton_Click( object sender )
		{
			foreach( Control control in Controls )
				control.Visible = false;
			Controls.Add( new OptionsWindow() );
		}

		void ProfilingToolButton_Click( object sender )
		{
			SetShouldDetach();
			GameEngineApp.ShowProfilingTool( true );
		}

		void aboutButton_Click( object sender )
		{
			foreach( Control control in Controls )
				control.Visible = false;
			Controls.Add( new AboutWindow() );
		}

		protected override void OnControlDetach( Control control )
		{
			base.OnControlDetach( control );

			if( ( control as OptionsWindow ) != null ||
				( control as MapsWindow ) != null ||
				( control as WorldLoadSaveWindow ) != null ||
				( control as AboutWindow ) != null )
			{
				foreach( Control c in Controls )
					c.Visible = true;
			}
		}

		void exitToMainMenuButton_Click( object sender )
		{
			MapSystemWorld.MapDestroy();
			EntitySystemWorld.Instance.WorldDestroy();

			GameEngineApp.Instance.Server_DestroyServer( "The server has been destroyed" );
			GameEngineApp.Instance.Client_DisconnectFromServer();

			//close all windows
			foreach( Control control in GameEngineApp.Instance.ControlManager.Controls )
				control.SetShouldDetach();
			//create main menu
			GameEngineApp.Instance.ControlManager.Controls.Add( new MainMenuWindow() );
		}

		void exitButton_Click( object sender )
		{
			GameEngineApp.Instance.SetFadeOutScreenAndExit();
		}

		void resumeButton_Click( object sender )
		{
			SetShouldDetach();
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( base.OnKeyDown( e ) )
				return true;
			if( e.Key == EKeys.Escape )
			{
				SetShouldDetach();
				return true;
			}
			return false;
		}

	}
}
