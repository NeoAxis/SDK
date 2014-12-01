using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using Engine;
using Engine.Utils;

namespace WinFormsMultiViewAppExample
{
	public partial class OptionsDialog : Form
	{
		static string lastOpenedNodeFullPath = "";

		///////////////////////////////////////////

		class ToolsLocalizedPropertyDescriptor : PropertyDescriptor, _IWrappedPropertyDescriptor
		{
			object obj;
			PropertyInfo property;
			string translateGroup;

			//

			internal ToolsLocalizedPropertyDescriptor( object obj, PropertyInfo property, Attribute[] attrs,
				string translateGroup )
				: base( property.Name, attrs )
			{
				this.obj = obj;
				this.property = property;
				this.translateGroup = translateGroup;
			}

			public override Type ComponentType
			{
				get { return obj.GetType(); }
			}

			public override Type PropertyType
			{
				get { return property.PropertyType; }
			}

			public override object GetValue( object component )
			{
				return property.GetValue( obj, null );
			}

			public override bool IsReadOnly
			{
				get { return !property.CanWrite; }
			}

			public override void SetValue( object component, object value )
			{
				property.SetValue( obj, value, null );
			}

			public override bool CanResetValue( object component )
			{
				DefaultValueAttribute[] attributes = (DefaultValueAttribute[])
					property.GetCustomAttributes( typeof( DefaultValueAttribute ), true );
				if( attributes.Length != 0 )
				{
					if( !object.Equals( attributes[ 0 ].Value, GetValue( component ) ) )
						return true;
				}
				return false;
			}

			public override void ResetValue( object component )
			{
				DefaultValueAttribute[] attributes = (DefaultValueAttribute[])
					property.GetCustomAttributes( typeof( DefaultValueAttribute ), true );
				if( attributes.Length != 0 )
				{
					if( !object.Equals( attributes[ 0 ].Value, GetValue( component ) ) )
						SetValue( component, attributes[ 0 ].Value );
				}
			}

			public override bool ShouldSerializeValue( object component )
			{
				DefaultValueAttribute[] attributes = (DefaultValueAttribute[])
					property.GetCustomAttributes( typeof( DefaultValueAttribute ), true );
				if( attributes.Length == 0 )
					return true;
				return !object.Equals( attributes[ 0 ].Value, GetValue( component ) );
			}

			public override string DisplayName
			{
				get
				{
					return ToolsLocalization.Translate( translateGroup, base.DisplayName );
				}
			}

			public override string Category
			{
				get
				{
					return ToolsLocalization.Translate( translateGroup, base.Category );
				}
			}

			public object GetWrappedOwner()
			{
				return obj;
			}

			public PropertyInfo GetWrappedProperty()
			{
				return property;
			}

		}

		///////////////////////////////////////////

		class ToolsLocalizedTypeDescriptor : CustomTypeDescriptor, _IWrappedCustomTypeDescriptor
		{
			object obj;
			string translateGroup;
			PropertyDescriptorCollection propertiesCollection;

			//

			public ToolsLocalizedTypeDescriptor( object obj, string translateGroup )
			{
				this.obj = obj;
				this.translateGroup = translateGroup;
			}

			public object Obj
			{
				get { return obj; }
			}

			public override PropertyDescriptorCollection GetProperties( Attribute[] attributes )
			{
				return GetProperties();
			}

			public override PropertyDescriptorCollection GetProperties()
			{
				if( propertiesCollection == null )
				{
					propertiesCollection = new PropertyDescriptorCollection( null );

					Set<string> addedPropertyNames = new Set<string>();

					Stack<Type> typeHierarchy = new Stack<Type>();
					{
						Type type = obj.GetType();
						while( type != null )
						{
							typeHierarchy.Push( type );
							type = type.BaseType;
						}
					}

					while( typeHierarchy.Count != 0 )
					{
						Type type = typeHierarchy.Pop();

						PropertyInfo[] properties = type.GetProperties(
							BindingFlags.Instance | BindingFlags.Public );

						foreach( PropertyInfo property in properties )
						{
							if( !addedPropertyNames.Contains( property.Name ) )
							{
								addedPropertyNames.Add( property.Name );

								List<Attribute> attributes = new List<Attribute>();
								foreach( Attribute attribute in property.GetCustomAttributes( false ) )
									attributes.Add( attribute );

								propertiesCollection.Add( new ToolsLocalizedPropertyDescriptor( obj, property,
									attributes.ToArray(), translateGroup ) );
							}
						}
					}

				}

				return propertiesCollection;
			}

			public override object GetPropertyOwner( PropertyDescriptor pd )
			{
				return this;
			}

			public object GetWrapperOwner()
			{
				return obj;
			}
		}

		///////////////////////////////////////////

		public OptionsDialog()
		{
			InitializeComponent();
		}

		public void AddLeaf( OptionsLeaf leaf, bool selected )
		{
			TreeNode node = new TreeNode( ToolsLocalization.Translate( "OptionsDialog", leaf.ToString() ) );
			//node.Name = leaf.ToString();
			node.Tag = leaf;

			Image image = leaf.GetImage();
			if( image != null )
			{
				imageList.Images.Add( image );
				node.ImageIndex = imageList.Images.Count - 1;
				node.SelectedImageIndex = node.ImageIndex;
			}

			treeView.Nodes.Add( node );

			if( selected )
				treeView.SelectedNode = node;
		}

