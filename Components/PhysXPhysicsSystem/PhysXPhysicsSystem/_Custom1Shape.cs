// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.MathEx;
using Engine.FileSystem;
using Engine.Utils;
using Engine.PhysicsSystem;

namespace PhysXPhysicsSystem
{
	//custom shape example
	//as box shape
	//see PhysXBody.PushToWorld() method also.

	class _Custom1Shape : Shape
	{
		Vec3 dimensions = new Vec3( 1, 1, 1 );
		Vec3 halfDimensions = new Vec3( .5f, .5f, .5f );

		//

		internal _Custom1Shape() : base( Type._Custom1 ) { }

		[DefaultValue( typeof( Vec3 ), "1 1 1" )]
		[Category( "Box" )]
		[RefreshProperties( RefreshProperties.Repaint )]
		public Vec3 Dimensions
		{
			get { return dimensions; }
			set
			{
				if( Body.PushedToWorld )
					Log.Fatal( "Unable to change _Custom1Shape.Dimensions while body is created in the world." );

				value.X = Math.Abs( value.X );
				value.Y = Math.Abs( value.Y );
				value.Z = Math.Abs( value.Z );
				if( value.X == 0 )
					value.X = .01f;
				if( value.Y == 0 )
					value.Y = .01f;
				if( value.Z == 0 )
					value.Z = .01f;

				dimensions = value;
				halfDimensions = dimensions * .5f;

				Body._NeedUpdateCachedBounds();
				NeedUpdateCachedVolumeAndBodyMass();
			}
		}

		public override string ToString()
		{
			return "_Custom1";
		}

		protected override void OnDebugRender( DebugGeometry debugGeometry,
			ref Mat4 bodyTransform, bool simpleGeometry )
		{
			Bounds localBounds = new Bounds( -halfDimensions, halfDimensions );
			Mat4 t = bodyTransform;
			if( !IsIdentityTransform )
				t *= GetTransform();
			Box box = new Box( localBounds ) * t;
			debugGeometry.AddBox( box );
		}

		protected override bool OnIsContainsPoint( ref Vec3 localPosition )
		{
			Bounds localBounds = new Bounds( -halfDimensions, halfDimensions );
			return localBounds.IsContainsPoint( localPosition );
		}

		protected override float OnGetPointDistance( ref Vec3 localPosition )
		{
			Bounds localBounds = new Bounds( -halfDimensions, halfDimensions );

			Vec3 distance = Vec3.Zero;

			if( localPosition.X < localBounds.Minimum.X )
				distance.X = localBounds.Minimum.X - localPosition.X;
			else if( localPosition.X > localBounds.Maximum.X )
				distance.X = localPosition.X - localBounds.Maximum.X;

			if( localPosition.Y < localBounds.Minimum.Y )
				distance.Y = localBounds.Minimum.Y - localPosition.Y;
			else if( localPosition.Y > localBounds.Maximum.Y )
				distance.Y = localPosition.Y - localBounds.Maximum.Y;

			if( localPosition.Z < localBounds.Minimum.Z )
				distance.Z = localBounds.Minimum.Z - localPosition.Z;
			else if( localPosition.Z > localBounds.Maximum.Z )
				distance.Z = localPosition.Z - localBounds.Maximum.Z;

			if( distance == Vec3.Zero )
				return 0;

			float d = 0;
			if( distance.X != 0 )
				d += distance.X * distance.X;
			if( distance.Y != 0 )
				d += distance.Y * distance.Y;
			if( distance.Z != 0 )
				d += distance.Z * distance.Z;
			return MathFunctions.Sqrt( d );
		}

		public override void GetGlobalBounds( out Bounds bounds )
		{
			Vec3 globalPos = Body.Position;
			Quat globalRot = Body.Rotation;
			if( !IsIdentityTransform )
			{
				globalPos += Body.Rotation * Position;
				globalRot *= Rotation;
			}
			Mat3 globalRotMat3;
			globalRot.ToMat3( out globalRotMat3 );

			Vec3 axMat0 = halfDimensions.X * globalRotMat3.Item0;
			Vec3 axMat1 = halfDimensions.Y * globalRotMat3.Item1;
			Vec3 axMat2 = halfDimensions.Z * globalRotMat3.Item2;

			Vec3 temp0 = new Vec3( globalPos - axMat0 );
			Vec3 temp1 = new Vec3( globalPos + axMat0 );
			Vec3 temp2 = new Vec3( axMat1 - axMat2 );
			Vec3 temp3 = new Vec3( axMat1 + axMat2 );

			bounds = new Bounds( temp0 - temp3 );
			bounds.Add( temp1 - temp3 );
			bounds.Add( temp1 + temp2 );
			bounds.Add( temp0 + temp2 );
			bounds.Add( temp0 - temp2 );
			bounds.Add( temp1 - temp2 );
			bounds.Add( temp1 + temp3 );
			bounds.Add( temp0 + temp3 );
		}

		protected override void OnCopyDataFrom( Shape source )
		{
			base.OnCopyDataFrom( source );
			Dimensions = ( (_Custom1Shape)source ).Dimensions;
		}

		protected override bool OnLoad( TextBlock block, string modelFileName )
		{
			if( !base.OnLoad( block, modelFileName ) )
				return false;
			if( block.IsAttributeExist( "dimensions" ) )
				Dimensions = Vec3.Parse( block.GetAttribute( "dimensions" ) );
			return true;
		}

		protected override void OnSave( TextBlock block )
		{
			base.OnSave( block );
			if( Dimensions != new Vec3( 1, 1, 1 ) )
				block.SetAttribute( "dimensions", Dimensions.ToString() );
		}

		protected override float OnCalculateVolume()
		{
			return Dimensions.X * Dimensions.Y * Dimensions.Z;
		}

	}
}
