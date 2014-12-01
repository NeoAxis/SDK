// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Engine;
using Engine.MathEx;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Renderer;
using Engine.PhysicsSystem;
using ProjectCommon;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="Decal"/> entity type.
	/// </summary>
	public class DecalType : MapObjectType
	{
		public DecalType()
		{
			AllowEmptyName = false;
		}

		protected override void OnLoaded()
		{
			base.OnLoaded();
			CreatableInMapEditor = false;
		}
	}

	public class Decal : MapObject
	{
		DecalCreator creator;
		MapObject parentMapObject;

		Vertex[] vertices;
		int[] indices;
		string sourceMaterialName;

		//for StaticGeometry, HeightTerrain and static MapObjects
		StaticMeshObject staticMeshObject;
		float needMaterialAlpha = 1;
		Material clonedStandardMaterial;
		ShaderBaseMaterial shaderBaseMaterial;

		float fadeTime;

		///////////////////////////////////////////

		[StructLayout( LayoutKind.Sequential )]
		public struct Vertex
		{
			public Vec3 position;
			public Vec3 normal;
			public Vec2 texCoord;
			public Vec3 tangent;

			public Vertex( Vec3 position, Vec3 normal, Vec2 texCoord, Vec3 tangent )
			{
				this.position = position;
				this.normal = normal;
				this.texCoord = texCoord;
				this.tangent = tangent;
			}
		}

		///////////////////////////////////////////

		DecalType _type = null; public new DecalType Type { get { return _type; } }

		public Decal()
		{
			AllowSave = false;
		}

		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
		}

		protected override void OnDestroy()
		{
			if( creator != null )
			{
				//remove from DecalCreator
				creator.RemoveDecal( this );

				DestroyDecal();
			}

			base.OnDestroy();
		}

		public void Init( DecalCreator creator, Vertex[] vertices, int[] indices,
			string materialName, MapObject parentMapObject )
		{
			this.creator = creator;
			this.vertices = vertices;
			this.indices = indices;
			this.sourceMaterialName = materialName;
			this.parentMapObject = parentMapObject;
			if( parentMapObject != null )
				SubscribeToDeletionEvent( parentMapObject );

			CreateDecal();
		}

		protected override void OnDeleteSubscribedToDeletionEvent( Entity entity )
		{
			base.OnDeleteSubscribedToDeletionEvent( entity );

			if( entity == parentMapObject )
			{
				parentMapObject = null;
				SetForDeletion( false );
			}
		}

		void CreateDecal()
		{
			Bounds bounds = Bounds.Cleared;
			foreach( Vertex vertex in vertices )
				bounds.Add( vertex.position );

			VertexData vertexData = new VertexData();
			IndexData indexData = new IndexData();

			//init vertexData
			VertexDeclaration declaration = vertexData.VertexDeclaration;
			declaration.AddElement( 0, 0, VertexElementType.Float3, VertexElementSemantic.Position );
			declaration.AddElement( 0, 12, VertexElementType.Float3, VertexElementSemantic.Normal );
			declaration.AddElement( 0, 24, VertexElementType.Float2,
				VertexElementSemantic.TextureCoordinates, 0 );
			declaration.AddElement( 0, 32, VertexElementType.Float3, VertexElementSemantic.Tangent );

			VertexBufferBinding bufferBinding = vertexData.VertexBufferBinding;
			HardwareVertexBuffer vertexBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(
				44, vertices.Length, HardwareBuffer.Usage.StaticWriteOnly );
			bufferBinding.SetBinding( 0, vertexBuffer, true );
			vertexData.VertexCount = vertices.Length;

			//init indexData
			Trace.Assert( vertices.Length < 65536, "Decal: vertices.Length < 65536" );

			HardwareIndexBuffer indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer(
				HardwareIndexBuffer.IndexType._16Bit, indices.Length,
				HardwareBuffer.Usage.StaticWriteOnly );
			indexData.SetIndexBuffer( indexBuffer, true );
			indexData.IndexCount = indices.Length;

			//init material
			Material material = null;

			shaderBaseMaterial = HighLevelMaterialManager.Instance.
				GetMaterialByName( sourceMaterialName ) as ShaderBaseMaterial;
			//only default shader technique for ShaderBase material
			if( shaderBaseMaterial != null && !shaderBaseMaterial.IsDefaultTechniqueCreated() )
				shaderBaseMaterial = null;

			if( shaderBaseMaterial != null )
			{
				//ShaderBase material
				material = shaderBaseMaterial.BaseMaterial;
			}
			else
			{
				//standard material or fallback ShaderBase technique
				Material sourceMaterial = MaterialManager.Instance.GetByName( sourceMaterialName );
				if( sourceMaterial != null )
				{
					//clone standard material
					clonedStandardMaterial = MaterialManager.Instance.Clone( sourceMaterial,
						MaterialManager.Instance.GetUniqueName( sourceMaterialName + "_Cloned" ) );
					material = clonedStandardMaterial;
				}
			}

			staticMeshObject = SceneManager.Instance.CreateStaticMeshObject( bounds + Position,
				Position, Quat.Identity, new Vec3( 1, 1, 1 ), true, material, vertexData, indexData, true );
			staticMeshObject.AddToRenderQueue += StaticMeshObject_AddToRenderQueue;

			UpdateBuffers();
		}

		void DestroyDecal()
		{
			if( staticMeshObject != null )
			{
				staticMeshObject.AddToRenderQueue -= StaticMeshObject_AddToRenderQueue;
				staticMeshObject.Dispose();
				staticMeshObject = null;
			}

			if( clonedStandardMaterial != null )
			{
				clonedStandardMaterial.Dispose();
				clonedStandardMaterial = null;
			}
		}

		void UpdateBuffers()
		{
			unsafe
			{
				VertexData vertexData = staticMeshObject.VertexData;
				HardwareVertexBuffer vertexBuffer = vertexData.VertexBufferBinding.GetBuffer( 0 );
				Vertex* buffer = (Vertex*)vertexBuffer.Lock(
					HardwareBuffer.LockOptions.Normal ).ToPointer();
				foreach( Vertex vertex in vertices )
				{
					*buffer = vertex;
					buffer++;
				}
				vertexBuffer.Unlock();
			}

			unsafe
			{
				HardwareIndexBuffer indexBuffer = staticMeshObject.IndexData.IndexBuffer;
				ushort* buffer = (ushort*)indexBuffer.Lock(
					HardwareBuffer.LockOptions.Normal ).ToPointer();
				foreach( int index in indices )
				{
					*buffer = (ushort)index;
					buffer++;
				}
				indexBuffer.Unlock();
			}
		}

		protected override bool OnCancelDeletion()
		{
			if( creator.Type.FadeTime != 0 )
			{
				if( fadeTime == 0 )
				{
					SubscribeToTickEvent();
					fadeTime = .001f;
				}
				return true;
			}

			return base.OnCancelDeletion();
		}

		void DoTick()
		{
			if( fadeTime >= 0 )
			{
				float typeFadeTime = creator.Type.FadeTime;

				fadeTime += TickDelta;

				if( fadeTime >= typeFadeTime )
				{
					fadeTime = -1;
					SetForDeletion( false );
				}

				if( typeFadeTime != 0 )
				{
					needMaterialAlpha = 1.0f - fadeTime / typeFadeTime;
					MathFunctions.Clamp( ref needMaterialAlpha, 0, 1 );
				}
			}
		}

		protected override void OnTick()
		{
			base.OnTick();
			DoTick();
		}

		protected override void Client_OnTick()
		{
			base.Client_OnTick();
			DoTick();
		}

		void StaticMeshObject_AddToRenderQueue( StaticMeshObject staticMeshObject,
			Camera camera, ref bool allowRender )
		{
			if( needMaterialAlpha != 1 )
			{
				if( shaderBaseMaterial != null )
				{
					//ShaderBase high level material

					float diffusePower = shaderBaseMaterial.DiffusePower;
					ColorValue diffuse = shaderBaseMaterial.DiffuseColor *
						new ColorValue( diffusePower, diffusePower, diffusePower, 1 );
					staticMeshObject.SetCustomGpuParameter(
						(int)ShaderBaseMaterial.GpuParameters.dynamicDiffuseScale,
						new Vec4( diffuse.Red, diffuse.Green, diffuse.Blue,
						diffuse.Alpha * needMaterialAlpha ) );
				}
				else if( clonedStandardMaterial != null )
				{
					//standard material

					foreach( Technique technique in clonedStandardMaterial.Techniques )
					{
						foreach( Pass pass in technique.Passes )
						{
							ColorValue diffuse = pass.Diffuse;
							pass.Diffuse = new ColorValue( diffuse.Red, diffuse.Green,
								diffuse.Blue, needMaterialAlpha );
						}
					}
				}
			}
		}

	}
}
