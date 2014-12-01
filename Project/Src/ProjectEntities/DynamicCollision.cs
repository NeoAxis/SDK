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
	/// Defines the <see cref="DynamicCollision"/> entity type.
	/// </summary>
	public class DynamicCollisionType : MapObjectType
	{
	}

	/// <summary>
	/// Represents creation of dynamic obstacles. 
	/// By means of this class it is possible to set limiting area of movings for map objects.
	/// </summary>
	public class DynamicCollision : MapObject
	{
		[FieldSerialize]
		bool active = true;

		//

		DynamicCollisionType _type = null; public new DynamicCollisionType Type { get { return _type; } }

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			UpdatePhysicsModel();
		}

		[DefaultValue( true )]
		public bool Active
		{
			get { return active; }
			set
			{
				active = value;
				UpdatePhysicsModel();
			}
		}

		/// <summary>Overridden from <see cref="Engine.MapSystem.MapObject.OnSetTransform(ref Vec3,ref Quat,ref Vec3)"/>.</summary>
		protected override void OnSetTransform( ref Vec3 pos, ref Quat rot, ref Vec3 scl )
		{
			base.OnSetTransform( ref pos, ref rot, ref scl );
			UpdatePhysicsModel();
		}

		void UpdatePhysicsModel()
		{
			DestroyPhysicsModel();

			if( active )
			{
				CreatePhysicsModel();

				Body body = PhysicsModel.CreateBody();
				body.Static = true;
				body.SetTransform( Position, Rotation );

				BoxShape shape = body.CreateBoxShape();
				shape.ContactGroup = (int)ContactGroup.Dynamic;// Static;
				shape.Dimensions = Scale;

				body.PushedToWorld = true;
			}
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
					if( PhysicsModel != null )
					{
						foreach( Body body in PhysicsModel.Bodies )
							body.DebugRender( camera.DebugGeometry, 0, 1, true, true, new ColorValue( 1, 0, 0 ) );
					}
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
			DynamicMeshManager.Block block = manager.GetBlockFromCacheOrCreate( "DynamicCollision.Editor_RenderSelectionBorder: Box" );
			block.AddBox( false, new Box( Vec3.Zero, Vec3.One, Mat3.Identity ), null );

			Box box = GetBox();
			manager.AddBlockToScene( block, box.Center, box.Axis.ToQuat(), box.Extents, false, material );
		}
	}
}
