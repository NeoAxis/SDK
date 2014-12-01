// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Engine;
using Engine.MathEx;
using Engine.FileSystem;
using Engine.Renderer;
using Engine.Utils;
using ProjectCommon;
using ProjectEntities;

namespace Game
{
	/// <summary>
	/// Defines an input point in the application.
	/// </summary>
	public static class Program
	{
		public static bool needRestartApplication;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			if( Debugger.IsAttached )
			{
				Main2();
			}
			else
			{
				try
				{
					Main2();
				}
				catch( Exception e )
				{
					Log.FatalAsException( e.ToString() );
				}
			}
		}

		static void Main2()
		{
			//initialize file sytem of the engine
			if( !VirtualFileSystem.Init( "user:Logs/Game.log", true, null, null, null, null ) )
				return;

			//configure general settings
			EngineApp.ConfigName = "user:Configs/Game.config";
			if( PlatformInfo.Platform == PlatformInfo.Platforms.Windows )
				EngineApp.UseDirectInputForMouseRelativeMode = true;
			EngineApp.AllowJoysticksAndCustomInputDevices = true;
			EngineApp.AllowWriteEngineConfigFile = true;
			EngineApp.AllowChangeVideoMode = true;
			//Change Floating Point Model for FPU math calculations. Default is Strict53Bits.
			//FloatingPointModel.Model = FloatingPointModel.Models.Strict53Bits;

			//init engine application
			EngineApp.Init( new GameEngineApp() );
			//enable support field and properties serialization for GameEngineApp class.
			EngineApp.Instance.Config.RegisterClassParameters( typeof( GameEngineApp ) );

			//update window
			EngineApp.Instance.WindowTitle = "Game";
			if( PlatformInfo.Platform == PlatformInfo.Platforms.Windows )
				EngineApp.Instance.Icon = Game.Properties.Resources.Logo;

			//create game console
			EngineConsole.Init();

			//EngineApp.Instance.SuspendWorkingWhenApplicationIsNotActive = false;

			//create and run application loop.
			if( EngineApp.Instance.Create() )
				EngineApp.Instance.Run();

			EngineApp.Shutdown();

			Log.DumpToFile( "Program END\r\n" );

			VirtualFileSystem.Shutdown();

			if( needRestartApplication )
				Process.Start( System.Reflection.Assembly.GetExecutingAssembly().Location, "" );
		}
	}
}
