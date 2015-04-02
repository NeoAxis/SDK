// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.EntitySystem;
using Engine.MapSystem;

namespace ProjectEntities
{
	[Entity.Component.SupportedClass( typeof( Entity ) )]
	public class EntityComponent_Example : Entity.Component
	{
		//check if we can create resources for this component. IsReadyToCreate?
		//if( Enabled && Owner.IsPostCreated && ( !OnlyForEditor || EntitySystemWorld.Instance.IsEditor() ) && AddedToListOfComponents )

		[Entity.FieldSerialize]
		MapObject exampleReferenceToObject;
		[Entity.FieldSerialize]
		int exampleIntegerValue = 10;

		//

		public EntityComponent_Example( Entity owner, object userData )
			: base( owner, userData )
		{
		}

		protected override void OnDeleteSubscribedToDeletionEvent( Entity entity )
		{
			base.OnDeleteSubscribedToDeletionEvent( entity );

			//clear reference when entity was deleted
			if( exampleReferenceToObject == entity )
				exampleReferenceToObject = null;
		}

		[Category( "Example" )]
		public MapObject ExampleReferenceToObject
		{
			get { return exampleReferenceToObject; }
			set
			{
				//remove relationship with object to get event when entity was deleted. OnRelatedEntityDelete method.
				if( exampleReferenceToObject != null )
					Owner.UnsubscribeToDeletionEvent( exampleReferenceToObject );

				exampleReferenceToObject = value;

				//add relationship with object to get event when entity was deleted. OnRelatedEntityDelete method.
				if( exampleReferenceToObject != null )
					Owner.SubscribeToDeletionEvent( exampleReferenceToObject );
			}
		}

		[DefaultValue( 10 )]
		[Category( "Example" )]
		public int ExampleIntegerValue
		{
			get { return exampleIntegerValue; }
			set { exampleIntegerValue = value; }
		}
	}
}
