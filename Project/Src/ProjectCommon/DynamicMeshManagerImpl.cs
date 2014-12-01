// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Engine;
using Engine.FileSystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.Utils;

namespace ProjectCommon
{
	public class DynamicMeshManagerImpl : DynamicMeshManager
	{
		float maxLifeTimeNotUsedDataInCache;
		bool showScene;
		bool sceneUnderConstruction;

		Dictionary<string, BlockImpl> blockCache = new Dictionary<string, BlockImpl>();
		Set<BlockImpl> blocksWithoutCaching = new Set<BlockImpl>();

		//scene data
		List<SceneItem> sceneItems = new List<SceneItem>();
		int sceneItemsShrinkMaxLength;
		int sceneItemsShrinkCounter;
		SceneObject[] sceneObjectsArray;

		static long uniqueMeshNameCounter;

		MaterialManagerWithoutTexturesClass materialManagerWithoutTextures;
		MaterialManagerWithTexturesClass materialManagerWithTextures;
		long uniqueMaterialNameForMaterialsWithTextures;

		static Vertex[] boxVertices;
		static int[] boxIndices;

		///////////////////////////////////////////

		class BlockImpl : Block
		{
			static CompileBatchComparer compileBatchComparer = new CompileBatchComparer();

			public DynamicMeshManagerImpl owner;
			bool disposed;
			List<InputItem> inputData = new List<InputItem>();
			public CompiledItem[] compiledData;

			public SceneObjectCachingData freeSceneObjectsCache;

			//for caching support
			public bool useCaching;
			public float lastUsingTime;

			///////////////

			class InputItem
			{
				//general data
				public enum DataTypes
				{
					Mesh,
					Triangles,
				}
				public DataTypes dataType;

				//AddMesh
				public string meshName;

				//AddTriangles
				public Vertex[] trianglesVertices;
				public VertexComponents trianglesVertexComponents;
				public int[] trianglesIndices;

				//general
				public Vec3 position;
				public Quat rotation;
				public Vec3 scale;
				public bool allowBatching;
				public MaterialDataImpl material;

				public static bool AddTrianglesCanMerge( InputItem item1, InputItem item2 )
				{
					if( item1.trianglesVertexComponents != item2.trianglesVertexComponents )
						return false;

					if( !MaterialDataImpl.IsExact( item1.material, item2.material ) )
						return false;
					//if( item1.material != null || item2.material != null )
					//{
					//   if( item1.material == null || item2.material == null )
					//      return false;
					//   if( !MaterialDataImpl.IsExact( item1.material, item2.material ) )
					//      return false;
					//}

					return true;
				}

				public int AddTrianglesOneBatchGetHashCode()
				{
					int hash = trianglesVertexComponents.GetHashCode();
					if( material != null )
						hash ^= material.GetHashCodeForBatching();
					return hash;
				}
			}

			///////////////

			public class CompiledItem
			{
				public Mesh mesh;
				public bool needDisposeMesh;

				public Vec3 position;
				public Quat rotation;
				public Vec3 scale;

				public MaterialDataImpl material;
			}

			///////////////

			public class SceneObjectCachingData
			{
				BlockImpl owner;
				Dictionary<int, List<SceneObject>> sceneObjectsByHash = new Dictionary<int, List<SceneObject>>();

				//

				public SceneObjectCachingData( BlockImpl owner )
				{
					this.owner = owner;
				}

				/// <summary>
				/// Get scene objects from the cache only if available scene objects with exact settings.
				/// </summary>
				/// <param name="pos"></param>
				/// <param name="rot"></param>
				/// <param name="scl"></param>
				/// <param name="material"></param>
				/// <returns></returns>
				public SceneObject GetFromCacheExact( ref Vec3 pos, ref Quat rot, ref Vec3 scl, MaterialDataImpl material )
				{
					int hash = pos.GetHashCode() ^ rot.GetHashCode() ^ scl.GetHashCode();

					List<SceneObject> list;
					if( sceneObjectsByHash.TryGetValue( hash, out list ) )
					{
						for( int n = 0; n < list.Count; n++ )
						{
							SceneObject sceneObject = list[ n ];
							if( sceneObject != null )
							{
								if( sceneObject.position == pos && sceneObject.rotation == rot && sceneObject.scale == scl &&
									MaterialDataImpl.IsExact( material, sceneObject.material ) )
								{
									//found scene object with exact settings!

									//remove from the list
									if( n == list.Count - 1 )//this is last item of list?
									{
										list.RemoveAt( n );
										while( list.Count != 0 && list[ list.Count - 1 ] == null )
											list.RemoveAt( list.Count - 1 );

										if( list.Count == 0 )
										{
											//remove list from the dictionary
											sceneObjectsByHash.Remove( hash );
										}
									}
									else
										list[ n ] = null;

									return sceneObject;
								}
							}
						}
					}

					return null;
				}

				/// <summary>
				/// Get scene objects from the cache.
				/// </summary>
				/// <param name="pos"></param>
				/// <param name="rot"></param>
				/// <param name="scl"></param>
				/// <param name="material"></param>
				/// <param name="exactTransform"></param>
				/// <param name="exactMaterial"></param>
				/// <returns></returns>
				public SceneObject GetFromCacheNotExact( ref Vec3 pos, ref Quat rot, ref Vec3 scl, MaterialDataImpl material,
					out bool exactTransform, out bool exactMaterial )
				{
					int hash = pos.GetHashCode() ^ rot.GetHashCode() ^ scl.GetHashCode();

					//find with exact transform
					{
						List<SceneObject> list;
						if( sceneObjectsByHash.TryGetValue( hash, out list ) )
						{
							for( int n = 0; n < list.Count; n++ )
							{
								SceneObject sceneObject = list[ n ];
								if( sceneObject != null )
								{
									if( sceneObject.position == pos && sceneObject.rotation == rot && sceneObject.scale == scl )
									{
										exactTransform = true;
										exactMaterial = MaterialDataImpl.IsExact( material, sceneObject.material );

										//remove from the list
										if( n == list.Count - 1 )//this is last item of list?
										{
											list.RemoveAt( n );
											while( list.Count != 0 && list[ list.Count - 1 ] == null )
												list.RemoveAt( list.Count - 1 );

											if( list.Count == 0 )
											{
												//remove list from the dictionary
												sceneObjectsByHash.Remove( hash );
											}
										}
										else
											list[ n ] = null;

										return sceneObject;
									}
								}
							}
						}
					}

					//get first any
					foreach( KeyValuePair<int, List<SceneObject>> pair in sceneObjectsByHash )
					{
						List<SceneObject> list = pair.Value;
						if( list.Count == 0 )
							Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: SceneObjectCachingData: GetFromCacheNotExact: list.Count == 0." );

						for( int n = 0; n < list.Count; n++ )
						{
							SceneObject sceneObject = list[ n ];
							if( sceneObject != null )
							{
								exactTransform = sceneObject.position == pos && sceneObject.rotation == rot && sceneObject.scale == scl;
								exactMaterial = MaterialDataImpl.IsExact( material, sceneObject.material );

								//remove from the list
								if( n == list.Count - 1 )//this is last item of list?
								{
									list.RemoveAt( n );
									while( list.Count != 0 && list[ list.Count - 1 ] == null )
										list.RemoveAt( list.Count - 1 );

									if( list.Count == 0 )
									{
										//remove list from the dictionary
										sceneObjectsByHash.Remove( pair.Key );
									}
								}
								else
									list[ n ] = null;

								return sceneObject;
							}
						}

						Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: SceneObjectCachingData: GetFromCacheNotExact: Internal error." );
					}

					//the cache is empty
					exactTransform = false;
					exactMaterial = false;
					return null;
				}

				/// <summary>
				/// Add scene object to the cache.
				/// </summary>
				/// <param name="sceneObject"></param>
				/// <param name="time"></param>
				public void Add( SceneObject sceneObject, float time )
				{
					sceneObject.SetVisible( false );

					sceneObject.inCacheFromTime = time;

					int hash = sceneObject.position.GetHashCode() ^ sceneObject.rotation.GetHashCode() ^
						sceneObject.scale.GetHashCode();

					List<SceneObject> list;
					if( sceneObjectsByHash.TryGetValue( hash, out list ) )
					{
						for( int n = 0; n < list.Count; n++ )
						{
							if( list[ n ] == null )
							{
								list[ n ] = sceneObject;
								return;
							}
						}
						list.Add( sceneObject );
						return;
					}
					else
					{
						list = new List<SceneObject>();
						list.Add( sceneObject );
						sceneObjectsByHash.Add( hash, list );
						return;
					}
				}

				public void DeleteAll()
				{
					foreach( KeyValuePair<int, List<SceneObject>> pair in sceneObjectsByHash )
					{
						List<SceneObject> list = pair.Value;
						for( int n = 0; n < list.Count; n++ )
						{
							SceneObject sceneObject = list[ n ];
							if( sceneObject != null )
								sceneObject.Dispose();
						}
					}
					sceneObjectsByHash.Clear();
				}

				public int GetSceneObjectCount()
				{
					int count = 0;
					foreach( List<SceneObject> list in sceneObjectsByHash.Values )
					{
						for( int n = 0; n < list.Count; n++ )
						{
							if( list[ n ] != null )
								count++;
						}
					}
					return count;
				}

				public bool IsEmpty()
				{
					return sceneObjectsByHash.Count == 0;
				}

				public void DeleteLongTimeNotUsedObjects( float time )
				{
					float maxLifeTimeNotUsedDataInCache = owner.owner.maxLifeTimeNotUsedDataInCache;
					List<int> needRemoveHashes = null;

					foreach( KeyValuePair<int, List<SceneObject>> pair in sceneObjectsByHash )
					{
						int hash = pair.Key;
						List<SceneObject> list = pair.Value;

						for( int n = 0; n < list.Count; n++ )
						{
							SceneObject sceneObject = list[ n ];
							if( sceneObject != null )
							{
								if( time - sceneObject.inCacheFromTime > maxLifeTimeNotUsedDataInCache )
								{
									//found! now delete it.
									sceneObject.Dispose();

									//remove from the list
									if( n == list.Count - 1 )//this is last item of list?
									{
										list.RemoveAt( n );
										while( list.Count != 0 && list[ list.Count - 1 ] == null )
											list.RemoveAt( list.Count - 1 );

										if( list.Count == 0 )
										{
											if( needRemoveHashes == null )
												needRemoveHashes = new List<int>( 32 );
											needRemoveHashes.Add( hash );
										}
									}
									else
										list[ n ] = null;
								}
							}
						}
					}

					if( needRemoveHashes != null )
					{
						for( int n = 0; n < needRemoveHashes.Count; n++ )
							sceneObjectsByHash.Remove( needRemoveHashes[ n ] );
					}
				}
			}

			///////////////

			class CompileBatchComparer : IEqualityComparer<InputItem>
			{
				public bool Equals( InputItem item1, InputItem item2 )
				{
					return InputItem.AddTrianglesCanMerge( item1, item2 );
				}

				public int GetHashCode( InputItem obj )
				{
					return obj.AddTrianglesOneBatchGetHashCode();
				}
			}

			///////////////

			internal BlockImpl( DynamicMeshManagerImpl owner )
			{
				this.owner = owner;
			}

