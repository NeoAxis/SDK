// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Text;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.Utils;
using ProjectCommon;

namespace ProjectEntities
{
	/// <summary>
	/// Defines the <see cref="Car"/> entity type.
	/// </summary>
	public class CarType : UnitType
	{
		//Masses
		[FieldSerialize]
		float massChassis = 1500;

		//Differential
		[FieldSerialize]
		PhysicsVehicle.DifferentialTypes differentialType = PhysicsVehicle.DifferentialTypes.LimitedSlipAllWheelDrive;
		[FieldSerialize]
		float differentialFrontRearSplit = 0.45f;
		[FieldSerialize]
		float differentialFrontLeftRightSplit = 0.5f;
		[FieldSerialize]
		float differentialRearLeftRightSplit = 0.5f;
		[FieldSerialize]
		float differentialCenterBias = 1.3f;
		[FieldSerialize]
		float differentialFrontBias = 1.3f;
		[FieldSerialize]
		float differentialRearBias = 1.3f;

		//Engine
		[FieldSerialize]
		float enginePeakTorque = 500;
		[FieldSerialize]
		float engineMaxRPM = 6000;
		[FieldSerialize]
		float engineDampingRateFullThrottle = .15f;
		[FieldSerialize]
		float engineDampingRateZeroThrottleClutchEngaged = 2.0f;
		[FieldSerialize]
		float engineDampingRateZeroThrottleClutchDisengaged = .35f;
		[FieldSerialize]
		List<EngineTorqueCurveItem> engineTorqueCurve = new List<EngineTorqueCurveItem>();

		//Gears
		[FieldSerialize]
		List<GearItem> gears = new List<GearItem>();
		[FieldSerialize]
		float gearsSwitchTime = .5f;

		//Clutch
		[FieldSerialize]
		float clutchStrength = 10;

		//Ackermann steer accuracy
		[FieldSerialize]
		float ackermannSteerAccuracy = 1;

		//Wheels
		[FieldSerialize]
		WheelItem wheelFrontLeft = new WheelItem( WheelNames.FrontLeft );
		[FieldSerialize]
		WheelItem wheelFrontRight = new WheelItem( WheelNames.FrontRight );
		[FieldSerialize]
		WheelItem wheelRearLeft = new WheelItem( WheelNames.RearLeft );
		[FieldSerialize]
		WheelItem wheelRearRight = new WheelItem( WheelNames.RearRight );

		//Tire types
		[FieldSerialize]
		List<TireTypeItem> tireTypes = new List<TireTypeItem>();

		//steer vs forward speed table
		[FieldSerialize]
		List<SteerVsForwardSpeedTableItem> steerVsForwardSpeedTable = new List<SteerVsForwardSpeedTableItem>();

		///////////////////////////////////////////

		public enum WheelNames
		{
			FrontLeft,
			FrontRight,
			RearLeft,
			RearRight,
		}

		///////////////////////////////////////////

		public class EngineTorqueCurveItem
		{
			[FieldSerialize]
			float normalizedTorque;
			[FieldSerialize]
			float normalizedRev;

			/// <summary>
			/// Normalised torque (torque/maxTorque).
			/// </summary>
			[Description( "Normalized torque (torque/maxTorque)." )]
			public float NormalizedTorque
			{
				get { return normalizedTorque; }
				set { normalizedTorque = value; }
			}

			/// <summary>
			/// Normalised engine revs (revs/maxRevs).
			/// </summary>
			[Description( "Normalized engine revs (revs/maxRevs)." )]
			public float NormalizedRev
			{
				get { return normalizedRev; }
				set { normalizedRev = value; }
			}

			public override string ToString()
			{
				return string.Format( "Torque {0}; Rev {1}", NormalizedTorque, NormalizedRev );
			}
		}

		///////////////////////////////////////////

		public class GearItem
		{
			[FieldSerialize]
			int number;
			[FieldSerialize]
			float ratio;

			/// <summary>
			/// The number of the gear. Set -1 to for reverse gear, set 0 for neutral gear.
			/// </summary>
			[Description( "The number of the gear. Set -1 to for reverse gear, set 0 for neutral gear." )]
			[DefaultValue( 0 )]
			public int Number
			{
				get { return number; }
				set { number = value; }
			}

			/// <summary>
			/// Gear ratio.
			/// </summary>
			[Description( "Gear ratio." )]
			[DefaultValue( 0.0f )]
			public float Ratio
			{
				get { return ratio; }
				set { ratio = value; }
			}

			public override string ToString()
			{
				return string.Format( "Gear {0}; Ratio {1}", Number, Ratio );
			}
		}

		///////////////////////////////////////////

		public enum TireTypeNames
		{
			Wets,
			Slicks,
			Ice,
			Mud,
		}

		///////////////////////////////////////////

		public class TireTypeItem
		{
			[FieldSerialize]
			TireTypeNames name = TireTypeNames.Wets;

			//Wheels
			[FieldSerialize]
			TireWheelItem wheelFrontLeft = new TireWheelItem( WheelNames.FrontLeft );
			[FieldSerialize]
			TireWheelItem wheelFrontRight = new TireWheelItem( WheelNames.FrontRight );
			[FieldSerialize]
			TireWheelItem wheelRearLeft = new TireWheelItem( WheelNames.RearLeft );
			[FieldSerialize]
			TireWheelItem wheelRearRight = new TireWheelItem( WheelNames.RearRight );

			//Material friction multipliers
			[FieldSerialize]
			List<FrictionMultiplierItem> frictionMultipliers = new List<FrictionMultiplierItem>();

			///////////////

			[TypeConverter( typeof( ExpandableObjectConverter ) )]
			public class TireWheelItem
			{
				WheelNames wheelName;

				[FieldSerialize]
				float tireLatStiffX = 2.0f;
				[FieldSerialize]
				float tireLatStiffY = 18;//0.3125f * ( 180.0f / 3.14159f );
				[FieldSerialize]
				float tireLongitudinalStiffnessPerUnitGravity = 1000.0f;
				[FieldSerialize]
				float tireCamberStiffness = 57;//1.0f * ( 180.0f / 3.14159f );

				[FieldSerialize]
				float frictionVsSlipGraphZeroLongitudinalSlip = 1.0f;
				[FieldSerialize]
				float frictionVsSlipGraphLongitudinalSlipWithMaximumFriction = 0.1f;
				[FieldSerialize]
				float frictionVsSlipGraphMaximumFriction = 1.0f;
				[FieldSerialize]
				float frictionVsSlipGraphEndPointOfGraph = 1.0f;
				[FieldSerialize]
				float frictionVsSlipGraphValueOfFrictionForSlipsGreaterThanEndPointOfGraph = 1.0f;

				//

				internal TireWheelItem( WheelNames wheelName )
				{
					this.wheelName = wheelName;
				}

				public override string ToString()
				{
					return string.Format( "Wheel: {0}", wheelName );
				}

				/// <summary>
				/// Tire lateral stiffness is typically a graph of tire load that has linear behaviour near zero load and flattens 
				/// at large loads.  mLatStiffX describes the minimum normalised load (load/restLoad) that gives a flat lateral 
				/// stiffness response.
				/// </summary>
				[Description( "Tire lateral stiffness is typically a graph of tire load that has linear behaviour near zero load and flattens at large loads.  mLatStiffX describes the minimum normalised load (load/restLoad) that gives a flat lateral stiffness response." )]
				[DefaultValue( 2.0f )]
				public float TireLatStiffX
				{
					get { return tireLatStiffX; }
					set
					{
						if( value < .0001f )
							value = .0001f;
						tireLatStiffX = value;
					}
				}

