// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.EntitySystem;
using Engine.MapSystem;

namespace ProjectEntities
{
	public class ExampleEntityExtendedProperties : EntityExtendedProperties
	{
		[FieldSerialize]
		MapObject exampleReferenceToObject;
		[FieldSerialize]
		int exampleIntegerValue = 10;

		//

		protected override void OnDeleteSubscribedToDeletionEvent( Entity entity )
		{
			base.OnDeleteSubscribedToDeletionEvent( entity );

			//clear reference when entity was deleted
			if( exampleReferenceToObject == entity )
				exampleReferenceToObject = null;
		}

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
		public int ExampleIntegerValue
		{
			get { return exampleIntegerValue; }
			set { exampleIntegerValue = value; }
		}
	}
}
