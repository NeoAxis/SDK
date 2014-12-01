// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace WinFormsMultiViewAppExample
{
	public static class TreeViewUtils
	{
		public static void ExpandAllPathToNode( TreeNode node )
		{
			TreeNode n = node;
			while( n != null )
			{
				n.Expand();
				n = n.Parent;
			}
		}

		public static TreeNode GetNodeByFullPath( TreeView treeView, string fullPath )
		{
			if( string.IsNullOrEmpty( fullPath ) )
				return null;

			string[] names = fullPath.Split( "\\/".ToCharArray() );

			TreeNode node = null;
			foreach( string name in names )
			{
				if( node == null )
					node = treeView.Nodes[ name ];
				else
					node = node.Nodes[ name ];
				if( node == null )
					return null;
			}
			return node;
		}

		static TreeNode FindNodeByTagRecursive( TreeNode parentNode, object tag )
		{
			if( parentNode.Tag == tag )
				return parentNode;

			foreach( TreeNode node in parentNode.Nodes )
			{
				TreeNode n = FindNodeByTagRecursive( node, tag );
				if( n != null )
					return n;
			}
			return null;
		}

		public static TreeNode FindNodeByTag( TreeView treeView, object tag )
		{
			foreach( TreeNode node in treeView.Nodes )
			{
				TreeNode n = FindNodeByTagRecursive( node, tag );
				if( n != null )
					return n;
			}
			return null;
		}

		public static TreeNode GetNeedSelectNodeAfterRemoveNode( TreeNode node )
		{
			TreeNode parent = node.Parent;

			int n;
			for( n = 0; n < parent.Nodes.Count; n++ )
				if( parent.Nodes[ n ] == node )
					break;
			if( n + 1 < parent.Nodes.Count )
				return parent.Nodes[ n + 1 ];
			else if( n - 1 >= 0 )
				return parent.Nodes[ n - 1 ];
			else
				return parent;
		}

		public delegate bool EnumerateAllNodesDelegate( TreeNode node );

		static bool EnumerateAllNodes( TreeNode node, EnumerateAllNodesDelegate callback )
		{
			if( !callback( node ) )
				return false;
			foreach( TreeNode child in node.Nodes )
			{
				if( !EnumerateAllNodes( child, callback ) )
					return false;
			}
			return true;
		}

		public static bool EnumerateAllNodes( TreeView treeView, EnumerateAllNodesDelegate callback )
		{
			foreach( TreeNode node in treeView.Nodes )
			{
				if( !EnumerateAllNodes( node, callback ) )
					return false;
			}
			return true;
		}

	}
}