			public void Dispose()
			{
				if( freeSceneObjectsCache != null )
					freeSceneObjectsCache.DeleteAll();

				DestroyCompiledData();
				disposed = true;
			}

			public override bool IsCompiled()
			{
				return compiledData != null;
			}

			void AddTrianglesInternal( bool allowBatching, Vertex[] vertices, VertexComponents vertexComponents,
				int[] indices, ref Vec3 position, ref Quat rotation, ref Vec3 scale, MaterialData material, bool cloneArrays )
			{
				if( vertices.Length != 0 && indices.Length != 0 )
				{
					InputItem item = new InputItem();
					item.dataType = InputItem.DataTypes.Triangles;
					if( cloneArrays )
						item.trianglesVertices = (Vertex[])vertices.Clone();
					else
						item.trianglesVertices = vertices;
					item.trianglesVertexComponents = vertexComponents;
					if( cloneArrays )
						item.trianglesIndices = (int[])indices.Clone();
					else
						item.trianglesIndices = indices;
					item.position = position;
					item.rotation = rotation;
					item.scale = scale;
					item.allowBatching = allowBatching;
					item.material = (MaterialDataImpl)material;
					inputData.Add( item );
				}
			}

			public override void AddTriangles( bool allowBatching, Vertex[] vertices, VertexComponents vertexComponents,
				int[] indices, Vec3 position, Quat rotation, Vec3 scale, MaterialData material )
			{
				if( disposed )
					Log.Fatal( "DynamicMeshManagerImpl: Block: AddTriangles: The block has been disposed." );
				if( indices.Length % 3 != 0 )
					Log.Fatal( "DynamicMeshManagerImpl: Block: AddTriangles: Invalid index count." );

				MaterialDataImpl materialImpl;
				if( material != null )
				{
					materialImpl = (MaterialDataImpl)material;
					if( materialImpl.owner != owner )
						Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddTriangles: Invalid material. material.owner != this." );
				}
				else
					materialImpl = null;

				if( IsCompiled() )
					return;

				AddTrianglesInternal( allowBatching, vertices, vertexComponents, indices, ref position, ref rotation, ref scale,
					material, true );
			}

			public override void AddTriangles( bool allowBatching, Vec3[] vertices, int[] indices, Vec3 position, Quat rotation,
				Vec3 scale, MaterialData material )
			{
				if( disposed )
					Log.Fatal( "DynamicMeshManagerImpl: Block: AddTriangles: The block has been disposed." );
				if( indices.Length % 3 != 0 )
					Log.Fatal( "DynamicMeshManagerImpl: Block: AddTriangles: Invalid index count." );

				MaterialDataImpl materialImpl;
				if( material != null )
				{
					materialImpl = (MaterialDataImpl)material;
					if( materialImpl.owner != owner )
						Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddTriangles: Invalid material. material.owner != this." );
				}
				else
					materialImpl = null;

				if( IsCompiled() )
					return;

				if( vertices.Length != 0 && indices.Length != 0 )
				{
					Vertex[] vertices2 = new Vertex[ vertices.Length ];
					for( int n = 0; n < vertices.Length; n++ )
					{
						Vertex vertex = new Vertex();
						vertex.position = vertices[ n ];
						vertices2[ n ] = vertex;
					}
					AddTrianglesInternal( allowBatching, vertices2, 0, (int[])indices.Clone(), ref position, ref rotation,
						ref scale, material, false );
				}
			}

			public override void AddMesh( bool allowBatching, string meshName, Vec3 position, Quat rotation, Vec3 scale,
				MaterialData overrideMaterial )
			{
				if( disposed )
					Log.Fatal( "DynamicMeshManagerImpl: Block: AddMesh: The block has been disposed." );

				MaterialDataImpl materialImpl;
				if( overrideMaterial != null )
				{
					materialImpl = (MaterialDataImpl)overrideMaterial;
					if( materialImpl.owner != owner )
						Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddMesh: Invalid material. overrideMaterial.owner != this." );
				}
				else
					materialImpl = null;

				if( IsCompiled() )
					return;

				InputItem item = new InputItem();
				item.dataType = InputItem.DataTypes.Mesh;
				item.meshName = meshName;
				item.position = position;
				item.rotation = rotation;
				item.scale = scale;
				item.allowBatching = allowBatching;
				item.material = materialImpl;
				inputData.Add( item );
			}

			CompiledItem CompileInputItems( List<InputItem> inputItems )
			{
				InputItem inputItem0 = inputItems[ 0 ];

				bool allTransformsAreEqual = true;
				if( inputItems.Count > 1 )
				{
					for( int n = 1; n < inputItems.Count; n++ )
					{
						InputItem item = inputItems[ n ];
						if( inputItem0.position != item.position || inputItem0.rotation != item.rotation ||
							inputItem0.scale != item.scale )
						{
							allTransformsAreEqual = false;
							break;
						}
					}
				}

				int totalVertices = 0;
				int totalIndices = 0;
				for( int n = 0; n < inputItems.Count; n++ )
				{
					InputItem item = inputItems[ n ];
					totalVertices += item.trianglesVertices.Length;
					totalIndices += item.trianglesIndices.Length;
				}

				CompiledItem compiledItem = new CompiledItem();

				uniqueMeshNameCounter++;
				if( uniqueMeshNameCounter == long.MaxValue )
					uniqueMeshNameCounter = 0;

				string meshName = MeshManager.Instance.GetUniqueName(
					string.Format( "DynamicMeshManagerImpl{0}", uniqueMeshNameCounter ) );
				compiledItem.mesh = MeshManager.Instance.CreateManual( meshName );

				SubMesh subMesh = compiledItem.mesh.CreateSubMesh();
				subMesh.UseSharedVertices = false;
				subMesh.MaterialName = "White";

				//init vertex data
				VertexData vertexData = new VertexData();
				VertexDeclaration declaration = vertexData.VertexDeclaration;

				int vertexSize = 12;
				declaration.AddElement( 0, 0, VertexElementType.Float3, VertexElementSemantic.Position );
				if( ( inputItem0.trianglesVertexComponents & VertexComponents.Normal ) != 0 )
				{
					declaration.AddElement( 0, 12, VertexElementType.Float3, VertexElementSemantic.Normal );
					vertexSize = 24;
				}
				if( ( inputItem0.trianglesVertexComponents & VertexComponents.Color ) != 0 )
				{
					declaration.AddElement( 0, 24, VertexElementType.Float4, VertexElementSemantic.Diffuse );
					vertexSize = 40;
				}
				if( ( inputItem0.trianglesVertexComponents & VertexComponents.TexCoord0 ) != 0 )
				{
					declaration.AddElement( 0, 40, VertexElementType.Float2, VertexElementSemantic.TextureCoordinates, 0 );
					vertexSize = 48;
				}
				if( ( inputItem0.trianglesVertexComponents & VertexComponents.TexCoord1 ) != 0 )
				{
					declaration.AddElement( 0, 48, VertexElementType.Float2, VertexElementSemantic.TextureCoordinates, 1 );
					vertexSize = 56;
				}
				if( ( inputItem0.trianglesVertexComponents & VertexComponents.TexCoord2 ) != 0 )
				{
					declaration.AddElement( 0, 56, VertexElementType.Float2, VertexElementSemantic.TextureCoordinates, 2 );
					vertexSize = 64;
				}
				if( ( inputItem0.trianglesVertexComponents & VertexComponents.TexCoord3 ) != 0 )
				{
					declaration.AddElement( 0, 64, VertexElementType.Float2, VertexElementSemantic.TextureCoordinates, 3 );
					vertexSize = 72;
				}
				if( ( inputItem0.trianglesVertexComponents & VertexComponents.Tangent ) != 0 )
				{
					declaration.AddElement( 0, 72, VertexElementType.Float4, VertexElementSemantic.Tangent );
					vertexSize = 88;
				}

				VertexBufferBinding bufferBinding = vertexData.VertexBufferBinding;
				HardwareVertexBuffer vertexBuffer = HardwareBufferManager.Instance.CreateVertexBuffer( vertexSize, totalVertices,
					HardwareBuffer.Usage.StaticWriteOnly );
				bufferBinding.SetBinding( 0, vertexBuffer, true );
				vertexData.VertexCount = totalVertices;

				Bounds bounds = Bounds.Cleared;
				float boundingRadiusSqr = .001f;

				//copy data to the vertex buffer, apply transform, calculate bounds
				unsafe
				{
					IntPtr buffer = vertexBuffer.Lock( HardwareBuffer.LockOptions.Normal );

					byte* destPointer = (byte*)buffer;

					int vertexPosition = 0;

					for( int nInputItem = 0; nInputItem < inputItems.Count; nInputItem++ )
					{
						InputItem item = inputItems[ nInputItem ];

						fixed( Vertex* pVertices = item.trianglesVertices )
						{
							Mat4 transform = Mat4.Zero;
							Mat3 transformRotation = Mat3.Zero;
							if( !allTransformsAreEqual )
							{
								transformRotation = item.rotation.ToMat3();
								if( item.scale != Vec3.One )
									transformRotation *= Mat3.FromScale( item.scale );
								transform = new Mat4( transformRotation, item.position );
							}

							Vertex* sourcePointer = pVertices;
							for( int nVertex = 0; nVertex < item.trianglesVertices.Length; nVertex++ )
							{
								NativeUtils.CopyMemory( (IntPtr)destPointer, (IntPtr)sourcePointer, vertexSize );

								Vertex* pVertex = (Vertex*)destPointer;

								//apply transform
								if( !allTransformsAreEqual )
								{
									pVertex->position = transform * pVertex->position;
									if( ( inputItem0.trianglesVertexComponents & VertexComponents.Normal ) != 0 )
										pVertex->normal = transformRotation * pVertex->normal;
									if( ( inputItem0.trianglesVertexComponents & VertexComponents.Tangent ) != 0 )
										pVertex->tangent = new Vec4( transformRotation * pVertex->tangent.ToVec3(), pVertex->tangent.W );
								}

								//calculate bounds
								bounds.Add( pVertex->position );
								float radiusSqr = pVertex->position.LengthSqr();
								if( radiusSqr > boundingRadiusSqr )
									boundingRadiusSqr = radiusSqr;

								sourcePointer++;
								destPointer += vertexSize;
							}
						}
						vertexPosition += item.trianglesVertices.Length;
					}

					vertexBuffer.Unlock();

					if( vertexPosition != totalVertices )
						Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: CompileInputItems: vertexPosition != totalVertices." );
				}

				subMesh.VertexData = vertexData;

				//init index buffer
				{
					int[] indices = new int[ totalIndices ];

					int indexPosition = 0;
					int vertexPosition = 0;
					for( int nInputItem = 0; nInputItem < inputItems.Count; nInputItem++ )
					{
						InputItem item = inputItems[ nInputItem ];
						for( int nIndex = 0; nIndex < item.trianglesIndices.Length; nIndex++ )
						{
							indices[ indexPosition ] = item.trianglesIndices[ nIndex ] + vertexPosition;
							indexPosition++;
						}
						vertexPosition += item.trianglesVertices.Length;
					}

					subMesh.IndexData = IndexData.CreateFromArray( indices, 0, indices.Length, false );

					if( indexPosition != totalIndices )
						Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: CompileInputItems: indexPosition != totalIndices." );
				}

				compiledItem.mesh.SetBoundsAndRadius( bounds, MathFunctions.Sqrt( boundingRadiusSqr ) );
				compiledItem.needDisposeMesh = true;

				if( allTransformsAreEqual )
				{
					compiledItem.position = inputItem0.position;
					compiledItem.rotation = inputItem0.rotation;
					compiledItem.scale = inputItem0.scale;
				}
				else
				{
					compiledItem.rotation = Quat.Identity;
					compiledItem.scale = Vec3.One;
				}

				compiledItem.material = inputItem0.material;

				return compiledItem;
			}