		public void AddLeaf( OptionsLeaf leaf )
		{
			AddLeaf( leaf, false );
		}

		public void AddLeaf( OptionsLeaf leaf, OptionsLeaf parentLeaf, bool selected )
		{
			TreeNode parentNode = TreeViewUtils.FindNodeByTag( treeView, parentLeaf );

			TreeNode node = new TreeNode( ToolsLocalization.Translate( "OptionsDialog", leaf.ToString() ) );
			//node.Name = leaf.ToString();
			node.Tag = leaf;

			Image image = leaf.GetImage();
			if( image != null )
			{
				imageList.Images.Add( image );
				node.ImageIndex = imageList.Images.Count - 1;
				node.SelectedImageIndex = node.ImageIndex;
			}

			parentNode.Nodes.Add( node );

			parentNode.Expand();

			if( selected )
				treeView.SelectedNode = node;
		}

		public void AddLeaf( OptionsLeaf leaf, OptionsLeaf parentLeaf )
		{
			AddLeaf( leaf, parentLeaf, false );
		}

		private void treeView_AfterSelect( object sender, TreeViewEventArgs e )
		{
			propertyGrid.SelectedObject = new ToolsLocalizedTypeDescriptor(
				(OptionsLeaf)e.Node.Tag, "OptionsDialog" );
		}

		bool OnIsAllowOKRecursive( TreeNode node )
		{
			OptionsLeaf leaf = (OptionsLeaf)node.Tag;

			if( !leaf.OnIsAllowOK() )
				return false;

			foreach( TreeNode child in node.Nodes )
				if( !OnIsAllowOKRecursive( child ) )
					return false;

			return true;
		}

		void OnOKRecursive( TreeNode node )
		{
			OptionsLeaf leaf = (OptionsLeaf)node.Tag;
			leaf.OnOK();
			foreach( TreeNode child in node.Nodes )
				OnOKRecursive( child );
		}

		private void OptionsDialog_Load( object sender, EventArgs e )
		{
			if( lastOpenedNodeFullPath != null && treeView.SelectedNode == null )
			{
				TreeNode node = TreeViewUtils.GetNodeByFullPath( treeView, lastOpenedNodeFullPath );
				if( node != null )
					treeView.SelectedNode = node;

			}

			Translate();
		}

		private void NewOptionsDialog_FormClosing( object sender, FormClosingEventArgs e )
		{
			if( DialogResult == DialogResult.OK )
			{
				foreach( TreeNode node in treeView.Nodes )
				{
					if( !OnIsAllowOKRecursive( node ) )
					{
						treeView.SelectedNode = node;
						e.Cancel = true;
						return;
					}
				}

				foreach( TreeNode node in treeView.Nodes )
					OnOKRecursive( node );

				if( treeView.SelectedNode != null )
					lastOpenedNodeFullPath = treeView.SelectedNode.FullPath;
				else
					lastOpenedNodeFullPath = "";
			}
		}

		private void contextMenuStripPropertyGrid_Opening( object sender, CancelEventArgs e )
		{
			GridItem gridItem = propertyGrid.SelectedGridItem;

			bool canReset = false;

			if( gridItem != null && gridItem.PropertyDescriptor != null )
			{
				PropertyDescriptor descriptor = gridItem.PropertyDescriptor;

				try
				{
					if( descriptor.GetType().Name == "MergePropertyDescriptor" )
					{
						//hack for multiselection.
						FieldInfo field = descriptor.GetType().GetField(
							"descriptors", BindingFlags.Instance | BindingFlags.NonPublic );
						PropertyDescriptor[] childDescriptors = (PropertyDescriptor[])field.GetValue( descriptor );

						for( int n = 0; n < childDescriptors.Length; n++ )
						{
							PropertyDescriptor childDescriptor = childDescriptors[ n ];

							if( childDescriptor.CanResetValue( propertyGrid.SelectedObjects[ n ] ) )
							{
								canReset = true;
								break;
							}
						}
					}
					else
					{
						object component = null;

						GridItem parent = gridItem.Parent;

						if( parent.GridItemType == GridItemType.Category || parent.GridItemType == GridItemType.Root )
							component = propertyGrid.SelectedObject;
						else if( parent.GridItemType == GridItemType.Property )
							component = parent.Value;

						if( descriptor.CanResetValue( component ) )
							canReset = true;
					}
				}
				catch
				{
					canReset = true;
				}
			}

			contextMenuStripPropertyGrid.Items[ 0 ].Enabled = canReset;
		}

		private void resetToolStripMenuItem_Click( object sender, EventArgs e )
		{
			GridItem gridItem = propertyGrid.SelectedGridItem;
			if( gridItem != null && gridItem.PropertyDescriptor != null )
				propertyGrid.ResetSelectedProperty();
		}

		void Translate()
		{
			Text = ToolsLocalization.Translate( "OptionsDialog", Text );

			foreach( Control control in Controls )
			{
				if( control is Label || control is Button )
					control.Text = ToolsLocalization.Translate( "OptionsDialog", control.Text );
			}

			resetToolStripMenuItem.Text = 				
				ToolsLocalization.Translate( "OptionsDialog", resetToolStripMenuItem.Text );
		}

	}
}