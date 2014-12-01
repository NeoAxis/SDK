// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="Switch"/> entity type.
	/// </summary>
	public class SwitchType : DynamicType
	{
		[FieldSerialize]
		string useMeshAlias;

		public string UseMeshAlias
		{
			get { return useMeshAlias; }
			set { useMeshAlias = value; }
		}
	}

	/// <summary>
	/// Base class for defines the user switches. (Booleans and quantitatives).
	/// </summary>
	public class Switch : Dynamic
	{
		MapObjectAttachedMesh useAttachedMesh;

		//

		SwitchType _type = null; public new SwitchType Type { get { return _type; } }

		public delegate void ValueChangeDelegate( Switch entity );

		[LogicSystemBrowsable( true )]
		public event ValueChangeDelegate ValueChange;

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			SubscribeToTickEvent();
			useAttachedMesh = GetFirstAttachedObjectByAlias( Type.UseMeshAlias ) as MapObjectAttachedMesh;
		}

		protected virtual void OnValueChange()
		{
			if( ValueChange != null )
				ValueChange( this );
		}

		[Browsable( false )]
		public MapObjectAttachedMesh UseAttachedMesh
		{
			get { return useAttachedMesh; }
		}

	}
}
