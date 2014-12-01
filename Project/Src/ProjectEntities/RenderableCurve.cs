// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Drawing.Design;
using Engine.EntitySystem;
using Engine.MathEx;
using Engine.Utils;
using Engine.Renderer;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using ProjectCommon;
using Engine;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="RenderableCurvePoint"/> entity type.
	/// </summary>
	[AllowToCreateTypeBasedOnThisClass( false )]
	public class RenderableCurvePointType : MapCurvePointType
	{
	}

	/// <summary>
	/// Defines the camera point for <see cref="RenderableCurve"/>.
	/// </summary>
	public class RenderableCurvePoint : MapCurvePoint
	{
		[FieldSerialize]
		float overrideRadius = -1;

		[TypeField]
		RenderableCurvePointType __type = null;
		/// <summary>
		/// Gets the entity type.
		/// </summary>
		public new RenderableCurvePointType Type { get { return __type; } }

		[DefaultValue( -1.0f )]
		public float OverrideRadius
		{
			get { return overrideRadius; }
			set
			{
				if( overrideRadius == value )
					return;
				overrideRadius = value;

				RenderableCurve curve = Owner as RenderableCurve;
				if( curve != null )
					curve.SetNeedUpdate();
			}
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	/// <summary>
	/// Defines the <see cref="RenderableCurve"/> entity type.
	/// </summary>
	public class RenderableCurveType : MapCurveType
	{
	}

	/// <summary>
	/// Defines the camera on a map moving on a curve.
	/// </summary>
	//[ExtendedFunctionalityDescriptor( "Engine.MapSystem.Editor.RenderableCurveExtendedFunctionalityDescriptor, ProjectEntities.Editor" )]
	public class RenderableCurve : MapCurve
	{
		public enum RadiusCurveTypes
		{
			UniformCubicSpline,
			Bezier,
			Line,
		}
		[FieldSerialize]
		RadiusCurveTypes radiusCurveType = RadiusCurveTypes.UniformCubicSpline;
		[FieldSerialize]
		float radius = .05f;
		[FieldSerialize]
		int pathSteps = 20;
		[FieldSerialize]
		int shapeSegments = 12;
		[FieldSerialize]
		string materialName = "White";
		[FieldSerialize]
		float textureCoordinatesTilesPerMeter = 1;
		[FieldSerialize]
		bool collision;
		[FieldSerialize]
		string collisionMaterialName = "Default";

		bool needUpdate;

		Mesh mesh;
		MeshObject meshObject;
		SceneNode sceneNode;
		Body collisionBody;

		static long uniqueMeshIdentifier;

		///////////////////////////////////////////

		[StructLayout( LayoutKind.Sequential )]
		struct Vertex
		{
			public Vec3 position;
			public Vec3 normal;
			public Vec2 texCoord;
			public Vec4 tangent;

			public Vertex( Vec3 position, Vec3 normal, Vec2 texCoord, Vec4 tangent )
			{
				this.position = position;
				this.normal = normal;
				this.texCoord = texCoord;
				this.tangent = tangent;
			}
		}

		///////////////////////////////////////////

		[TypeField]
		RenderableCurveType __type = null;
		/// <summary>
		/// Gets the entity type.
		/// </summary>
		public new RenderableCurveType Type { get { return __type; } }

		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			UpdateGeometry();
		}

		protected override void OnDestroy()
		{
			DestroyGeometry();

			base.OnDestroy();
		}

		/// <summary>
		/// Type of curve to calculate radius. The available types of curves: cubic spline (UniformCubicSpline), Bezier curves (Bezier), flat lines (Line), rounded at corvers with specified radius (RoundedLine).
		/// </summary>
		[LocalizedDescription( "Type of curve to calculate radius. The available types of curves: cubic spline (UniformCubicSpline), Bezier curves (Bezier), flat lines (Line), rounded at corvers with specified radius (RoundedLine).", "MapCurve" )]
		[DefaultValue( RadiusCurveTypes.UniformCubicSpline )]
		public RadiusCurveTypes RadiusCurveType
		{
			get { return radiusCurveType; }
			set
			{
				if( radiusCurveType == value )
					return;
				radiusCurveType = value;

				needUpdate = true;
			}
		}

		[DefaultValue( .05f )]
		public float Radius
		{
			get { return radius; }
			set
			{
				if( radius == value )
					return;
				radius = value;

				needUpdate = true;
			}
		}

		[DefaultValue( 20 )]
		public int PathSteps
		{
			get { return pathSteps; }
			set
			{
				if( EngineApp.Instance.ApplicationType == EngineApp.ApplicationTypes.MapEditor )
				{
					if( value > 100 )
					{
						if( EditorMessageBox.Show( "You specify a big value. Continue?", "Map Editor",
							EditorMessageBox.Buttons.YesNo, EditorMessageBox.Icon.Question ) == EditorMessageBox.Result.No )
							return;
					}
				}

				if( value < 1 )
					value = 1;
				if( pathSteps == value )
					return;
				pathSteps = value;

				needUpdate = true;
			}
		}

		[DefaultValue( 12 )]
		public int ShapeSegments
		{
			get { return shapeSegments; }
			set
			{
				if( EngineApp.Instance.ApplicationType == EngineApp.ApplicationTypes.MapEditor )
				{
					if( value > 100 )
					{
						if( EditorMessageBox.Show( "You specify a big value. Continue?", "Map Editor",
							EditorMessageBox.Buttons.YesNo, EditorMessageBox.Icon.Question ) == EditorMessageBox.Result.No )
							return;
					}
				}
				if( value < 2 )
					value = 2;
				if( shapeSegments == value )
					return;
				shapeSegments = value;

				needUpdate = true;
			}
		}

		[DefaultValue( "White" )]
		[Editor( typeof( EditorMaterialUITypeEditor ), typeof( UITypeEditor ) )]
		public string MaterialName
		{
			get { return materialName; }
			set
			{
				if( materialName == value )
					return;
				materialName = value;

				needUpdate = true;
			}
		}

		[DefaultValue( 1.0f )]
		public float TextureCoordinatesTilesPerMeter
		{
			get { return textureCoordinatesTilesPerMeter; }
			set
			{
				if( textureCoordinatesTilesPerMeter == value )
					return;
				textureCoordinatesTilesPerMeter = value;

				needUpdate = true;
			}
		}

		[DefaultValue( false )]
		public bool Collision
		{
			get { return collision; }
			set
			{
				if( collision == value )
					return;
				collision = value;

				needUpdate = true;
			}
		}

		[DefaultValue( "Default" )]
		[Editor( typeof( PhysicsWorld.MaterialNameEditor ), typeof( UITypeEditor ) )]
		public string CollisionMaterialName
		{
			get { return collisionMaterialName; }
			set
			{
				if( collisionMaterialName == value )
					return;
				collisionMaterialName = value;

				if( collisionBody != null )
					needUpdate = true;
			}
		}

		protected override void OnRenderFrame()
		{
			base.OnRenderFrame();

			if( needUpdate )
				UpdateGeometry();

			//{
			//   Curve c = GetPositionCurve();
			//   if( c != null && c.Times.Count > 1 )
			//   {
			//      float max = c.Times[ c.Times.Count - 1 ];

			//      for( float t = 0; t < max; t += .1f )
			//      {
			//         Camera camera = RendererWorld.Instance.DefaultCamera;
			//         camera.DebugGeometry.Color = new ColorValue( 1, 0, 0 );
			//         camera.DebugGeometry.AddSphere( new Sphere( c.CalculateValueByTime( t ), .1f ) );
			//      }
			//   }
			//}
		}

		protected override void OnRender( Camera camera )
		{
			base.OnRender( camera );

			if( sceneNode != null )
				sceneNode.Visible = Visible;

			//debug draw physics
			if( EngineDebugSettings.DrawStaticPhysics && camera.Purpose == Camera.Purposes.MainCamera )
			{
				if( collisionBody != null )
				{
					float distanceSqr = MapBounds.GetPointDistanceSqr( camera.Position );
					float farClipDistance = camera.FarClipDistance;
					if( distanceSqr < farClipDistance * farClipDistance )
					{
						camera.DebugGeometry.SetSpecialDepthSettings( false, true );

						const float drawAsBoundsDistance = 100;
						if( distanceSqr < drawAsBoundsDistance * drawAsBoundsDistance )
						{
							collisionBody.DebugRender( camera.DebugGeometry, 0, .5f, true, false, ColorValue.Zero );
						}
						else
						{
							camera.DebugGeometry.Color = collisionBody.Sleeping ? new ColorValue( 0, 0, 1, .5f ) :
								new ColorValue( 0, 1, 0, .5f );
							camera.DebugGeometry.AddBounds( collisionBody.GetGlobalBounds() );
						}

						camera.DebugGeometry.RestoreDefaultDepthSettings();
					}
				}
			}
		}

		/// <summary>
		/// Called when at updating a curve.
		/// </summary>
		protected override void OnUpdateCurve()
		{
			base.OnUpdateCurve();
			needUpdate = true;
		}

		unsafe void UpdateGeometry()
		{
			DestroyGeometry();

			Curve positionCurve = GetPositionCurve();

			Curve radiusCurve = null;
			{
				bool existsSpecialRadius = false;
				foreach( MapCurvePoint point in Points )
				{
					RenderableCurvePoint point2 = point as RenderableCurvePoint;
					if( point2 != null && point2.OverrideRadius >= 0 )
					{
						existsSpecialRadius = true;
						break;
					}
				}

				if( existsSpecialRadius )
				{
					switch( radiusCurveType )
					{
					case RadiusCurveTypes.UniformCubicSpline:
						radiusCurve = new UniformCubicSpline();
						break;
					case RadiusCurveTypes.Bezier:
						radiusCurve = new BezierCurve();
						break;
					case RadiusCurveTypes.Line:
						radiusCurve = new LineCurve();
						break;
					}

					for( int n = 0; n < Points.Count; n++ )
					{
						MapCurvePoint point = Points[ n ];

						if( !point.Editor_IsExcludedFromWorld() )
						{
							float rad = radius;
							RenderableCurvePoint renderableCurvePoint = point as RenderableCurvePoint;
							if( renderableCurvePoint != null && renderableCurvePoint.OverrideRadius >= 0 )
								rad = renderableCurvePoint.OverrideRadius;
							radiusCurve.AddValue( point.Time, new Vec3( rad, 0, 0 ) );
						}
					}
				}
			}

			//create mesh
			Vertex[] vertices = null;
			int[] indices = null;
			if( positionCurve != null && positionCurve.Values.Count > 1 && Points.Count >= 2 )
			{
				Vec3 positionOffset = -Position;

				int steps = ( Points.Count - 1 ) * pathSteps + 1;
				int vertexCount = steps * ( shapeSegments + 1 );
				int indexCount = ( steps - 1 ) * shapeSegments * 2 * 3;

				vertices = new Vertex[ vertexCount ];
				indices = new int[ indexCount ];

				//fill data
				{
					int currentVertex = 0;
					int currentIndex = 0;
					float currentDistance = 0;
					Vec3 lastPosition = Vec3.Zero;
					Quat lastRot = Quat.Identity;

					for( int nStep = 0; nStep < steps; nStep++ )
					{
						int startStepVertexIndex = currentVertex;

						float coefficient = (float)nStep / (float)( steps - 1 );
						Vec3 pos = CalculateCurvePointByCoefficient( coefficient ) + positionOffset;

						Quat rot;
						{
							Vec3 v = CalculateCurvePointByCoefficient( coefficient + .3f / (float)( steps - 1 ) ) -
								CalculateCurvePointByCoefficient( coefficient );
							if( v != Vec3.Zero )
								rot = Quat.FromDirectionZAxisUp( v.GetNormalize() );
							else
								rot = lastRot;
						}

						if( nStep != 0 )
							currentDistance += ( pos - lastPosition ).Length();

						float rad;
						if( radiusCurve != null )
						{
							Range range = new Range( radiusCurve.Times[ 0 ], radiusCurve.Times[ radiusCurve.Times.Count - 1 ] );
							float t = range.Minimum + ( range.Maximum - range.Minimum ) * coefficient;
							rad = radiusCurve.CalculateValueByTime( t ).X;
						}
						else
							rad = radius;

						for( int nSegment = 0; nSegment < shapeSegments + 1; nSegment++ )
						{
							float rotateCoefficient = ( (float)nSegment / (float)( shapeSegments ) );
							float angle = rotateCoefficient * MathFunctions.PI * 2;
							Vec3 p = pos + rot * new Vec3( 0, MathFunctions.Cos( angle ) * rad, MathFunctions.Sin( angle ) * rad );

							Vertex vertex = new Vertex();
							vertex.position = p;
							Vec3 pp = p - pos;
							if( pp != Vec3.Zero )
								vertex.normal = pp.GetNormalize();
							else
								vertex.normal = Vec3.XAxis;
							//vertex.normal = ( p - pos ).GetNormalize();
							vertex.texCoord = new Vec2( currentDistance * textureCoordinatesTilesPerMeter, rotateCoefficient + .25f );
							vertex.tangent = new Vec4( rot.GetForward(), 1 );
							vertices[ currentVertex++ ] = vertex;
						}

						if( nStep < steps - 1 )
						{
							for( int nSegment = 0; nSegment < shapeSegments; nSegment++ )
							{
								indices[ currentIndex++ ] = startStepVertexIndex + nSegment;
								indices[ currentIndex++ ] = startStepVertexIndex + nSegment + 1;
								indices[ currentIndex++ ] = startStepVertexIndex + nSegment + 1 + shapeSegments + 1;
								indices[ currentIndex++ ] = startStepVertexIndex + nSegment + 1 + shapeSegments + 1;
								indices[ currentIndex++ ] = startStepVertexIndex + nSegment + shapeSegments + 1;
								indices[ currentIndex++ ] = startStepVertexIndex + nSegment;
							}
						}

						lastPosition = pos;
						lastRot = rot;
					}
					if( currentVertex != vertexCount )
						Log.Fatal( "RenderableCurve: UpdateRenderingGeometry: currentVertex != vertexCount." );
					if( currentIndex != indexCount )
						Log.Fatal( "RenderableCurve: UpdateRenderingGeometry: currentIndex != indexCount." );
				}

				if( vertices.Length != 0 && indices.Length != 0 )
				{
					//create mesh
					string meshName = MeshManager.Instance.GetUniqueName(
						string.Format( "__RenderableCurve_{0}_{1}", Name, uniqueMeshIdentifier ) );
					uniqueMeshIdentifier++;
					//string meshName = MeshManager.Instance.GetUniqueName( string.Format( "__RenderableCurve_{0}", Name ) );
					mesh = MeshManager.Instance.CreateManual( meshName );
					SubMesh subMesh = mesh.CreateSubMesh();
					subMesh.UseSharedVertices = false;

					//init vertexData
					VertexDeclaration declaration = subMesh.VertexData.VertexDeclaration;
					declaration.AddElement( 0, 0, VertexElementType.Float3, VertexElementSemantic.Position );
					declaration.AddElement( 0, 12, VertexElementType.Float3, VertexElementSemantic.Normal );
					declaration.AddElement( 0, 24, VertexElementType.Float2, VertexElementSemantic.TextureCoordinates, 0 );
					declaration.AddElement( 0, 32, VertexElementType.Float4, VertexElementSemantic.Tangent, 0 );

					fixed( Vertex* pVertices = vertices )
					{
						subMesh.VertexData = VertexData.CreateFromArray( declaration, (IntPtr)pVertices,
							vertices.Length * Marshal.SizeOf( typeof( Vertex ) ) );
					}
					subMesh.IndexData = IndexData.CreateFromArray( indices, 0, indices.Length, false );

					//set material
					subMesh.MaterialName = materialName;

					//set mesh gabarites
					Bounds bounds = Bounds.Cleared;
					foreach( Vertex vertex in vertices )
						bounds.Add( vertex.position );
					mesh.SetBoundsAndRadius( bounds, bounds.GetRadius() );
				}
			}

			//create MeshObject, SceneNode
			if( mesh != null )
			{
				meshObject = SceneManager.Instance.CreateMeshObject( mesh.Name );
				if( meshObject != null )
				{
					meshObject.SetMaterialNameForAllSubObjects( materialName );
					meshObject.CastShadows = true;

					sceneNode = new SceneNode();
					sceneNode.Attach( meshObject );
					//apply offset
					sceneNode.Position = Position;
					MapObject.AssociateSceneNodeWithMapObject( sceneNode, this );
				}
			}

			//create collision body
			if( mesh != null && collision )
			{
				Vec3[] positions = new Vec3[ vertices.Length ];
				for( int n = 0; n < vertices.Length; n++ )
					positions[ n ] = vertices[ n ].position;
				string meshPhysicsMeshName = PhysicsWorld.Instance.AddCustomMeshGeometry( positions, indices, null,
					MeshShape.MeshTypes.TriangleMesh, 0, 0 );

				collisionBody = PhysicsWorld.Instance.CreateBody();
				collisionBody.Static = true;
				collisionBody._InternalUserData = this;
				collisionBody.Position = Position;

				MeshShape shape = collisionBody.CreateMeshShape();
				shape.MeshName = meshPhysicsMeshName;
				shape.MaterialName = CollisionMaterialName;
				shape.ContactGroup = (int)ContactGroup.Collision;
				//shape.VehicleDrivableSurface = collisionVehicleDrivableSurface;

				collisionBody.PushedToWorld = true;
			}

			needUpdate = false;
		}

		void DestroyGeometry()
		{
			if( collisionBody != null )
			{
				collisionBody.Dispose();
				collisionBody = null;
			}

			if( sceneNode != null )
			{
				sceneNode.Dispose();
				sceneNode = null;
			}

			if( meshObject != null )
			{
				meshObject.Dispose();
				meshObject = null;
			}

			if( mesh != null )
			{
				mesh.Dispose();
				mesh = null;
			}
		}

		public void SetNeedUpdate()
		{
			needUpdate = true;
		}
	}
}
