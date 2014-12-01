// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.Renderer;
using Engine.UISystem;
using Engine.MathEx;
using Engine.Utils;

namespace Game
{
	public class GUISamples1Window : Control
	{
		Control window;
		ComboBox comboBoxDrawAreaMode;
		Font drawAreaBigFont;
		Font drawAreaSmallFont;

		///////////////////////////////////////////

		enum DrawAreaModes
		{
			Triangles,
			Quads,
			Lines,
			Text,
		}

		///////////////////////////////////////////

		protected override void OnAttach()
		{
			base.OnAttach();

			//create window
			window = ControlDeclarationManager.Instance.CreateControl( "Gui\\GUISamples1Window.gui" );
			Controls.Add( window );

			window.Controls[ "CustomShaderMode" ].Enable = RenderSystem.Instance.HasShaderModel3();

			( (Button)window.Controls[ "Close" ] ).Click += Close_Click;
			( (Button)window.Controls[ "PlayVideo" ] ).Click += PlayVideo_Click;

			//Password edit box
			{
				EditBox editBox = window.Controls[ "PasswordEditBox" ] as EditBox;
				if( editBox != null )
				{
					//configure password feature
					editBox.UpdatingTextControl = delegate( EditBox sender, ref string text )
					{
						text = new string( '*', sender.Text.Length );
						if( sender.Focused )
							text += "_";
					};

					editBox.TextChange += passwordEditBox_TextChange;
				}
			}

			//Text typing filter edit box (numbers only)
			{
				EditBox editBox = window.Controls[ "TextTypingFilterEditBox" ] as EditBox;
				if( editBox != null )
				{
					editBox.TextTypingFilter = delegate( EditBox sender, EKeys key, char character, string newText )
					{
						if( character != 0 )
						{
							if( character < '0' || character > '9' )
								return false;
						}
						return true;
					};
				}
			}

			//DrawAreaMode
			{
				ComboBox comboBox = window.Controls[ "DrawAreaMode" ] as ComboBox;
				comboBoxDrawAreaMode = comboBox;
				if( comboBox != null )
				{
					foreach( DrawAreaModes mode in Enum.GetValues( typeof( DrawAreaModes ) ) )
						comboBox.Items.Add( mode.ToString() );
				}
				comboBox.SelectedIndex = 0;
			}

			//draw area: subscribe for render event
			{
				Control control = window.Controls[ "DrawArea" ];
				if( control != null )
					control.RenderUI += new RenderUIDelegate( DrawArea_RenderUI );
			}

			//load fonts
			drawAreaBigFont = FontManager.Instance.LoadFont( "Default", .04f );
			drawAreaSmallFont = FontManager.Instance.LoadFont( "Default", .015f );

			BackColor = new ColorValue( 0, 0, 0, .5f );
			MouseCover = true;
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( base.OnKeyDown( e ) )
				return true;

			if( e.Key == EKeys.Escape )
			{
				Close();
				return true;
			}

			return false;
		}

		void Close_Click( Button sender )
		{
			Close();
		}

		void PlayVideo_Click( Button sender )
		{
			VideoBox videoBox = window.Controls[ "VideoBox" ] as VideoBox;

			if( string.IsNullOrEmpty( videoBox.FileName ) || videoBox.IsEndOfFile() )
			{
				if( string.IsNullOrEmpty( videoBox.FileName ) )
					videoBox.FileName = "_ResourceEditorSamples\\Video\\Test.ogv";
				if( videoBox.IsEndOfFile() )
					videoBox.Rewind();
			}
			else
				videoBox.FileName = "";
		}

		void Close()
		{
			SetShouldDetach();
		}

		bool IsCustomShaderModeEnabled()
		{
			CheckBox checkBox = window.Controls[ "CustomShaderMode" ] as CheckBox;
			if( checkBox != null && checkBox.Checked )
				return true;
			return false;
		}

		protected override void OnBeforeRenderUIWithChildren( GuiRenderer renderer )
		{
			if( IsCustomShaderModeEnabled() )
			{
				//enable custom shader mode

				List<GuiRenderer.CustomShaderModeTexture> additionalTextures =
					new List<GuiRenderer.CustomShaderModeTexture>();
				additionalTextures.Add( new GuiRenderer.CustomShaderModeTexture(
					"GUI\\Textures\\Engine.png", false ) );

				List<GuiRenderer.CustomShaderModeParameter> parameters =
					new List<GuiRenderer.CustomShaderModeParameter>();
				float offsetX = ( EngineApp.Instance.Time / 60 ) % 1;
				Vec2 mouse = EngineApp.Instance.MousePosition;
				parameters.Add( new GuiRenderer.CustomShaderModeParameter( "testParameter",
					new Vec4( offsetX, mouse.X, mouse.Y, 0 ) ) );

				renderer.PushCustomShaderMode( "Base\\Shaders\\CustomGuiRenderingExample.cg_hlsl",
					additionalTextures, parameters );

				////second way: bind custom shader mode to this control and to all children.
				//EnableCustomShaderMode( true, "Base\\Shaders\\CustomGuiRenderingExample.cg_hlsl",
				//   additionalTextures, parameters );
			}

			base.OnBeforeRenderUIWithChildren( renderer );
		}

		protected override void OnAfterRenderUIWithChildren( GuiRenderer renderer )
		{
			base.OnAfterRenderUIWithChildren( renderer );

			//disable custom shader mode
			if( IsCustomShaderModeEnabled() )
				renderer.PopCustomShaderMode();
		}

