// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Engine;
using Engine.FileSystem;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Renderer;

namespace WinFormsAppFramework
{
	public static class WinFormsAppWorld
	{
		internal static List<RenderTargetUserControl> renderTargetUserControls =
			new List<RenderTargetUserControl>();

		static Control windowHandleControl;

		static bool duringWarningOrErrorMessageBox;

		//

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
				duringWarningOrErrorMessageBox = true;
				MessageBox.Show( text, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning );
				duringWarningOrErrorMessageBox = false;
			};

			Log.Handlers.ErrorHandler += delegate( string text, ref bool handled, ref bool dumpToLogFile )
			{
				handled = true;
				duringWarningOrErrorMessageBox = true;
				MessageBox.Show( text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning );
				duringWarningOrErrorMessageBox = false;
			};

			if( !EngineApp.Init( engineAppInstance ) )
				return false;

			return true;
		}

		public static bool Create( Form mainApplicationForm )
		{
			if( windowHandleControl != null )
				Log.Fatal( "WinFormsAppWorld: Create: WinformsAppWorld is already created." );
			if( !mainApplicationForm.IsHandleCreated )
				Log.Fatal( "WinFormsAppWorld: Create: mainApplicationForm: Handle is not created." );

			windowHandleControl = new Control();
			windowHandleControl.Parent = mainApplicationForm;
			windowHandleControl.Location = new System.Drawing.Point( 0, 0 );
			windowHandleControl.Size = new System.Drawing.Size( 10, 10 );
			windowHandleControl.Visible = false;
			windowHandleControl.CreateControl();

			EngineApp.Instance.WindowHandle = mainApplicationForm.Handle;
			if( !EngineApp.Instance.Create() )
				return false;

			return true;
		}

		public static bool Init( EngineApp engineAppInstance, Form mainApplicationForm, string logFileName,
			bool correctCurrentDirectory, string specialExecutableDirectoryPath, string specialResourceDirectoryPath,
			string specialUserDirectoryPath, string specialNativeLibrariesDirectoryPath )
		{
			if( !InitWithoutCreation( engineAppInstance, logFileName, correctCurrentDirectory, specialExecutableDirectoryPath,
				specialResourceDirectoryPath, specialUserDirectoryPath, specialNativeLibrariesDirectoryPath ) )
				return false;
			if( !Create( mainApplicationForm ) )
				return false;
			return true;
		}

		public static void Shutdown()
		{
			while( renderTargetUserControls.Count != 0 )
				renderTargetUserControls[ 0 ].Destroy();

			EngineApp.Instance.Destroy();
			EngineApp.Shutdown();
			Log.DumpToFile( "Program END\r\n" );
			VirtualFileSystem.Shutdown();

			//bug fix for ".NET-BroadcastEventWindow" error
			Application.Exit();
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
