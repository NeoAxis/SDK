// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.UISystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.SoundSystem;

namespace Game
{
	/// <summary>
	/// Defines a NeoAxis Engine Logo window.
	/// </summary>
	public class EngineLogoWindow : Control
	{
		const float lifeTime = 10;
		Texture engineTexture;

		//

		protected override void OnAttach()
		{
			base.OnAttach();

			engineTexture = TextureManager.Instance.Load( "GUI\\Textures\\Engine.png" );

			EngineApp.Instance.MouseRelativeMode = true;

			SoundWorld.Instance.SetListener( new Vec3( 1000, 1000, 1000 ),
				Vec3.Zero, new Vec3( 1, 0, 0 ), new Vec3( 0, 0, 1 ) );

			ResetTime();
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( base.OnKeyDown( e ) )
				return true;
			if( e.Key == EKeys.Escape || e.Key == EKeys.Return || e.Key == EKeys.Space )
			{
				Destroy( true );
				return true;
			}
			return false;
		}

		protected override bool OnMouseDown( EMouseButtons button )
		{
			if( button == EMouseButtons.Left || button == EMouseButtons.Right )
			{
				Destroy( true );
				return true;
			}
			return base.OnMouseDown( button );
		}

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );
			if( Time > lifeTime )
				Destroy( false );
		}

		void Destroy( bool breaked )
		{
			SetShouldDetach();

			//always go to main menu

			//if( breaked )
			//{
			EngineApp.Instance.MouseRelativeMode = false;
			EngineApp.Instance.MousePosition = new Vec2( .9f, .8f );

			//go to main menu
			GameEngineApp.Instance.ControlManager.Controls.Add( new MainMenuWindow() );
			//}
			//else
			//{
			//   //go to product logo window
			//   GameEngineApp.Instance.ControlManager.Controls.Add( new ProductLogoWindow() );
			//}
		}

		protected override void OnRenderUI( GuiRenderer renderer )
		{
			base.OnRenderUI( renderer );

			Vec2 size = new Vec2( 512, 215.0f );
			size *= 1.0f + Time * .015f;
			size /= new Vec2( 768.0f * renderer.AspectRatio, 768.0f );

			Rect rectangle = new Rect( -size / 2, size / 2 ) + new Vec2( .5f, .5f );

			float alpha = 0;

			if( Time > 2 && Time <= 3 )
				alpha = Time - 2;
			else if( Time > 3 && Time <= lifeTime - 2 - 2 )
				alpha = 1;
			else if( Time >= lifeTime - 2 - 2 && Time < lifeTime - 1 )
				alpha = 1 - ( Time - ( lifeTime - 2 - 2 ) ) / 3;

			if( alpha != 0 )
				renderer.AddQuad( rectangle, new Rect( 0, 0, 1, 1 ), engineTexture,
					new ColorValue( 1, 1, 1, alpha ) );
		}

	}
}