				/// <summary>
				/// Tire lateral stiffness is a graph of tire load that has linear behavior near zero load and flattens at large 
				/// loads. TireLatStiffY describes the maximum possible lateral stiffness divided by the rest tire load, specified 
				/// in "per radian".
				/// </summary>
				[Description( "Tire lateral stiffness is a graph of tire load that has linear behavior near zero load and flattens at large loads. TireLatStiffY describes the maximum possible lateral stiffness divided by the rest tire load, specified in \"per radian\"." )]
				[DefaultValue( 18.0f )]//0.3125f * ( 180.0f / 3.14159f ) )]
				public float TireLatStiffY
				{
					get { return tireLatStiffY; }
					set
					{
						if( value < .0001f )
							value = .0001f;
						tireLatStiffY = value;
					}
				}

				/// <summary>
				/// Tire Longitudinal stiffness per unit longitudinal slip per unit gravity, specified in N per radian per unit 
				/// gravitational acceleration Longitudinal stiffness of the tire per unit longitudinal slip is calculated as 
				/// gravitationalAcceleration * TireLongitudinalStiffnessPerUnitGravity.
				/// </summary>
				[Description( "Tire Longitudinal stiffness per unit longitudinal slip per unit gravity, specified in N per radian per unit gravitational acceleration Longitudinal stiffness of the tire per unit longitudinal slip is calculated as gravitationalAcceleration * TireLongitudinalStiffnessPerUnitGravity." )]
				[DefaultValue( 1000.0f )]
				public float TireLongitudinalStiffnessPerUnitGravity
				{
					get { return tireLongitudinalStiffnessPerUnitGravity; }
					set
					{
						if( value < .0001f )
							value = .0001f;
						tireLongitudinalStiffnessPerUnitGravity = value;
					}
				}

				/// <summary>
				/// Camber stiffness, specified in N per radian.
				/// </summary>
				[Description( "Camber stiffness, specified in N per radian." )]
				[DefaultValue( 57.0f )]//1.0f * ( 180.0f / 3.14159f ) )]
				public float TireCamberStiffness
				{
					get { return tireCamberStiffness; }
					set
					{
						if( value < 0 )
							value = 0;
						tireCamberStiffness = value;
					}
				}

				//

				/// <summary>
				/// Graph of friction vs longitudinal slip with 3 points. This is the friction available at zero longitudinal slip.
				/// </summary>
				[Description( "Graph of friction vs longitudinal slip with 3 points. This is the friction available at zero longitudinal slip." )]
				[DefaultValue( 1.0f )]
				public float FrictionVsSlipGraphZeroLongitudinalSlip
				{
					get { return frictionVsSlipGraphZeroLongitudinalSlip; }
					set
					{
						if( value < .0001f )
							value = .0001f;
						frictionVsSlipGraphZeroLongitudinalSlip = value;
					}
				}

				/// <summary>
				/// Graph of friction vs longitudinal slip with 3 points. This is the value of longitudinal slip with maximum 
				/// friction.
				/// </summary>
				[Description( "Graph of friction vs longitudinal slip with 3 points. This is the value of longitudinal slip with maximum friction." )]
				[DefaultValue( .1f )]
				public float FrictionVsSlipGraphLongitudinalSlipWithMaximumFriction
				{
					get { return frictionVsSlipGraphLongitudinalSlipWithMaximumFriction; }
					set
					{
						if( value < .0001f )
							value = .0001f;
						frictionVsSlipGraphLongitudinalSlipWithMaximumFriction = value;
					}
				}

				/// <summary>
				/// Graph of friction vs longitudinal slip with 3 points. This is the maximum friction.
				/// </summary>
				[Description( "Graph of friction vs longitudinal slip with 3 points. This is the maximum friction." )]
				[DefaultValue( 1.0f )]
				public float FrictionVsSlipGraphMaximumFriction
				{
					get { return frictionVsSlipGraphMaximumFriction; }
					set { frictionVsSlipGraphMaximumFriction = value; }
				}

				/// <summary>
				/// Graph of friction vs longitudinal slip with 3 points. This is the end point of the graph.
				/// </summary>
				[Description( "Graph of friction vs longitudinal slip with 3 points. This is the end point of the graph." )]
				[DefaultValue( 1.0f )]
				public float FrictionVsSlipGraphEndPointOfGraph
				{
					get { return frictionVsSlipGraphEndPointOfGraph; }
					set { frictionVsSlipGraphEndPointOfGraph = value; }
				}

				/// <summary>
				/// Graph of friction vs longitudinal slip with 3 points. This is the value of friction for slips greater than end 
				/// point of the graph.
				/// </summary>
				[Description( "Graph of friction vs longitudinal slip with 3 points. This is the value of friction for slips greater than end point of the graph." )]
				[DefaultValue( 1.0f )]
				public float FrictionVsSlipGraphValueOfFrictionForSlipsGreaterThanEndPointOfGraph
				{
					get { return frictionVsSlipGraphValueOfFrictionForSlipsGreaterThanEndPointOfGraph; }
					set { frictionVsSlipGraphValueOfFrictionForSlipsGreaterThanEndPointOfGraph = value; }
				}
			}

			///////////////

			public class FrictionMultiplierItem
			{
				[FieldSerialize]
				string surfaceMaterialName = "";
				[FieldSerialize]
				float value = 1;

				/// <summary>
				/// Gets or sets physical material of the surface. If you specify an empty string, the value will be used for all 
				/// materials which are not specified in this list.
				/// </summary>
				[DefaultValue( "" )]
				[Editor( typeof( PhysicsWorld.MaterialNameEditor ), typeof( UITypeEditor ) )]
				public string SurfaceMaterialName
				{
					get { return surfaceMaterialName; }
					set { surfaceMaterialName = value; }
				}

				/// <summary>
				/// Gets or sets friction value.
				/// </summary>
				[DefaultValue( 1.0f )]
				public float Value
				{
					get { return this.value; }
					set { this.value = value; }
				}

				public override string ToString()
				{
					return string.Format( "{0}; {1}", surfaceMaterialName, value );
				}
			}

			///////////////

			/// <summary>
			/// Gets or sets type of tire name.
			/// </summary>
			[DefaultValue( TireTypeNames.Wets )]
			public TireTypeNames Name
			{
				get { return name; }
				set { name = value; }
			}

			public TireWheelItem WheelFrontLeft
			{
				get { return wheelFrontLeft; }
			}

			public TireWheelItem WheelFrontRight
			{
				get { return wheelFrontRight; }
			}

			public TireWheelItem WheelRearLeft
			{
				get { return wheelRearLeft; }
			}

			public TireWheelItem WheelRearRight
			{
				get { return wheelRearRight; }
			}

			public List<FrictionMultiplierItem> FrictionMultipliers
			{
				get { return frictionMultipliers; }
			}

			public override string ToString()
			{
				return string.Format( "Tire: {0}", name );
			}
		}

		///////////////////////////////////////////

		[TypeConverter( typeof( ExpandableObjectConverter ) )]
		public class WheelItem
		{
			WheelNames wheelName;

			[FieldSerialize]
			float mass = 20;

			[FieldSerialize]
			float wheelDampingRate = 0.25f;
			[FieldSerialize]
			float wheelMaxBrakeTorque = 1500.0f;
			[FieldSerialize]
			float wheelMaxHandBrakeTorque;
			[FieldSerialize]
			Degree wheelMaxSteer;
			[FieldSerialize]
			Degree wheelToeAngle;

			[FieldSerialize]
			float suspensionSpringStrength = 35000;
			[FieldSerialize]
			float suspensionSpringDamperRate = 4500;
			[FieldSerialize]
			float suspensionMaxCompression = .3f;
			[FieldSerialize]
			float suspensionMaxDroop = .1f;
			[FieldSerialize]
			float suspensionSprungMassCoefficient = .25f;

			[FieldSerialize]
			Vec3 suspensionForceApplicationPointOffset = new Vec3( 0, 0, 0 );
			[FieldSerialize]
			Vec3 tireForceApplicationPointOffset = new Vec3( 0, 0, 0 );

