// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Engine;
using Engine.Renderer;
using Engine.MathEx;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.UISystem;
using Engine.PhysicsSystem;
using Engine.Utils;
using ProjectCommon;
using ProjectEntities;

namespace Game
{
	/// <summary>
	/// Defines a game window for Mountain Village Demo.
	/// </summary>
	public class VillageDemoGameWindow : ActionGameWindow
	{
		List<MapCameraCurve> cameraCurves;
		float cameraCurvesTotalTime;

		bool demoMode;
		float demoModeTime;

		float screenTextAlpha;
		float lastTimeOfKeyDownOrMouseMove;
		Vec2 screenTextLastMousePosition;

		///////////////////////////////////////////

		protected override void OnAttach()
		{
			base.OnAttach();

			GenerateCameraCurvesList();

			//reset camera
			cameraType = ActionGameWindow.CameraType.FPS;
			FreeCameraEnabled = false;
			screenTextLastMousePosition = MousePosition;

			StartDemoMode();
		}

		void GenerateCameraCurvesList()
		{
			//cameraCurves
			cameraCurves = new List<MapCameraCurve>();
			foreach( Entity entity in Map.Instance.Children )
			{
				MapCameraCurve curve = entity as MapCameraCurve;
				if( curve != null )
				{
					cameraCurves.Add( curve );
				}
			}
			ListUtils.SelectionSort( cameraCurves, delegate( MapCameraCurve curve1, MapCameraCurve curve2 )
			{
				return string.Compare( curve1.Name, curve2.Name );
			} );

			cameraCurvesTotalTime = 0;
			foreach( MapCameraCurve curve in cameraCurves )
				cameraCurvesTotalTime += curve.GetCurveMaxTime();
		}

		public void StartDemoMode()
		{
			demoMode = true;
			demoModeTime = 0;
		}

		public void StopDemoMode()
		{
			demoMode = false;
			demoModeTime = 0;
		}

		void GetDemoModeMapCurve( out MapCameraCurve outCurve, out float outCurveTime )
		{
			outCurve = null;
			outCurveTime = 0;

			float remainingTime = demoModeTime;

			foreach( MapCameraCurve curve in cameraCurves )
			{
				float length = curve.GetCurveMaxTime();
				if( remainingTime < length )
				{
					outCurve = curve;
					outCurveTime = remainingTime;
					return;
				}
				remainingTime -= length;
				if( remainingTime < 0 )
					break;
			}

		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			lastTimeOfKeyDownOrMouseMove = EngineApp.Instance.Time;

			//If atop openly any window to not process
			if( Controls.Count != 1 )
				return base.OnKeyDown( e );

			if( demoMode )
			{
				if( e.Key == EKeys.Space )
				{
					StopDemoMode();
					return true;
				}
			}

			return base.OnKeyDown( e );
		}

		protected override bool OnMouseDown( EMouseButtons button )
		{
			if( demoMode )
			{
				Vec2 viewportSize = RendererWorld.Instance.DefaultViewport.DimensionsInPixels.Size.ToVec2();
				Vec2 offset = 3.0f / viewportSize;
				if( Math.Abs( MousePosition.X - screenTextLastMousePosition.X ) > offset.X ||
					Math.Abs( MousePosition.Y - screenTextLastMousePosition.Y ) > offset.Y )
				{
					lastTimeOfKeyDownOrMouseMove = EngineApp.Instance.Time;
					screenTextLastMousePosition = MousePosition;
				}
			}

			return base.OnMouseDown( button );
		}

		protected override void OnMouseMove()
		{
			lastTimeOfKeyDownOrMouseMove = EngineApp.Instance.Time;

			base.OnMouseMove();
		}