			public override void AddBox( bool allowBatching, Box box, MaterialData material )
			{
				if( disposed )
					Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddBox: The block has been disposed." );

				MaterialDataImpl materialImpl;
				if( material != null )
				{
					materialImpl = (MaterialDataImpl)material;
					if( materialImpl.owner != owner )
						Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddBox: Invalid material. material.owner != this." );
				}
				else
					materialImpl = null;

				if( IsCompiled() )
					return;

				if( boxVertices == null )
				{
					boxVertices = new Vertex[]
					{
						new Vertex( new Vec3( -1, 1, -1 ), new Vec3( 0, 1, 0 ) ),
						new Vertex( new Vec3( -1, 1, 1 ), new Vec3( 0, 1, 0 ) ),
						new Vertex( new Vec3( 1, 1, 1 ), new Vec3( 0, 1, 0 ) ),
						new Vertex( new Vec3( 1, 1, -1 ), new Vec3( 0, 1, 0 ) ),
						new Vertex( new Vec3( -1, -1, -1 ), new Vec3( 0, -1, 0 ) ),
						new Vertex( new Vec3( 1, -1, -1 ), new Vec3( 0, -1, 0 ) ),
						new Vertex( new Vec3( 1, -1, 1 ), new Vec3( 0, -1, 0 ) ),
						new Vertex( new Vec3( -1, -1, 1 ), new Vec3( 0, -1, 0 ) ),
						new Vertex( new Vec3( -1, 1, -1 ), new Vec3( 0, 0, -1 ) ),
						new Vertex( new Vec3( 1, 1, -1 ), new Vec3( 0, 0, -1 ) ),
						new Vertex( new Vec3( 1, -1, -1 ), new Vec3( 0, 0, -1 ) ),
						new Vertex( new Vec3( -1, -1, -1 ), new Vec3( 0, 0, -1 ) ),
						new Vertex( new Vec3( 1, 1, -1 ), new Vec3( 1, 0, 0 ) ),
						new Vertex( new Vec3( 1, 1, 1 ), new Vec3( 1, 0, 0 ) ),
						new Vertex( new Vec3( 1, -1, 1 ), new Vec3( 1, 0, 0 ) ),
						new Vertex( new Vec3( 1, -1, -1 ), new Vec3( 1, 0, 0 ) ),
						new Vertex( new Vec3( 1, 1, 1 ), new Vec3( 0, 0, 1 ) ),
						new Vertex( new Vec3( -1, 1, 1 ), new Vec3( 0, 0, 1 ) ),
						new Vertex( new Vec3( -1, -1, 1 ), new Vec3( 0, 0, 1 ) ),
						new Vertex( new Vec3( 1, -1, 1 ), new Vec3( 0, 0, 1 ) ),
						new Vertex( new Vec3( -1, 1, 1 ), new Vec3( -1, 0, 0 ) ),
						new Vertex( new Vec3( -1, 1, -1 ), new Vec3( -1, 0, 0 ) ),
						new Vertex( new Vec3( -1, -1, -1 ), new Vec3( -1, 0, 0 ) ),
						new Vertex( new Vec3( -1, -1, 1 ), new Vec3( -1, 0, 0 ) )
					};
				}

				if( boxIndices == null )
				{
					boxIndices = new int[] { 
						0, 1, 2, 2, 
						3, 0, 4, 5, 
						6, 6, 7, 4, 
						8, 9, 10, 10,
						11, 8, 12, 13, 
						14, 14, 15, 12, 
						16, 17, 18, 18, 
						19, 16, 20, 21, 
						22, 22, 23, 20 };
				}

				Vec3 position = box.Center;
				Quat rotation = box.Axis.ToQuat();
				Vec3 scale = box.Extents;

				AddTrianglesInternal( allowBatching, boxVertices, VertexComponents.Normal, DynamicMeshManagerImpl.boxIndices,
					ref position, ref rotation, ref scale, material, false );
			}

			public override void AddSphere( bool allowBatching, Vec3 position, Quat rotation, float radius, int hSegments,
				int vSegments, MaterialData material )
			{
				//TO DO: add caching for hSegments, vSegments

				if( disposed )
					Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddSphere: The block has been disposed." );

				if( hSegments < 3 )
					Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddSphere: hSegments < 3." );
				if( vSegments < 2 )
					Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddSphere: vSegments < 2." );
				if( vSegments % 2 != 0 )
					Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddSphere: vSegments % 2 != 0." );

				MaterialDataImpl materialImpl;
				if( material != null )
				{
					materialImpl = (MaterialDataImpl)material;
					if( materialImpl.owner != owner )
						Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddSphere: Invalid material. material.owner != this." );
				}
				else
					materialImpl = null;

				if( IsCompiled() )
					return;

				Vertex[] vertices;
				int[] indices;

				//vertices
				{
					int vertexCount = hSegments * ( vSegments - 1 ) + 2;
					vertices = new Vertex[ vertexCount ];

					float[] cosTable = new float[ hSegments ];
					float[] sinTable = new float[ hSegments ];
					{
						float angleStep = MathFunctions.PI * 2 / hSegments;
						for( int n = 0; n < hSegments; n++ )
						{
							float angle = angleStep * n;
							cosTable[ n ] = MathFunctions.Cos( angle );
							sinTable[ n ] = MathFunctions.Sin( angle );
						}
					}

					int currentVertex = 0;

					int levelCount = vSegments + 1;

					for( int v = 0; v < levelCount; v++ )
					{
						if( v == 0 )
						{
							Vertex vertex = new Vertex();
							vertex.position = new Vec3( 0, 0, 1 );
							vertex.normal = vertex.position;
							vertices[ currentVertex++ ] = vertex;
						}
						else if( v == vSegments )
						{
							Vertex vertex = new Vertex();
							vertex.position = new Vec3( 0, 0, -1 );
							vertex.normal = vertex.position;
							vertices[ currentVertex++ ] = vertex;
						}
						else
						{
							float c = ( (float)v / (float)vSegments );
							float angle = -( c * 2 - 1 ) * MathFunctions.PI / 2;
							float hRadius = MathFunctions.Cos( angle );
							float h = MathFunctions.Sin( angle );

							for( int n = 0; n < hSegments; n++ )
							{
								Vertex vertex = new Vertex();
								vertex.position = new Vec3( cosTable[ n ] * hRadius, sinTable[ n ] * hRadius, h );
								vertex.normal = vertex.position;
								vertices[ currentVertex++ ] = vertex;
							}
						}
					}

					if( vertices.Length != currentVertex )
						Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddSphere: vertices.Length != currentVertex." );
				}

				//indices
				{
					int indexCount = hSegments * ( vSegments - 2 ) * 2 * 3 + hSegments * 3 + hSegments * 3;
					indices = new int[ indexCount ];

					int levelCount = vSegments + 1;
					int currentIndex = 0;

					for( int v = 0; v < levelCount - 1; v++ )
					{
						int index;
						int nextIndex;

						if( v != 0 )
						{
							index = 1 + ( v - 1 ) * hSegments;
							nextIndex = index + hSegments;
						}
						else
						{
							index = 0;
							nextIndex = 1;
						}

						for( int n = 0; n < hSegments; n++ )
						{
							int start = n;
							int end = ( n + 1 ) % hSegments;

							if( v == 0 )
							{

								indices[ currentIndex++ ] = index;
								indices[ currentIndex++ ] = nextIndex + start;
								indices[ currentIndex++ ] = nextIndex + end;
							}
							else if( v == vSegments - 1 )
							{
								indices[ currentIndex++ ] = index + end;
								indices[ currentIndex++ ] = index + start;
								indices[ currentIndex++ ] = nextIndex;
							}
							else
							{
								indices[ currentIndex++ ] = index + end;
								indices[ currentIndex++ ] = index + start;
								indices[ currentIndex++ ] = nextIndex + end;

								indices[ currentIndex++ ] = nextIndex + start;
								indices[ currentIndex++ ] = nextIndex + end;
								indices[ currentIndex++ ] = index + start;
							}
						}
					}

					if( indices.Length != currentIndex )
						Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddSphere: indices.Length != currentIndex." );
				}

				Vec3 scale = new Vec3( radius, radius, radius );
				AddTrianglesInternal( allowBatching, vertices, VertexComponents.Normal, indices, ref position, ref rotation,
					ref scale, material, false );
			}

			public override void AddCone( bool allowBatching, Vec3 position, Quat rotation, float length, float radius,
				int segments, bool addSide, bool addBottom, MaterialData material )
			{
				if( !addSide && !addBottom )
					return;

				if( disposed )
					Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddCone: The block has been disposed." );

				//if( radius <= 0 )
				//   Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddCone: radius <= 0." );
				//if( height <= 0 )
				//   Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddCone: height <= 0." );
				if( segments < 3 )
					Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddCone: segments < 3." );

				MaterialDataImpl materialImpl;
				if( material != null )
				{
					materialImpl = (MaterialDataImpl)material;
					if( materialImpl.owner != owner )
						Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddCone: Invalid material. material.owner != this." );
				}
				else
					materialImpl = null;

				if( IsCompiled() )
					return;

				Vertex[] vertices;
				int[] indices;

				int bottomIndex = 0;

				//vertices
				{
					int vertexCount = 0;
					if( addSide )
						vertexCount += segments * 2;
					if( addBottom )
						vertexCount += segments + 1;
					vertices = new Vertex[ vertexCount ];

					float[] cosTable = new float[ segments ];
					float[] sinTable = new float[ segments ];
					{
						float angleStep = MathFunctions.PI * 2 / segments;
						for( int n = 0; n < segments; n++ )
						{
							float angle = angleStep * n;
							cosTable[ n ] = MathFunctions.Cos( angle );
							sinTable[ n ] = MathFunctions.Sin( angle );
						}
					}

					int currentVertex = 0;

					if( addSide )
					{
						for( int n = 0; n < segments; n++ )
						{
							Vec3 normal;
							if( length != 0 )
							{
								normal = new Vec3( radius / length, cosTable[ n ], sinTable[ n ] );
								normal.Normalize();
							}
							else
								normal = new Vec3( 1, 0, 0 );

							{
								Vertex vertex = new Vertex();
								vertex.position = new Vec3( length, 0, 0 );
								vertex.normal = normal;
								vertices[ currentVertex++ ] = vertex;
							}

							{
								Vertex vertex = new Vertex();
								vertex.position = new Vec3( 0, cosTable[ n ] * radius, sinTable[ n ] * radius );
								vertex.normal = normal;
								vertices[ currentVertex++ ] = vertex;
							}
						}
					}

					if( addBottom )
					{
						{
							bottomIndex = currentVertex;
							Vertex vertex = new Vertex();
							vertex.position = new Vec3( 0, 0, 0 );
							vertex.normal = new Vec3( -1, 0, 0 );
							vertices[ currentVertex++ ] = vertex;
						}

						for( int n = 0; n < segments; n++ )
						{
							Vertex vertex = new Vertex();
							vertex.position = new Vec3( 0, cosTable[ n ] * radius, sinTable[ n ] * radius );
							vertex.normal = new Vec3( -1, 0, 0 );
							vertices[ currentVertex++ ] = vertex;
						}
					}

					if( vertices.Length != currentVertex )
						Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddCone: vertices.Length != currentVertex." );
				}

				//indices
				{
					int indexCount = 0;
					if( addSide )
						indexCount += segments * 3;
					if( addBottom )
						indexCount += segments * 3;
					indices = new int[ indexCount ];

					int currentIndex = 0;

					if( addSide )
					{
						for( int n = 0; n < segments; n++ )
						{
							indices[ currentIndex++ ] = n * 2;
							indices[ currentIndex++ ] = n * 2 + 1;
							indices[ currentIndex++ ] = ( ( n + 1 ) % segments ) * 2 + 1;
						}
					}
					if( addBottom )
					{
						for( int n = 0; n < segments; n++ )
						{
							indices[ currentIndex++ ] = bottomIndex + 1 + ( n + 1 ) % segments;
							indices[ currentIndex++ ] = bottomIndex + 1 + n;
							indices[ currentIndex++ ] = bottomIndex;
						}
					}

					if( indices.Length != currentIndex )
						Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddCone: indices.Length != currentIndex." );
				}

				Vec3 scale = Vec3.One;
				AddTrianglesInternal( allowBatching, vertices, VertexComponents.Normal, indices, ref position, ref rotation,
					ref scale, material, false );
			}