			///////////////

			internal WheelItem( WheelNames wheelName )
			{
				this.wheelName = wheelName;
			}

			public override string ToString()
			{
				return string.Format( "Wheel: {0}", wheelName );
			}

			[DefaultValue( 20.0f )]
			public float Mass
			{
				get { return mass; }
				set
				{
					if( value < .0001f )
						value = .0001f;
					mass = value;
				}
			}

			/// <summary>
			/// Damping rate applied to wheel.
			/// </summary>
			[Description( "Damping rate applied to wheel." )]
			[DefaultValue( 0.25f )]
			public float WheelDampingRate
			{
				get { return wheelDampingRate; }
				set
				{
					if( value < 0 )
						value = 0;
					wheelDampingRate = value;
				}
			}

			/// <summary>
			/// Max brake torque that can be applied to wheel, specified in Nm.
			/// </summary>
			[Description( "Max brake torque that can be applied to wheel, specified in Nm." )]
			[DefaultValue( 1500.0f )]
			public float WheelMaxBrakeTorque
			{
				get { return wheelMaxBrakeTorque; }
				set
				{
					if( value < 0 )
						value = 0;
					wheelMaxBrakeTorque = value;
				}
			}

			/// <summary>
			/// Max handbrake torque that can be applied to wheel, specified in Nm.
			/// </summary>
			[Description( "Max handbrake torque that can be applied to wheel, specified in Nm." )]
			[DefaultValue( 0.0f )]
			public float WheelMaxHandBrakeTorque
			{
				get { return wheelMaxHandBrakeTorque; }
				set
				{
					if( value < 0 )
						value = 0;
					wheelMaxHandBrakeTorque = value;
				}
			}

			/// <summary>
			/// Max steer angle that can be achieved by the wheel, specified in degrees.
			/// </summary>
			[Description( "Max steer angle that can be achieved by the wheel, specified in degrees." )]
			[DefaultValue( typeof( Degree ), "0" )]
			public Degree WheelMaxSteer
			{
				get { return wheelMaxSteer; }
				set
				{
					if( value < 0 )
						value = 0;
					if( value > new Degree( 89.9999f ) )
						value = new Degree( 89.9999f );
					wheelMaxSteer = value;
				}
			}

			/// <summary>
			/// Wheel toe angle, specified in degrees.
			/// </summary>
			[Description( "Wheel toe angle, specified in degrees." )]
			[DefaultValue( typeof( Degree ), "0" )]
			public Degree WheelToeAngle
			{
				get { return wheelToeAngle; }
				set
				{
					if( value < new Degree( -89.9999f ) )
						value = new Degree( -89.9999f );
					if( value > new Degree( 89.9999f ) )
						value = new Degree( 89.9999f );
					wheelToeAngle = value;
				}
			}

			/// <summary>
			/// Spring strength of suspension unit, specified in N m^-1.
			/// </summary>
			[Description( "Spring strength of suspension unit, specified in N m^-1." )]
			[DefaultValue( 35000.0f )]
			public float SuspensionSpringStrength
			{
				get { return suspensionSpringStrength; }
				set
				{
					if( value < .0001f )
						value = .0001f;
					suspensionSpringStrength = value;
				}
			}

			/// <summary>
			/// Spring damper rate of suspension unit, specified in s^-1.
			/// </summary>
			[Description( "Spring damper rate of suspension unit, specified in s^-1." )]
			[DefaultValue( 4500.0f )]
			public float SuspensionSpringDamperRate
			{
				get { return suspensionSpringDamperRate; }
				set
				{
					if( value < 0 )
						value = 0;
					suspensionSpringDamperRate = value;
				}
			}

			/// <summary>
			/// Maximum compression allowed by suspension spring, specified in m.
			/// </summary>
			[Description( "Maximum compression allowed by suspension spring, specified in m." )]
			[DefaultValue( .3f )]
			public float SuspensionMaxCompression
			{
				get { return suspensionMaxCompression; }
				set
				{
					if( value < .0001f )
						value = .0001f;
					suspensionMaxCompression = value;
				}
			}

			/// <summary>
			/// Maximum elongation allowed by suspension spring, specified in m.
			/// </summary>
			[Description( "Maximum elongation allowed by suspension spring, specified in m." )]
			[DefaultValue( .1f )]
			public float SuspensionMaxDroop
			{
				get { return suspensionMaxDroop; }
				set
				{
					if( value < .0001f )
						value = .0001f;
					suspensionMaxDroop = value;
				}
			}

			/// <summary>
			/// Mass of vehicle that is supported by suspension spring, specified in coefficient of the chassis mass.
			/// </summary>
			[Description( "Mass of vehicle that is supported by suspension spring, specified in coefficient of the chassis mass." )]
			[DefaultValue( 0.25f )]
			public float SuspensionSprungMassCoefficient// SuspensionSprungMass
			{
				get { return suspensionSprungMassCoefficient; }
				set
				{
					if( value < .0001f )
						value = .0001f;
					suspensionSprungMassCoefficient = value;
				}
			}

			/// <summary>
			/// Gets or sets the application point of the suspension force of the suspension of the wheel. Specified relative to 
			/// the centre of the wheel.
			/// </summary>
			[Description( "Gets or sets the application point of the suspension force of the suspension of the wheel. Specified relative to the centre of the wheel." )]
			[DefaultValue( typeof( Vec3 ), "0 0 0" )]
			public Vec3 SuspensionForceApplicationPointOffset
			{
				get { return suspensionForceApplicationPointOffset; }
				set { suspensionForceApplicationPointOffset = value; }
			}

			/// <summary>
			/// Gets or sets the application point of the tire force of the tire of the wheel. Specified relative to the centre 
			/// of the wheel.
			/// </summary>
			[Description( "Gets or sets the application point of the tire force of the tire of the wheel. Specified relative to the centre of mass of the wheel." )]
			[DefaultValue( typeof( Vec3 ), "0 0 0" )]
			public Vec3 TireForceApplicationPointOffset
			{
				get { return tireForceApplicationPointOffset; }
				set { tireForceApplicationPointOffset = value; }
			}
		}

		///////////////////////////////////////////

		public class SteerVsForwardSpeedTableItem
		{
			[FieldSerialize]
			float forwardSpeed;
			[FieldSerialize]
			float steer = 1;

			[DefaultValue( 0.0f )]
			public float ForwardSpeed
			{
				get { return forwardSpeed; }
				set
				{
					if( value < 0 )
						value = 0;
					forwardSpeed = value;
				}
			}

			[DefaultValue( 1.0f )]
			public float Steer
			{
				get { return steer; }
				set
				{
					MathFunctions.Saturate( ref value );
					steer = value;
				}
			}

			public override string ToString()
			{
				return string.Format( "Forward speed: {0}; Steer: {1}", forwardSpeed, steer );
			}
		}

		///////////////////////////////////////////

		[DefaultValue( 1500.0f )]
		public float MassChassis
		{
			get { return massChassis; }
			set
			{
				if( value < .0001f )
					value = .0001f;
				massChassis = value;
			}
		}

		//

		/// <summary>
		/// Type of differential.
		/// </summary>
		[Description( "Type of differential." )]
		[DefaultValue( PhysicsVehicle.DifferentialTypes.LimitedSlipAllWheelDrive )]
		public PhysicsVehicle.DifferentialTypes DifferentialType
		{
			get { return differentialType; }
			set { differentialType = value; }
		}

