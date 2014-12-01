// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Engine;
using Engine.Renderer;
using Engine.MathEx;
using Engine.SoundSystem;
using Engine.Utils;

namespace ProjectCommon
{
	public sealed class EngineConsole
	{
		static EngineConsole instance;

		//visual
		bool needLoadTextureAndFont = true;
		Font font;
		Texture texture;

		Vec2 textureOffset;
		float maxTransparency = .9f;

		//commands
		Command.Method defaultCommandHandler;
		List<Command> commands = new List<Command>();

		//actions
		bool active;
		bool autoOpening = true;
		float transparency;

		struct OldString
		{
			public string text;
			public ColorValue color;

			public OldString( string text, ColorValue color )
			{
				this.text = text;
				this.color = color;
			}
		}
		List<OldString> strings = new List<OldString>();
		int stringDownPosition = -1;
		string currentString = "";

		//history
		List<string> history = new List<string>();
		int currentHistoryIndex;

		///////////////////////////////////////////

		public class Command
		{
			internal string name;
			internal Method handler;
			internal MethodExtended extendedHandler;
			internal string description;
			internal object userData;

			public delegate void Method( string arguments );
			public delegate void MethodExtended( string arguments, object userData );

			public string Name
			{
				get { return name; }
			}

			public Method Handler
			{
				get { return handler; }
			}

			public MethodExtended ExtendedHandler
			{
				get { return extendedHandler; }
			}

			public string Description
			{
				get { return description; }
			}

			public object UserData
			{
				get { return userData; }
			}
		}

		///////////////////////////////////////////

		public static void Init()
		{
			if( instance != null )
				Log.Fatal( "EngineConsole: Init: instance != null." );
			instance = new EngineConsole();
			instance.InitInternal();
		}

		public static void Shutdown()
		{
			if( instance != null )
			{
				instance.ShutdownInternal();
				instance = null;
			}
		}

		void InitInternal()
		{
			AddStandardCommands();

			//add commands for EngineApp.Instance.Config fields and properties
			foreach( Config.Parameter parameter in EngineApp.Instance.Config.Parameters )
				RegisterConfigParameter( parameter );

			Print( "Welcome to NeoAxis 3D Engine!", new ColorValue( 1, 1, 0 ) );
			Print( "Use \"commands\" command to look the list of available commands" );
			Print( "Press \"Tab\" to autocompletion the command name" );
			Print( "Press \"Up\", \"Down\" to select the last processed commands" );
			Print( "Press \"Page Up\", \"Page Down\" to scroll the history of the commands" );
		}

		void ShutdownInternal()
		{
		}

		static public EngineConsole Instance
		{
			get { return instance; }
		}

		public bool Active
		{
			get { return active; }
			set
			{
				if( this.active == value )
					return;

				this.active = value;
				currentString = "";
			}
		}

		public bool AutoOpening
		{
			get { return autoOpening; }
			set { autoOpening = value; }
		}

		void AddCommand( string name, Command.Method handler, Command.MethodExtended extendedHandler,
			string description, object userData )
		{
			if( GetCommandByName( name ) != null )
				return;

			Command command = new Command();
			command.name = name;
			command.handler = handler;
			command.extendedHandler = extendedHandler;
			command.description = description;
			command.userData = userData;

			for( int n = 0; n < commands.Count; n++ )
			{
				if( string.Equals( name, commands[ n ].Name, StringComparison.OrdinalIgnoreCase ) )
				{
					commands.Insert( n, command );
					return;
				}
			}
			commands.Add( command );
		}


		public void AddCommand( string name, Command.Method handler, string description )
		{
			AddCommand( name, handler, null, description, null );
		}

		public void AddCommand( string name, Command.MethodExtended handler, object userData,
			string description )
		{
			AddCommand( name, null, handler, description, userData );
		}

		public void AddCommand( string name, Command.Method handler )
		{
			AddCommand( name, handler, null );
		}

		public void AddCommand( string name, Command.MethodExtended handler, object userData )
		{
			AddCommand( name, handler, userData, null );
		}

		public void RemoveCommand( string name )
		{
			Command command = GetCommandByName( name );
			if( command != null )
				commands.Remove( command );
		}

