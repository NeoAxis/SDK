// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Design;
using System.ComponentModel;
using Engine.Utils;
using Engine.Renderer;
using Engine.FileSystem;
using Engine.MathEx;

namespace ProjectCommon
{
	/// <summary>
	/// Material for grass and trees. Adds wave in the vertex shader.
	/// </summary>
	[Description( "Based on ShaderBaseMaterial class in intended for rendering of vegetation. The class adds wind waving animation." )]
	public class VegetationMaterial : ShaderBaseMaterial
	{
		static List<VegetationMaterial> allVegetationMaterials = new List<VegetationMaterial>();

		//

		bool receiveObjectsPositionsFromVertices;

		float windEffectFactor = 1f;

		float bendScale = 0.02f;
		float bendVariation = 0.005f;
		float bendFrequency = 1f;

		bool detailBending = false;
		float branchAmplitude = 0.01f;
		float leafAmplitude = 0.01f;
		float branchFrequency = 1f;
		float leafFrequency = 1f;

		Vec2 windSpeed = new Vec2( 1, 0 ); //so we can see it in Resource Editor

		///////////////////////////////////////////

		//gpu parameters constants, expanding ShaderBaseMaterial.GpuParameters enumeration.
		public enum VegetationGpuParameters
		{
			windAnimationParameters = GpuParameters.LastIndex, //float4: windSpeed.XY, waveSpeed.XY
			mainBendingParameters, //float4: bendScale, bendVariation, bendFrequency, UNUSED
			detailBendingParameters, //float4: branchAmplitude, leafAmplitude, branchFrequency, leafFrequency
		}

		///////////////////////////////////////////

		public static IList<VegetationMaterial> AllVegetationMaterials
		{
			get { return allVegetationMaterials.AsReadOnly(); }
		}

		public VegetationMaterial()
		{
			allVegetationMaterials.Add( this );
		}

		public override void Dispose()
		{
			allVegetationMaterials.Remove( this );
			base.Dispose();
		}

		/// <summary>
		/// Gets or sets a value which indicates it is necessary to reveice objects 
		/// positions from the vertices.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Usually it is necessary for batched geometry (for waving).
		/// </para>
		/// <para>
		/// Positions will be taken from Type: TextureCoordinates, Index: 4.
		/// </para>
		/// </remarks>
		[LocalizedCategory( "Vegetation", "VegetationMaterial" )]
		[LocalizedDisplayName( "ReceiveObjectsPositionsFromVertices", "VegetationMaterial" )]
		[Description( "Reveice objects positions from the vertices. Usually it is necessary for batched geometry (for waving). Positions will be taken from Type: TextureCoordinates, Index: 4." )]
		[DefaultValue( false )]
		public bool ReceiveObjectsPositionsFromVertices
		{
			get { return receiveObjectsPositionsFromVertices; }
			set { receiveObjectsPositionsFromVertices = value; }
		}

		[LocalizedCategory( "Vegetation", "VegetationMaterial" )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 2 )]
		[DefaultValue( 1f )]
		public float WindEffectFactor
		{
			get { return windEffectFactor; }
			set
			{
				if( windEffectFactor == value )
					return;
				windEffectFactor = value;
				UpdateWindAnimationParameters();
			}
		}

