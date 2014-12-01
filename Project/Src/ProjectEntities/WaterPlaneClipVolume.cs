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
	/// Defines the <see cref="WaterPlaneClipVolume"/> entity type.
	/// </summary>
	public class WaterPlaneClipVolumeType : MapObjectType
	{
	}

	/// <summary>
	/// Addition class for WaterPlane.
	/// By this class is possible to disable reflections for objects inside specified volume.
	/// </summary>
	public class WaterPlaneClipVolume : MapObject
	{
		static List<WaterPlaneClipVolume> instances = new List<WaterPlaneClipVolume>();

		///////////////////////////////////////////

		WaterPlaneClipVolumeType _type = null; public new WaterPlaneClipVolumeType Type { get { return _type; } }

		[Browsable( false )]
		public static List<WaterPlaneClipVolume> Instances
		{
			get { return instances; }
		}

		protected override void OnPostCreate( bool loaded )
		{
			if( !instances.Contains( this ) )
				instances.Add( this );
			base.OnPostCreate( loaded );
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			instances.Remove( this );
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

			if( EntitySystemWorld.Instance.IsEditor() && EditorLayer.Visible ||
				EngineDebugSettings.DrawGameSpecificDebugGeometry )
			{
				if( camera.Purpose == Camera.Purposes.MainCamera )
				{
					camera.DebugGeometry.Color = new ColorValue( 1, 0, 0 );
					camera.DebugGeometry.AddBox( GetBox() );
				}
			}
		}

		public override bool Editor_CheckSelectionByRay( Ray ray, out float rayScale, ref float priority )
		{
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
			DynamicMeshManager.Block block = manager.GetBlockFromCacheOrCreate(
				"WaterPlaneClipVolume.Editor_RenderSelectionBorder: Box" );
			block.AddBox( false, new Box( Vec3.Zero, Vec3.One, Mat3.Identity ), null );

			Box box = GetBox();
			manager.AddBlockToScene( block, box.Center, box.Axis.ToQuat(), box.Extents, false, material );
		}
	}
}
