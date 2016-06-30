// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Reflection;
using Engine;
using Engine.FileSystem;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.SoundSystem;
using Engine.UISystem;
using Engine.Networking;
using WPFAppFramework;
using ProjectCommon;

namespace WPFSimpleExample
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void Window_Loaded( object sender, RoutedEventArgs e )
		{
			//NeoAxis initialization
			if( !WPFAppWorld.Init( new WPFAppEngineApp( EngineApp.ApplicationTypes.Simulation ), this,
				"user:Logs/WPFSimpleExample.log", true, null, null, null, null ) )
			{
				Close();
				return;
			}

			renderTargetUserControl1.AutomaticUpdateFPS = 60;
			renderTargetUserControl1.KeyDown += renderTargetUserControl1_KeyDown;
			renderTargetUserControl1.KeyUp += renderTargetUserControl1_KeyUp;
			renderTargetUserControl1.MouseDown += renderTargetUserControl1_MouseDown;
			renderTargetUserControl1.MouseUp += renderTargetUserControl1_MouseUp;
			renderTargetUserControl1.MouseMove += renderTargetUserControl1_MouseMove;
			renderTargetUserControl1.Tick += renderTargetUserControl1_Tick;
			renderTargetUserControl1.Render += renderTargetUserControl1_Render;
			renderTargetUserControl1.RenderUI += renderTargetUserControl1_RenderUI;

			const string startMapName = "Maps\\Demos\\Village Demo\\Map\\Map.map";

			//generate map list
			{
				string[] mapList = VirtualDirectory.GetFiles( "", "*.map", SearchOption.AllDirectories );
				foreach( string mapName in mapList )
				{
					comboBoxMaps.Items.Add( mapName );
					if( mapName == startMapName )
						comboBoxMaps.SelectedIndex = comboBoxMaps.Items.Count - 1;
				}
			}

			//load map
			WPFAppWorld.MapLoad( startMapName, true );
		}

		private void Window_Closed( object sender, EventArgs e )
		{
			//NeoAxis shutdown
			WPFAppWorld.Shutdown();
		}

		void renderTargetUserControl1_KeyDown( object sender, KeyEventArgs e )
		{
			//process keys for control here.

			//for checking key state use:
			//renderTargetUserControl1.IsKeyPressed();
		}

		void renderTargetUserControl1_KeyUp( object sender, KeyEventArgs e )
		{
			//process keys for control here.
		}

		void renderTargetUserControl1_MouseDown( object sender, MouseButtonEventArgs e )
		{
		}

		void renderTargetUserControl1_MouseUp( object sender, MouseButtonEventArgs e )
		{
		}

		void renderTargetUserControl1_MouseMove( object sender, MouseEventArgs e )
		{
		}

		void renderTargetUserControl1_Tick( RenderTargetUserControl sender, float delta )
		{
		}

		void renderTargetUserControl1_Render( RenderTargetUserControl sender, Camera camera )
		{
			//update camera
			if( Map.Instance != null )
			{
				Vec3 position;
				Vec3 forward;
				Vec3 up;
				Degree fov;

				//find first MapCamera object on the map
				MapCamera mapCamera = FindFirstMapCamera();
				if( mapCamera != null )
				{
					position = mapCamera.Position;
					forward = mapCamera.Rotation * new Vec3( 1, 0, 0 );
					up = Vec3.ZAxis;
					if( mapCamera.Fov != 0 )
						fov = mapCamera.Fov;
					else
						fov = Map.Instance.Fov;
				}
				else
				{
					position = Vec3.Zero;
					forward = new Vec3( 1, 0, 0 );
					up = Vec3.ZAxis;
					fov = Map.Instance.Fov;
				}

				renderTargetUserControl1.CameraNearFarClipDistance = Map.Instance.NearFarClipDistance;
				renderTargetUserControl1.CameraFixedUp = up;
				renderTargetUserControl1.CameraFov = fov;
				renderTargetUserControl1.CameraPosition = position;
				renderTargetUserControl1.CameraDirection = forward;
			}

			//update sound listener
			if( SoundWorld.Instance != null )
				SoundWorld.Instance.SetListener( camera.Position, Vec3.Zero, camera.Direction, camera.Up );
		}

		void renderTargetUserControl1_RenderUI( RenderTargetUserControl sender, GuiRenderer renderer )
		{
			string text = "NeoAxis 3D Engine " + EngineVersionInformation.Version;
			renderer.AddText( text, new Vec2( .01f, .01f ), HorizontalAlign.Left,
				 VerticalAlign.Top, new ColorValue( 1, 1, 1 ) );
		}

		private void buttonClose_Click( object sender, RoutedEventArgs e )
		{
			Close();
		}

		private void buttonLoadMap_Click( object sender, RoutedEventArgs e )
		{
			if( comboBoxMaps.SelectedIndex == -1 )
				return;
			string mapName = (string)comboBoxMaps.SelectedItem;
			WPFAppWorld.MapLoad( mapName, true );
		}

		private void buttonDestroy_Click( object sender, RoutedEventArgs e )
		{
			WPFAppWorld.WorldDestroy();
		}

		MapCamera FindFirstMapCamera()
		{
			foreach( Entity entity in Map.Instance.Children )
			{
				MapCamera mapCamera = entity as MapCamera;
				if( mapCamera != null )
					return mapCamera;
			}
			return null;
		}
	}
}