			public override void AddCapsule( bool allowBatching, Vec3 position, Quat rotation, float length, float radius,
				int hSegments, int vSegments, MaterialData material )
			{
				if( disposed )
					Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddCapsule: The block has been disposed." );

				//if( radius <= 0 )
				//   Log.Fatal( "GeometryGenerator: GenerateCapsule: radius <= 0." );
				//if( height <= 0 )
				//   Log.Fatal( "GeometryGenerator: GenerateCapsule: height <= 0." );
				if( hSegments < 3 )
					Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddCapsule: hSegments < 3." );
				if( vSegments < 3 )
					Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddCapsule: vSegments < 3." );
				if( vSegments % 2 != 1 )
					Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddCapsule: vSegments % 2 != 1." );

				MaterialDataImpl materialImpl;
				if( material != null )
				{
					materialImpl = (MaterialDataImpl)material;
					if( materialImpl.owner != owner )
						Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddCapsule: Invalid material. material.owner != this." );
				}
				else
					materialImpl = null;

				if( IsCompiled() )
					return;

				Vertex[] vertices;
				int[] indices;

				//vertices
				{
					int vertexCount = hSegments * ( vSegments - 1 ) + 2;
					vertices = new Vertex[ vertexCount ];

					float[] cosTable = new float[ hSegments ];
					float[] sinTable = new float[ hSegments ];
					{
						float angleStep = MathFunctions.PI * 2 / hSegments;
						for( int n = 0; n < hSegments; n++ )
						{
							float angle = angleStep * n;
							cosTable[ n ] = MathFunctions.Cos( angle );
							sinTable[ n ] = MathFunctions.Sin( angle );
						}
					}

					int currentVertex = 0;
					int levelCount = vSegments + 1;

					for( int v = 0; v < levelCount; v++ )
					{
						if( v == 0 )
						{
							Vertex vertex = new Vertex();
							vertex.position = new Vec3( 0, 0, radius + length * .5f );
							vertex.normal = new Vec3( 0, 0, 1 );
							vertices[ currentVertex++ ] = vertex;
						}
						else if( v == vSegments )
						{
							Vertex vertex = new Vertex();
							vertex.position = new Vec3( 0, 0, -radius - length * .5f );
							vertex.normal = new Vec3( 0, 0, -1 );
							vertices[ currentVertex++ ] = vertex;
						}
						else
						{
							bool top = v <= vSegments / 2;
							float c;
							if( top )
								c = ( (float)v / (float)( vSegments - 1 ) );
							else
								c = ( (float)( v - 1 ) / (float)( vSegments - 1 ) );
							float angle = -( c * 2 - 1 ) * MathFunctions.PI / 2;
							float hRadius = MathFunctions.Cos( angle ) * radius;
							float h = MathFunctions.Sin( angle ) * radius + ( top ? length * .5f : -length * .5f );

							for( int n = 0; n < hSegments; n++ )
							{
								Vertex vertex = new Vertex();
								vertex.position = new Vec3( cosTable[ n ] * hRadius, sinTable[ n ] * hRadius, h );
								vertex.normal = new Vec3( cosTable[ n ] * hRadius, sinTable[ n ] * hRadius,
									MathFunctions.Sin( angle ) * radius ).GetNormalize();
								vertices[ currentVertex++ ] = vertex;
							}
						}
					}

					if( vertices.Length != currentVertex )
						Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddCapsule: vertices.Length != currentVertex." );
				}

				//indices
				{
					int indexCount = hSegments * ( vSegments - 2 ) * 2 * 3 + hSegments * 3 + hSegments * 3;
					indices = new int[ indexCount ];

					int levelCount = vSegments + 1;
					int currentIndex = 0;

					for( int v = 0; v < levelCount - 1; v++ )
					{
						int index;
						int nextIndex;

						if( v != 0 )
						{
							index = 1 + ( v - 1 ) * hSegments;
							nextIndex = index + hSegments;
						}
						else
						{
							index = 0;
							nextIndex = 1;
						}

						for( int n = 0; n < hSegments; n++ )
						{
							int start = n;
							int end = ( n + 1 ) % hSegments;

							if( v == 0 )
							{
								indices[ currentIndex++ ] = index;
								indices[ currentIndex++ ] = nextIndex + start;
								indices[ currentIndex++ ] = nextIndex + end;
							}
							else if( v == vSegments - 1 )
							{
								indices[ currentIndex++ ] = index + end;
								indices[ currentIndex++ ] = index + start;
								indices[ currentIndex++ ] = nextIndex;
							}
							else
							{
								indices[ currentIndex++ ] = index + end;
								indices[ currentIndex++ ] = index + start;
								indices[ currentIndex++ ] = nextIndex + end;

								indices[ currentIndex++ ] = nextIndex + start;
								indices[ currentIndex++ ] = nextIndex + end;
								indices[ currentIndex++ ] = index + start;
							}
						}
					}

					if( indices.Length != currentIndex )
						Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddCapsule: indices.Length != currentIndex." );
				}

				Vec3 scale = Vec3.One;
				AddTrianglesInternal( allowBatching, vertices, VertexComponents.Normal, indices, ref position, ref rotation,
					ref scale, material, false );
			}

			public override void AddCylinder( bool allowBatching, Vec3 position, Quat rotation, float height, float radius,
				int segments, bool addTop, bool addSide, bool addBottom, MaterialData material )
			{
				if( disposed )
					Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddCylinder: The block has been disposed." );

				//if( radius <= 0 )
				//   Log.Fatal( "GeometryGenerator: GenerateCylinder: radius <= 0." );
				//if( height <= 0 )
				//   Log.Fatal( "GeometryGenerator: GenerateCylinder: height <= 0." );
				if( segments < 3 )
					Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddCylinder: segments < 3." );

				MaterialDataImpl materialImpl;
				if( material != null )
				{
					materialImpl = (MaterialDataImpl)material;
					if( materialImpl.owner != owner )
						Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddCylinder: Invalid material. material.owner != this." );
				}
				else
					materialImpl = null;

				if( IsCompiled() )
					return;

				Vertex[] vertices;
				int[] indices;

				int topIndex = 0;
				int topStartIndex = 0;

				int bottomIndex = 0;
				int bottomStartIndex = 0;

				int sideTopIndex = 0;
				int sideBottomIndex = 0;

				//vertices
				{
					int vertexCount = 0;

					if( addSide )
						vertexCount += segments * 2;
					if( addTop )
						vertexCount += segments + 1;
					if( addBottom )
						vertexCount += segments + 1;
					vertices = new Vertex[ vertexCount ];

					float[] cosTable = new float[ segments ];
					float[] sinTable = new float[ segments ];
					{
						float angleStep = MathFunctions.PI * 2 / segments;
						for( int n = 0; n < segments; n++ )
						{
							float angle = angleStep * n;
							cosTable[ n ] = MathFunctions.Cos( angle );
							sinTable[ n ] = MathFunctions.Sin( angle );
						}
					}

					int currentVertex = 0;

					if( addSide )
					{
						sideTopIndex = currentVertex;
						for( int n = 0; n < segments; n++ )
						{
							vertices[ currentVertex++ ] = new Vertex(
								new Vec3( cosTable[ n ] * radius, sinTable[ n ] * radius, height * .5f ),
								new Vec3( cosTable[ n ], sinTable[ n ], 0 ) );
						}

						sideBottomIndex = currentVertex;
						for( int n = 0; n < segments; n++ )
						{
							vertices[ currentVertex++ ] = new Vertex(
								new Vec3( cosTable[ n ] * radius, sinTable[ n ] * radius, -height * .5f ),
								new Vec3( cosTable[ n ], sinTable[ n ], 0 ) );
						}
					}

					if( addTop )
					{
						topStartIndex = currentVertex;
						for( int n = 0; n < segments; n++ )
						{
							vertices[ currentVertex++ ] = new Vertex(
								new Vec3( cosTable[ n ] * radius, sinTable[ n ] * radius, height * .5f ),
								new Vec3( 0, 0, 1 ) );
						}

						topIndex = currentVertex;
						vertices[ currentVertex++ ] = new Vertex(
							new Vec3( 0, 0, height * .5f ),
							new Vec3( 0, 0, 1 ) );
					}

					if( addBottom )
					{
						bottomStartIndex = currentVertex;
						for( int n = 0; n < segments; n++ )
						{
							vertices[ currentVertex++ ] = new Vertex(
								new Vec3( cosTable[ n ] * radius, sinTable[ n ] * radius, -height * .5f ),
								new Vec3( 0, 0, -1 ) );
						}

						bottomIndex = currentVertex;
						vertices[ currentVertex++ ] = new Vertex(
							new Vec3( 0, 0, -height * .5f ),
							new Vec3( 0, 0, -1 ) );
					}

					if( vertices.Length != currentVertex )
						Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddCylinder: vertices.Length != currentVertex." );
				}

				//indices
				{
					int indexCount = 0;

					if( addSide )
						indexCount += segments * 2 * 3;
					if( addTop )
						indexCount += segments * 3;
					if( addBottom )
						indexCount += segments * 3;

					indices = new int[ indexCount ];

					int currentIndex = 0;

					if( addSide )
					{
						for( int n = 0; n < segments; n++ )
						{
							int start = n;
							int end = ( n + 1 ) % segments;

							indices[ currentIndex++ ] = sideTopIndex + end;
							indices[ currentIndex++ ] = sideTopIndex + start;
							indices[ currentIndex++ ] = sideBottomIndex + start;

							indices[ currentIndex++ ] = sideBottomIndex + start;
							indices[ currentIndex++ ] = sideBottomIndex + end;
							indices[ currentIndex++ ] = sideTopIndex + end;
						}
					}
					if( addTop )
					{
						for( int n = 0; n < segments; n++ )
						{
							int start = n;
							int end = ( n + 1 ) % segments;

							indices[ currentIndex++ ] = topStartIndex + start;
							indices[ currentIndex++ ] = topStartIndex + end;
							indices[ currentIndex++ ] = topIndex;
						}
					}
					if( addBottom )
					{
						for( int n = 0; n < segments; n++ )
						{
							int start = n;
							int end = ( n + 1 ) % segments;

							indices[ currentIndex++ ] = bottomStartIndex + end;
							indices[ currentIndex++ ] = bottomStartIndex + start;
							indices[ currentIndex++ ] = bottomIndex;
						}
					}

					if( indices.Length != currentIndex )
						Log.Fatal( "DynamicMeshManagerImpl: BlockImpl: AddCylinder: indices.Length != currentIndex." );
				}

				Vec3 scale = Vec3.One;
				AddTrianglesInternal( allowBatching, vertices, VertexComponents.Normal, indices, ref position, ref rotation,
					ref scale, material, false );
			}