		/// <summary>
		/// Ratio of torque split between front and rear (more than 0.5 means more to front, less than 0.5 means more to rear). 
		/// Only applied to LimitedSlipAllWheelDrive and OpenAllWheelDrive differential types.
		/// </summary>
		[Description( "Ratio of torque split between front and rear (more than 0.5 means more to front, less than 0.5 means more to rear). Only applied to LimitedSlipAllWheelDrive and OpenAllWheelDrive differential types." )]
		[DefaultValue( 0.45f )]
		public float DifferentialFrontRearSplit
		{
			get { return differentialFrontRearSplit; }
			set
			{
				MathFunctions.Saturate( ref value );
				differentialFrontRearSplit = value;
			}
		}

		/// <summary>
		/// Ratio of torque split between front-left and front-right (more than 0.5 means more to front-left, less than 0.5 
		/// means more to front-right). Only applied to LimitedSlipAllWheelDrive, OpenAllWheelDrive and 
		/// LimitedSlipFrontWheelDrive differential types.
		/// </summary>
		[Description( "Ratio of torque split between front-left and front-right (more than 0.5 means more to front-left, less than 0.5 means more to front-right). Only applied to LimitedSlipAllWheelDrive, OpenAllWheelDrive and LimitedSlipFrontWheelDrive differential types." )]
		[DefaultValue( 0.5f )]
		public float DifferentialFrontLeftRightSplit
		{
			get { return differentialFrontLeftRightSplit; }
			set
			{
				MathFunctions.Saturate( ref value );
				differentialFrontLeftRightSplit = value;
			}
		}

		/// <summary>
		/// Ratio of torque split between rear-left and rear-right (more than 0.5 means more to rear-left, less than 0.5 means 
		/// more to rear-right). Only applied to LimitedSlipAllWheelDrive, OpenAllWheelDrive and 
		/// LimitedSlipRearWheelDrive differential types.
		/// </summary>
		[Description( "Ratio of torque split between rear-left and rear-right (more than 0.5 means more to rear-left, less than 0.5 means more to rear-right). Only applied to LimitedSlipAllWheelDrive, OpenAllWheelDrive and LimitedSlipRearWheelDrive differential types." )]
		[DefaultValue( 0.5f )]
		public float DifferentialRearLeftRightSplit
		{
			get { return differentialRearLeftRightSplit; }
			set
			{
				MathFunctions.Saturate( ref value );
				differentialRearLeftRightSplit = value;
			}
		}

		/// <summary>
		/// Maximum allowed ratio of average front wheel rotation speed and rear wheel rotation speeds. 
		/// Only applied to LimitedSlipAllWheelDrive differential types.
		/// </summary>
		[Description( "Maximum allowed ratio of average front wheel rotation speed and rear wheel rotation speeds. Only applied to LimitedSlipAllWheelDrive differential types." )]
		[DefaultValue( 1.3f )]
		public float DifferentialCenterBias
		{
			get { return differentialCenterBias; }
			set
			{
				if( value < 1 )
					value = 1;
				differentialCenterBias = value;
			}
		}

		/// <summary>
		/// Maximum allowed ratio of front-left and front-right wheel rotation speeds. 
		/// Only applied to LimitedSlipAllWheelDrive and LimitedSlipFrontWheelDrive differential types.
		/// </summary>
		[Description( "Maximum allowed ratio of front-left and front-right wheel rotation speeds. Only applied to LimitedSlipAllWheelDrive and LimitedSlipFrontWheelDrive differential types." )]
		[DefaultValue( 1.3f )]
		public float DifferentialFrontBias
		{
			get { return differentialFrontBias; }
			set
			{
				if( value < 1 )
					value = 1;
				differentialFrontBias = value;
			}
		}

		/// <summary>
		/// Maximum allowed ratio of rear-left and rear-right wheel rotation speeds. 
		/// Only applied to LimitedSlipAllWheelDrive and LimitedSlipRearWheelDrive differential types.
		/// </summary>
		[Description( "Maximum allowed ratio of rear-left and rear-right wheel rotation speeds. Only applied to LimitedSlipAllWheelDrive and LimitedSlipRearWheelDrive differential types." )]
		[DefaultValue( 1.3f )]
		public float DifferentialRearBias
		{
			get { return differentialRearBias; }
			set
			{
				if( value < 1 )
					value = 1;
				differentialRearBias = value;
			}
		}

		//

		/// <summary>
		/// Maximum torque available to apply to the engine, specified in Nm. Please note that to optimize the implementation 
		/// the engine has a hard-coded inertia of 1kgm^2. As a consequence the magnitude of the engine's angular acceleration 
		/// is exactly equal to the magnitude of the torque driving the engine. To simulate engines with different inertias 
		/// (!=1kgm^2) adjust either the entries of EngineTorqueCurve or EnginePeakTorque accordingly.
		/// </summary>
		[Description( "Maximum torque available to apply to the engine, specified in Nm. Please note that to optimize the implementation the engine has a hard-coded inertia of 1kgm^2. As a consequence the magnitude of the engine's angular acceleration is exactly equal to the magnitude of the torque driving the engine. To simulate engines with different inertias (!=1kgm^2) adjust either the entries of EngineTorqueCurve or EnginePeakTorque accordingly." )]
		[DefaultValue( 500.0f )]
		public float EnginePeakTorque
		{
			get { return enginePeakTorque; }
			set
			{
				if( value < .0001f )
					value = .0001f;
				enginePeakTorque = value;
			}
		}

		/// <summary>
		/// Maximum rotation speed of the engine, specified in revolutions per minute (RPM).
		/// </summary>
		[Description( "Maximum rotation speed of the engine, specified in revolutions per minute (RPM)." )]
		[DefaultValue( 6000.0f )]
		public float EngineMaxRPM
		{
			get { return engineMaxRPM; }
			set
			{
				if( value < .0001f )
					value = .0001f;
				engineMaxRPM = value;
			}
		}

		/// <summary>
		/// Damping rate of engine in s^-1 when full throttle is applied. Damping rate applied at run-time is an interpolation 
		/// between EngineDampingRateZeroThrottleClutchEngaged and EngineDampingRateFullThrottle if the clutch is engaged.
		/// If the clutch is disengaged (in neutral gear) the damping rate applied at run-time is an interpolation between 
		/// EngineDampingRateZeroThrottleClutchDisengaged and EngineDampingRateFullThrottle.
		/// </summary>
		[Description( "Damping rate of engine in s^-1 when full throttle is applied. Damping rate applied at run-time is an interpolation between EngineDampingRateZeroThrottleClutchEngaged and EngineDampingRateFullThrottle if the clutch is engaged.  If the clutch is disengaged (in neutral gear) the damping rate applied at run-time is an interpolation between EngineDampingRateZeroThrottleClutchDisengaged and EngineDampingRateFullThrottle." )]
		[DefaultValue( .15f )]
		public float EngineDampingRateFullThrottle
		{
			get { return engineDampingRateFullThrottle; }
			set
			{
				if( value < .0001f )
					value = .0001f;
				engineDampingRateFullThrottle = value;
			}
		}

		/// <summary>
		/// Damping rate of engine in s^-1 at zero throttle when the clutch is engaged. Damping rate applied at run-time is an 
		/// interpolation between EngineDampingRateZeroThrottleClutchEngaged and EngineDampingRateFullThrottle if the clutch 
		/// is engaged. If the clutch is disengaged (in neutral gear) the damping rate applied at run-time is an interpolation 
		/// between EngineDampingRateZeroThrottleClutchDisengaged and EngineDampingRateFullThrottle.
		/// </summary>
		[Description( "Damping rate of engine in s^-1 at zero throttle when the clutch is engaged. Damping rate applied at run-time is an interpolation between EngineDampingRateZeroThrottleClutchEngaged and EngineDampingRateFullThrottle if the clutch is engaged. If the clutch is disengaged (in neutral gear) the damping rate applied at run-time is an interpolation between EngineDampingRateZeroThrottleClutchDisengaged and EngineDampingRateFullThrottle." )]
		[DefaultValue( 2.0f )]
		public float EngineDampingRateZeroThrottleClutchEngaged
		{
			get { return engineDampingRateZeroThrottleClutchEngaged; }
			set
			{
				if( value < .0001f )
					value = .0001f;
				engineDampingRateZeroThrottleClutchEngaged = value;
			}
		}