		Command.Method DefaultCommandHandler
		{
			get { return defaultCommandHandler; }
			set { defaultCommandHandler = value; }
		}

		public ReadOnlyCollection<Command> Commands
		{
			get { return commands.AsReadOnly(); }
		}

		Command GetCommandByName( string name )
		{
			foreach( Command command in commands )
				if( string.Equals( command.Name, name, StringComparison.OrdinalIgnoreCase ) )
					return command;
			return null;
		}

		//Actions

		public void Print( string text, ColorValue color )
		{
			if( text == null )
				text = "null";

			while( strings.Count > 256 )
				strings.RemoveAt( 0 );

			GuiRenderer renderer = EngineApp.Instance.ScreenGuiRenderer;
			if( font != null && renderer != null )
			{
				Font.WordWrapLinesItem[] items = font.GetWordWrapLines( renderer, text, .98f );
				foreach( Font.WordWrapLinesItem item in items )
				{
					if( stringDownPosition == strings.Count - 1 )
						stringDownPosition++;
					strings.Add( new OldString( item.Text, color ) );
				}
			}
			else
			{
				if( stringDownPosition == strings.Count - 1 )
					stringDownPosition++;
				strings.Add( new OldString( text, color ) );
			}
		}

		public void Print( string text )
		{
			Print( text, new ColorValue( 1, 1, 1 ) );
		}

		public void PrintFormatted( string format, params object[] arguments )
		{
			Print( string.Format( format, arguments ) );
		}

		public bool ExecuteString( string str )
		{
			if( str == null )
				return false;

			str = str.Trim();

			if( str == "" )
				return false;

			string name;
			string args;
			{
				int index = str.IndexOf( ' ' );

				if( index != -1 )
				{
					name = str.Substring( 0, index );
					args = str.Substring( index + 1, str.Length - index - 1 );
				}
				else
				{
					name = str;
					args = "";
				}
			}

			name = name.Trim();
			args = args.Trim();

			history.Add( str );
			currentHistoryIndex = history.Count;
			while( history.Count > 256 )
				history.RemoveAt( 0 );

			Command command = GetCommandByName( name );
			if( command == null )
			{
				if( defaultCommandHandler != null )
					defaultCommandHandler( str );
				else
					Print( string.Format( "Unknown command \"{0}\"", name ), new ColorValue( 1, 0, 0 ) );
				return false;
			}

			if( command.Handler != null )
				command.Handler( args );
			if( command.ExtendedHandler != null )
				command.ExtendedHandler( args, command.UserData );

			return true;
		}

		public void Clear()
		{
			strings.Clear();
			stringDownPosition = -1;
		}

