// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing.Design;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.Renderer;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Utils;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="DynamicSinusoidSurface"/> entity type.
	/// </summary>
	public class DynamicSinusoidSurfaceType : MapObjectType
	{
	}

	/// <summary>
	/// Example of dynamic geometry.
	/// </summary>
	public class DynamicSinusoidSurface : MapObject
	{
		const int tesselation = 30;

		Mesh mesh;
		MapObjectAttachedMesh attachedMesh;

		bool needRecreateMesh;
		bool needUpdateGeometry;

		///////////////////////////////////////////

		[StructLayout( LayoutKind.Sequential )]
		struct Vertex
		{
			public Vec3 position;
			public Vec3 normal;
			public Vec2 texCoord;
			//public Vec4 tangents;
		}

		///////////////////////////////////////////

		DynamicSinusoidSurfaceType _type = null; public new DynamicSinusoidSurfaceType Type { get { return _type; } }

		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			if( EngineApp.Instance.ApplicationType != EngineApp.ApplicationTypes.ResourceEditor )
			{
				CreateMesh();
				AttachMeshToEntity();
				needUpdateGeometry = true;
			}

			SceneManager.Instance.FogAndShadowSettingsChanged += SceneManager_FogAndShadowSettingsChanged;
		}

		protected override void OnDestroy()
		{
			SceneManager.Instance.FogAndShadowSettingsChanged -= SceneManager_FogAndShadowSettingsChanged;

			DetachMeshFromEntity();
			DestroyMesh();

			base.OnDestroy();
		}

		void SceneManager_FogAndShadowSettingsChanged( bool fogModeChanged, bool shadowTechniqueChanged )
		{
			//fix for stencil shadows
			if( shadowTechniqueChanged )
				needRecreateMesh = true;
		}

		void CreateMesh()
		{
			DetachMeshFromEntity();
			DestroyMesh();

			int maxVertexCount = tesselation * tesselation;
			int maxIndexCount = ( tesselation - 1 ) * ( tesselation - 1 ) * 6;

			string meshName = MeshManager.Instance.GetUniqueName( "DynamicSinusoidSurface" );
			mesh = MeshManager.Instance.CreateManual( meshName );

			SubMesh subMesh = mesh.CreateSubMesh();
			subMesh.UseSharedVertices = false;

			//init vertexData
			VertexDeclaration declaration = subMesh.VertexData.VertexDeclaration;
			declaration.AddElement( 0, 0, VertexElementType.Float3, VertexElementSemantic.Position );
			declaration.AddElement( 0, 12, VertexElementType.Float3, VertexElementSemantic.Normal );
			declaration.AddElement( 0, 24, VertexElementType.Float2, VertexElementSemantic.TextureCoordinates, 0 );
			//declaration.AddElement( 0, 32, VertexElementType.Float4, VertexElementSemantic.Tangent, 0 );

			HardwareBuffer.Usage usage = HardwareBuffer.Usage.DynamicWriteOnly;//HardwareBuffer.Usage.StaticWriteOnly;

			HardwareVertexBuffer vertexBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(
				Marshal.SizeOf( typeof( Vertex ) ), maxVertexCount, usage );
			subMesh.VertexData.VertexBufferBinding.SetBinding( 0, vertexBuffer, true );
			subMesh.VertexData.VertexCount = maxVertexCount;

			HardwareIndexBuffer indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer(
				HardwareIndexBuffer.IndexType._16Bit, maxIndexCount, usage );
			subMesh.IndexData.SetIndexBuffer( indexBuffer, true );
			subMesh.IndexData.IndexCount = maxIndexCount;

			//set material
			subMesh.MaterialName = "DynamicSinusoidSurface";

			//set mesh gabarites
			Bounds bounds = new Bounds( -Scale / 2, Scale / 2 );
			mesh.SetBoundsAndRadius( bounds, bounds.GetRadius() );
		}

		void AttachMeshToEntity()
		{
			attachedMesh = new MapObjectAttachedMesh();
			attachedMesh.CastDynamicShadows = true;
			attachedMesh.MeshName = mesh.Name;

			//don't scale attached mesh
			attachedMesh.UseOwnerScale = false;

			Attach( attachedMesh );

			RecalculateMapBounds();
		}

		void DetachMeshFromEntity()
		{
			if( attachedMesh != null )
			{
				Detach( attachedMesh );
				attachedMesh = null;
			}
		}

		void DestroyMesh()
		{
			if( mesh != null )
			{
				mesh.Dispose();
				mesh = null;
			}
		}

		protected override void OnSetTransform( ref Vec3 pos, ref Quat rot, ref Vec3 scl )
		{
			//need recreate mesh when scale was changed.
			if( scl != Scale )
				needRecreateMesh = true;

			base.OnSetTransform( ref pos, ref rot, ref scl );
		}

		protected override void OnRenderFrame()
		{
			base.OnRenderFrame();

			//recreate mesh when needed
			if( attachedMesh != null && needRecreateMesh )
			{
				CreateMesh();
				AttachMeshToEntity();
			}

			//update each frame (remove it when you want update geometry once at creation).
			needUpdateGeometry = true;

			//update geometry
			if( attachedMesh != null && needUpdateGeometry )
			{
				float time = RendererWorld.Instance.FrameRenderTime;
				UpdateGeometry( time );
				needUpdateGeometry = false;
			}
		}

		protected override void OnRender( Camera camera )
		{
			base.OnRender( camera );

		}

		unsafe void UpdateGeometry( float time )
		{
			//generate geometry
			Vertex[] vertices = new Vertex[ tesselation * tesselation ];
			ushort[] indices = new ushort[ ( tesselation - 1 ) * ( tesselation - 1 ) * 6 ];
			{
				//vertices
				int vertexPosition = 0;
				for( int y = 0; y < tesselation; y++ )
				{
					for( int x = 0; x < tesselation; x++ )
					{
						Vertex vertex = new Vertex();

						Vec2 pos2 = new Vec2(
							(float)x / (float)( tesselation - 1 ) - .5f,
							(float)y / (float)( tesselation - 1 ) - .5f );
						float posZ = MathFunctions.Sin( pos2.Length() * 30 - time * 2 ) / 2;
						MathFunctions.Clamp( ref posZ, -5f, .5f );

						vertex.position = new Vec3( pos2.X, pos2.Y, posZ ) * Scale;
						vertex.normal = Vec3.Zero;
						vertex.texCoord = new Vec2( pos2.X + .5f, pos2.Y + .5f );
						//vertex.tangents = Vec4.Zero;

						vertices[ vertexPosition ] = vertex;
						vertexPosition++;
					}
				}

				//indices
				int indexPosition = 0;
				for( int y = 0; y < tesselation - 1; y++ )
				{
					for( int x = 0; x < tesselation - 1; x++ )
					{
						indices[ indexPosition ] = (ushort)( tesselation * y + x );
						indexPosition++;
						indices[ indexPosition ] = (ushort)( tesselation * y + x + 1 );
						indexPosition++;
						indices[ indexPosition ] = (ushort)( tesselation * ( y + 1 ) + x + 1 );
						indexPosition++;

						indices[ indexPosition ] = (ushort)( tesselation * ( y + 1 ) + x + 1 );
						indexPosition++;
						indices[ indexPosition ] = (ushort)( tesselation * ( y + 1 ) + x );
						indexPosition++;
						indices[ indexPosition ] = (ushort)( tesselation * y + x );
						indexPosition++;
					}
				}

				//calculate vertex normals
				fixed( Vertex* pVertices = vertices )
				{
					int triangleCount = indices.Length / 3;
					for( int n = 0; n < triangleCount; n++ )
					{
						int index0 = indices[ n * 3 + 0 ];
						int index1 = indices[ n * 3 + 1 ];
						int index2 = indices[ n * 3 + 2 ];

						Vec3 pos0 = pVertices[ index0 ].position;
						Vec3 pos1 = pVertices[ index1 ].position;
						Vec3 pos2 = pVertices[ index2 ].position;

						Vec3 normal = Vec3.Cross( pos1 - pos0, pos2 - pos0 );
						normal.Normalize();

						pVertices[ index0 ].normal += normal;
						pVertices[ index1 ].normal += normal;
						pVertices[ index2 ].normal += normal;
					}

					//normalize
					for( int n = 0; n < vertices.Length; n++ )
						pVertices[ n ].normal = pVertices[ n ].normal.GetNormalize();
				}
			}

			SubMesh subMesh = mesh.SubMeshes[ 0 ];

			//copy data to vertex buffer
			{
				HardwareVertexBuffer vertexBuffer = subMesh.VertexData.VertexBufferBinding.GetBuffer( 0 );

				IntPtr buffer = vertexBuffer.Lock( HardwareBuffer.LockOptions.Discard );
				fixed( Vertex* pVertices = vertices )
				{
					NativeUtils.CopyMemory( buffer, (IntPtr)pVertices, vertices.Length * sizeof( Vertex ) );
				}
				vertexBuffer.Unlock();
			}

			//copy data to index buffer
			{
				HardwareIndexBuffer indexBuffer = subMesh.IndexData.IndexBuffer;
				IntPtr buffer = indexBuffer.Lock( HardwareBuffer.LockOptions.Discard );
				fixed( ushort* pIndices = indices )
				{
					NativeUtils.CopyMemory( buffer, (IntPtr)pIndices, indices.Length * sizeof( ushort ) );
				}
				indexBuffer.Unlock();
			}

			////calculate mesh tangent vectors
			//mesh.BuildTangentVectors( VertexElementSemantic.Tangent, 0, 0, true );
		}

	}
}
