// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Engine.Renderer;

namespace WinFormsMultiViewAppExample
{
	public partial class SplashForm : Form
	{
		static SplashForm instance;

		float time;
		bool allowClose;

		public static SplashForm Instance
		{
			get { return instance; }
		}

		public SplashForm( Image image )
		{
			instance = this;
			InitializeComponent();

			Size = image.Size;
			BackgroundImage = image;
		}

		public bool AllowClose
		{
			get { return allowClose; }
			set
			{
				if( value && !allowClose )
					timer1.Start();

				allowClose = value;
			}
		}

		private void timer1_Tick( object sender, EventArgs e )
		{
			time += (float)timer1.Interval / 1000.0f;

			bool allowAlpha = false;
			{
				if( RenderSystem.Instance != null && RenderSystem.Instance.IsDirect3D() )
				{
					if( RenderSystem.Instance.GPUIsGeForce() || RenderSystem.Instance.GPUIsRadeon() )
						allowAlpha = true;
				}
			}

			float opacity = 1.0f;

			if( allowAlpha )
			{
				if( time > 0 )
					opacity = ( 1.0f - time ) / 1;
				if( opacity < 0 )
					opacity = 0;
			}

			if( Opacity != opacity )
				Opacity = opacity;

			if( time > 1 )
			{
				timer1.Stop();
				Close();
			}

		}

		private void SplashForm_FormClosed( object sender, FormClosedEventArgs e )
		{
			instance = null;
		}
	}
}