		public bool DoKeyDown( KeyEvent e )
		{
			if( e.Key == EKeys.Oemtilde || e.Key == EKeys.Paragraph )
			{
				e.SuppressKeyPress = true;
				if( !Active )
					Active = AutoOpening || EngineApp.Instance.IsKeyPressed( EKeys.Control );
				else
				{
					Active = false;
					if( EngineApp.Instance.IsKeyPressed( EKeys.Control ) )
						AutoOpening = !AutoOpening;
				}
				return true;
			}

			if( !active )
				return false;

			switch( e.Key )
			{
			case EKeys.Up:
				if( currentHistoryIndex > 0 )
					currentHistoryIndex--;
				if( currentHistoryIndex < history.Count )
					currentString = history[ currentHistoryIndex ];
				return true;

			case EKeys.Down:
				if( currentHistoryIndex < history.Count )
					currentHistoryIndex++;
				if( currentHistoryIndex != 0 && currentHistoryIndex <= history.Count )
				{
					currentString = "";
					if( currentHistoryIndex < history.Count )
						currentString = history[ currentHistoryIndex ];
				}
				return true;

			case EKeys.PageUp:
				stringDownPosition -= 8;
				if( stringDownPosition < 0 )
					stringDownPosition = 0;
				if( stringDownPosition > strings.Count - 1 )
					stringDownPosition = strings.Count - 1;
				return true;

			case EKeys.PageDown:
				stringDownPosition += 8;
				if( stringDownPosition > strings.Count - 1 )
					stringDownPosition = strings.Count - 1;
				return true;

			case EKeys.Home:
				stringDownPosition = 0;
				if( stringDownPosition < 0 )
					stringDownPosition = 0;
				if( stringDownPosition > strings.Count - 1 )
					stringDownPosition = strings.Count - 1;
				return true;

			case EKeys.End:
				stringDownPosition = strings.Count - 1;
				return true;

			case EKeys.Back:
				if( currentString != "" )
					currentString = currentString.Substring( 0, currentString.Length - 1 );
				return true;

			case EKeys.Return:
				{
					stringDownPosition = strings.Count - 1;

					currentString = currentString.Trim();

					if( currentString != "" )
					{
						Print( currentString, new ColorValue( 0, 1, 0 ) );
						ExecuteString( currentString );
						currentString = "";
					}
				}
				return true;

			case EKeys.Tab:
				{
					string str = currentString.Trim();

					if( str.Length != 0 )
					{
						int count = 0;
						string lastFounded = "";
						for( int n = 0; n < commands.Count; n++ )
						{
							string name = commands[ n ].Name;

							if( name.Length >= str.Length && string.Equals( name.Substring(
								0, str.Length ), str, StringComparison.OrdinalIgnoreCase ) )
							{
								count++;
								lastFounded = commands[ n ].Name;
							}
						}

						if( count != 1 )
						{
							List<string> list = new List<string>( 128 );

							for( int n = 0; n < commands.Count; n++ )
							{
								string name = commands[ n ].Name;

								if( name.Length >= str.Length && string.Equals( name.Substring(
									0, str.Length ), str, StringComparison.OrdinalIgnoreCase ) )
								{
									list.Add( name );
								}
							}

							foreach( string s in list )
								Print( s );

							currentString = str;

							if( list.Count != 0 )
							{
								int pos = currentString.Length;
								while( true )
								{
									if( list[ 0 ].Length <= pos )
										break;
									char c = list[ 0 ][ pos ];
									for( int n = 1; n < list.Count; n++ )
									{
										if( list[ n ].Length <= pos )
											goto end;
										if( c != list[ n ][ pos ] )
											goto end;
									}
									currentString += c.ToString();
									pos++;
								}
								end: ;
							}
						}
						else
							currentString = lastFounded + " ";
					}
				}
				return true;
			}

			return true;
		}

		public bool DoKeyPress( KeyPressEvent e )
		{
			if( !active )
				return false;

			if( currentString.Length < 1024 )
			{
				bool allowCharacter = false;

				if( font != null )
					allowCharacter = e.KeyChar >= 32 && font.IsCharacterInitialized( e.KeyChar );
				else
					allowCharacter = e.KeyChar >= 32 && e.KeyChar < 128;

				if( allowCharacter )
					currentString += e.KeyChar.ToString();
			}

			return true;
		}

		public bool DoMouseWheel( int delta )
		{
			if( !active )
				return false;

			int step = delta / 10;
			stringDownPosition -= step;
			if( stringDownPosition < 0 )
				stringDownPosition = 0;
			if( stringDownPosition > strings.Count - 1 )
				stringDownPosition = strings.Count - 1;

			return true;
		}

		public void DoTick( float delta )
		{
			if( active )
			{
				transparency += delta;
				if( transparency > maxTransparency )
					transparency = maxTransparency;
			}
			else
			{
				transparency -= delta;
				if( transparency < 0 )
					transparency = 0;
			}

			textureOffset.X += delta / 30.0f;
			if( textureOffset.X > 1.0f )
				textureOffset.X -= 1.0f;
			textureOffset.Y += delta / 120.0f;
			if( textureOffset.Y > 1.0f )
				textureOffset.Y -= 1.0f;
		}

