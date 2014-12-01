// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Engine;
using Engine.Renderer;
using Engine.MathEx;
using Engine.SoundSystem;
using Engine.UISystem;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.FileSystem;
using Engine.Utils;
using ProjectCommon;
using ProjectEntities;

namespace Game
{
	/// <summary>
	/// Defines a game window for PlatformerDemo.
	/// </summary>
	public class PlatformerDemoGameWindow : GameWindow
	{
		//HUD screen
		Control hudControl;

		//

		protected override void OnAttach()
		{
			base.OnAttach();

			//To load the HUD screen
			hudControl = ControlDeclarationManager.Instance.CreateControl( "Maps\\PlatformerDemo\\Gui\\HUD.gui" );
			//Attach the HUD screen to the this window
			Controls.Add( hudControl );

			//CutSceneManager specific
			if( CutSceneManager.Instance != null )
			{
				CutSceneManager.Instance.CutSceneEnableChange += delegate( CutSceneManager manager )
				{
					if( manager.CutSceneEnable )
					{
						//Cut scene activated. All keys and buttons need to reset.
						EngineApp.Instance.KeysAndMouseButtonUpAll();
						GameControlsManager.Instance.DoKeyUpAll();
					}
				};
			}
		}

		protected override void OnDetach()
		{
			base.OnDetach();
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnKeyDown( e );

			//change camera type
			if( e.Key == EKeys.F7 )
			{
				FreeCameraEnabled = !FreeCameraEnabled;
				GameEngineApp.Instance.AddScreenMessage(
					string.Format( "Camera type: {0}", FreeCameraEnabled ? "Free" : "Default" ) );
				return true;
			}

			//GameControlsManager
			if( EntitySystemWorld.Instance.Simulation )
			{
				if( !FreeCameraEnabled && !IsCutSceneEnabled() )
				{
					if( GameControlsManager.Instance.DoKeyDown( e ) )
						return true;
				}
			}

			return base.OnKeyDown( e );
		}

		protected override bool OnKeyPress( KeyPressEvent e )
		{
			return base.OnKeyPress( e );
		}

		protected override bool OnKeyUp( KeyEvent e )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnKeyUp( e );

			//GameControlsManager
			GameControlsManager.Instance.DoKeyUp( e );

			return base.OnKeyUp( e );
		}

		protected override bool OnMouseDown( EMouseButtons button )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnMouseDown( button );

			//GameControlsManager
			if( EntitySystemWorld.Instance.Simulation )
			{
				if( !FreeCameraEnabled && !IsCutSceneEnabled() )
				{
					if( GameControlsManager.Instance.DoMouseDown( button ) )
						return true;
				}
			}

			return base.OnMouseDown( button );
		}

		protected override bool OnMouseUp( EMouseButtons button )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnMouseUp( button );

			//GameControlsManager
			GameControlsManager.Instance.DoMouseUp( button );

			return base.OnMouseUp( button );
		}

		protected override bool OnMouseDoubleClick( EMouseButtons button )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnMouseDoubleClick( button );

			return base.OnMouseDoubleClick( button );
		}

		protected override void OnMouseMove()
		{
			base.OnMouseMove();

			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return;

			//ignore mouse move events when Profiling Tool is activated.
			if( ProfilingToolWindow.Instance != null && !ProfilingToolWindow.Instance.Background )
				return;

			//GameControlsManager
			if( EntitySystemWorld.Instance.Simulation && EngineApp.Instance.MouseRelativeMode )
			{
				if( !FreeCameraEnabled && !IsCutSceneEnabled() )
				{
					Vec2 mouseOffset = MousePosition;
					GameControlsManager.Instance.DoMouseMoveRelative( mouseOffset );
				}
			}
		}

		protected override bool OnMouseWheel( int delta )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnMouseWheel( delta );

			return base.OnMouseWheel( delta );
		}

		protected override bool OnJoystickEvent( JoystickInputEvent e )
		{
			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnJoystickEvent( e );

			//GameControlsManager
			if( EntitySystemWorld.Instance.Simulation )
			{
				if( !FreeCameraEnabled && !IsCutSceneEnabled() )
				{
					if( GameControlsManager.Instance.DoJoystickEvent( e ) )
						return true;
				}
			}

			return base.OnJoystickEvent( e );
		}

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );

			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return;

			if( GameEngineApp.Instance.ControlManager != null &&
				GameEngineApp.Instance.ControlManager.IsControlFocused() )
				return;

			//GameControlsManager
			if( EntitySystemWorld.Instance.Simulation )
			{
				if( !FreeCameraEnabled && !IsCutSceneEnabled() )
					GameControlsManager.Instance.DoTick( delta );
			}
		}

		protected override void OnRender()
		{
			base.OnRender();

			UpdateHUD();
		}

		/// <summary>
		/// Updates HUD screen
		/// </summary>
		void UpdateHUD()
		{
			hudControl.Visible = EngineDebugSettings.DrawGui;

			//Game

			hudControl.Controls[ "Game" ].Visible = !FreeCameraEnabled && !IsCutSceneEnabled();

			//CutScene
			{
				hudControl.Controls[ "CutScene" ].Visible = IsCutSceneEnabled();

				if( CutSceneManager.Instance != null )
				{
					//CutSceneFade
					float fadeCoef = 0;
					if( CutSceneManager.Instance != null )
						fadeCoef = CutSceneManager.Instance.GetFadeCoefficient();
					hudControl.Controls[ "CutSceneFade" ].BackColor = new ColorValue( 0, 0, 0, fadeCoef );

					//Message
					{
						string text;
						ColorValue color;
						CutSceneManager.Instance.GetMessage( out text, out color );
						if( text == null )
							text = "";

						TextBox textBox = (TextBox)hudControl.Controls[ "CutScene/Message" ];
						textBox.Text = text;
						textBox.TextColor = color;
					}
				}
			}
		}

		protected override void OnRenderUI( GuiRenderer renderer )
		{
			base.OnRenderUI( renderer );
		}

		Unit GetPlayerUnit()
		{
			if( PlayerIntellect.Instance == null )
				return null;
			return PlayerIntellect.Instance.ControlledObject;
		}

		bool IsCutSceneEnabled()
		{
			return CutSceneManager.Instance != null && CutSceneManager.Instance.CutSceneEnable;
		}

		protected override void OnGetCameraTransform( out Vec3 position, out Vec3 forward,
			out Vec3 up, ref Degree cameraFov )
		{
			position = Vec3.Zero;
			forward = Vec3.XAxis;
			up = Vec3.ZAxis;

			Unit unit = GetPlayerUnit();
			if( unit == null )
				return;

			PlayerIntellect.Instance.FPSCamera = false;

			//To use data about orientation the camera if the cut scene is switched on
			if( IsCutSceneEnabled() )
			{
				if( CutSceneManager.Instance.GetCamera( out position, out forward, out up, out cameraFov ) )
					return;
			}

			float distance = 25;
			position = unit.GetInterpolatedPosition() + new Vec3( 0, -distance, 0 );
			forward = Vec3.YAxis;
			up = Vec3.ZAxis;
		}

		public Control HUDControl
		{
			get { return hudControl; }
		}
	}
}
