// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using Engine;
using Engine.UISystem;
using Engine.MapSystem;
using Engine.MathEx;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="GameGuiObject"/> entity type.
	/// </summary>
	public class GameGuiObjectType : DynamicType
	{
	}

	public class GameGuiObject : Dynamic
	{
		GameGuiObjectType _type = null; public new GameGuiObjectType Type { get { return _type; } }

		[FieldSerialize]
		string initialControl = "";

		MapObjectAttachedGui attachedGuiObject;
		In3dControlManager controlManager;
		Control mainControl;

		[Editor( typeof( EditorGuiUITypeEditor ), typeof( UITypeEditor ) )]
		public string InitialControl
		{
			get { return initialControl; }
			set
			{
				initialControl = value;
				CreateMainControl();
			}
		}

		[Browsable( false )]
		[LogicSystemBrowsable( true )]
		public In3dControlManager ControlManager
		{
			get { return controlManager; }
		}

		[Browsable( false )]
		[LogicSystemBrowsable( true )]
		public Control MainControl
		{
			get { return mainControl; }
			set { mainControl = value; }
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				attachedGuiObject = attachedObject as MapObjectAttachedGui;
				if( attachedGuiObject != null )
				{
					controlManager = attachedGuiObject.ControlManager;
					break;
				}
			}

			CreateMainControl();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			mainControl = null;
			controlManager = null;
			base.OnDestroy();
		}

		void CreateMainControl()
		{
			if( mainControl != null )
			{
				mainControl.Parent.Controls.Remove( mainControl );
				mainControl = null;
			}

			if( controlManager != null && !string.IsNullOrEmpty( initialControl ) )
			{
				mainControl = ControlDeclarationManager.Instance.CreateControl( initialControl );
				if( mainControl != null )
					controlManager.Controls.Add( mainControl );
			}

			//update MapBounds
			SetTransform( Position, Rotation, Scale );
		}
	}
}