		/// <summary>
		/// Damping rate of engine in s^-1 at zero throttle when the clutch is disengaged (in neutral gear). Damping rate applied 
		/// at run-time is an interpolation between EngineDampingRateZeroThrottleClutchEngaged and EngineDampingRateFullThrottle 
		/// if the clutch is engaged.  If the clutch is disengaged (in neutral gear) the damping rate applied at run-time is an 
		/// interpolation between EngineDampingRateZeroThrottleClutchDisengaged and EngineDampingRateFullThrottle.
		/// </summary>
		[Description( "Damping rate of engine in s^-1 at zero throttle when the clutch is disengaged (in neutral gear). Damping rate applied at run-time is an interpolation between EngineDampingRateZeroThrottleClutchEngaged and EngineDampingRateFullThrottle if the clutch is engaged.  If the clutch is disengaged (in neutral gear) the damping rate applied at run-time is an interpolation between EngineDampingRateZeroThrottleClutchDisengaged and EngineDampingRateFullThrottle." )]
		[DefaultValue( .35f )]
		public float EngineDampingRateZeroThrottleClutchDisengaged
		{
			get { return engineDampingRateZeroThrottleClutchDisengaged; }
			set
			{
				if( value < .0001f )
					value = .0001f;
				engineDampingRateZeroThrottleClutchDisengaged = value;
			}
		}

		/// <summary>
		/// Graph of normalized torque (torque/maxTorque) against normalized engine revs (revs/maxRevs).
		/// </summary>
		[Description( "Graph of normalized torque (torque/maxTorque) against normalized engine revs (revs/maxRevs)." )]
		public List<EngineTorqueCurveItem> EngineTorqueCurve
		{
			get { return engineTorqueCurve; }
		}

		/// <summary>
		/// The list of gears.
		/// </summary>
		[Description( "The list of gears." )]
		public List<GearItem> Gears
		{
			get { return gears; }
		}

		/// <summary>
		/// Time it takes to switch gear, specified in seconds.
		/// </summary>
		[DefaultValue( .5f )]
		[Description( "Time it takes to switch gear, specified in seconds." )]
		public float GearsSwitchTime
		{
			get { return gearsSwitchTime; }
			set
			{
				if( value < 0 )
					value = 0;
				gearsSwitchTime = value;
			}
		}

		/// <summary>
		/// Strength of clutch. Torque generated by clutch is proportional to the clutch strength and the velocity difference 
		/// between the engine speed and the speed of the driven wheels after accounting for the gear ratio.
		/// </summary>
		[DefaultValue( 10.0f )]
		[Description( "Strength of clutch. Torque generated by clutch is proportional to the clutch strength and the velocity difference between the engine speed and the speed of the driven wheels after accounting for the gear ratio." )]
		public float ClutchStrength
		{
			get { return clutchStrength; }
			set
			{
				if( value < .0001f )
					value = .0001f;
				clutchStrength = value;
			}
		}

		/// <summary>
		/// Accuracy of Ackermann steer calculation. Accuracy with value 0.0f results in no Ackermann steer-correction. 
		/// Accuracy with value 1.0 results in perfect Ackermann steer-correction.
		/// </summary>
		[DefaultValue( 1.0f )]
		[Description( "Accuracy of Ackermann steer calculation. Accuracy with value 0.0f results in no Ackermann steer-correction. Accuracy with value 1.0 results in perfect Ackermann steer-correction." )]
		public float AckermannSteerAccuracy
		{
			get { return ackermannSteerAccuracy; }
			set
			{
				MathFunctions.Saturate( ref value );
				ackermannSteerAccuracy = value;
			}
		}

		public WheelItem WheelFrontLeft
		{
			get { return wheelFrontLeft; }
		}

		public WheelItem WheelFrontRight
		{
			get { return wheelFrontRight; }
		}

		public WheelItem WheelRearLeft
		{
			get { return wheelRearLeft; }
		}

		public WheelItem WheelRearRight
		{
			get { return wheelRearRight; }
		}

		/// <summary>
		/// The list of tire types.
		/// </summary>
		[TypeConverter( typeof( CollectionTypeConverter ) )]
		[Editor( "ProjectEntities.Editor.CarType_TireTypesCollectionEditor, ProjectEntities.Editor", typeof( UITypeEditor ) )]
		[Description( "The list of tire types." )]
		public List<TireTypeItem> TireTypes
		{
			get { return tireTypes; }
		}

		/// <summary>
		/// Table of steer multipliers depending on the forward speed.
		/// </summary>
		[TypeConverter( typeof( CollectionTypeConverter ) )]
		[Editor( "ProjectEntities.Editor.CarType_SteerVsForwardSpeedTableCollectionEditor, ProjectEntities.Editor", typeof( UITypeEditor ) )]
		[Description( "Table of steer multipliers depending on the forward speed." )]
		public List<SteerVsForwardSpeedTableItem> SteerVsForwardSpeedTable
		{
			get { return steerVsForwardSpeedTable; }
		}

		public TireTypeItem FindTireTypeItem( TireTypeNames name )
		{
			foreach( TireTypeItem item in tireTypes )
			{
				if( item.Name == name )
					return item;
			}
			return null;
		}
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public class Car : Unit, ProfilingToolWindow.ICarInfo
	{
		[FieldSerialize]
		CarType.TireTypeNames tireType = CarType.TireTypeNames.Wets;
		[FieldSerialize]
		bool autoGear = true;
		[FieldSerialize]
		bool autoGearAutoReverse = true;

		PhysicsVehicle physicsVehicle;

		//Minefield specific
		float minefieldUpdateTimer;

		MapObjectAttachedObject[] wheelAttachedObjects;

		//networking support. data on client machine to transfer position of wheels.
		Radian[] networkingClient_wheelsRotationAngle;
		Radian[] networkingClient_wheelsSteer;
		float[] networkingClient_wheelsSuspensionJounce;

		float allowToSleepTime;

		///////////////////////////////////////////

		enum NetworkMessages
		{
			WheelsPositionToClient,
		}

		///////////////////////////////////////////

		CarType _type = null; public new CarType Type { get { return _type; } }

		///////////////////////////////////////////

		[DefaultValue( CarType.TireTypeNames.Wets )]
		public CarType.TireTypeNames TireType
		{
			get { return tireType; }
			set
			{
				if( tireType == value )
					return;
				tireType = value;

				//recreate vehicle
				if( physicsVehicle != null )
					CreatePhysicsVehicle();
			}
		}

		[DefaultValue( true )]
		public bool AutoGear
		{
			get { return autoGear; }
			set
			{
				autoGear = value;
				if( physicsVehicle != null )
					physicsVehicle.AutoGear = autoGear;
			}
		}

		[DefaultValue( true )]
		public bool AutoGearAutoReverse
		{
			get { return autoGearAutoReverse; }
			set { autoGearAutoReverse = value; }
		}

		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			if( EntitySystemWorld.Instance.WorldSimulationType != WorldSimulationTypes.Editor )
			{
				SubscribeToTickEvent();
				CreatePhysicsVehicle();

				wheelAttachedObjects = new MapObjectAttachedObject[ 4 ];
				wheelAttachedObjects[ 0 ] = GetFirstAttachedObjectByAlias( "wheelFrontLeft" );
				wheelAttachedObjects[ 1 ] = GetFirstAttachedObjectByAlias( "wheelFrontRight" );
				wheelAttachedObjects[ 2 ] = GetFirstAttachedObjectByAlias( "wheelRearLeft" );
				wheelAttachedObjects[ 3 ] = GetFirstAttachedObjectByAlias( "wheelRearRight" );
			}
		}

