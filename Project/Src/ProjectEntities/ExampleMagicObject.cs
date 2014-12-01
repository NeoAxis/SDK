// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using Engine.MathEx;
using Engine.Renderer;
using Engine.MapSystem;
using Engine.PhysicsSystem;

namespace ProjectEntities
{
	public class ExampleMagicObjectType : DynamicType
	{
		[FieldSerialize]
		string blinkMaterialName;

		/// <summary>
		/// Gets or sets the name of a replacing material.
		/// </summary>
		[Editor( typeof( EditorMaterialUITypeEditor ), typeof( UITypeEditor ) )]
		public string BlinkMaterialName
		{
			get { return blinkMaterialName; }
			set { blinkMaterialName = value; }
		}
	}

	public class ExampleMagicObject : Dynamic
	{
		ExampleMagicObjectType _type = null; public new ExampleMagicObjectType Type { get { return _type; } }

		MeshObject blinkMeshObject;
		string originalMaterialName;

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			//To find the first attached mesh
			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				MapObjectAttachedMesh attachedMesh = attachedObject as MapObjectAttachedMesh;
				if( attachedMesh != null )
				{
					blinkMeshObject = attachedMesh.MeshObject;
					break;
				}
			}

			//To save the original name of a material
			if( blinkMeshObject != null )
				originalMaterialName = blinkMeshObject.SubObjects[ 0 ].MaterialName;
		}

		//This method is called when the entity receives damages
		protected override void OnDamage( MapObject prejudicial, Vec3 pos, Shape shape, float damage,
			bool allowMoveDamageToParent )
		{
			base.OnDamage( prejudicial, pos, shape, damage, allowMoveDamageToParent );

			if( blinkMeshObject != null )
			{
				//To change a material
				if( blinkMeshObject.SubObjects[ 0 ].MaterialName == originalMaterialName )
					blinkMeshObject.SetMaterialNameForAllSubObjects( Type.BlinkMaterialName );
				else
					blinkMeshObject.SetMaterialNameForAllSubObjects( originalMaterialName );
			}
		}

	}
}