			public override void AddLine( bool allowBatching, Vec3 start, Vec3 end, float thickness, int segments, bool addCaps,
				MaterialData material )
			{
				Vec3 position = ( start + end ) * .5f;
				Vec3 direction = end - start;
				float length = direction.Normalize();
				length += thickness;
				Quat rotation = Quat.FromDirectionZAxisUp( direction ) * Mat3.FromRotateByY( MathFunctions.PI / 2 ).ToQuat();
				AddCylinder( allowBatching, position, rotation, length, thickness * .5f, segments, addCaps, true, addCaps, material );
			}

			unsafe static Vertex[] GetVerticesFromVertexData( VertexData vertexData, out VertexComponents components )
			{
				int vertexCount = vertexData.VertexCount;

				components = 0;
				Vertex[] vertices = new Vertex[ vertexCount ];

				fixed( Vertex* pVertices = vertices )
				{
					//enumerate source buffers
					for( int source = 0; source < vertexData.VertexBufferBinding.GetBufferCount(); source++ )
					{
						if( vertexData.VertexBufferBinding.IsBufferBound( source ) )
						{
							HardwareVertexBuffer buffer = vertexData.VertexBufferBinding.GetBuffer( source );
							int vertexSize = buffer.VertexSizeInBytes;
							byte* sourcePointer = (byte*)buffer.Lock( HardwareBuffer.LockOptions.ReadOnly );
							sourcePointer += vertexData.VertexStart * vertexSize;

							foreach( VertexElement element in vertexData.VertexDeclaration.GetElements() )
							{
								if( element.Source == source )
								{
									//positions
									if( element.Semantic == VertexElementSemantic.Position )
									{
										if( element.Type == VertexElementType.Float3 )
										{
											//copy data
											Vertex* pVertex = pVertices;
											byte* pointer = sourcePointer + element.Offset;
											for( int n = 0; n < vertexCount; n++ )
											{
												pVertex->position = *(Vec3*)pointer;
												pVertex++;
												pointer += vertexSize;
											}
										}
										else if( element.Type == VertexElementType.Float2 )
										{
											//copy data
											Vertex* pVertex = pVertices;
											byte* pointer = sourcePointer + element.Offset;
											for( int n = 0; n < vertexCount; n++ )
											{
												pVertex->position = new Vec3( *(Vec2*)pointer, 0 );
												pVertex++;
												pointer += vertexSize;
											}
										}
									}
									//normals
									else if( element.Semantic == VertexElementSemantic.Normal && element.Type == VertexElementType.Float3 )
									{
										components |= VertexComponents.Normal;

										//copy data
										Vertex* pVertex = pVertices;
										byte* pointer = sourcePointer + element.Offset;
										for( int n = 0; n < vertexCount; n++ )
										{
											pVertex->normal = *(Vec3*)pointer;
											pVertex++;
											pointer += vertexSize;
										}
									}
									//texture coordinates
									else if( element.Semantic == VertexElementSemantic.TextureCoordinates &&
										element.Type == VertexElementType.Float2 )
									{
										switch( element.Index )
										{
										case 0:
											{
												components |= VertexComponents.TexCoord0;

												//copy data
												Vertex* pVertex = pVertices;
												byte* pointer = sourcePointer + element.Offset;
												for( int n = 0; n < vertexCount; n++ )
												{
													pVertex->texCoord0 = *(Vec2*)pointer;
													pVertex++;
													pointer += vertexSize;
												}
											}
											break;

										case 1:
											{
												components |= VertexComponents.TexCoord1;

												//copy data
												Vertex* pVertex = pVertices;
												byte* pointer = sourcePointer + element.Offset;
												for( int n = 0; n < vertexCount; n++ )
												{
													pVertex->texCoord1 = *(Vec2*)pointer;
													pVertex++;
													pointer += vertexSize;
												}
											}
											break;

										case 2:
											{
												components |= VertexComponents.TexCoord2;

												//copy data
												Vertex* pVertex = pVertices;
												byte* pointer = sourcePointer + element.Offset;
												for( int n = 0; n < vertexCount; n++ )
												{
													pVertex->texCoord2 = *(Vec2*)pointer;
													pVertex++;
													pointer += vertexSize;
												}
											}
											break;

										case 3:
											{
												components |= VertexComponents.TexCoord3;

												//copy data
												Vertex* pVertex = pVertices;
												byte* pointer = sourcePointer + element.Offset;
												for( int n = 0; n < vertexCount; n++ )
												{
													pVertex->texCoord3 = *(Vec2*)pointer;
													pVertex++;
													pointer += vertexSize;
												}
											}
											break;
										}
									}
									//tangents
									else if( element.Semantic == VertexElementSemantic.Tangent &&
										element.Type == VertexElementType.Float4 )
									{
										components |= VertexComponents.Tangent;

										//copy data
										Vertex* pVertex = pVertices;
										byte* pointer = sourcePointer + element.Offset;
										for( int n = 0; n < vertexCount; n++ )
										{
											pVertex->tangent = *(Vec4*)pointer;
											pVertex++;
											pointer += vertexSize;
										}
									}
									//color
									else if( element.Semantic == VertexElementSemantic.Diffuse )
									{
										if( element.Type == VertexElementType.Float4 )
										{
											components |= VertexComponents.Color;

											//copy data
											Vertex* pVertex = pVertices;
											byte* pointer = sourcePointer + element.Offset;
											for( int n = 0; n < vertexCount; n++ )
											{
												pVertex->color = new ColorValue( *(Vec4*)pointer );
												pVertex++;
												pointer += vertexSize;
											}
										}
										else if( element.Type == VertexElementType.Float3 )
										{
											components |= VertexComponents.Color;

											//copy data
											Vertex* pVertex = pVertices;
											byte* pointer = sourcePointer + element.Offset;
											for( int n = 0; n < vertexCount; n++ )
											{
												Vec3 v = *(Vec3*)pointer;
												pVertex->color = new ColorValue( v.X, v.Y, v.Z, 1 );
												pVertex++;
												pointer += vertexSize;
											}
										}
										else if( element.Type == VertexElementType.Color || element.Type == VertexElementType.ColorABGR ||
											element.Type == VertexElementType.ColorARGB )//|| element.Type == VertexElementType.UByte4 )
										{
											components |= VertexComponents.Color;

											VertexElementType elementType = element.Type;
											if( elementType == VertexElementType.Color )
											{
												if( RenderSystem.Instance.IsDirect3D() )
													elementType = VertexElementType.ColorARGB;
												else
													elementType = VertexElementType.ColorABGR;
											}

											if( elementType == VertexElementType.ColorARGB )
											{
												//copy data
												Vertex* pVertex = pVertices;
												byte* pointer = sourcePointer + element.Offset;
												for( int n = 0; n < vertexCount; n++ )
												{
													uint color = *(uint*)pointer;
													float a = ( ( color & 0xFF000000 ) >> 24 ) / 255.0f;
													float r = ( ( color & 0x00FF0000 ) >> 16 ) / 255.0f;
													float g = ( ( color & 0x0000FF00 ) >> 8 ) / 255.0f;
													float b = ( ( color & 0x000000FF ) >> 0 ) / 255.0f;
													pVertex->color = new ColorValue( r, g, b, a );
													pVertex++;
													pointer += vertexSize;
												}
											}
											else
											{
												//copy data
												Vertex* pVertex = pVertices;
												byte* pointer = sourcePointer + element.Offset;
												for( int n = 0; n < vertexCount; n++ )
												{
													uint color = *(uint*)pointer;
													float a = ( ( color & 0xFF000000 ) >> 24 ) / 255.0f;
													float b = ( ( color & 0x00FF0000 ) >> 16 ) / 255.0f;
													float g = ( ( color & 0x0000FF00 ) >> 8 ) / 255.0f;
													float r = ( ( color & 0x000000FF ) >> 0 ) / 255.0f;
													pVertex->color = new ColorValue( r, g, b, a );
													pVertex++;
													pointer += vertexSize;
												}
											}
										}
									}
								}
							}

							buffer.Unlock();
						}
					}
				}

				return vertices;
			}