		protected override void OnGetCameraTransform( out Vec3 position, out Vec3 forward, out Vec3 up,
			ref Degree cameraFov )
		{
			//camera management for demo mode
			if( demoMode && !FreeCameraEnabled )
			{
				MapCameraCurve curve;
				float curveTime;
				GetDemoModeMapCurve( out curve, out curveTime );
				if( curve != null )
				{
					curve.CalculateCameraPositionByTime( curveTime, out position, out forward, out up,
						out cameraFov );

					//Hide FPS mode weapon
					if( PlayerIntellect.Instance != null )
						PlayerIntellect.Instance.FPSCamera = false;

					return;
				}
			}

			base.OnGetCameraTransform( out position, out forward, out up, ref cameraFov );
		}

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );

			//time counter for demo mode
			if( EntitySystemWorld.Instance.Simulation && !EntitySystemWorld.Instance.SystemPauseOfSimulation )
			{
				if( demoMode )
				{
					float step = RendererWorld.Instance.FrameRenderTimeStep;
					if( EngineApp.Instance.IsKeyPressed( EKeys.C ) )
						step *= 10;

					demoModeTime += step;
					if( demoModeTime >= cameraCurvesTotalTime )
						demoModeTime = 0;
				}
			}
		}

		void DrawFadingScreenQuad( GuiRenderer renderer )
		{

			const float fadeTime = 1;

			float minTimeToPoint;
			{
				//distance to start time.
				minTimeToPoint = demoModeTime;

				float time = 0;
				foreach( MapCameraCurve curve in cameraCurves )
				{
					float length = curve.GetCurveMaxTime();
					time += length;

					float d = Math.Abs( demoModeTime - time );
					if( d < minTimeToPoint )
						minTimeToPoint = d;
				}

				//first half second is always black
				if( demoModeTime < .5f )
					minTimeToPoint = 0;
			}

			//draw fading quad
			if( minTimeToPoint < fadeTime )
			{
				float alpha = minTimeToPoint / fadeTime;
				MathFunctions.Saturate( ref alpha );
				alpha = 1.0f - alpha;
				if( alpha > .001f )
					renderer.AddQuad( new Rect( 0, 0, 1, 1 ), new ColorValue( 0, 0, 0, alpha ) );
			}
		}

		void AddTextWithShadow( GuiRenderer renderer, string text, Vec2 position,
			HorizontalAlign horizontalAlign, VerticalAlign verticalAlign, ColorValue color )
		{
			Vec2 shadowOffset = 2.0f / RendererWorld.Instance.DefaultViewport.DimensionsInPixels.Size.ToVec2();

			renderer.AddText( text, position + shadowOffset, horizontalAlign, verticalAlign,
				new ColorValue( 0, 0, 0, color.Alpha / 2 ) );
			renderer.AddText( text, position, horizontalAlign, verticalAlign, color );
		}

		void DrawScreenText( GuiRenderer renderer )
		{
			const float fadeSpeed = 1;

			float step = RendererWorld.Instance.FrameRenderTimeStep;

			bool needShow = false;
			if( EngineApp.Instance.Time - lastTimeOfKeyDownOrMouseMove < 5 )
				needShow = true;

			if( needShow )
			{
				screenTextAlpha += step / fadeSpeed;
				if( screenTextAlpha > 1 )
					screenTextAlpha = 1;
			}
			else
			{
				screenTextAlpha -= step / fadeSpeed;
				if( screenTextAlpha < 0 )
					screenTextAlpha = 0;
			}

			string text = LanguageManager.Instance.Translate( "UISystem", "Press Space to play" );
			AddTextWithShadow( renderer, text, new Vec2( .5f, .8f ), HorizontalAlign.Center,
				VerticalAlign.Center, new ColorValue( 1, 1, 1, screenTextAlpha ) );
		}

		protected override void OnRender()
		{
			base.OnRender();

			//hide HUD control
			if( demoMode && !FreeCameraEnabled )
			{
				if( HUDControl != null )
					HUDControl.Visible = false;
			}
		}

		protected override void OnRenderUI( GuiRenderer renderer )
		{
			base.OnRenderUI( renderer );

			if( demoMode && !FreeCameraEnabled )
			{
				DrawFadingScreenQuad( renderer );
				DrawScreenText( renderer );
			}
		}

	}
}
