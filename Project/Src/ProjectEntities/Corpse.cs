// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.EntitySystem;
using Engine.Renderer;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using ProjectCommon;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="Corpse"/> entity type.
	/// </summary>
	public class CorpseType : DynamicType
	{
	}

	/// <summary>
	/// Gives an opportunity to create corpses. A difference of a corpse from usual 
	/// object that he changes the orientation depending on a surface. 
	/// Also the class operates animations.
	/// </summary>
	public class Corpse : Dynamic
	{
		CorpseType _type = null; public new CorpseType Type { get { return _type; } }

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			SubscribeToTickEvent();

			//play death animation
			AnimationTree tree = GetFirstAnimationTree();
			if( tree != null )
				tree.ActivateTrigger( "death" );
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( PhysicsModel != null )
			{
				foreach( Body body in PhysicsModel.Bodies )
				{
					body.AngularVelocity = Vec3.Zero;

					Angles angles = Rotation.ToAngles();
					if( Math.Abs( angles.Roll ) > 30 || Math.Abs( angles.Pitch ) > 30 )
					{
						Quat oldRotation = body.OldRotation;
						body.Rotation = new Angles( 0, 0, angles.Yaw ).ToQuat();
						body.OldRotation = oldRotation;
					}
				}
			}
		}
	}
}