			public unsafe void Compile()
			{
				if( IsCompiled() )
					return;

				DestroyCompiledData();

				List<CompiledItem> compiledDataList = new List<CompiledItem>();

				Dictionary<InputItem, List<InputItem>> batches =
					new Dictionary<InputItem, List<InputItem>>( inputData.Count, compileBatchComparer );

				foreach( InputItem inputItem in inputData )
				{
					if( inputItem.dataType == InputItem.DataTypes.Mesh )
					{
						if( !string.IsNullOrEmpty( inputItem.meshName ) )
						{
							//meshes

							//load mesh
							Mesh mesh = MeshManager.Instance.GetByName( inputItem.meshName );
							if( mesh == null )
								mesh = MeshManager.Instance.Load( inputItem.meshName );
							if( mesh == null )
							{
								Log.Warning( "DynamicMeshManagerImpl: Compile: Unable to load mesh with name \"{0}\".",
									inputItem.meshName );
								continue;
							}

							if( inputItem.allowBatching )
							{
								//make batches, compile later.

								//create input items from the mesh
								List<InputItem> meshInputItems = new List<InputItem>( mesh.SubMeshes.Length );
								foreach( SubMesh subMesh in mesh.SubMeshes )
								{
									InputItem item = new InputItem();
									item.dataType = InputItem.DataTypes.Triangles;

									//create triangles, indices from sub mesh
									VertexData vertexData = subMesh.UseSharedVertices ? mesh.SharedVertexData : subMesh.VertexData;
									item.trianglesVertices = GetVerticesFromVertexData( vertexData, out item.trianglesVertexComponents );
									item.trianglesIndices = subMesh.IndexData.GetIndices();
									item.position = inputItem.position;
									item.rotation = inputItem.rotation;
									item.scale = inputItem.scale;
									item.allowBatching = false;

									if( inputItem.material != null )
										item.material = inputItem.material;
									else
										item.material = (MaterialDataImpl)owner.CreateMaterial( subMesh.MaterialName );

									meshInputItems.Add( item );
								}

								//add input items to the batches dictionary
								foreach( InputItem meshInputItem in meshInputItems )
								{
									List<InputItem> list;
									if( !batches.TryGetValue( meshInputItem, out list ) )
									{
										list = new List<InputItem>();
										batches.Add( meshInputItem, list );
									}
									list.Add( meshInputItem );
								}
							}
							else
							{
								//compile non batched items
								CompiledItem compiledItem = new CompiledItem();
								compiledItem.mesh = mesh;
								compiledItem.needDisposeMesh = false;
								compiledItem.position = inputItem.position;
								compiledItem.rotation = inputItem.rotation;
								compiledItem.scale = inputItem.scale;
								compiledItem.material = inputItem.material;
								compiledDataList.Add( compiledItem );
							}
						}
					}
					else if( inputItem.dataType == InputItem.DataTypes.Triangles )
					{
						//triangles

						if( inputItem.allowBatching )
						{
							//make batches, compile later.

							List<InputItem> list;
							if( !batches.TryGetValue( inputItem, out list ) )
							{
								list = new List<InputItem>();
								batches.Add( inputItem, list );
							}
							list.Add( inputItem );
						}
						else
						{
							//compile non batched items
							List<InputItem> list = new List<InputItem>( 1 );
							list.Add( inputItem );
							CompiledItem compiledItem = CompileInputItems( list );
							compiledDataList.Add( compiledItem );
						}
					}
				}

				//generate batches for triangles
				foreach( List<InputItem> list in batches.Values )
				{
					CompiledItem compiledItem = CompileInputItems( list );
					compiledDataList.Add( compiledItem );
				}

				compiledData = compiledDataList.ToArray();

				inputData.Clear();
				inputData = null;
			}

			void DestroyCompiledData()
			{
				if( compiledData != null )
				{
					foreach( CompiledItem item in compiledData )
					{
						if( item.needDisposeMesh && item.mesh != null )
							item.mesh.Dispose();
					}
					compiledData = null;
				}
			}
		}

		///////////////////////////////////////////

		class MaterialDataImpl : MaterialData
		{
			public DynamicMeshManagerImpl owner;

			public string materialName;
			public int materialNameHashCode;

			public MaterialParameters parameters;
			public int materialManagerWithoutTexturesIndex;

			//

			public MaterialDataImpl( DynamicMeshManagerImpl owner )
			{
				this.owner = owner;
			}

			public override string MaterialName
			{
				get { return materialName; }
			}

			public override MaterialParameters Parameters
			{
				get { return parameters; }
			}

			public static bool IsExact( MaterialDataImpl material1, MaterialDataImpl material2 )
			{
				if( material1 != material2 )
				{
					if( material1 == null || material2 == null )
						return false;

					if( material1.materialNameHashCode != material2.materialNameHashCode )
						return false;
					if( material1.materialNameHashCode != 0 || material2.materialNameHashCode != 0 )
					{
						if( string.Compare( material1.materialName, material2.materialName, true ) != 0 )
							return false;
					}

					if( material1.parameters != material2.parameters )
					{
						if( material1.parameters == null || material2.parameters == null )
							return false;
						if( material1.materialManagerWithoutTexturesIndex != material2.materialManagerWithoutTexturesIndex )
							return false;
						if( !MaterialParameters.Equals( material1.parameters, material2.parameters ) )
							return false;
					}
				}
				return true;
			}

			public int GetHashCodeForBatching()
			{
				if( !string.IsNullOrEmpty( materialName ) )
					return materialNameHashCode;// materialName.GetHashCode();
				else
					return parameters.GetHashCode();
			}
		}

		///////////////////////////////////////////

		struct SceneItem
		{
			public BlockImpl block;
			public Vec3 position;
			public Quat rotation;
			public Vec3 scale;
			public bool castShadows;
			public MaterialDataImpl material;
		}

		///////////////////////////////////////////

		class SceneObject
		{
			public BlockImpl block;
			public Vec3 position;
			public Quat rotation = Quat.Identity;
			public Vec3 scale = Vec3.One;
			public bool castShadows;
			public MaterialDataImpl material;
			public MeshObjectItem[] meshObjects;
			public float inCacheFromTime;

			///////////////

			public struct MeshObjectItem
			{
				public MeshObject meshObject;
				public SceneNode sceneNode;
				public MaterialDataImpl assignedMaterial;
			}

			///////////////

			public void Dispose()
			{
				if( meshObjects != null )
				{
					foreach( MeshObjectItem item in meshObjects )
					{
						if( item.sceneNode != null )
							item.sceneNode.Dispose();
						if( item.meshObject != null )
							item.meshObject.Dispose();
					}
					meshObjects = null;
				}
			}

			public void SetTranform( ref Vec3 pos, ref Quat rot, ref Vec3 scl, bool checkNoChanges )
			{
				if( !checkNoChanges || position != pos || rotation != rot || scale != scl )
				{
					position = pos;
					rotation = rot;
					scale = scl;

					for( int nMeshObject = 0; nMeshObject < meshObjects.Length; nMeshObject++ )
					{
						MeshObjectItem meshObjectItem = meshObjects[ nMeshObject ];
						BlockImpl.CompiledItem compiledItem = block.compiledData[ nMeshObject ];
						meshObjectItem.sceneNode.Position = position + rotation * ( compiledItem.position * scale );
						meshObjectItem.sceneNode.Rotation = rotation * compiledItem.rotation;
						meshObjectItem.sceneNode.Scale = scale * compiledItem.scale;
					}
				}
			}

			public void SetVisible( bool visible )
			{
				for( int n = 0; n < meshObjects.Length; n++ )
				{
					SceneObject.MeshObjectItem item = meshObjects[ n ];
					if( item.sceneNode != null )
						item.sceneNode.Visible = visible;
				}
			}

			public void SetCastShadows( bool castShadows, bool checkNoChanges )
			{
				if( !checkNoChanges || this.castShadows != castShadows )
				{
					this.castShadows = castShadows;

					for( int n = 0; n < meshObjects.Length; n++ )
					{
						SceneObject.MeshObjectItem item = meshObjects[ n ];
						if( item.meshObject != null && item.meshObject.CastShadows != castShadows )
							item.meshObject.CastShadows = castShadows;
					}
				}
			}

			static void SetMeshObjectMaterial( DynamicMeshManagerImpl owner, MeshObject meshObject, MaterialDataImpl material,
				ref bool materialsUpdated, ref bool gpuParametersUpdated )
			{
				if( !string.IsNullOrEmpty( material.MaterialName ) )
				{
					//update material names
					meshObject.SetMaterialNameForAllSubObjects( material.MaterialName );
					materialsUpdated = true;
				}
				else if( string.IsNullOrEmpty( material.parameters.DiffuseMap ) )
				{
					//MaterialParameter without diffuse map

					//if( material.parameters == null )
					//{
					//   Log.Fatal( "DynamicMeshManagerImpl: SceneObject: SetMeshObjectMaterial: material.MaterialName is empty " +
					//      "and material.Parameters == null." );
					//}

					ShaderBaseMaterial realMaterial = owner.materialManagerWithoutTextures.GetOrCreateRealMaterial(
						material.parameters, material.materialManagerWithoutTexturesIndex );
					if( realMaterial != null )
					{
						//update material names
						meshObject.SetMaterialNameForAllSubObjects( realMaterial.Name );
						materialsUpdated = true;

						//update gpu parameters
						foreach( MeshObject.SubObject subObject in meshObject.SubObjects )
						{
							subObject.SetCustomGpuParameter( (int)ShaderBaseMaterial.GpuParameters.dynamicDiffuseScale,
								material.parameters.DiffuseColor.ToVec4() );
							ColorValue spec = material.parameters.SpecularColor;
							subObject.SetCustomGpuParameter( (int)ShaderBaseMaterial.GpuParameters.dynamicSpecularScaleAndShininess,
								new Vec4( spec.Red, spec.Green, spec.Blue, material.parameters.SpecularShininess ) );
						}
						gpuParametersUpdated = true;
					}
				}
				else
				{
					//MaterialParameter with diffuse map

					ShaderBaseMaterial realMaterial = owner.materialManagerWithTextures.GetOrCreateRealMaterial( material.parameters );
					if( realMaterial != null )
					{
						//update material names
						meshObject.SetMaterialNameForAllSubObjects( realMaterial.Name );
						materialsUpdated = true;

						//update gpu parameters
						foreach( MeshObject.SubObject subObject in meshObject.SubObjects )
						{
							subObject.SetCustomGpuParameter( (int)ShaderBaseMaterial.GpuParameters.dynamicDiffuseScale,
								material.parameters.DiffuseColor.ToVec4() );
							ColorValue spec = material.parameters.SpecularColor;
							subObject.SetCustomGpuParameter( (int)ShaderBaseMaterial.GpuParameters.dynamicSpecularScaleAndShininess,
								new Vec4( spec.Red, spec.Green, spec.Blue, material.parameters.SpecularShininess ) );
						}
						gpuParametersUpdated = true;
					}
				}
			}

			public void SetMaterial( MaterialDataImpl materialOfSceneItem, bool checkNoChanges )
			{
				//check for no changes
				if( !checkNoChanges || !MaterialDataImpl.IsExact( this.material, materialOfSceneItem ) )
				{
					this.material = materialOfSceneItem;

					//update mesh objects
					for( int nMeshObject = 0; nMeshObject < meshObjects.Length; nMeshObject++ )
					{
						MeshObjectItem item = meshObjects[ nMeshObject ];

						MeshObject meshObject = item.meshObject;
						if( meshObject != null )
						{
							//get new material for mesh object
							MaterialDataImpl materialForItem;
							if( materialOfSceneItem != null )
								materialForItem = materialOfSceneItem;
							else
								materialForItem = block.compiledData[ nMeshObject ].material;

							//need to update material for mesh object
							if( item.assignedMaterial != materialForItem )
							{
								bool materialsUpdated = false;
								bool gpuParametersUpdated = false;

								//set material to mesh object
								if( materialForItem != null )
								{
									//update material name, gpu parameters
									SetMeshObjectMaterial( block.owner, meshObject, materialForItem, ref materialsUpdated,
										ref gpuParametersUpdated );
								}

								//restore original material names from the mesh. reset gpu parameters.
								if( item.assignedMaterial != null && ( !materialsUpdated || !gpuParametersUpdated ) )
								{
									Mesh mesh = meshObject.Mesh;
									for( int nSubMesh = 0; nSubMesh < meshObject.SubObjects.Length; nSubMesh++ )
									{
										MeshObject.SubObject subObject = meshObject.SubObjects[ nSubMesh ];
										string materialName = mesh.SubMeshes[ nSubMesh ].MaterialName;

										//remove custom gpu parameters
										if( !gpuParametersUpdated )
										{
											//remove custom gpu parameters
											subObject.RemoveCustomGpuParameter( (int)ShaderBaseMaterial.GpuParameters.dynamicDiffuseScale );
											subObject.RemoveCustomGpuParameter(
												(int)ShaderBaseMaterial.GpuParameters.dynamicSpecularScaleAndShininess );

											//
											////TO DO: need command to remove custom gpu parameters
											////workaround
											//ShaderBaseMaterial m = HighLevelMaterialManager.Instance.
											//   GetMaterialByName( materialName ) as ShaderBaseMaterial;
											//if( m != null )
											//{
											//   float p = m.DiffusePower;
											//   ColorValue diffuse = m.DiffuseColor * new ColorValue( p, p, p, 1 );
											//   subObject.SetCustomGpuParameter( (int)ShaderBaseMaterial.GpuParameters.dynamicDiffuseScale,
											//      diffuse.ToVec4() );

											//   Vec3 specular = m.SpecularColor.ToVec4().ToVec3() * m.SpecularPower;
											//   subObject.SetCustomGpuParameter(
											//      (int)ShaderBaseMaterial.GpuParameters.dynamicSpecularScaleAndShininess,
											//      new Vec4( specular.X, specular.Y, specular.Z, material.parameters.SpecularShininess ) );
											//}
										}

										//restore material name
										if( !materialsUpdated )
											subObject.MaterialName = materialName;
									}
								}

								item.assignedMaterial = materialForItem;
							}
						}
					}
				}
			}
		}

