// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Renderer;
using Engine.Utils;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="Terminal"/> entity type.
	/// </summary>
	public class TerminalType : GameGuiObjectType
	{
	}

	public class Terminal : GameGuiObject
	{
		TerminalType _type = null; public new TerminalType Type { get { return _type; } }

		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			if( EntitySystemWorld.Instance.IsClientOnly() )
			{
				MapObjectAttachedObject attachedObject = GetFirstAttachedObjectByAlias( "clientNotSupported" );
				if( attachedObject != null )
					attachedObject.Visible = true;
			}
		}
	}
}