		public void DoRenderUI()
		{
			GuiRenderer renderer = EngineApp.Instance.ScreenGuiRenderer;

			if( transparency == 0.0f )
				return;

			//load backgrouund texture and font
			if( texture != null && texture.IsDisposed() )
				needLoadTextureAndFont = true;
			if( needLoadTextureAndFont )
			{
				texture = TextureManager.Instance.Load( "GUI\\Textures\\Console.png", Texture.Type.Type2D, 0 );
				font = FontManager.Instance.LoadFont( "Default", .025f );
				needLoadTextureAndFont = false;
			}

			if( font == null )
				return;

			Vec2 viewportSize = renderer.ViewportForScreenGuiRenderer.DimensionsInPixels.Size.ToVec2();
			Vec2 shadowOffset = new Vec2( 1, 1 ) / viewportSize;

			//draw background
			Rect textureRect = new Rect( 0, 0, 10 * renderer.AspectRatio, 10 / 2 );
			textureRect -= textureOffset;
			renderer.AddQuad( new Rect( 0, 0, 1, .5f ), textureRect, texture, new ColorValue( 1, 1, 1, transparency ), false );

			//draw border line
			renderer.AddQuad( new Rect( 0, .5f, 1, .508f ), new ColorValue( 0.29f, 0.6f, 0.86f, 0.9f * transparency ) );

			//draw background info
			string staticText = string.Format(
				"NeoAxis 3D Engine {0}\r\nPress \"~\" to hide console\r\nPress \"Ctrl + ~\" to hide and disable auto opening",
				EngineVersionInformation.Version );
			renderer.AddTextWordWrap( staticText, new Rect( 0, 0, .995f, .495f ), HorizontalAlign.Right, false, VerticalAlign.Bottom, 0,
				new ColorValue( .5f, .5f, .6f, transparency ) );

			float fontheight = font.Height;

			float x = .01f;

			float y = .5f - fontheight;

			string str;
			if( stringDownPosition != strings.Count - 1 )
			{
				str = "- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -" +
					" - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -" +
					" - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -" +
					" - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -" +
					" - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -";
			}
			else
				str = currentString + "_";

			renderer.PushClipRectangle( new Rect( 0, 0, .975f, 1 ) );
			renderer.AddText( font, str, new Vec2( x, y ) + shadowOffset, HorizontalAlign.Left,
				VerticalAlign.Center, new ColorValue( 0, 0, 0, transparency / 2 ) );
			renderer.AddText( font, str, new Vec2( x, y ), HorizontalAlign.Left,
				VerticalAlign.Center, new ColorValue( 1, 1, 1, transparency ) );
			renderer.PopClipRectangle();

			y -= fontheight + fontheight * .5f;

			int startpos = stringDownPosition;
			if( startpos > strings.Count - 1 )
				startpos = strings.Count - 1;
			for( int n = startpos; n >= 0 && y - fontheight > 0; n-- )
			{
				renderer.AddText( font, strings[ n ].text, new Vec2( x, y ) + shadowOffset, HorizontalAlign.Left,
					VerticalAlign.Center, strings[ n ].color * new ColorValue( 0, 0, 0, transparency / 2 ) );
				renderer.AddText( font, strings[ n ].text, new Vec2( x, y ), HorizontalAlign.Left,
					VerticalAlign.Center, strings[ n ].color * new ColorValue( 1, 1, 1, transparency ) );
				y -= fontheight;
			}
		}

		void OnConsoleConfigCommand( string arguments, object userData )
		{
			Config.Parameter parameter = (Config.Parameter)userData;

			if( string.IsNullOrEmpty( arguments ) )
			{
				object value = parameter.GetValue();
				Print( string.Format( "Value: \"{0}\", Default value: \"{1}\"",
					value != null ? value : "(null)", parameter.DefaultValue ) );
				return;
			}

			try
			{
				if( parameter.Field != null )
				{
					object value = SimpleTypesUtils.GetSimpleTypeValue( parameter.Field.FieldType,
						arguments );
					if( value == null )
						throw new Exception( "Not simple type" );
					parameter.Field.SetValue( null, value );
				}
				else if( parameter.Property != null )
				{
					object value = SimpleTypesUtils.GetSimpleTypeValue( parameter.Property.PropertyType,
						arguments );
					if( value == null )
						throw new Exception( "Not simple type" );
					parameter.Property.SetValue( null, value, null );
				}
			}
			catch( FormatException e )
			{
				string s = "";
				if( parameter.Field != null )
					s = parameter.Field.FieldType.ToString();
				else if( parameter.Property != null )
					s = parameter.Property.PropertyType.ToString();
				Print( string.Format( "Config : Invalid parameter format \"{0}\" {1}", s,
					e.Message ), new ColorValue( 1, 0, 0 ) );
			}
		}