		///////////////////////////////////////////

		class MaterialManagerWithoutTexturesClass
		{
			DynamicMeshManagerImpl manager;
			ShaderBaseMaterial[] realMaterials = new ShaderBaseMaterial[ 2048 ];

			///////////////

			[Flags]
			enum Combinations
			{
				Blending1 = 1 << 0,
				Blending2 = 1 << 1,
				Lighting = 1 << 2,
				AllowFog = 1 << 3,
				DoubleSided = 1 << 4,
				ReceiveShadows = 1 << 5,
				DepthWrite = 1 << 6,
				DepthTest = 1 << 7,
				DiffuseUseVertexColor = 1 << 8,
				Specular = 1 << 9,
			}

			///////////////

			internal MaterialManagerWithoutTexturesClass( DynamicMeshManagerImpl manager )
			{
				this.manager = manager;
			}

			public void Dispose()
			{
				DisposeAllRealMaterials();
			}

			public void DisposeAllRealMaterials()
			{
				for( int n = 0; n < realMaterials.Length; n++ )
				{
					ShaderBaseMaterial material = realMaterials[ n ];
					if( material != null )
					{
						material.Dispose();
						realMaterials[ n ] = null;
					}
				}
			}

			public int GetIndex( MaterialParameters parameters )
			{
				int index = 0;

				switch( parameters.Blending )
				{
				case MaterialParameters.BlendingTypes.AlphaBlend:
					index |= (int)Combinations.Blending1;
					break;
				case MaterialParameters.BlendingTypes.AlphaAdd:
					index |= (int)Combinations.Blending2;
					break;
				}
				if( parameters.Lighting )
					index |= (int)Combinations.Lighting;
				if( parameters.AllowFog )
					index |= (int)Combinations.AllowFog;
				if( parameters.DoubleSided )
					index |= (int)Combinations.DoubleSided;
				if( parameters.ReceiveShadows )
					index |= (int)Combinations.ReceiveShadows;
				if( parameters.DepthWrite )
					index |= (int)Combinations.DepthWrite;
				if( parameters.DepthTest )
					index |= (int)Combinations.DepthTest;
				if( parameters.DiffuseUseVertexColor )
					index |= (int)Combinations.DiffuseUseVertexColor;
				if( parameters.SpecularColor != new ColorValue( 0, 0, 0 ) )
					index |= (int)Combinations.Specular;

				return index;
			}

			public ShaderBaseMaterial GetOrCreateRealMaterial( MaterialParameters parameters, int index )
			{
				if( realMaterials[ index ] == null )
					realMaterials[ index ] = manager.CreateRealMaterial( parameters, index );
				return realMaterials[ index ];
			}
		}

		///////////////////////////////////////////

		class MaterialManagerWithTexturesClass
		{
			DynamicMeshManagerImpl manager;

			Dictionary<MaterialParameters, ShaderBaseMaterial> realMaterials =
				new Dictionary<MaterialParameters, ShaderBaseMaterial>();

			///////////////

			internal MaterialManagerWithTexturesClass( DynamicMeshManagerImpl manager )
			{
				this.manager = manager;
			}

			public void Dispose()
			{
				DisposeAllRealMaterials();
			}

			public void DisposeAllRealMaterials()
			{
				foreach( KeyValuePair<MaterialParameters, ShaderBaseMaterial> pair in realMaterials )
					pair.Value.Dispose();
				realMaterials.Clear();

				//for( int n = 0; n < realMaterials.Length; n++ )
				//{
				//   ShaderBaseMaterial material = realMaterials[ n ];
				//   if( material != null )
				//   {
				//      material.Dispose();
				//      realMaterials[ n ] = null;
				//   }
				//}
			}

			public ShaderBaseMaterial GetOrCreateRealMaterial( MaterialParameters parameters )
			{
				ShaderBaseMaterial material;
				if( !realMaterials.TryGetValue( parameters, out material ) )
				{
					material = manager.CreateRealMaterial( parameters, 0 );
					if( material != null )
						realMaterials.Add( parameters, material );
				}

				return material;
			}
		}

		///////////////////////////////////////////

		/// <summary>
		/// 
		/// </summary>
		/// <param name="maxLifeTimeNotUsedDataInCache">Specify Zero to disable caching.</param>
		public DynamicMeshManagerImpl( float maxLifeTimeNotUsedDataInCache )
		{
			materialManagerWithoutTextures = new MaterialManagerWithoutTexturesClass( this );
			materialManagerWithTextures = new MaterialManagerWithTexturesClass( this );

			this.maxLifeTimeNotUsedDataInCache = maxLifeTimeNotUsedDataInCache;
		}

		public override void Dispose()
		{
			ClearScene( true, true );
			materialManagerWithoutTextures.Dispose();
			materialManagerWithTextures.Dispose();
		}

		public override Block GetBlockFromCache( string uniqueKeyForCaching )
		{
			BlockImpl block;
			if( blockCache.TryGetValue( uniqueKeyForCaching, out block ) )
			{
				block.lastUsingTime = EngineApp.Instance.Time;
				return block;
			}
			return null;
		}

		public override Block CreateBlock( string uniqueKeyForCaching )
		{
			BlockImpl block = new BlockImpl( this );

			if( maxLifeTimeNotUsedDataInCache != 0 && !string.IsNullOrEmpty( uniqueKeyForCaching ) )
			{
				//add to the cache
				try
				{
					blockCache.Add( uniqueKeyForCaching, block );
				}
				catch( ArgumentException )
				{
					Log.Fatal( "DynamicMeshManagerImpl: CreateBlock: Block with key \"{0}\" is already in the cache.",
						uniqueKeyForCaching );
				}
				block.useCaching = true;
				block.lastUsingTime = EngineApp.Instance.Time;
			}
			else
			{
				//no caching
				blocksWithoutCaching.Add( block );
			}

			return block;
		}

		public override void BeginScene()
		{
			if( sceneUnderConstruction )
				Log.Fatal( "DynamicMeshManagerImpl: BeginScene: The scene is already under construction." );

			ClearScene( false, false );
			sceneUnderConstruction = true;
		}

		public override void AddBlockToScene( Block block, Vec3 position, Quat rotation, Vec3 scale, bool castShadows,
			MaterialData material )
		{
			if( !sceneUnderConstruction )
				Log.Fatal( "DynamicMeshManagerImpl: AddBlockToScene: The scene is not under construction." );

			MaterialDataImpl materialImpl;
			if( material != null )
			{
				materialImpl = (MaterialDataImpl)material;
				if( materialImpl.owner != this )
					Log.Fatal( "DynamicMeshManagerImpl: AddBlockToScene: Invalid material. material.owner != this." );
			}
			else
				materialImpl = null;

			//TO DO: we can compile data from EndScene() method. Also parallelization on the threads is possible.
			BlockImpl blockImpl = (BlockImpl)block;
			if( !blockImpl.IsCompiled() )
				blockImpl.Compile();

			if( blockImpl.IsCompiled() )
			{
				SceneItem item = new SceneItem();
				item.block = blockImpl;
				item.position = position;
				item.rotation = rotation;
				item.scale = scale;
				item.castShadows = castShadows;
				item.material = materialImpl;
				sceneItems.Add( item );
			}
		}

		public override void EndScene()
		{
			if( !sceneUnderConstruction )
				Log.Fatal( "DynamicMeshManagerImpl: EndScene: The scene is not under construction." );

			sceneUnderConstruction = false;

			//construct scene objects
			{
				sceneObjectsArray = new SceneObject[ sceneItems.Count ];

				//try get scene object from the cache with exact settings (equal transform, equal material)
				for( int nSceneObject = 0; nSceneObject < sceneObjectsArray.Length; nSceneObject++ )
				{
					SceneItem item = sceneItems[ nSceneObject ];
					if( item.block.freeSceneObjectsCache != null )
					{
						SceneObject sceneObject = item.block.freeSceneObjectsCache.GetFromCacheExact( ref item.position,
							ref item.rotation, ref item.scale, item.material );
						if( sceneObject != null )
						{
							//update cast shadows flag
							sceneObject.SetCastShadows( item.castShadows, true );
							//show scene object
							sceneObject.SetVisible( showScene );

							sceneObjectsArray[ nSceneObject ] = sceneObject;
						}
					}
				}

				//try get scene object from the cache with different settings (diffrent transform or/and different material)
				for( int nSceneObject = 0; nSceneObject < sceneObjectsArray.Length; nSceneObject++ )
				{
					if( sceneObjectsArray[ nSceneObject ] == null )
					{
						SceneItem item = sceneItems[ nSceneObject ];
						if( item.block.freeSceneObjectsCache != null )
						{
							bool exactTransform;
							bool exactMaterial;
							SceneObject sceneObject = item.block.freeSceneObjectsCache.GetFromCacheNotExact( ref item.position,
								ref item.rotation, ref item.scale, item.material, out exactTransform, out exactMaterial );
							if( sceneObject != null )
							{
								//update transform
								if( !exactTransform )
									sceneObject.SetTranform( ref item.position, ref item.rotation, ref item.scale, true );

								//update material
								if( !exactMaterial )
									sceneObject.SetMaterial( item.material, true );

								//update cast shadows flag
								sceneObject.SetCastShadows( item.castShadows, true );
								//show scene object
								sceneObject.SetVisible( showScene );

								sceneObjectsArray[ nSceneObject ] = sceneObject;
							}
						}
					}
				}

				//create new scene objects
				for( int nSceneObject = 0; nSceneObject < sceneObjectsArray.Length; nSceneObject++ )
				{
					if( sceneObjectsArray[ nSceneObject ] == null )
					{
						SceneItem item = sceneItems[ nSceneObject ];

						SceneObject sceneObject = new SceneObject();
						sceneObject.block = item.block;

						//create mesh objects
						sceneObject.meshObjects = new SceneObject.MeshObjectItem[ item.block.compiledData.Length ];
						for( int nMeshObject = 0; nMeshObject < sceneObject.meshObjects.Length; nMeshObject++ )
						{
							SceneObject.MeshObjectItem meshObjectItem = new SceneObject.MeshObjectItem();

							BlockImpl.CompiledItem compiledItem = item.block.compiledData[ nMeshObject ];

							//create mesh object
							meshObjectItem.meshObject = SceneManager.Instance.CreateMeshObject( compiledItem.mesh.Name );
							if( meshObjectItem.meshObject != null )
							{
								//disable lods
								meshObjectItem.meshObject.SetMeshLodBias( 1, 0, 0 );

								//create scene node
								meshObjectItem.sceneNode = new SceneNode();
								meshObjectItem.sceneNode.Visible = showScene;
								meshObjectItem.sceneNode.Attach( meshObjectItem.meshObject );
							}

							sceneObject.meshObjects[ nMeshObject ] = meshObjectItem;
						}

						sceneObject.SetCastShadows( item.castShadows, false );
						sceneObject.SetTranform( ref item.position, ref item.rotation, ref item.scale, false );
						sceneObject.SetMaterial( item.material, false );

						sceneObjectsArray[ nSceneObject ] = sceneObject;
					}
				}
			}
		}