		DrawAreaModes GetDrawAreaMode()
		{
			if( comboBoxDrawAreaMode.SelectedIndex != -1 )
				return (DrawAreaModes)comboBoxDrawAreaMode.SelectedIndex;
			return 0;
		}

		void DrawArea_RenderUI( Control sender, GuiRenderer renderer )
		{
			Rect rectangle = sender.GetScreenRectangle();
			bool clipRectangle = true;
			ColorValue[] colors = new ColorValue[]{
				new ColorValue( 1 ,0, 0 ),
				new ColorValue( 0, 1, 0 ),
				new ColorValue( 0, 0, 1 ),
				new ColorValue( 1, 1, 0 ),
				new ColorValue( 1, 1, 1 )};

			if( clipRectangle )
				renderer.PushClipRectangle( rectangle );

			//draw triangles
			if( GetDrawAreaMode() == DrawAreaModes.Triangles )
			{
				float distance = rectangle.GetSize().X / 2;

				List<GuiRenderer.TriangleVertex> triangles = new List<GuiRenderer.TriangleVertex>( 256 );

				Radian angle = -EngineApp.Instance.Time;

				const int steps = 30;
				Vec2 lastPosition = Vec2.Zero;
				for( int step = 0; step < steps + 1; step++ )
				{
					Vec2 localPos = new Vec2( MathFunctions.Cos( angle ), MathFunctions.Sin( angle ) ) * distance;
					Vec2 pos = rectangle.GetCenter() + new Vec2( localPos.X, localPos.Y * renderer.AspectRatio );

					if( step != 0 )
					{
						ColorValue color = colors[ step % colors.Length ];
						ColorValue color2 = color;
						color2.Alpha = 0;
						triangles.Add( new GuiRenderer.TriangleVertex( rectangle.GetCenter(), color ) );
						triangles.Add( new GuiRenderer.TriangleVertex( lastPosition, color2 ) );
						triangles.Add( new GuiRenderer.TriangleVertex( pos, color2 ) );
					}

					angle += ( MathFunctions.PI * 2 ) / steps;
					lastPosition = pos;
				}

				renderer.AddTriangles( triangles );
			}

			//draw quads
			if( GetDrawAreaMode() == DrawAreaModes.Quads )
			{
				//draw background
				{
					Texture texture = TextureManager.Instance.Load( "GUI\\Textures\\NeoAxisLogo.png" );
					renderer.AddQuad( rectangle, new Rect( 0, -.3f, 1, 1.4f ), texture,
						new ColorValue( 1, 1, 1 ), true );
				}

				//draw bars
				{
					float time = EngineApp.Instance.Time;

					EngineRandom random = new EngineRandom( 0 );

					int count = 15;
					float stepOffset = rectangle.GetSize().X / count;
					float size = stepOffset * .9f;
					for( int n = 0; n < count; n++ )
					{
						float v = MathFunctions.Cos( time * random.NextFloat() );
						float v2 = ( v + 1 ) / 2;

						ColorValue color = colors[ n % colors.Length ];
						Rect rect = new Rect(
							rectangle.Left + stepOffset * n, rectangle.Bottom - rectangle.GetSize().Y * v2,
							rectangle.Left + stepOffset * n + size, rectangle.Bottom );
						renderer.AddQuad( rect, color );
					}
				}
			}

			//draw lines
			if( GetDrawAreaMode() == DrawAreaModes.Lines )
			{
				float maxDistance;
				{
					Vec2 s = rectangle.GetSize() / 2;
					maxDistance = MathFunctions.Sqrt( s.X * s.X + s.Y * s.Y );
				}

				int step = 0;
				float distance = 0;
				Radian angle = -EngineApp.Instance.Time;
				Vec2 lastPosition = Vec2.Zero;

				while( distance < maxDistance )
				{
					Vec2 localPos = new Vec2( MathFunctions.Cos( angle ), MathFunctions.Sin( angle ) ) * distance;
					Vec2 pos = rectangle.GetCenter() + new Vec2( localPos.X, localPos.Y * renderer.AspectRatio );

					if( step != 0 )
					{
						ColorValue color = colors[ step % colors.Length ];
						renderer.AddLine( lastPosition, pos, color );
					}

					step++;
					angle += MathFunctions.PI / 10;
					distance += .001f;
					lastPosition = pos;
				}
			}

			//draw text
			if( GetDrawAreaMode() == DrawAreaModes.Text )
			{
				string text;

				//draw text with specified font.
				text = "Map Editor is a tool to create worlds for your project. The tool is a complex editor to manage " +
					"objects on the map.";
				renderer.AddTextWordWrap( drawAreaBigFont, text, rectangle, HorizontalAlign.Left, false, VerticalAlign.Top, 0,
					new ColorValue( 1, 1, 1 ) );

				//draw text with word wrap.
				text = "Deployment Tool is a tool to deploy the final version of your application to specified platforms. " +
					"This utility is useful to automate the final product's creation.";
				renderer.AddTextWordWrap( drawAreaSmallFont, text, rectangle, HorizontalAlign.Right, false, VerticalAlign.Bottom, 0,
					new ColorValue( 1, 1, 0 ) );
			}

			if( clipRectangle )
				renderer.PopClipRectangle();
		}

		void passwordEditBox_TextChange( Control sender )
		{
			Control control = window.Controls[ "PasswordEditBoxCheck" ];
			if( control != null )
				control.Text = sender.Text;
		}
	}
}
