// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Utils;

namespace WinFormsMultiViewAppExample
{
	public partial class EntityTypesForm : WeifenLuo.WinFormsUI.Docking.DockContent
	{
		public delegate void TypeChangeDelegate( EntityType entityType );
		public event TypeChangeDelegate TypeChange;

		///////////////////////////////////////////

		class NodeSorter : IComparer
		{
			public int Compare( object x, object y )
			{
				TreeNode node1 = x as TreeNode;
				TreeNode node2 = y as TreeNode;

				if( ( node1.Nodes.Count != 0 ) || ( node2.Nodes.Count != 0 ) )
				{
					if( node1.Nodes.Count == 0 )
						return 1;
					if( node2.Nodes.Count == 0 )
						return -1;
				}
				return string.Compare( node1.Text, node2.Text );
			}
		}

		///////////////////////////////////////////

		public EntityTypesForm()
		{
			InitializeComponent();
		}

		public void UpdateTree()
		{
			treeView.BeginUpdate();

			treeView.Nodes.Clear();

			foreach( EntityType type in EntityTypes.Instance.Types )
			{
				if( type.CreatableInMapEditor )
					NodeCreate( type );
			}

			GroupsDeleteOneChild();

			treeView.TreeViewNodeSorter = new NodeSorter();
			treeView.Sort();

			if( treeView.Nodes.Count == 1 )
				treeView.Nodes[ 0 ].Expand();
			foreach( TreeNode node in treeView.Nodes )
			{
				if( node.Text == "Types" )
				{
					node.Expand();
					break;
				}
			}

			treeView.EndUpdate();
		}

		public void ClearSelection()
		{
			if( treeView.Nodes.Count != 0 && treeView.Nodes[ 0 ].Nodes.Count != 0 )
				treeView.SelectedNode = treeView.Nodes[ 0 ];
			else
				treeView.SelectedNode = null;

			if( TypeChange != null )
				TypeChange( TypeSelected );
		}

		private void treeView_AfterSelect( object sender, TreeViewEventArgs e )
		{
			if( TypeChange != null )
				TypeChange( TypeSelected );
		}

		void NodeCreate( EntityType type )
		{
			string path = Path.GetDirectoryName( type.FilePath );

			if( path != "" )
			{
				TreeNode groupNode = GroupNodeFindByPath( path );
				if( groupNode == null )
					groupNode = GroupNodeCreate( path );

				TreeNode node = new TreeNode( type.FullName, 1, 1 );
				groupNode.Nodes.Add( node );
				node.Tag = type;
			}
			else
			{
				TreeNode node = new TreeNode( type.FullName, 1, 1 );
				treeView.Nodes.Add( node );
				node.Tag = type;
			}
		}

		TreeNode GetChildGroupNodeByName( TreeNodeCollection groupNodeChildren, string name )
		{
			foreach( TreeNode node in groupNodeChildren )
				if( node.Tag == null && node.Text == name )
					return node;
			return null;
		}

		TreeNode GroupNodeFindByPath( string path )
		{
			string[] listPath = path.Split( "\\/".ToCharArray() );
			Trace.Assert( listPath.Length != 0, "listPath.Length != 0" );

			TreeNode groupNode = null;
			foreach( string folder in listPath )
			{
				if( groupNode == null )
					groupNode = GetChildGroupNodeByName( treeView.Nodes, folder );
				else
					groupNode = GetChildGroupNodeByName( groupNode.Nodes, folder );

				if( groupNode == null )
					return null;
			}
			return groupNode;
		}

		TreeNode GroupNodeCreate( string path )
		{
			string[] listPath = path.Split( "\\/".ToCharArray() );
			Trace.Assert( listPath.Length != 0, "listPath.Length != 0" );

			TreeNode groupNode = null;

			foreach( string folder in listPath )
			{
				TreeNode childGroup;
				if( groupNode == null )
					childGroup = GetChildGroupNodeByName( treeView.Nodes, folder );
				else
					childGroup = GetChildGroupNodeByName( groupNode.Nodes, folder );

				if( childGroup == null )
				{
					childGroup = new TreeNode( folder, 0, 0 );
					if( groupNode == null )
						treeView.Nodes.Add( childGroup );
					else
						groupNode.Nodes.Add( childGroup );
				}

				groupNode = childGroup;
			}
			return groupNode;
		}

		void GroupsDeleteOneChild()
		{
			while( true )
			{
				bool breaked = !TreeViewUtils.EnumerateAllNodes( treeView, delegate( TreeNode node )
				{
					if( node.Nodes.Count == 1 && node.Nodes[ 0 ].Tag != null )
					{
						TreeNode oldNode = node.Nodes[ 0 ];

						TreeNode newNode = new TreeNode( oldNode.Text, oldNode.ImageIndex,
							oldNode.SelectedImageIndex );
						newNode.Tag = oldNode.Tag;

						if( node.Parent == null )
							treeView.Nodes.Add( newNode );
						else
							node.Parent.Nodes.Add( newNode );

						node.Remove();
						return false;
					}

					return true;
				} );

				if( !breaked )
					break;
			}
		}

		public EntityType TypeSelected
		{
			get
			{
				TreeNode node = treeView.SelectedNode;
				if( node == null )
					return null;
				return node.Tag as EntityType;
			}
		}
	}
}