		public override void ClearScene( bool clearCaches, bool destroyMaterials )
		{
			if( sceneUnderConstruction )
				Log.Fatal( "DynamicMeshManagerImpl: ClearScene: The scene is under construction." );

			float time = EngineApp.Instance.Time;

			//clear scene objects from the scene. move scene objects to the cache or delete all for clearCaches == true.
			if( sceneObjectsArray != null )
			{
				foreach( SceneObject sceneObject in sceneObjectsArray )
				{
					if( sceneObject != null )
					{
						BlockImpl block = sceneObject.block;
						if( maxLifeTimeNotUsedDataInCache != 0 && !clearCaches && block.useCaching )
						{
							//move scene object to the cache
							if( block.freeSceneObjectsCache == null )
								block.freeSceneObjectsCache = new BlockImpl.SceneObjectCachingData( block );
							block.freeSceneObjectsCache.Add( sceneObject, time );
						}
						else
						{
							//delete scene object
							sceneObject.Dispose();
						}
					}
				}
				sceneObjectsArray = null;
			}

			//finish scene clearing
			if( sceneItems.Count > sceneItemsShrinkMaxLength )
				sceneItemsShrinkMaxLength = sceneItems.Count;
			sceneItems.Clear();
			sceneItemsShrinkCounter--;
			if( sceneItemsShrinkCounter <= 0 )
			{
				if( sceneItems.Capacity > sceneItemsShrinkMaxLength )
					sceneItems = new List<SceneItem>( sceneItemsShrinkMaxLength );
				sceneItemsShrinkMaxLength = 0;
				sceneItemsShrinkCounter = 50;
			}

			//delete scene objects from the cache which are not used long time or delete all for clearCaches == true.
			{
				foreach( BlockImpl block in blockCache.Values )
				{
					if( block.freeSceneObjectsCache != null )
					{
						if( maxLifeTimeNotUsedDataInCache != 0 && !clearCaches )
							block.freeSceneObjectsCache.DeleteLongTimeNotUsedObjects( time );
						else
							block.freeSceneObjectsCache.DeleteAll();
					}
				}
				foreach( BlockImpl block in blocksWithoutCaching )
				{
					if( block.freeSceneObjectsCache != null )
					{
						if( maxLifeTimeNotUsedDataInCache != 0 && !clearCaches )
							block.freeSceneObjectsCache.DeleteLongTimeNotUsedObjects( time );
						else
							block.freeSceneObjectsCache.DeleteAll();
					}
				}
			}

			//delete blocks from the cache which are not used long time or delete all for clearCaches == true.
			{
				//delete blocks from the cache
				{
					if( maxLifeTimeNotUsedDataInCache != 0 && !clearCaches )
					{
						DeleteLongTimeNotUsedBlocksFromCache();
					}
					else
					{
						foreach( BlockImpl block in blockCache.Values )
						{
							if( block.freeSceneObjectsCache != null && !block.freeSceneObjectsCache.IsEmpty() )
								Log.Fatal( "DynamicMeshManagerImpl: Internal error: block.freeSceneObjectsCache != null && !block.freeSceneObjectsCache.IsEmpty()." );
							block.Dispose();
						}
						if( clearCaches )
							blockCache = new Dictionary<string, BlockImpl>( 32 );
						else
							blockCache.Clear();
					}
				}

				//delete blocks from blocksWithoutCaching
				{
					Set<BlockImpl> savedBlocks = null;

					foreach( BlockImpl block in blocksWithoutCaching )
					{
						if( block.freeSceneObjectsCache != null && !block.freeSceneObjectsCache.IsEmpty() )
						{
							if( savedBlocks == null )
								savedBlocks = new Set<BlockImpl>( 32 );
							savedBlocks.Add( block );
						}
						else
							block.Dispose();
					}

					if( savedBlocks != null )
					{
						blocksWithoutCaching = savedBlocks;
					}
					else
					{
						if( clearCaches )
							blocksWithoutCaching = new Set<BlockImpl>( 32 );
						else
							blocksWithoutCaching.Clear();
					}
				}
			}

			if( destroyMaterials )
			{
				materialManagerWithoutTextures.DisposeAllRealMaterials();
				materialManagerWithTextures.DisposeAllRealMaterials();
			}
		}

		public override bool ShowScene
		{
			get { return showScene; }
			set
			{
				if( showScene == value )
					return;
				showScene = value;

				if( sceneObjectsArray != null )
				{
					foreach( SceneObject sceneObject in sceneObjectsArray )
					{
						if( sceneObject != null && sceneObject.meshObjects != null )
						{
							foreach( SceneObject.MeshObjectItem item in sceneObject.meshObjects )
							{
								if( item.sceneNode != null )
									item.sceneNode.Visible = showScene;
							}
						}
					}
				}
			}
		}

		public override bool IsSceneEmpty()
		{
			return sceneItems.Count == 0;
		}

		protected override MaterialData OnCreateMaterial( string materialName )
		{
			MaterialDataImpl material = new MaterialDataImpl( this );
			material.materialName = materialName;
			material.materialNameHashCode = materialName.GetHashCode();
			return material;
		}

		protected override MaterialData OnCreateMaterial( MaterialParameters parameters )
		{
			MaterialDataImpl material = new MaterialDataImpl( this );
			material.parameters = parameters;
			if( string.IsNullOrEmpty( parameters.DiffuseMap ) )
				material.materialManagerWithoutTexturesIndex = materialManagerWithoutTextures.GetIndex( parameters );
			return material;
		}

		public override void GetStatistics( out int blocksInCache, out int blocksNotCached, out int sceneObjectsInScene,
			out int freeSceneObjects )
		{
			blocksInCache = blockCache.Count;
			blocksNotCached = blocksWithoutCaching.Count;

			sceneObjectsInScene = 0;
			if( sceneObjectsArray != null )
			{
				for( int n = 0; n < sceneObjectsArray.Length; n++ )
				{
					if( sceneObjectsArray[ n ] != null )
						sceneObjectsInScene++;
				}
			}

			freeSceneObjects = 0;
			foreach( BlockImpl block in blockCache.Values )
			{
				if( block.freeSceneObjectsCache != null )
					freeSceneObjects += block.freeSceneObjectsCache.GetSceneObjectCount();
			}
			foreach( BlockImpl block in blocksWithoutCaching )
			{
				if( block.freeSceneObjectsCache != null )
					freeSceneObjects += block.freeSceneObjectsCache.GetSceneObjectCount();
			}
		}

		void DeleteLongTimeNotUsedBlocksFromCache()
		{
			float time = EngineApp.Instance.Time;

			List<KeyValuePair<string, BlockImpl>> pairsToDelete = null;

			foreach( KeyValuePair<string, BlockImpl> pair in blockCache )
			{
				string key = pair.Key;
				BlockImpl block = pair.Value;

				if( time - block.lastUsingTime > maxLifeTimeNotUsedDataInCache )
				{
					if( block.freeSceneObjectsCache == null || block.freeSceneObjectsCache.IsEmpty() )
					{
						if( pairsToDelete == null )
							pairsToDelete = new List<KeyValuePair<string, BlockImpl>>( 32 );
						pairsToDelete.Add( pair );
					}
				}
			}

			if( pairsToDelete != null )
			{
				for( int n = 0; n < pairsToDelete.Count; n++ )
				{
					string key = pairsToDelete[ n ].Key;
					BlockImpl block = pairsToDelete[ n ].Value;

					blockCache.Remove( key );
					block.Dispose();
				}
			}
		}

		ShaderBaseMaterial CreateRealMaterial( MaterialParameters parameters, int indexForMaterialsWithoutTexture )
		{
			string materialName;
			if( !string.IsNullOrEmpty( parameters.DiffuseMap ) )
			{
				uniqueMaterialNameForMaterialsWithTextures++;
				if( uniqueMaterialNameForMaterialsWithTextures == long.MaxValue )
					uniqueMaterialNameForMaterialsWithTextures = 0;

				materialName = HighLevelMaterialManager.Instance.GetUniqueMaterialName(
					string.Format( "DynamicMeshManagerImpl_MaterialManager_{0}_{1}", parameters.DiffuseMap,
					uniqueMaterialNameForMaterialsWithTextures ) );
			}
			else
			{
				materialName = HighLevelMaterialManager.Instance.GetUniqueMaterialName(
					string.Format( "DynamicMeshManagerImpl_MaterialManager_{0}", indexForMaterialsWithoutTexture ) );
			}

			ShaderBaseMaterial material = (ShaderBaseMaterial)HighLevelMaterialManager.Instance.CreateMaterial( materialName,
				"ShaderBaseMaterial" );

			//general parameters
			switch( parameters.Blending )
			{
			case MaterialParameters.BlendingTypes.Opaque:
				material.Blending = ShaderBaseMaterial.MaterialBlendingTypes.Opaque;
				break;
			case MaterialParameters.BlendingTypes.AlphaBlend:
				material.Blending = ShaderBaseMaterial.MaterialBlendingTypes.AlphaBlend;
				break;
			case MaterialParameters.BlendingTypes.AlphaAdd:
				material.Blending = ShaderBaseMaterial.MaterialBlendingTypes.AlphaAdd;
				break;
			}
			material.Lighting = parameters.Lighting;
			material.AllowFog = parameters.AllowFog;
			material.DoubleSided = parameters.DoubleSided;
			material.ReceiveShadows = parameters.ReceiveShadows;
			material.DepthWrite = parameters.DepthWrite;
			material.DepthTest = parameters.DepthTest;
			material.DiffuseVertexColor = parameters.DiffuseUseVertexColor;
			//diffuse color
			material.DiffuseScaleDynamic = true;

			//specular color
			if( parameters.SpecularColor != new ColorValue( 0, 0, 0 ) )
			{
				material.SpecularScaleDynamic = true;
				material.SpecularShininess = parameters.SpecularShininess;
			}

			//diffuse map
			if( !string.IsNullOrEmpty( parameters.DiffuseMap ) )
			{
				material.Diffuse1Map.Texture = parameters.DiffuseMap;
				material.Diffuse1Map.Clamp = parameters.DiffuseMapClamp;
			}

			material.PostCreate();

			return material;
		}
	}
}
