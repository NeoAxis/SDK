// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.Utils;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="FreezeObjectsArea"/> entity type.
	/// </summary>
	public class FreezeObjectsAreaType : MapObjectType
	{
	}

	/// <summary>
	/// </summary>
	public class FreezeObjectsArea : MapObject
	{
		[FieldSerialize( "enabled" )]
		bool enabled = true;
		[FieldSerialize( "checkByCameraDistance" )]
		float checkByCameraDistance; //if 0, then no check by camera distance.
		[FieldSerialize( "areaType" )]
		AreaTypes areaType = AreaTypes.Unfreezing;

		[FieldSerialize( "showBorderInSimulation" )]
		bool showBorderInSimulation;

		//[FieldSerialize( "manageObjectsTicks" )]
		//bool manageObjectsTicks = true;
		//[FieldSerialize( "manageObjectsRendering" )]
		//bool manageObjectsRendering = true;

		static List<FreezeObjectsArea> instances = new List<FreezeObjectsArea>();

		float lastCheckTime;
		bool lastCheckUnfreezing;

		///////////////////////////////////////////

		public enum AreaTypes
		{
			Unfreezing,
			//Freezing,
		}

		///////////////////////////////////////////

		FreezeObjectsAreaType _type = null; public new FreezeObjectsAreaType Type { get { return _type; } }

		/// <summary>
		/// Gets or sets a value indicating whether the area enabled.
		/// </summary>
		[DefaultValue( true )]
		[LocalizedDisplayName( "Enabled", "FreezeObjectsArea" )]
		[LocalizedDescription( "A value indicating whether the area enabled.", "FreezeObjectsArea" )]
		public bool Enabled
		{
			get { return enabled; }
			set { enabled = value; }
		}

		/// <summary>
		/// Specifies the value for activation area by camera distance. Set \"0\" to disable checking by camera distance.
		/// </summary>
		[DefaultValue( 0.0f )]
		[LocalizedDisplayName( "Check By Camera Distance", "FreezeObjectsArea" )]
		[LocalizedDescription( "Specifies the value for activation area by camera distance. Set \"0\" to disable checking by camera distance.", "FreezeObjectsArea" )]
		public float CheckByCameraDistance
		{
			get { return checkByCameraDistance; }
			set { checkByCameraDistance = value; }
		}

		[DefaultValue( AreaTypes.Unfreezing )]
		[LocalizedDisplayName( "Area Type", "FreezeObjectsArea" )]
		public AreaTypes AreaType
		{
			get { return areaType; }
			set { areaType = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the area borders must be visible in Game.exe.
		/// </summary>
		[DefaultValue( false )]
		[LocalizedDisplayName( "Show Border In Simulation", "FreezeObjectsArea" )]
		[LocalizedDescription( "A value indicating whether the area borders must be visible in Game.exe.", "FreezeObjectsArea" )]
		public bool ShowBorderInSimulation
		{
			get { return showBorderInSimulation; }
			set { showBorderInSimulation = value; }
		}

		[Browsable( false )]
		public static List<FreezeObjectsArea> Instances
		{
			get { return instances; }
		}

		protected override void OnPostCreate( bool loaded )
		{
			if( !instances.Contains( this ) )
				instances.Add( this );

			base.OnPostCreate( loaded );

			_FreezeObjectsManagerNeverFreeze = true;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			instances.Remove( this );
		}

		protected override void OnSetTransform( ref Vec3 pos, ref Quat rot, ref Vec3 scl )
		{
			base.OnSetTransform( ref pos, ref rot, ref scl );

			if( FreezeObjectsManager.Instance != null )
				FreezeObjectsManager.Instance.SetNeedRegenerateGrid();
		}

		protected override void OnCalculateMapBounds( ref Bounds bounds )
		{
			base.OnCalculateMapBounds( ref bounds );
			bounds.Add( GetBox().ToBounds() );
		}

		/// <summary>Overridden from <see cref="Engine.MapSystem.MapObject.OnRender(Camera)"/>.</summary>
		protected override void OnRender( Camera camera )
		{
			base.OnRender( camera );

			bool show = false;
			if( ShowBorderInSimulation && EngineApp.Instance.ApplicationType == EngineApp.ApplicationTypes.Simulation )
				show = true;
			if( MapEditorInterface.Instance != null )//MapEditorInterface.Instance.IsEntitySelected( this ) )
				show = true;
			if( EntitySystemWorld.Instance.IsEditor() && !EditorLayer.Visible )
				show = false;

			if( show && camera == RendererWorld.Instance.DefaultCamera )
			//if( show && camera.Purpose == Camera.Purposes.MainCamera )
			{
				if( FreezeObjectsManager.Instance != null && FreezeObjectsManager.Instance.GridIsCreated() && Enabled )
					camera.DebugGeometry.Color = new ColorValue( 1, 0, 0 );
				else
					camera.DebugGeometry.Color = new ColorValue( 0, 0, 1 );
				camera.DebugGeometry.AddBox( GetBox() );
			}
		}

		public override bool Editor_CheckSelectionByRay( Ray ray, out float rayScale, ref float priority )
		{
			priority = .95f;

			//skip when inside the volume
			if( GetBox().IsContainsPoint( ray.Origin ) )
			{
				rayScale = 0;
				return false;
			}

			//select by the volume
			float scale1, scale2;
			if( GetBox().RayIntersection( ray, out scale1, out scale2 ) )
			{
				rayScale = Math.Min( scale1, scale2 );
				return true;
			}
			rayScale = 0;
			return false;
		}

		public override void Editor_RenderSelectionBorder( Camera camera, bool simpleGeometry, DynamicMeshManager manager,
			DynamicMeshManager.MaterialData material )
		{
			DynamicMeshManager.Block block = manager.GetBlockFromCacheOrCreate( "FreezeObjectsArea.Editor_RenderSelectionBorder: Box" );
			block.AddBox( false, new Box( Vec3.Zero, Vec3.One, Mat3.Identity ), null );

			Box box = GetBox();
			manager.AddBlockToScene( block, box.Center, box.Axis.ToQuat(), box.Extents, false, material );
		}

		internal void _UpdateLastCheckState( out bool unfreezing )
		{
			//update cached state
			Camera camera = RendererWorld.Instance.DefaultCamera;
			float time = RendererWorld.Instance.FrameRenderTime;
			if( lastCheckTime != time )
			{
				lastCheckTime = time;

				lastCheckUnfreezing = enabled;

				if( lastCheckUnfreezing && checkByCameraDistance != 0 )
				{
					Sphere s = new Sphere( camera.Position, checkByCameraDistance );
					Box box = GetBox();
					if( !s.IsIntersectsBox( ref box ) )
						lastCheckUnfreezing = false;
				}
			}

			unfreezing = lastCheckUnfreezing;
		}
	}
}
