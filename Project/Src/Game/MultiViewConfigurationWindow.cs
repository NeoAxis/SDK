// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.Renderer;
using Engine.UISystem;
using Engine.MathEx;
using Engine.Utils;
using Engine.MapSystem;
using ProjectCommon;

namespace Game
{
	public class MultiViewConfigurationWindow : Control
	{
		static ViewConfigurations lastViewsConfiguration = ViewConfigurations.NoViews;
		Control window;
		ListBox listBoxViews;
		CheckBox checkBoxShowMainScene;
		ScrollBar scrollBarViewsOpacity;
		CheckBox checkBoxDrawDebugInfo;

		///////////////////////////////////////////

		public enum ViewConfigurations
		{
			NoViews,
			SplitByScreens,
			Three3x1,
			Four2x2,
		}

		///////////////////////////////////////////

		protected override void OnAttach()
		{
			base.OnAttach();

			//create window
			window = ControlDeclarationManager.Instance.CreateControl( "Gui\\MultiViewConfigurationWindow.gui" );
			Controls.Add( window );

			//update views list box
			listBoxViews = (ListBox)window.Controls[ "ViewList" ];
			listBoxViews.Items.Clear();
			foreach( ViewConfigurations configuration in Enum.GetValues( typeof( ViewConfigurations ) ) )
				listBoxViews.Items.Add( configuration.ToString() );
			listBoxViews.SelectedIndex = (int)lastViewsConfiguration;
			listBoxViews.SelectedIndexChange += listBoxViews_SelectedIndexChange;

			//showMainScene checkbox
			checkBoxShowMainScene = (CheckBox)window.Controls[ "ShowMainScene" ];
			if( MultiViewRenderingManager.Instance != null )
				checkBoxShowMainScene.Checked = MultiViewRenderingManager.Instance.MainCameraDraw3DScene;
			checkBoxShowMainScene.CheckedChange += checkBoxShowMainScene_CheckedChange;
			checkBoxShowMainScene.Enable = listBoxViews.SelectedIndex != 0;

			//view opacity
			scrollBarViewsOpacity = (ScrollBar)window.Controls[ "ViewsOpacity" ];
			if( MultiViewRenderingManager.Instance != null && MultiViewRenderingManager.Instance.Views.Count > 0 )
				scrollBarViewsOpacity.Value = MultiViewRenderingManager.Instance.Views[ 0 ].Opacity;
			scrollBarViewsOpacity.ValueChange += scrollBarViewsOpacity_ValueChange;
			scrollBarViewsOpacity.Enable = listBoxViews.SelectedIndex != 0;

			checkBoxDrawDebugInfo = (CheckBox)window.Controls[ "DrawDebugInfo" ];
			if( MultiViewRenderingManager.Instance != null )
				checkBoxDrawDebugInfo.Checked = MultiViewRenderingManager.Instance.DrawDebugInfo;
			checkBoxDrawDebugInfo.CheckedChange += checkBoxDrawDebugInfo_CheckedChange;
			checkBoxDrawDebugInfo.Enable = listBoxViews.SelectedIndex != 0;

			( (Button)window.Controls[ "Close" ] ).Click += Close_Click;

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

		void Close()
		{
			SetShouldDetach();
		}

		void Close_Click( Button sender )
		{
			Close();
		}

		void listBoxViews_SelectedIndexChange( ListBox sender )
		{
			SetViewConfiguration( (ViewConfigurations)listBoxViews.SelectedIndex );

			checkBoxShowMainScene.Enable = listBoxViews.SelectedIndex != 0;
			scrollBarViewsOpacity.Enable = listBoxViews.SelectedIndex != 0;
			checkBoxDrawDebugInfo.Enable = listBoxViews.SelectedIndex != 0;
		}

		void checkBoxShowMainScene_CheckedChange( CheckBox sender )
		{
			bool drawMainScene = listBoxViews.SelectedIndex == 0 || checkBoxShowMainScene.Checked;
			if( MultiViewRenderingManager.Instance != null )
				MultiViewRenderingManager.Instance.MainCameraDraw3DScene = drawMainScene;
		}

		void scrollBarViewsOpacity_ValueChange( ScrollBar sender )
		{
			if( MultiViewRenderingManager.Instance != null )
			{
				foreach( MultiViewRenderingManager.View view in MultiViewRenderingManager.Instance.Views )
					view.Opacity = scrollBarViewsOpacity.Value;
			}
		}

		void checkBoxDrawDebugInfo_CheckedChange( CheckBox sender )
		{
			if( MultiViewRenderingManager.Instance != null )
				MultiViewRenderingManager.Instance.DrawDebugInfo = checkBoxDrawDebugInfo.Checked;
		}

		void SetViewConfiguration( ViewConfigurations configuration )
		{
			lastViewsConfiguration = configuration;

			if( configuration != ViewConfigurations.NoViews )
			{
				if( MultiViewRenderingManager.Instance == null )
					MultiViewRenderingManager.Init();

				MultiViewRenderingManager.Instance.RemoveAllViews();

				switch( configuration )
				{
				case ViewConfigurations.SplitByScreens:
					if( !EngineApp.Instance.MultiMonitorMode && EngineApp.Instance.AllDisplays.Count > 1 )
					{
						string text = LanguageManager.Instance.Translate( "UISystem",
							"To run engine on multi monitor system activate video " +
							"mode \"Use all displays\" in the Configurator.exe." );
						Log.Warning( text );
					}
					else
					{
						RectI totalBounds = RectI.Cleared;
						foreach( DisplayInfo display in EngineApp.Instance.AllDisplays )
							totalBounds.Add( display.Bounds );

						foreach( DisplayInfo display in EngineApp.Instance.AllDisplays )
						{
							Rect rectangle = ( display.Bounds - totalBounds.LeftTop ).ToRect() /
								EngineApp.Instance.VideoMode.ToVec2();
							MultiViewRenderingManager.Instance.AddView( rectangle );
						}
					}
					break;

				case ViewConfigurations.Three3x1:
					MultiViewRenderingManager.Instance.AddView( new Rect( 0, 0, .331f, 1 ) );
					MultiViewRenderingManager.Instance.AddView( new Rect( .335f, 0, .664f, 1 ) );
					MultiViewRenderingManager.Instance.AddView( new Rect( .668f, 0, 1, 1 ) );
					break;

				case ViewConfigurations.Four2x2:
					MultiViewRenderingManager.Instance.AddView( new Rect( 0, 0, .498f, .495f ) );
					MultiViewRenderingManager.Instance.AddView( new Rect( .502f, 0, 1, .495f ) );
					MultiViewRenderingManager.Instance.AddView( new Rect( 0, .505f, .498f, 1 ) );
					MultiViewRenderingManager.Instance.AddView( new Rect( .502f, .505f, 1, 1 ) );
					break;
				}

				MultiViewRenderingManager.Instance.MainCameraDraw3DScene = checkBoxShowMainScene.Checked;
				MultiViewRenderingManager.Instance.DrawDebugInfo = checkBoxDrawDebugInfo.Checked;

				foreach( MultiViewRenderingManager.View view in MultiViewRenderingManager.Instance.Views )
				{
					view.Opacity = scrollBarViewsOpacity.Value;
					view.Render += view_Render;
				}
			}
			else
			{
				MultiViewRenderingManager.Shutdown();
			}
		}

		static void view_Render( MultiViewRenderingManager.View view, Camera camera )
		{
			Camera defaultCamera = RendererWorld.Instance.DefaultCamera;
			Viewport defaultViewport = RendererWorld.Instance.DefaultViewport;

			//set up camera
			//camera.ProjectionType = defaultCamera.ProjectionType;
			//camera.OrthoWindowHeight = defaultCamera.OrthoWindowHeight;
			//camera.NearClipDistance = defaultCamera.NearClipDistance;
			//camera.FarClipDistance = defaultCamera.FarClipDistance;
			//camera.Fov = defaultCamera.Fov;
			//camera.FixedUp = defaultCamera.FixedUp;
			//camera.Direction = defaultCamera.Direction;
			//camera.Position = defaultCamera.Position;

			int index = MultiViewRenderingManager.Instance.Views.IndexOf( view );

			//set Orthographic camera for second and third views
			if( index == 1 )
			{
				camera.ProjectionType = ProjectionTypes.Orthographic;
				camera.OrthoWindowHeight = 50;
				camera.FixedUp = new Vec3( 0, 0, 1 );
				camera.Direction = new Vec3( 0, -1, 0 );
				camera.Position = defaultCamera.Position + new Vec3( 0, 50, 0 );
			}
			if( index == 2 )
			{
				camera.ProjectionType = ProjectionTypes.Orthographic;
				camera.OrthoWindowHeight = 70;
				camera.FixedUp = new Vec3( 0, 1, 0 );
				camera.Direction = new Vec3( 0, 0, -1 );
				camera.Position = defaultCamera.Position + new Vec3( 0, 0, 70 );
			}

			//set up material scheme for viewport
			view.Viewport.MaterialScheme = defaultViewport.MaterialScheme;
		}
	}
}