		protected override void OnDestroy()
		{
			DestroyPhysicsVehicle();
			base.OnDestroy();
		}

		protected override void OnIntellectCommand( Intellect.Command command )
		{
			base.OnIntellectCommand( command );

			if( ( EntitySystemWorld.Instance.IsServer() || EntitySystemWorld.Instance.IsSingle() ) && physicsVehicle != null )
			{
				bool gearUp = command.KeyPressed && command.Key == GameControlKeys.VehicleGearUp;
				bool gearDown = command.KeyPressed && command.Key == GameControlKeys.VehicleGearDown;
				if( gearUp || gearDown )
				{
					int currentGear = physicsVehicle.GetCurrentGear();
					int targetGear = physicsVehicle.GetTargetGear();

					if( gearUp )
					{
						if( !AutoGear || ( AutoGear && currentGear <= 0 && currentGear == targetGear ) )
							physicsVehicle.StartGearChange( currentGear + 1 );
					}
					if( gearDown )
					{
						if( !AutoGear || ( AutoGear && currentGear >= 0 && currentGear == targetGear ) )
							physicsVehicle.StartGearChange( currentGear - 1 );
					}
				}
			}
		}

		protected override void OnSuspendPhysicsDuringMapLoading( bool suspend )
		{
			base.OnSuspendPhysicsDuringMapLoading( suspend );

			//After loading a map, the physics simulate 5 seconds, that bodies have fallen asleep.
			//During this time we will disable physics for this entity.
			if( PhysicsModel != null )
			{
				if( suspend )
					DestroyPhysicsVehicle();
				foreach( Body body in PhysicsModel.Bodies )
				{
					body.Static = suspend;
					if( !suspend )
						body.Sleeping = false;
				}
				if( !suspend )
					CreatePhysicsVehicle();

				allowToSleepTime = 0;
			}
		}

		void TickGearsAutoReverse()
		{
			if( !IsInAir() )
			{
				float forwardSpeed = GetForwardSpeed();
				int currentGear = physicsVehicle.GetCurrentGear();

				const float forwardSpeedTheshold = .1f;

				bool inputDigitalAccel = Intellect.GetControlKeyStrength( GameControlKeys.Forward ) != 0;
				bool inputDigitalBrake = Intellect.GetControlKeyStrength( GameControlKeys.Backward ) != 0;

				if( inputDigitalBrake && forwardSpeed < forwardSpeedTheshold && ( currentGear == 0 || currentGear == 1 ) )
					physicsVehicle.StartGearChange( currentGear - 1 );
				if( inputDigitalAccel && forwardSpeed < forwardSpeedTheshold && ( currentGear == -1 || currentGear == 0 ) )
					physicsVehicle.StartGearChange( currentGear + 1 );
			}
		}

		protected override void OnTick()
		{
			base.OnTick();

			if( physicsVehicle != null )
			{
				//player controlling

				//used to produce smooth vehicle driving control values.
				PhysicsVehicle.InputSmoothingSettings smoothingSettings = new PhysicsVehicle.InputSmoothingSettings();
				smoothingSettings.RiseRateAccel = 3;
				smoothingSettings.RiseRateBrake = 3;
				smoothingSettings.RiseRateSteer = 2.5f;
				smoothingSettings.RiseRateHandbrake = 10;
				smoothingSettings.FallRateAccel = 5;
				smoothingSettings.FallRateBrake = 5;
				smoothingSettings.FallRateSteer = 5;
				smoothingSettings.FallRateHandbrake = 10;

				Pair<float, float>[] steerVsForwardSpeedTable = new Pair<float, float>[ Type.SteerVsForwardSpeedTable.Count ];
				for( int n = 0; n < steerVsForwardSpeedTable.Length; n++ )
				{
					CarType.SteerVsForwardSpeedTableItem item = Type.SteerVsForwardSpeedTable[ n ];
					steerVsForwardSpeedTable[ n ] = new Pair<float, float>( item.ForwardSpeed, item.Steer );
				}

				float accel = 0;
				float brake = 0;
				float steer = 0;
				float handbrake = 0;

				if( Intellect != null )
				{
					if( AutoGear && AutoGearAutoReverse )
						TickGearsAutoReverse();

					accel = Intellect.GetControlKeyStrength( GameControlKeys.Forward );
					brake = Intellect.GetControlKeyStrength( GameControlKeys.Backward );
					//swap accel brake
					if( AutoGear && AutoGearAutoReverse && physicsVehicle.GetCurrentGear() == -1 )
						MathFunctions.Swap( ref accel, ref brake );

					steer = 0;
					float left = Intellect.GetControlKeyStrength( GameControlKeys.Left );
					if( left > 0 )
						steer += -left;
					float right = Intellect.GetControlKeyStrength( GameControlKeys.Right );
					if( right > 0 )
						steer += right;

					handbrake = Intellect.GetControlKeyStrength( GameControlKeys.VehicleHandbrake );
				}
				else
				{
					//switch on handbrake
					handbrake = 1;
				}

				physicsVehicle.SetInputData( PhysicsVehicle.InputDataTypes.Analog, smoothingSettings, steerVsForwardSpeedTable,
					accel, brake, steer, handbrake );

				//sleeping state update
				{
					bool enable = false;

					if( accel != 0 )
						enable = true;
					if( brake != 0 )
						enable = true;
					if( steer != 0 )
						enable = true;
					foreach( bool isAir in physicsVehicle.AreWheelsInAir() )
					{
						if( isAir )
							enable = true;
					}
					if( physicsVehicle.BaseBody.LinearVelocity.Length() > .07f )
						enable = true;

					if( enable )
						allowToSleepTime = 0;
					else
						allowToSleepTime += TickDelta;
					
					if( allowToSleepTime > 1 )
					{
						physicsVehicle.EnableUpdate = false;
						physicsVehicle.BaseBody.Sleepiness = 1;
						physicsVehicle.BaseBody.Sleeping = true;
					}
					else
					{
						physicsVehicle.EnableUpdate = true;
						physicsVehicle.BaseBody.Sleepiness = 0;
						physicsVehicle.BaseBody.Sleeping = false;
					}
				}
			}

			//Minefield specific
			TickMinefields();

			//send position of wheels to client machines
			if( EntitySystemWorld.Instance.IsServer() && Type.NetworkType == EntityNetworkTypes.Synchronized )
			{
				if( physicsVehicle != null )
					Server_SendWheelsPositionToAllClients();
			}
		}

		protected override void OnRenderFrame()
		{
			base.OnRenderFrame();

			//update position, rotation of attached meshes of wheels
			if( wheelAttachedObjects != null )
			{
				Radian[] wheelsRotationAngle = null;
				Radian[] wheelsSteer = null;
				float[] wheelsSuspensionJounce = null;
				if( EntitySystemWorld.Instance.IsClientOnly() && networkingClient_wheelsRotationAngle != null )
				{
					wheelsRotationAngle = networkingClient_wheelsRotationAngle;
					wheelsSteer = networkingClient_wheelsSteer;
					wheelsSuspensionJounce = networkingClient_wheelsSuspensionJounce;
				}
				else if( physicsVehicle != null )
				{
					wheelsRotationAngle = physicsVehicle.GetWheelsRotationAngle();
					wheelsSteer = physicsVehicle.GetWheelsSteer();
					wheelsSuspensionJounce = physicsVehicle.GetWheelsSuspensionJounce();
				}

				if( wheelsRotationAngle != null )
				{
					for( int wheelIndex = 0; wheelIndex < 4; wheelIndex++ )
					{
						MapObjectAttachedObject attachedObject = wheelAttachedObjects[ wheelIndex ];
						if( attachedObject != null )
						{
							attachedObject.PositionOffset = attachedObject.TypeObject.Position +
								new Vec3( 0, 0, wheelsSuspensionJounce[ wheelIndex ] );
							Quat rotationAngle = new Angles( 0, -wheelsRotationAngle[ wheelIndex ].InDegrees(), 0 ).ToQuat();
							Quat rotationSteer = new Angles( 0, 0, -wheelsSteer[ wheelIndex ].InDegrees() ).ToQuat();
							attachedObject.RotationOffset = attachedObject.TypeObject.Rotation * rotationSteer * rotationAngle;
						}
					}
				}
			}
		}

