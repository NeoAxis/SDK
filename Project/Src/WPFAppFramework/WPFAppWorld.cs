// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Engine;
using Engine.FileSystem;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Renderer;

namespace WPFAppFramework
{
	public static class WPFAppWorld
	{
		internal static List<RenderTargetUserControl> renderTargetUserControls =
			 new List<RenderTargetUserControl>();

		static bool duringWarningOrErrorMessageBox;
		internal static bool duringOnRender;

		static Queue<LogMessage> cachedLogMessages = new Queue<LogMessage>();
		static System.Windows.Forms.Timer updateTimer;

		///////////////////////////////////////////

		class LogMessage
		{
			public string caption;
			public string text;

			public LogMessage( string caption, string text )
			{
				this.caption = caption;
				this.text = text;
			}
		};

		///////////////////////////////////////////

		public static bool InitWithoutCreation( EngineApp engineAppInstance, string logFileName, bool correctCurrentDirectory,
			string specialExecutableDirectoryPath, string specialResourceDirectoryPath, string specialUserDirectoryPath,
			string specialNativeLibrariesDirectoryPath )
		{
			if( !VirtualFileSystem.Init( logFileName, correctCurrentDirectory, specialExecutableDirectoryPath,
				specialResourceDirectoryPath, specialUserDirectoryPath, specialNativeLibrariesDirectoryPath ) )
				return false;

			Log.Handlers.WarningHandler += delegate( string text, ref bool handled, ref bool dumpToLogFile )
			{
				handled = true;

				if( duringOnRender || duringWarningOrErrorMessageBox )
				{
					cachedLogMessages.Enqueue( new LogMessage( "Warning", text ) );
				}
				else
				{
					duringWarningOrErrorMessageBox = true;
					MessageBox.Show( text, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning );
					duringWarningOrErrorMessageBox = false;
				}
			};

			Log.Handlers.ErrorHandler += delegate( string text, ref bool handled, ref bool dumpToLogFile )
			{
				handled = true;

				if( duringOnRender || duringWarningOrErrorMessageBox )
				{
					cachedLogMessages.Enqueue( new LogMessage( "Error", text ) );
				}
				else
				{
					duringWarningOrErrorMessageBox = true;
					MessageBox.Show( text, "Error", MessageBoxButton.OK, MessageBoxImage.Warning );
					duringWarningOrErrorMessageBox = false;
				}
			};

			//if( System.Environment.OSVersion.Version.Major >= 6 )
			//{
			//   if( MessageBox.Show(
			//      "Do you want to use D3DImage?\n\nD3DImage feature is a new level of interoperability between WPF and DirectX by allowing a custom Direct3D (D3D) surface to be blended with WPF's native D3D surface.",
			//      "D3DImage support", MessageBoxButton.YesNo ) == MessageBoxResult.Yes )
			//   {
			//      RendererWorld.InitializationOptions.AllowDirectX9Ex = true;
			//   }
			//}
			EngineApp.Init( engineAppInstance );

			updateTimer = new System.Windows.Forms.Timer();
			updateTimer.Interval = 100;
			updateTimer.Tick += updateTimer_Tick;
			updateTimer.Enabled = true;

			return true;
		}

		public static bool Create( Window mainApplicationWindow )
		{
			EngineApp.Instance.WindowHandle = new WindowInteropHelper( mainApplicationWindow ).Handle;
			if( !EngineApp.Instance.Create() )
				return false;
			return true;
		}

		public static bool Init( EngineApp engineAppInstance, Window mainApplicationWindow, string logFileName,
			bool correctCurrentDirectory, string specialExecutableDirectoryPath, string specialResourceDirectoryPath,
			string specialUserDirectoryPath, string specialNativeLibrariesDirectoryPath )
		{
			if( !InitWithoutCreation( engineAppInstance, logFileName, correctCurrentDirectory, specialExecutableDirectoryPath,
				specialResourceDirectoryPath, specialUserDirectoryPath, specialNativeLibrariesDirectoryPath ) )
				return false;
			if( !Create( mainApplicationWindow ) )
				return false;
			return true;
		}

		static void updateTimer_Tick( object sender, EventArgs e )
		{
			while( cachedLogMessages.Count != 0 )
			{
				LogMessage message = cachedLogMessages.Dequeue();

				duringWarningOrErrorMessageBox = true;
				MessageBox.Show( message.text, message.caption, MessageBoxButton.OK, MessageBoxImage.Warning );
				duringWarningOrErrorMessageBox = false;
			}
		}

		public static void Shutdown()
		{
			while( renderTargetUserControls.Count != 0 )
				renderTargetUserControls[ 0 ].Destroy();

			EngineApp.Instance.Destroy();
			EngineApp.Shutdown();
			Log.DumpToFile( "Program END\r\n" );
			VirtualFileSystem.Shutdown();
		}

		public static bool MapLoad( string virtualFileName, bool runSimulation )
		{
			//Destroy old
			WorldDestroy();

			//New
			if( !EntitySystemWorld.Instance.WorldCreate( WorldSimulationTypes.Single,
				 EntitySystemWorld.Instance.DefaultWorldType ) )
			{
				Log.Error( "EntitySystemWorld: WorldCreate failed." );
				return false;
			}

			if( !MapSystemWorld.MapLoad( virtualFileName ) )
				return false;

			//run simulation
			EntitySystemWorld.Instance.Simulation = runSimulation;

			return true;
		}

		public static void WorldDestroy()
		{
			MapSystemWorld.MapDestroy();

			if( EntitySystemWorld.Instance != null )
				EntitySystemWorld.Instance.WorldDestroy();
		}

		public static bool DuringWarningOrErrorMessageBox
		{
			get { return duringWarningOrErrorMessageBox; }
		}
	}
}
