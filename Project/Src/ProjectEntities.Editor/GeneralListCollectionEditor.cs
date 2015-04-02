// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Reflection;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using Engine;
using Engine.EntitySystem;
using Engine.Utils;
using Engine.MathEx;

namespace ProjectEntities.Editor
{
	public class ProjectEntitiesGeneralListCollectionEditor : CollectionEditor
	{
		//Values of item's properties.
		//We need save values of properties because need restore values when user cancel changes.
		Dictionary<object, List<Pair<PropertyInfo, object>>> propertyCopies;

		[Config( "ProjectEntitiesGeneralListCollectionEditor", "lastWindowPosition" )]
		public static Vec2I lastWindowPosition;
		[Config( "ProjectEntitiesGeneralListCollectionEditor", "lastWindowSize" )]
		public static Vec2I lastWindowSize;

		//

		public ProjectEntitiesGeneralListCollectionEditor( Type type )
			: base( type ) { }

		object GetOwner()
		{
			EntityTypeCustomTypeDescriptor typeDescriptor = Context.Instance as EntityTypeCustomTypeDescriptor;
			if( typeDescriptor != null )
				return typeDescriptor.EntityType;

			EntityCustomTypeDescriptor entityDescriptor = Context.Instance as EntityCustomTypeDescriptor;
			if( entityDescriptor != null )
				return entityDescriptor.Entity;

			return Context.Instance;
		}

		object GetList()
		{
			object owner = GetOwner();
			PropertyInfo listProperty = owner.GetType().GetProperty( Context.PropertyDescriptor.Name );
			object list = listProperty.GetValue( owner, null );
			return list;
		}

		int GetListCount( object list )
		{
			PropertyInfo property = list.GetType().GetProperty( "Count" );
			return (int)property.GetValue( list, null );
		}

		object GetListItem( object list, int index )
		{
			PropertyInfo property = list.GetType().GetProperty( "Item" );
			object item = property.GetValue( list, new object[] { index } );
			return item;
		}

		protected override CollectionEditor.CollectionForm CreateCollectionForm()
		{
			object list = GetList();

			//copy object's properties
			propertyCopies = new Dictionary<object, List<Pair<PropertyInfo, object>>>();

			int listCount = GetListCount( list );
			for( int n = 0; n < listCount; n++ )
			{
				object listItem = GetListItem( list, n );

				List<Pair<PropertyInfo, object>> pairList = new List<Pair<PropertyInfo, object>>();

				foreach( PropertyInfo property in listItem.GetType().GetProperties() )
				{
					if( !property.CanWrite )
						continue;
					BrowsableAttribute[] browsableAttributes = (BrowsableAttribute[])property.
						GetCustomAttributes( typeof( BrowsableAttribute ), true );
					if( browsableAttributes.Length != 0 )
					{
						bool browsable = true;
						foreach( BrowsableAttribute browsableAttribute in browsableAttributes )
						{
							if( !browsableAttribute.Browsable )
							{
								browsable = false;
								break;
							}
						}
						if( !browsable )
							continue;
					}

					object value = property.GetValue( listItem, null );
					pairList.Add( new Pair<PropertyInfo, object>( property, value ) );
				}

				propertyCopies.Add( listItem, pairList );
			}

			CollectionEditor.CollectionForm form = base.CreateCollectionForm();
			form.FormClosed += FormClosed;

			//enable fields with Config attribute
			EngineApp.Instance.Config.RegisterClassParameters( typeof( ProjectEntitiesGeneralListCollectionEditor ) );

			//restore saved window state
			if( lastWindowSize != Vec2I.Zero &&
				IsVisibleOnAnyScreen( new Rectangle( lastWindowPosition.X, lastWindowPosition.Y, lastWindowSize.X, lastWindowSize.Y ) ) )
			{
				form.Location = new Point( lastWindowPosition.X, lastWindowPosition.Y );
				form.Size = new Size( lastWindowSize.X, lastWindowSize.Y );
				form.StartPosition = FormStartPosition.Manual;
			}
			else
				form.Size = new System.Drawing.Size( 1100, 800 );

			//show descriptions
			try
			{
				FieldInfo field = form.GetType().GetField( "propertyBrowser",
					BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );
				PropertyGrid grid = field.GetValue( form ) as PropertyGrid;
				grid.HelpVisible = true;
			}
			catch { }

			return form;
		}

		static bool IsVisibleOnAnyScreen( Rectangle rect )
		{
			Rectangle rect2 = rect;
			if( rect.Size.Width > 60 && rect.Size.Height > 60 )
				rect2 = new Rectangle( rect2.Left + 30, rect2.Top + 30, rect2.Size.Width - 60, rect2.Size.Height - 60 );
			foreach( Screen screen in Screen.AllScreens )
				if( screen.WorkingArea.IntersectsWith( rect2 ) )
					return true;
			return false;
		}

		void FormClosed( object sender, FormClosedEventArgs e )
		{
			CollectionEditor.CollectionForm form = (CollectionEditor.CollectionForm)sender;

			//save window settings
			lastWindowPosition = new Vec2I( form.Location.X, form.Location.Y );
			lastWindowSize = new Vec2I( form.Size.Width, form.Size.Height );

			if( form.DialogResult == DialogResult.Cancel )
			{
				//restore properties
				foreach( KeyValuePair<object, List<Pair<PropertyInfo, object>>> keyValuePair in propertyCopies )
				{
					object listItem = keyValuePair.Key;
					List<Pair<PropertyInfo, object>> pairList = keyValuePair.Value;

					foreach( Pair<PropertyInfo, object> pair in pairList )
					{
						PropertyInfo property = pair.First;
						object value = pair.Second;
						property.SetValue( listItem, value, null );
					}
				}
			}
		}
	}
}