		protected override void OnRender( Camera camera )
		{
			base.OnRender( camera );

			//update visiblity of attached objects. hide car for First Person mode.
			{
				bool show = true;
				PlayerIntellect playerIntellect = Intellect as PlayerIntellect;
				if( playerIntellect != null && playerIntellect.FPSCamera )
					show = false;
				foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
					attachedObject.Visible = show;
			}

			//draw debug data
			if( camera.Purpose == Camera.Purposes.MainCamera && EngineDebugSettings.DrawDynamicPhysics && physicsVehicle != null )
			{
				float distanceSqr = MapBounds.GetPointDistanceSqr( camera.Position );
				float farClipDistance = camera.FarClipDistance;
				if( distanceSqr < farClipDistance * farClipDistance )
				{
					float renderTime = RendererWorld.Instance.FrameRenderTime;
					float timeInterpolate = ( renderTime - Entities.Instance.TickTime ) * EntitySystemWorld.Instance.GameFPS;
					MathFunctions.Clamp( ref timeInterpolate, 0, 1 );

					camera.DebugGeometry.SetSpecialDepthSettings( false, true );
					physicsVehicle.DebugRender( camera.DebugGeometry, timeInterpolate, 1, false, true, new ColorValue( 1, 0, 0 ) );
					camera.DebugGeometry.RestoreDefaultDepthSettings();
				}
			}
		}

		public bool IsInAir()
		{
			if( physicsVehicle != null )
			{
				foreach( bool inAir in physicsVehicle.AreWheelsInAir() )
				{
					if( !inAir )
						return false;
				}
				return true;
			}
			return false;
		}

		void UpdateWheelSettings( PhysicsVehicle.InitDataClass.WheelItem destination, CarType.WheelItem source,
			CarType.TireTypeItem.TireWheelItem tireSource )
		{
			destination.Mass = source.Mass;

			destination.WheelDampingRate = source.WheelDampingRate;
			destination.WheelMaxBrakeTorque = source.WheelMaxBrakeTorque;
			destination.WheelMaxHandBrakeTorque = source.WheelMaxHandBrakeTorque;
			destination.WheelMaxSteer = source.WheelMaxSteer;
			destination.WheelToeAngle = source.WheelToeAngle;
			destination.SuspensionSpringStrength = source.SuspensionSpringStrength;
			destination.SuspensionSpringDamperRate = source.SuspensionSpringDamperRate;
			destination.SuspensionMaxCompression = source.SuspensionMaxCompression;
			destination.SuspensionMaxDroop = source.SuspensionMaxDroop;
			destination.SuspensionSprungMassCoefficient = source.SuspensionSprungMassCoefficient;
			destination.SuspensionForceApplicationPointOffset = source.SuspensionForceApplicationPointOffset;
			destination.TireForceApplicationPointOffset = source.TireForceApplicationPointOffset;

			destination.TireLatStiffX = tireSource.TireLatStiffX;
			destination.TireLatStiffY = tireSource.TireLatStiffY;
			destination.TireLongitudinalStiffnessPerUnitGravity = tireSource.TireLongitudinalStiffnessPerUnitGravity;
			destination.TireCamberStiffness = tireSource.TireCamberStiffness;
			destination.FrictionVsSlipGraphZeroLongitudinalSlip = tireSource.FrictionVsSlipGraphZeroLongitudinalSlip;
			destination.FrictionVsSlipGraphLongitudinalSlipWithMaximumFriction = tireSource.FrictionVsSlipGraphLongitudinalSlipWithMaximumFriction;
			destination.FrictionVsSlipGraphMaximumFriction = tireSource.FrictionVsSlipGraphMaximumFriction;
			destination.FrictionVsSlipGraphEndPointOfGraph = tireSource.FrictionVsSlipGraphEndPointOfGraph;
			destination.FrictionVsSlipGraphValueOfFrictionForSlipsGreaterThanEndPointOfGraph = tireSource.FrictionVsSlipGraphValueOfFrictionForSlipsGreaterThanEndPointOfGraph;
		}

		void CreatePhysicsVehicle()
		{
			if( !PhysicsWorld.Instance.IsVehicleSupported )
			{
				Log.Warning( "Car: CreatePhysicsVehicle: Vehicles are not supported by \"{0}\".", PhysicsWorld.Instance.DriverName );
				return;
			}

			//save and restore velocities
			Vec3 lastLinearVelocity = Vec3.Zero;
			Vec3 lastAngularVelocity = Vec3.Zero;
			if( physicsVehicle != null )
			{
				lastLinearVelocity = physicsVehicle.BaseBody.LinearVelocity;
				lastAngularVelocity = physicsVehicle.BaseBody.AngularVelocity;
			}

			DestroyPhysicsVehicle();

			CarType.TireTypeItem tireTypeItem = Type.FindTireTypeItem( TireType );
			if( tireTypeItem == null )
				Log.Fatal( "Car: CreatePhysicsVehicle: Tire type with name \"{0}\" is not defined.", TireType );

			if( PhysicsModel == null )
				Log.Fatal( "Car: CreatePhysicsVehicle: PhysicsModel == null." );
			if( PhysicsModel.Bodies.Length == 0 )
				Log.Fatal( "Car: CreatePhysicsVehicle: PhysicsModel.Bodies.Length == 0." );
			Body baseBody = PhysicsModel.Bodies[ 0 ];
			if( baseBody.Shapes.Length < 5 )
				Log.Fatal( "Car: CreatePhysicsVehicle: baseBody.Shapes.Length < 5." );

			float totalMass = Type.MassChassis + Type.WheelFrontLeft.Mass + Type.WheelFrontRight.Mass +
				Type.WheelRearLeft.Mass + Type.WheelRearRight.Mass;

			if( Math.Abs( baseBody.Mass - totalMass ) > .1f )
				Log.Fatal( "Car: CreatePhysicsVehicle: Specified mass of physics model and masses defined in the car type are not equal. Need set equal masses for physics model and car type settings (MassChassis, wheels Mass properties)." );

			//recreate physics model
			PhysicsModel.PopFromWorld();
			PhysicsModel.PushToWorld();

			//Create vehicle
			physicsVehicle = PhysicsWorld.Instance.MainScene.CreateVehicle( baseBody );

			//Chassis mass
			physicsVehicle.InitData.MassChassis = Type.MassChassis;

			//Differential
			physicsVehicle.InitData.DifferentialType = Type.DifferentialType;
			physicsVehicle.InitData.DifferentialFrontRearSplit = Type.DifferentialFrontRearSplit;
			physicsVehicle.InitData.DifferentialFrontLeftRightSplit = Type.DifferentialFrontLeftRightSplit;
			physicsVehicle.InitData.DifferentialRearLeftRightSplit = Type.DifferentialRearLeftRightSplit;
			physicsVehicle.InitData.DifferentialCenterBias = Type.DifferentialCenterBias;
			physicsVehicle.InitData.DifferentialFrontBias = Type.DifferentialFrontBias;
			physicsVehicle.InitData.DifferentialRearBias = Type.DifferentialRearBias;

			//Engine
			physicsVehicle.InitData.EnginePeakTorque = Type.EnginePeakTorque;
			physicsVehicle.InitData.EngineMaxRPM = Type.EngineMaxRPM;
			physicsVehicle.InitData.EngineDampingRateFullThrottle = Type.EngineDampingRateFullThrottle;
			physicsVehicle.InitData.EngineDampingRateZeroThrottleClutchEngaged = Type.EngineDampingRateZeroThrottleClutchEngaged;
			physicsVehicle.InitData.EngineDampingRateZeroThrottleClutchDisengaged = Type.EngineDampingRateZeroThrottleClutchDisengaged;
			foreach( CarType.EngineTorqueCurveItem item in Type.EngineTorqueCurve )
			{
				physicsVehicle.InitData.EngineTorqueCurve.Add( new PhysicsVehicle.InitDataClass.EngineTorqueCurveItem(
					item.NormalizedTorque, item.NormalizedRev ) );
			}

			//Gears
			foreach( CarType.GearItem gearItem in Type.Gears )
				physicsVehicle.InitData.Gears.Add( new PhysicsVehicle.InitDataClass.GearItem( gearItem.Number, gearItem.Ratio ) );
			physicsVehicle.InitData.GearsSwitchTime = Type.GearsSwitchTime;

			//Clutch
			physicsVehicle.InitData.ClutchStrength = Type.ClutchStrength;

			//Ackermann steer accuracy
			physicsVehicle.InitData.AckermannSteerAccuracy = Type.AckermannSteerAccuracy;

			//Wheels
			UpdateWheelSettings( physicsVehicle.InitData.WheelFrontLeft, Type.WheelFrontLeft, tireTypeItem.WheelFrontLeft );
			UpdateWheelSettings( physicsVehicle.InitData.WheelFrontRight, Type.WheelFrontRight, tireTypeItem.WheelFrontRight );
			UpdateWheelSettings( physicsVehicle.InitData.WheelRearLeft, Type.WheelRearLeft, tireTypeItem.WheelRearLeft );
			UpdateWheelSettings( physicsVehicle.InitData.WheelRearRight, Type.WheelRearRight, tireTypeItem.WheelRearRight );

			//Tire settings
			foreach( CarType.TireTypeItem.FrictionMultiplierItem frictionItem in tireTypeItem.FrictionMultipliers )
			{
				physicsVehicle.InitData.TireFrictionMultipliers.Add( new PhysicsVehicle.InitDataClass.TireFrictionMultiplierItem(
					frictionItem.SurfaceMaterialName, frictionItem.Value ) );
			}

			//create vehicle on physics engine
			physicsVehicle.PushedToWorld = true;

			//update dynamic settings
			physicsVehicle.AutoGear = autoGear;

			physicsVehicle.BaseBody.LinearVelocity = lastLinearVelocity;
			physicsVehicle.BaseBody.AngularVelocity = lastAngularVelocity;

			//suspend before first call OnTick()
			physicsVehicle.EnableUpdate = false;
		}

