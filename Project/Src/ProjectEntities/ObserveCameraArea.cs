// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.Utils;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Renderer;
using Engine.PhysicsSystem;
using Engine.MathEx;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="ObserveCameraArea"/> entity type.
	/// </summary>
	public class ObserveCameraAreaType : MapObjectType
	{
	}

	public class ObserveCameraArea : MapObject
	{
		[FieldSerialize]
		MapCamera mapCamera;

		[FieldSerialize]
		MapCurve mapCurve;

		ObserveCameraAreaType _type = null; public new ObserveCameraAreaType Type { get { return _type; } }

		public MapCamera MapCamera
		{
			get { return mapCamera; }
			set
			{
				if( mapCamera != null )
					UnsubscribeToDeletionEvent( mapCamera );
				mapCamera = value;
				if( mapCamera != null )
					SubscribeToDeletionEvent( mapCamera );
			}
		}

		public MapCurve MapCurve
		{
			get { return mapCurve; }
			set
			{
				if( mapCurve != null )
					UnsubscribeToDeletionEvent( mapCurve );
				mapCurve = value;
				if( mapCurve != null )
					SubscribeToDeletionEvent( mapCurve );
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDeleteSubscribedToDeletionEvent(Entity)"/></summary>
		protected override void OnDeleteSubscribedToDeletionEvent( Entity entity )
		{
			base.OnDeleteSubscribedToDeletionEvent( entity );

			if( entity == mapCamera )
				mapCamera = null;
			if( entity == mapCurve )
				mapCurve = null;
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
					camera.DebugGeometry.Color = new ColorValue( 0, 0, 1 );
					camera.DebugGeometry.AddBox( GetBox() );
				}
			}
		}

	}
}
