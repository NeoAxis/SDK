// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.Renderer;
using Engine.Utils;
using ProjectCommon;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="Intellect"/> entity type.
	/// </summary>
	public abstract class IntellectType : EntityType
	{
	}

	/// <summary>
	/// This takes the form of either AI (Artificial Intelligence) or player control over a unit .
	/// </summary>
	/// <remarks>
	/// <para>
	/// There is inherit AI base base for an computer-controlled intellect. 
	/// For example, there is the <see cref="GameCharacterAI"/> class which is designed for the 
	/// management of a game character.
	/// </para>
	/// <para>
	/// Control by a live player (<see cref="PlayerIntellect"/>) is achieved through the commands 
	/// of pressed keys or the mouse for control of the unit or turret.
	/// </para>
	/// </remarks>
	public abstract class Intellect : Entity
	{
		[FieldSerialize]
		Unit controlledObject;

		static int controlKeyCount;
		float[] controlKeysStrength;

		[FieldSerialize]
		FactionType faction;

		[FieldSerialize]
		bool allowTakeItems;

		///////////////////////////////////////////

		public struct Command
		{
			GameControlKeys key;
			bool keyPressed;

			internal Command( GameControlKeys key, bool keyPressed )
			{
				this.key = key;
				this.keyPressed = keyPressed;
			}

			public GameControlKeys Key
			{
				get { return key; }
			}

			public bool KeyPressed
			{
				get { return keyPressed; }
			}

			public bool KeyReleased
			{
				get { return !keyPressed; }
			}
		}

		///////////////////////////////////////////

		enum NetworkMessages
		{
			ControlledObjectToClient,
		}

		///////////////////////////////////////////

		IntellectType _type = null; public new IntellectType Type { get { return _type; } }

		public Intellect()
		{
			//calculate controlKeyCount
			if( controlKeyCount == 0 )
			{
				foreach( object value in Enum.GetValues( typeof( GameControlKeys ) ) )
				{
					GameControlKeys controlKey = (GameControlKeys)value;
					if( (int)controlKey >= controlKeyCount )
						controlKeyCount = (int)controlKey + 1;
				}
			}

			controlKeysStrength = new float[ controlKeyCount ];
		}

		public float GetControlKeyStrength( GameControlKeys key )
		{
			return controlKeysStrength[ (int)key ];
		}

		public bool IsControlKeyPressed( GameControlKeys key )
		{
			return GetControlKeyStrength( key ) != 0.0f;
		}

		protected virtual void OnControlledObjectChange( Unit oldObject ) { }

		[Browsable( false )]
		public Unit ControlledObject
		{
			get { return controlledObject; }
			set
			{
				Unit oldObject = controlledObject;

				if( controlledObject != null )
					UnsubscribeToDeletionEvent( controlledObject );
				controlledObject = value;
				if( controlledObject != null )
					SubscribeToDeletionEvent( controlledObject );
				ResetControlKeys();

				OnControlledObjectChange( oldObject );

				if( EntitySystemWorld.Instance.IsServer() &&
					Type.NetworkType == EntityNetworkTypes.Synchronized )
				{
					Server_SendControlledObjectToClients(
						EntitySystemWorld.Instance.RemoteEntityWorlds );
				}
			}
		}

		[Browsable( false )]
		[LogicSystemBrowsable( true )]
		public FactionType Faction
		{
			get { return faction; }
			set { faction = value; }
		}

		protected override void OnDestroy()
		{
			ControlledObject = null;
			base.OnDestroy();
		}

		protected void ControlKeyPress( GameControlKeys controlKey, float strength )
		{
			if( strength <= 0.0f )
				Log.Fatal( "Intellect: ControlKeyPress: Invalid \"strength\"." );

			if( GetControlKeyStrength( controlKey ) == strength )
				return;

			controlKeysStrength[ (int)controlKey ] = strength;

			if( controlledObject != null )
				controlledObject.DoIntellectCommand( new Command( controlKey, true ) );
		}

		protected void ControlKeyRelease( GameControlKeys controlKey )
		{
			if( !IsControlKeyPressed( controlKey ) )
				return;

			controlKeysStrength[ (int)controlKey ] = 0;

			if( controlledObject != null )
				controlledObject.DoIntellectCommand( new Command( controlKey, false ) );
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDeleteSubscribedToDeletionEvent(Entity)"/></summary>
		protected override void OnDeleteSubscribedToDeletionEvent( Entity entity )
		{
			base.OnDeleteSubscribedToDeletionEvent( entity );

			if( controlledObject == entity )
				ControlledObject = null;
		}

		void ResetControlKeys()
		{
			for( int n = 0; n < controlKeysStrength.Length; n++ )
				controlKeysStrength[ n ] = 0;
		}

		protected virtual void OnControlledObjectRenderFrame() { }
		public void DoControlledObjectRenderFrame()
		{
			OnControlledObjectRenderFrame();
		}

		protected virtual void OnControlledObjectRender( Camera camera ) { }
		public void DoControlledObjectRender( Camera camera )
		{
			OnControlledObjectRender( camera );
		}

		public virtual bool IsActive()
		{
			return false;
		}

		public bool AllowTakeItems
		{
			get { return allowTakeItems; }
			set { allowTakeItems = value; }
		}

		protected override void Server_OnClientConnectedBeforePostCreate(
			RemoteEntityWorld remoteEntityWorld )
		{
			base.Server_OnClientConnectedBeforePostCreate( remoteEntityWorld );

			Server_SendControlledObjectToClients( new RemoteEntityWorld[] { remoteEntityWorld } );
		}

		void Server_SendControlledObjectToClients( IList<RemoteEntityWorld> remoteEntityWorlds )
		{
			SendDataWriter writer = BeginNetworkMessage( remoteEntityWorlds, typeof( Intellect ),
				(ushort)NetworkMessages.ControlledObjectToClient );
			//zero will sent if controlled object NetworkType != Synchronized
			writer.WriteVariableUInt32( ControlledObject != null ?
				ControlledObject.NetworkUIN : (uint)0 );
			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.ControlledObjectToClient )]
		void Client_ReceiveControlledObject( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			uint networkUIN = reader.ReadVariableUInt32();
			if( !reader.Complete() )
				return;

			if( networkUIN != 0 )
				ControlledObject = (Unit)Entities.Instance.GetByNetworkUIN( networkUIN );
			else
				ControlledObject = null;
		}

		public virtual bool IsAlwaysRun()
		{
			return false;
		}

	}
}
