// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.UISystem;
using Engine.Renderer;
using Engine.SoundSystem;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="CutSceneManager"/> entity type.
	/// </summary>
	[AllowToCreateTypeBasedOnThisClass( false )]
	public class CutSceneManagerType : MapGeneralObjectType
	{
		public CutSceneManagerType()
		{
			UniqueEntityInstance = true;
			AllowEmptyName = true;
		}
	}

	/// <summary>
	/// The manager of cut scenes. 
	/// That there was an opportunity to create a cut scenes on a map, 
	/// it is necessary to create in the Map Editor object "CutSceneManager".
	/// </summary>
	[LogicSystemBrowsable( true )]
	[LogicSystemCallStaticOverInstance]
	public class CutSceneManager : MapGeneralObject
	{
		static CutSceneManager instance;

		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		bool cutSceneEnable;

		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		MapCamera camera;
		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		MapCameraCurve cameraCurve;

		float lastTickTime;

		float oldCameraCurveTime;
		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		float cameraCurveTime;

		//fading
		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		float fadeCoefficient;

		enum FadeTask
		{
			None,
			In,
			Out,
			InOut
		}
		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		FadeTask fadeTask = FadeTask.None;

		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		float fadeTimeIn;
		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		float fadeTimeOut;

		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		string messageText;
		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		float messageRemainingTime;
		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		ColorValue messageColor;

		//

		CutSceneManagerType _type = null; public new CutSceneManagerType Type { get { return _type; } }

		public static CutSceneManager Instance
		{
			get { return instance; }
		}

		public CutSceneManager()
		{
			if( instance != null )
				Log.Fatal( "CutSceneManager: instance already created" );
			instance = this;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			if( instance == this )//for undo support
				instance = this;

			base.OnPostCreate( loaded );

			SubscribeToTickEvent();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			base.OnDestroy();

			if( instance == this )//for undo support
				instance = null;
		}

		public delegate void CutSceneEnableChangeDelegate( CutSceneManager manager );
		public event CutSceneEnableChangeDelegate CutSceneEnableChange;

		[LogicSystemBrowsable( true )]
		[Browsable( false )]
		public bool CutSceneEnable
		{
			get { return cutSceneEnable; }
			set
			{
				cutSceneEnable = value;
				ResetCamera();
				if( CutSceneEnableChange != null )
					CutSceneEnableChange( this );
			}
		}

		[LogicSystemBrowsable( true )]
		public void SetCamera( MapObject cameraOrCameraCurve )
		{
			camera = cameraOrCameraCurve as MapCamera;
			cameraCurve = cameraOrCameraCurve as MapCameraCurve;
			if( cameraCurve != null )
				cameraCurveTime = cameraCurve.GetCurveTimeRange().Minimum;
			oldCameraCurveTime = cameraCurveTime;
		}

		[LogicSystemBrowsable( true )]
		public void ResetCamera()
		{
			camera = null;
			cameraCurve = null;
		}

		float GetCameraCurveInterpolatedTime()
		{
			if( cameraCurveTime == oldCameraCurveTime )
				return cameraCurveTime;

			float t;

			float renderTime = RendererWorld.Instance.FrameRenderTime;
			float time = ( renderTime - lastTickTime ) * EntitySystemWorld.Instance.GameFPS;
			if( time < 0 ) time = 0;
			if( time < 1.0f )
				t = oldCameraCurveTime * ( 1.0f - time ) + cameraCurveTime * time;
			else
				t = cameraCurveTime;

			return t;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( CutSceneEnable )
			{
				if( lastTickTime != Entities.Instance.TickTime )
				{
					//OnUpdateOldTransform();
					lastTickTime = Entities.Instance.TickTime;
					oldCameraCurveTime = cameraCurveTime;
				}

				if( cameraCurve != null )
				{
					cameraCurveTime += TickDelta;
					if( cameraCurveTime > cameraCurve.GetCurveTimeRange().Maximum )
						cameraCurveTime = cameraCurve.GetCurveTimeRange().Maximum;
				}
			}

			TickFade();
			TickMessage();
		}

		void TickFade()
		{
			//TO DO: no interpolated for smooth rendering
			switch( fadeTask )
			{
			case FadeTask.In:
				fadeCoefficient += ( 1.0f / fadeTimeIn ) * TickDelta;
				if( fadeCoefficient >= 1 )
				{
					fadeCoefficient = 1;
					fadeTask = FadeTask.None;
				}
				break;

			case FadeTask.Out:
				fadeCoefficient -= ( 1.0f / fadeTimeOut ) * TickDelta;
				if( fadeCoefficient <= 0 )
				{
					fadeCoefficient = 0;
					fadeTask = FadeTask.None;
				}
				break;

			case FadeTask.InOut:
				fadeCoefficient += ( 1.0f / fadeTimeIn ) * TickDelta;
				if( fadeCoefficient >= 1 )
				{
					fadeCoefficient = 1;
					fadeTask = FadeTask.Out;
				}
				break;
			}
		}

		void TickMessage()
		{
			if( messageText != null )
			{
				messageRemainingTime -= TickDelta;
				if( messageRemainingTime <= 0 )
					messageText = null;
			}
		}

		public bool GetCamera( out Vec3 position, out Vec3 forward, out Vec3 up, out Degree fov )
		{
			if( camera != null )
			{
				position = camera.Position;
				forward = camera.Rotation * new Vec3( 1, 0, 0 );
				up = camera.Rotation * new Vec3( 0, 0, 1 );
				fov = camera.Fov;
				return true;
			}

			if( cameraCurve != null )
			{
				cameraCurve.CalculateCameraPositionByTime( GetCameraCurveInterpolatedTime(), out position,
					out forward, out up, out fov );
				return true;
			}

			position = Vec3.Zero;
			forward = Vec3.XAxis;
			up = Vec3.ZAxis;
			fov = 0;
			return false;
		}

		[LogicSystemBrowsable( true )]
		public void FadeIn( float time )
		{
			fadeTask = FadeTask.In;
			fadeTimeIn = time;
			if( fadeTimeIn == 0 )
				fadeTimeIn = 100000;
		}

		[LogicSystemBrowsable( true )]
		public void FadeOut( float time )
		{
			fadeTask = FadeTask.Out;
			fadeTimeOut = time;
			if( fadeTimeOut == 0 )
				fadeTimeOut = 100000;
		}

		[LogicSystemBrowsable( true )]
		public void FadeInOut( float timeIn, float timeOut )
		{
			fadeTask = FadeTask.InOut;
			fadeTimeIn = timeIn;
			fadeTimeOut = timeOut;
			if( fadeTimeIn == 0 )
				fadeTimeIn = 100000;
			if( fadeTimeOut == 0 )
				fadeTimeOut = 100000;
		}

		public float GetFadeCoefficient()
		{
			return fadeCoefficient;
		}

		[LogicSystemBrowsable( true )]
		public void PlaySound( string name )
		{
			if( string.IsNullOrEmpty( name ) )
				return;
			Sound sound = SoundWorld.Instance.SoundCreate( name, 0 );
			if( sound == null )
				return;
			SoundWorld.Instance.SoundPlay( sound, EngineApp.Instance.DefaultSoundChannelGroup, .5f );
		}

		[LogicSystemBrowsable( true )]
		public void SetMessage( string text, float time, ColorValue color )
		{
			messageText = text;
			messageRemainingTime = time;
			messageColor = color;
		}

		public bool GetMessage( out string text, out ColorValue color )
		{
			if( messageText != null )
			{
				text = messageText;
				color = messageColor;
				return true;
			}
			else
			{
				text = null;
				color = new ColorValue( 0, 0, 0 );
				return false;
			}
		}

		//old method. is not supported anymore.
		[LogicSystemBrowsable( true )]
		public void PlayObjectAnimation( MapObject obj, string animationName )
		{
			//if( obj == null )
			//   Log.Fatal( "CutSceneManager: PlayObjectAnimation: obj == null." );

			//if( obj.IsSetDeleted )
			//   return;

			////The class is not supported
			//Log.Fatal( "CutSceneManager: PlayObjectAnimation: The class \"{0}\" is not supported.",
			//   obj.GetType().Name );
		}

	}
}
