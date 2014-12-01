// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Renderer;
using Engine.Utils;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="GameCharacter"/> entity type.
	/// </summary>
	public class GameCharacterType : CharacterType
	{
		[FieldSerialize]
		[DefaultValue( typeof( Range ), "0 0" )]
		Range optimalAttackDistanceRange;

		///////////////////////////////////////////

		[DefaultValue( typeof( Range ), "0 0" )]
		public Range OptimalAttackDistanceRange
		{
			get { return optimalAttackDistanceRange; }
			set { optimalAttackDistanceRange = value; }
		}
	}

	public class GameCharacter : Character
	{
		GameCharacterType _type = null; public new GameCharacterType Type { get { return _type; } }

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			SubscribeToTickEvent();
		}

		protected override void OnRenderFrame()
		{
			UpdateAnimationTree();

			base.OnRenderFrame();
		}

		void UpdateAnimationTree()
		{
			if( EntitySystemWorld.Instance.Simulation && !EntitySystemWorld.Instance.SystemPauseOfSimulation )
			{
				AnimationTree tree = GetFirstAnimationTree();
				if( tree != null )
				{
					bool onGround = GetElapsedTimeSinceLastGroundContact() < .2f;//IsOnGround();

					bool move = false;
					Degree moveAngle = 0;
					float moveSpeed = 0;
					if( onGround && GroundRelativeVelocitySmooth.ToVec2().Length() > .05f )
					{
						move = true;
						Vec2 localVec = ( Rotation.GetInverse() * GroundRelativeVelocity ).ToVec2();
						Radian angle = MathFunctions.ATan( localVec.Y, localVec.X );
						moveAngle = angle.InDegrees();
						moveSpeed = GroundRelativeVelocitySmooth.ToVec2().Length();
					}

					tree.SetParameterValue( "move", move ? 1 : 0 );
					tree.SetParameterValue( "run", move && IsNeedRun() ? 1 : 0 );
					tree.SetParameterValue( "moveAngle", moveAngle );
					tree.SetParameterValue( "moveSpeed", moveSpeed );
					tree.SetParameterValue( "fly", !onGround ? 1 : 0 );
				}
			}
		}

		protected override void OnJump()
		{
			base.OnJump();

			//play jump animation
			foreach( AnimationTree tree in GetAllAnimationTrees() )
				tree.ActivateTrigger( "jump" );
		}
	}
}
