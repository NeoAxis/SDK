// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Windows.Forms;
using Engine;
using Engine.Renderer;
using Engine.MathEx;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.FileSystem;
using Engine.UISystem;
using Engine.SoundSystem;
using Engine.Utils;

namespace WinFormsAppFramework
{
	public class WinFormsAppEngineApp : EngineApp
	{
		static WinFormsAppEngineApp instance;

		bool automaticTicks = true;
		Timer tickTimer;

		//

		public WinFormsAppEngineApp( EngineApp.ApplicationTypes applicationType )
			: base( applicationType )
		{
			instance = this;
		}

		public static new WinFormsAppEngineApp Instance
		{
			get { return instance; }
		}

		protected override bool OnCreate()
		{
			if( !base.OnCreate() )
				return false;

			RendererWorld.Instance.DefaultCamera.Visible = false;

			ControlsWorld.Init();

			if( !EntitySystemWorld.Init( new EntitySystemWorld() ) )
				return false;

			if( automaticTicks )
				CreateTickTimer();

			return true;
		}

		protected override void OnDestroy()
		{
			DestroyTickTimer();

			EntitySystemWorld.Shutdown();
			ControlsWorld.Shutdown();

			base.OnDestroy();

			instance = null;
		}

		void CreateTickTimer()
		{
			DestroyTickTimer();

			const float fps = 80;
			float interval = ( 1.0f / fps ) * 1000.0f;
			tickTimer = new Timer();
			tickTimer.Interval = (int)interval;
			tickTimer.Tick += tickTimer_Tick;
			tickTimer.Enabled = true;
		}

		void DestroyTickTimer()
		{
			if( tickTimer != null )
			{
				tickTimer.Dispose();
				tickTimer = null;
			}
		}

		void tickTimer_Tick( object sender, EventArgs e )
		{
			DoTick();
		}

		protected override void OnTick( float delta )
		{
			base.OnTick( delta );

			//entity world tick
			EntitySystemWorldTick();
		}

		protected override void OnRenderFrame()
		{
			base.OnRenderFrame();
			
			RendererWorld.Instance.DefaultCamera.Visible = false;
		}

		public bool AutomaticTicks
		{
			get { return automaticTicks; }
			set
			{
				if( automaticTicks == value )
					return;

				automaticTicks = value;

				if( automaticTicks )
					CreateTickTimer();
				else
					DestroyTickTimer();
			}
		}

		public void EntitySystemWorldTick()
		{
			if( EntitySystemWorld.Instance != null )
				EntitySystemWorld.Instance.Tick();
		}
	}
}
