// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.Utils;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.Renderer;
using Engine.MathEx;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="Minefield"/> entity type.
	/// </summary>
	public class MinefieldType : MapObjectType
	{
	}

	public class Minefield : MapObject
	{
		static List<Minefield> instances = new List<Minefield>();

		//

		MinefieldType _type = null; public new MinefieldType Type { get { return _type; } }

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			instances.Add( this );
			base.OnPostCreate( loaded );
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			base.OnDestroy();
			instances.Remove( this );
		}

		/// <summary>Overridden from <see cref="Engine.MapSystem.MapObject.OnCalculateMapBounds(ref Bounds)"/>.</summary>
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
			DynamicMeshManager.Block block = manager.GetBlockFromCacheOrCreate( "Minefield.Editor_RenderSelectionBorder: Box" );
			block.AddBox( false, new Box( Vec3.Zero, Vec3.One, Mat3.Identity ), null );

			Box box = GetBox();
			manager.AddBlockToScene( block, box.Center, box.Axis.ToQuat(), box.Extents, false, material );
		}

		public static Minefield GetMinefieldByPosition( Vec3 position )
		{
			//TO DO: slow for many instances

			foreach( Minefield minefield in instances )
			{
				if( minefield.MapBounds.IsContainsPoint( position ) &&
					minefield.GetBox().IsContainsPoint( position ) )
				{
					return minefield;
				}
			}
			return null;
		}
	}
}
