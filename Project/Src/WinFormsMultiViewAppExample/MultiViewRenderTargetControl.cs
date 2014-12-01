// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.ComponentModel;
using Engine;
using Engine.Utils;
using Engine.MathEx;
using Engine.Renderer;
using WinFormsAppFramework;

namespace WinFormsMultiViewAppExample
{
	public class MultiViewRenderTargetControl : Control
	{
		View[] views = new View[ 0 ];

		///////////////////////////////////////////

		public delegate void TickDelegate( MultiViewRenderTargetControl sender, float delta );
		public event TickDelegate Tick;

		public delegate void RenderDelegate( MultiViewRenderTargetControl sender, int viewIndex,
			Camera camera );
		public event RenderDelegate Render;

		public delegate void RenderUIDelegate( MultiViewRenderTargetControl sender, int viewIndex,
			GuiRenderer renderer );
		public event RenderUIDelegate RenderUI;

		///////////////////////////////////////////

		public class View
		{
			MultiViewRenderTargetControl owner;
			Rect rectangle;
			RenderTargetUserControl control;

			internal View( MultiViewRenderTargetControl owner, Rect rectangle, RenderTargetUserControl control )
			{
				this.owner = owner;
				this.rectangle = rectangle;
				this.control = control;
			}

			public Rect Rectangle
			{
				get { return rectangle; }
			}

			public RenderTargetUserControl Control
			{
				get { return control; }
			}

			internal void UpdateControlLocationSize()
			{
				if( control != null && owner.Size.Width > 0 && owner.Size.Height > 0 )
				{
					Vec2 ownerSize = new Vec2( owner.Size.Width, owner.Size.Height );
					control.Location = new System.Drawing.Point(
						(int)( rectangle.Left * ownerSize.X ),
						(int)( rectangle.Top * ownerSize.Y ) );

					System.Drawing.Size size = new System.Drawing.Size(
						(int)( rectangle.GetSize().X * ownerSize.X ),
						(int)( rectangle.GetSize().Y * ownerSize.Y ) );
					if( size.Width < 1 )
						size.Width = 1;
					if( size.Height < 1 )
						size.Height = 1;
					control.Size = size;
				}
			}
		}

		///////////////////////////////////////////

		[Browsable( false )]
		public View[] Views
		{
			get { return views; }
		}

		public void SetViewsConfiguration( Rect[] rectangles )
		{
			DestroyViews();

			views = new View[ rectangles.Length ];

			for( int viewIndex = 0; viewIndex < views.Length; viewIndex++ )
			{
				RenderTargetUserControl control = new RenderTargetUserControl();
				Controls.Add( control );

				View view = new View( this, rectangles[ viewIndex ], control );
				views[ viewIndex ] = view;

				view.UpdateControlLocationSize();

				if( viewIndex == 0 )
					view.Control.Tick += Control_Tick;
				view.Control.Render += Control_Render;
				view.Control.RenderUI += Control_RenderUI;
			}
		}

		void Control_Tick( RenderTargetUserControl sender, float delta )
		{
			if( Tick != null )
				Tick( this, delta );
		}

		public int GetViewIndexByControl( RenderTargetUserControl control )
		{
			for( int n = 0; n < views.Length; n++ )
			{
				if( views[ n ].Control == control )
					return n;
			}
			return -1;
		}

		void Control_Render( RenderTargetUserControl sender, Camera camera )
		{
			if( Render != null )
			{
				int viewIndex = GetViewIndexByControl( sender );
				if( viewIndex != -1 )
					Render( this, viewIndex, camera );
			}
		}

		void Control_RenderUI( RenderTargetUserControl sender, GuiRenderer renderer )
		{
			if( RenderUI != null )
			{
				int viewIndex = GetViewIndexByControl( sender );
				if( viewIndex != -1 )
					RenderUI( this, viewIndex, renderer );
			}
		}

		public void DestroyViews()
		{
			foreach( View view in views )
				view.Control.Destroy();
			views = new View[ 0 ];
		}

		public void Destroy()
		{
			DestroyViews();
		}

		protected override void OnResize( EventArgs e )
		{
			base.OnResize( e );

			foreach( View view in views )
				view.UpdateControlLocationSize();
		}

		public void SetAutomaticUpdateFPSForAllViews( float value )
		{
			foreach( View view in views )
				view.Control.AutomaticUpdateFPS = value;
		}
	}
}