		void DestroyPhysicsVehicle()
		{
			if( physicsVehicle != null )
			{
				physicsVehicle.Dispose();
				physicsVehicle = null;
			}
		}

		public PhysicsVehicle GetPhysicsVehicle()
		{
			return physicsVehicle;
		}

		public float GetForwardSpeed()
		{
			if( physicsVehicle != null )
				return ( physicsVehicle.BaseBody.LinearVelocity * physicsVehicle.BaseBody.Rotation.GetInverse() ).X;
			return 0;
		}

		//Minefield specific
		void TickMinefields()
		{
			minefieldUpdateTimer -= TickDelta;
			if( minefieldUpdateTimer > 0 )
				return;
			minefieldUpdateTimer += 1;

			if( physicsVehicle != null && physicsVehicle.BaseBody.LinearVelocity != Vec3.Zero )
			{
				Minefield minefield = Minefield.GetMinefieldByPosition( Position );
				if( minefield != null )
					Die();
			}
		}

		public string GetInfoForProfilingTool()
		{
			PhysicsVehicle physicsVehicle = GetPhysicsVehicle();
			if( physicsVehicle != null )
			{
				List<string> lines = new List<string>();
				lines.Add( "Car name: " + Name );
				lines.Add( "Current gear: " + physicsVehicle.GetCurrentGear().ToString() );

				float forwardSpeed = GetForwardSpeed();
				lines.Add( "Forward speed (km/h): " + ( forwardSpeed * 3600.0f / 1000.0f ).ToString() );

				lines.Add( "Engine rotation speed (Degrees per second): " +
					( (int)physicsVehicle.GetEngineRotationSpeed().InDegrees() ).ToString() );
				lines.Add( "Engine rotation speed (RPM): " +
					( (int)( physicsVehicle.GetEngineRotationSpeed().InDegrees() * 60 / 360 ) ).ToString() );

				{
					string str = "";
					foreach( Radian v in physicsVehicle.GetWheelsRotationSpeed() )
						str += " " + ( (int)v.InDegrees() ).ToString();
					lines.Add( "Wheels rotation speed (Degrees per second): " + str );
				}

				{
					string str = "";
					foreach( Radian v in physicsVehicle.GetWheelsSteer() )
						str += " " + ( (int)v.InDegrees() ).ToString();
					lines.Add( "Wheels steer (Degrees): " + str );
				}

				{
					string str = "";
					foreach( Radian v in physicsVehicle.GetWheelsRotationAngle() )
						str += " " + ( (int)v.InDegrees() ).ToString();
					lines.Add( "Wheels rotation angle (Degrees): " + str );
				}

				{
					string str = "";
					foreach( float v in physicsVehicle.GetWheelsSuspensionJounce() )
						str += " " + v.ToString();
					lines.Add( "Wheels suspension jounce: " + str );
				}

				string inAir = "";
				foreach( bool isAir in physicsVehicle.AreWheelsInAir() )
					inAir += " " + isAir.ToString();
				lines.Add( "Wheels in the air: " + inAir );

				StringBuilder builder = new StringBuilder();
				foreach( string line in lines )
				{
					if( builder.Length != 0 )
						builder.Append( "\r\n" );
					builder.Append( line );
				}
				return builder.ToString();
			}
			return "";
		}

		void Server_SendWheelsPositionToAllClients()
		{
			SendDataWriter writer = BeginNetworkMessage( typeof( Car ), (ushort)NetworkMessages.WheelsPositionToClient );

			Radian[] wheelsRotationAngle = physicsVehicle.GetWheelsRotationAngle();
			Radian[] wheelsSteer = physicsVehicle.GetWheelsSteer();
			float[] wheelsSuspensionJounce = physicsVehicle.GetWheelsSuspensionJounce();
			for( int n = 0; n < 4; n++ )
			{
				writer.Write( wheelsRotationAngle[ n ] );
				writer.Write( wheelsSteer[ n ] );
				writer.Write( wheelsSuspensionJounce[ n ] );
			}

			EndNetworkMessage();
		}

		[NetworkReceive( NetworkDirections.ToClient, (ushort)NetworkMessages.WheelsPositionToClient )]
		void Client_ReceiveWheelsPositionToClient( RemoteEntityWorld sender, ReceiveDataReader reader )
		{
			networkingClient_wheelsRotationAngle = new Radian[ 4 ];
			networkingClient_wheelsSteer = new Radian[ 4 ];
			networkingClient_wheelsSuspensionJounce = new float[ 4 ];
			for( int n = 0; n < 4; n++ )
			{
				networkingClient_wheelsRotationAngle[ n ] = reader.ReadRadian();
				networkingClient_wheelsSteer[ n ] = reader.ReadRadian();
				networkingClient_wheelsSuspensionJounce[ n ] = reader.ReadSingle();
			}
			if( !reader.Complete() )
				return;
		}

		public override void GetFirstPersonCameraPosition( out Vec3 position, out Vec3 forward, out Vec3 up )
		{
			position = GetInterpolatedPosition() + Type.FPSCameraOffset * GetInterpolatedRotation();
			forward = GetInterpolatedRotation().GetForward();
			up = GetInterpolatedRotation().GetUp();
		}
	}
}