		[LocalizedCategory( "Vegetation", "VegetationMaterial" )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 1 )]
		[DefaultValue( 0.02f )]
		public float BendScale
		{
			get { return bendScale; }
			set
			{
				if( bendScale == value )
					return;
				bendScale = value;
				UpdateMainBendingParameters();
			}
		}

		[LocalizedCategory( "Vegetation", "VegetationMaterial" )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 1 )]
		[DefaultValue( 0.005f )]
		public float BendVariation
		{
			get { return bendVariation; }
			set
			{
				if( bendVariation == value )
					return;
				bendVariation = value;
				UpdateMainBendingParameters();
			}
		}

		[LocalizedCategory( "Vegetation", "VegetationMaterial" )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 2 )]
		[DefaultValue( 1f )]
		public float BendFrequency
		{
			get { return bendFrequency; }
			set
			{
				if( bendFrequency == value )
					return;
				bendFrequency = value;
				UpdateMainBendingParameters();
			}
		}

		[LocalizedCategory( "Vegetation", "VegetationMaterial" )]
		[DefaultValue( false )]
		public bool DetailBending
		{
			get { return detailBending; }
			set { detailBending = value; }
		}

		[LocalizedCategory( "Vegetation", "VegetationMaterial" )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 1 )]
		[DefaultValue( 0.01f )]
		public float BranchAmplitude
		{
			get { return branchAmplitude; }
			set
			{
				if( branchAmplitude == value )
					return;
				branchAmplitude = value;
				UpdateDetailBendingParameters();
			}
		}

		[LocalizedCategory( "Vegetation", "VegetationMaterial" )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 1 )]
		[DefaultValue( 0.01f )]
		public float LeafAmplitude
		{
			get { return leafAmplitude; }
			set
			{
				if( leafAmplitude == value )
					return;
				leafAmplitude = value;
				UpdateDetailBendingParameters();
			}
		}

		[LocalizedCategory( "Vegetation", "VegetationMaterial" )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 2 )]
		[DefaultValue( 1f )]
		public float BranchFrequency
		{
			get { return branchFrequency; }
			set
			{
				if( branchFrequency == value )
					return;
				branchFrequency = value;
				UpdateDetailBendingParameters();
			}
		}

		[LocalizedCategory( "Vegetation", "VegetationMaterial" )]
		[Editor( typeof( SingleValueEditor ), typeof( UITypeEditor ) )]
		[EditorLimitsRange( 0, 2 )]
		[DefaultValue( 1f )]
		public float LeafFrequency
		{
			get { return leafFrequency; }
			set
			{
				if( leafFrequency == value )
					return;
				leafFrequency = value;
				UpdateDetailBendingParameters();
			}
		}

		protected override void OnClone( HighLevelMaterial sourceMaterial )
		{
			base.OnClone( sourceMaterial );

			VegetationMaterial source = (VegetationMaterial)sourceMaterial;

			receiveObjectsPositionsFromVertices = source.receiveObjectsPositionsFromVertices;

			windEffectFactor = source.windEffectFactor;
			detailBending = source.detailBending;
			bendScale = source.bendScale;
			bendVariation = source.bendVariation;
			bendFrequency = source.bendFrequency;
			branchAmplitude = source.branchAmplitude;
			leafAmplitude = source.leafAmplitude;
			branchFrequency = source.branchFrequency;
			leafFrequency = source.leafFrequency;
		}

		protected override bool OnLoad( TextBlock block )
		{
			if( !base.OnLoad( block ) )
				return false;

			if( block.IsAttributeExist( "receiveObjectsPositionsFromVertices" ) )
			{
				receiveObjectsPositionsFromVertices =
					bool.Parse( block.GetAttribute( "receiveObjectsPositionsFromVertices" ) );
			}

			if( block.IsAttributeExist( "windEffectFactor" ) )
				windEffectFactor = float.Parse( block.GetAttribute( "windEffectFactor" ) );

			if( block.IsAttributeExist( "bendScale" ) )
				bendScale = float.Parse( block.GetAttribute( "bendScale" ) );
			if( block.IsAttributeExist( "bendVariation" ) )
				bendVariation = float.Parse( block.GetAttribute( "bendVariation" ) );
			if( block.IsAttributeExist( "bendFrequency" ) )
				bendFrequency = float.Parse( block.GetAttribute( "bendFrequency" ) );

			if( block.IsAttributeExist( "detailBending" ) )
				detailBending = bool.Parse( block.GetAttribute( "detailBending" ) );
			if( block.IsAttributeExist( "branchAmplitude" ) )
				branchAmplitude = float.Parse( block.GetAttribute( "branchAmplitude" ) );
			if( block.IsAttributeExist( "leafAmplitude" ) )
				leafAmplitude = float.Parse( block.GetAttribute( "leafAmplitude" ) );
			if( block.IsAttributeExist( "branchFrequency" ) )
				branchFrequency = float.Parse( block.GetAttribute( "branchFrequency" ) );
			if( block.IsAttributeExist( "leafFrequency" ) )
				leafFrequency = float.Parse( block.GetAttribute( "leafFrequency" ) );

			return true;
		}

		protected override void OnSave( TextBlock block )
		{
			base.OnSave( block );

			if( receiveObjectsPositionsFromVertices )
			{
				block.SetAttribute( "receiveObjectsPositionsFromVertices",
					receiveObjectsPositionsFromVertices.ToString() );
			}

			if( windEffectFactor != 1 )
				block.SetAttribute( "windEffectFactor", windEffectFactor.ToString() );

			if( bendScale != 0.02f )
				block.SetAttribute( "bendScale", bendScale.ToString() );
			if( bendVariation != 0.01f )
				block.SetAttribute( "bendVariation", bendVariation.ToString() );
			if( bendFrequency != 1 )
				block.SetAttribute( "bendFrequency", bendFrequency.ToString() );

			block.SetAttribute( "detailBending", detailBending.ToString() );
			if( branchAmplitude != 0.01f )
				block.SetAttribute( "branchAmplitude", branchAmplitude.ToString() );
			if( leafAmplitude != 0.01f )
				block.SetAttribute( "leafAmplitude", leafAmplitude.ToString() );
			if( branchFrequency != 1 )
				block.SetAttribute( "branchFrequency", branchFrequency.ToString() );
			if( leafFrequency != 1 )
				block.SetAttribute( "leafFrequency", leafFrequency.ToString() );
		}

		protected override string OnGetExtensionFileName()
		{
			return "Vegetation.shaderBaseExtension";
		}

		protected override void OnAddCompileArguments( StringBuilder arguments )
		{
			base.OnAddCompileArguments( arguments );

			if( receiveObjectsPositionsFromVertices )
				arguments.Append( " -DRECEIVE_OBJECTS_POSITIONS_FROM_VERTICES" );
			if( detailBending )
				arguments.Append( " -DDETAIL_BENDING" );
		}

		protected override bool OnInitBaseMaterial()
		{
			if( !base.OnInitBaseMaterial() )
				return false;

			if( IsDefaultTechniqueCreated() )
			{
				UpdateWindAnimationParameters();
				UpdateMainBendingParameters();
				UpdateDetailBendingParameters();
			}

			return true;
		}

		void UpdateWindAnimationParameters()
		{
			Vec2 scaledWindSpeed = windSpeed * windEffectFactor;

			//pre-calculate Wave Speed         
			Vec2 waveSpeed = new Vec2( 0.1f, 0.2f );
			waveSpeed += waveSpeed * 3 * scaledWindSpeed.Length();

			SetCustomGpuParameter( VegetationGpuParameters.windAnimationParameters,
				new Vec4( scaledWindSpeed.X, scaledWindSpeed.Y, waveSpeed.X, waveSpeed.Y ) );
		}

		void UpdateMainBendingParameters()
		{
			float scale = bendScale * windEffectFactor;
			float variation = bendVariation * windEffectFactor;
			float frequency = bendFrequency * windEffectFactor;

			SetCustomGpuParameter( VegetationGpuParameters.mainBendingParameters,
				new Vec4( scale, variation, frequency, 0 ) );
		}

		void UpdateDetailBendingParameters()
		{
			SetCustomGpuParameter( VegetationGpuParameters.detailBendingParameters,
				new Vec4( branchAmplitude, leafAmplitude, branchFrequency, leafFrequency ) );
		}

		void SetCustomGpuParameter( VegetationGpuParameters parameter, Vec4 value )
		{
			for( int nMaterial = 0; nMaterial < 4; nMaterial++ )
			{
				Material material = null;

				switch( nMaterial )
				{
				case 0:
					material = BaseMaterial;
					break;
				case 1:
					material = BaseMaterial.GetShadowTextureCasterMaterial( RenderLightType.Point );
					break;
				case 2:
					material = BaseMaterial.GetShadowTextureCasterMaterial( RenderLightType.Directional );
					break;
				case 3:
					material = BaseMaterial.GetShadowTextureCasterMaterial( RenderLightType.Spot );
					break;
				}

				if( material == null )
					continue;

				foreach( Technique technique in material.Techniques )
				{
					foreach( Pass pass in technique.Passes )
					{
						if( pass.VertexProgramParameters != null || pass.FragmentProgramParameters != null )
						{
							if( pass.VertexProgramParameters != null )
							{
								if( !pass.IsCustomGpuParameterInitialized( (int)parameter ) )
								{
									pass.VertexProgramParameters.SetNamedAutoConstant( parameter.ToString(),
										GpuProgramParameters.AutoConstantType.Custom, (int)parameter );
								}
							}

							if( pass.FragmentProgramParameters != null )
							{
								if( !pass.IsCustomGpuParameterInitialized( (int)parameter ) )
								{
									pass.FragmentProgramParameters.SetNamedAutoConstant( parameter.ToString(),
										GpuProgramParameters.AutoConstantType.Custom, (int)parameter );
								}
							}

							pass.SetCustomGpuParameter( (int)parameter, value );
						}
					}
				}
			}
		}

		protected override bool OnIsNeedSpecialShadowCasterMaterial()
		{
			return true;
			//return base.OnIsNeedSpecialShadowCasterMaterial();
		}

		public void UpdateGlobalWindSettings( Vec2 speed )
		{
			if( windSpeed != speed )
			{
				windSpeed = speed;
				UpdateWindAnimationParameters();
			}
		}

		public override bool IsSupportsStaticBatching()
		{
			return false;
		}
	}
}
