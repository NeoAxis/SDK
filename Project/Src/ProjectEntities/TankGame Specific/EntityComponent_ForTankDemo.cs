// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.EntitySystem;
using Engine.MapSystem;

namespace ProjectEntities
{
	public class EntityComponent_ForTankDemo : Entity.Component
	{
		//check if we can create resources for this component. IsReadyToCreate?
		//if( Enabled && Owner.IsPostCreated && ( !OnlyForEditor || EntitySystemWorld.Instance.IsEditor() ) && AddedToListOfComponents )

		[Entity.FieldSerialize]
		MapCurve way;
		[Entity.FieldSerialize]
		Region activateRegion;

		//

		public EntityComponent_ForTankDemo( Entity entity, object userData )
			: base( entity, userData )
		{
		}

		protected override void OnDeleteSubscribedToDeletionEvent( Entity entity )
		{
			base.OnDeleteSubscribedToDeletionEvent( entity );

			if( way == entity )
				way = null;
			if( activateRegion == entity )
				activateRegion = null;
		}

		public MapCurve Way
		{
			get { return way; }
			set
			{
				if( way != null )
					Owner.UnsubscribeToDeletionEvent( way );
				way = value;
				if( way != null )
					Owner.SubscribeToDeletionEvent( way );
			}
		}

		public Region ActivationRegion
		{
			get { return activateRegion; }
			set
			{
				if( activateRegion != null )
					Owner.UnsubscribeToDeletionEvent( activateRegion );
				activateRegion = value;
				if( activateRegion != null )
					Owner.SubscribeToDeletionEvent( activateRegion );
			}
		}
	}
}