		public void RegisterConfigParameter( Config.Parameter parameter )
		{
			string strType = "";
			if( parameter.Field != null )
				strType = parameter.Field.FieldType.Name;
			else if( parameter.Property != null )
				strType = parameter.Property.PropertyType.Name;

			string description = string.Format( "\"{0}\", Default: \"{1}\"", strType,
				parameter.DefaultValue );
			AddCommand( parameter.Name, OnConsoleConfigCommand, parameter, description );
		}

		public void ScrollDown()
		{
			stringDownPosition = strings.Count - 1;
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////

		//standard commands

		void OnConsoleQuit( string arguments )
		{
			EngineApp.Instance.SetNeedExit();
		}

		void OnConsoleClear( string arguments )
		{
			Clear();
		}

		void OnConsoleCommands( string arguments )
		{
			Print( "List of commands:" );
			foreach( Command command in commands )
			{
				string text = command.Name;
				if( command.Description != null )
					text += " (" + command.Description + ")";
				Print( text );
			}
		}

		void OnConsoleFullScreen( string arguments )
		{
			if( !string.IsNullOrEmpty( arguments ) )
			{
				try
				{
					bool value;
					if( arguments == "1" )
						value = true;
					else if( arguments == "0" )
						value = false;
					else
						value = bool.Parse( arguments );

					EngineApp.Instance.FullScreen = value;
				}
				catch( Exception ex )
				{
					Log.Warning( ex.Message );
				}
			}
			else
			{
				Print( string.Format( "Value: \"{0}\", Default value: \"{1}\"",
					EngineApp.Instance.FullScreen, true ) );
			}
		}

		void OnConsoleVideoMode( string arguments )
		{
			if( !string.IsNullOrEmpty( arguments ) )
			{
				try
				{
					Vec2I mode = Vec2I.Parse( arguments );

					if( EngineApp.Instance.FullScreen && !DisplaySettings.VideoModeExists( mode ) )
					{
						string text = string.Format( "Cannot change screen resolution to \"{0}x{1}\". " +
							"This resolution is not supported by the system.", mode.X, mode.Y );
						Log.Warning( text );
						return;
					}

					EngineApp.Instance.VideoMode = mode;
				}
				catch( Exception ex )
				{
					Log.Warning( ex.Message );
				}
			}
			else
			{
				Print( string.Format( "Value: \"{0}\"", EngineApp.Instance.VideoMode ) );
			}
		}

		void OnLogNativeMemoryStatistics( string arguments )
		{
			NativeMemoryManager.LogAllocationStatistics();
			Print( "Done. See log file in UserSettings\\Logs folder." );
		}

		void OnConsoleEngineTimeScale( string arguments )
		{
			if( !string.IsNullOrEmpty( arguments ) )
			{
				try
				{
					EngineApp.Instance.TimeScale = float.Parse( arguments );
				}
				catch( Exception ex )
				{
					Log.Warning( ex.Message );
				}
			}
			else
			{
				Print( string.Format( "Value: \"{0}\"", EngineApp.Instance.TimeScale ) );
			}
		}

		void OnConsoleSoundPitchScale( string arguments )
		{
			if( !string.IsNullOrEmpty( arguments ) )
			{
				try
				{
					SoundWorld.Instance.MasterChannelGroup.Pitch = float.Parse( arguments );
				}
				catch( Exception ex )
				{
					Log.Warning( ex.Message );
				}
			}
			else
			{
				Print( string.Format( "Value: \"{0}\"", SoundWorld.Instance.MasterChannelGroup.Pitch ) );
			}
		}

		void AddStandardCommands()
		{
			AddCommand( "quit", OnConsoleQuit );
			AddCommand( "exit", OnConsoleQuit );
			AddCommand( "clear", OnConsoleClear );
			AddCommand( "commands", OnConsoleCommands );
			if( EngineApp.AllowChangeVideoMode )
				AddCommand( "fullScreen", OnConsoleFullScreen );
			AddCommand( "videoMode", OnConsoleVideoMode );
			AddCommand( "logNativeMemoryStatistics", OnLogNativeMemoryStatistics );
			AddCommand( "engineTimeScale", OnConsoleEngineTimeScale );
			AddCommand( "soundPitchScale", OnConsoleSoundPitchScale );
		}
	}
}
