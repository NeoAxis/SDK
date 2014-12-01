// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.PhysicsSystem;
using Engine.Utils;
using Engine.MathEx;
using System.Runtime.InteropServices;
using PhysXNativeWrapper;

namespace PhysXPhysicsSystem
{
	struct PhysXNativeVehicleInitData
	{
		[DllImport( Wrapper.library, EntryPoint = "PhysXNativeVehicleInitData_Create", CallingConvention = Wrapper.convention )]
		public unsafe static extern IntPtr/*PhysXNativeVehicleInitData*/ Create();

		[DllImport( Wrapper.library, EntryPoint = "PhysXNativeVehicleInitData_Destroy", CallingConvention = Wrapper.convention )]
		public unsafe static extern void Destroy( IntPtr/*PhysXNativeVehicleInitData*/ data );

		[DllImport( Wrapper.library, EntryPoint = "PhysXNativeVehicleInitData_SetFloatParameter", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetParameter( IntPtr/*PhysXNativeVehicleInitData*/ data,
			[MarshalAs( UnmanagedType.LPWStr )] string name, float value );

		[DllImport( Wrapper.library, EntryPoint = "PhysXNativeVehicleInitData_SetStringParameter", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetParameter( IntPtr/*PhysXNativeVehicleInitData*/ data,
			[MarshalAs( UnmanagedType.LPWStr )] string name, [MarshalAs( UnmanagedType.LPWStr )] string value );
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	struct PhysXNativeVehicle
	{
		[DllImport( Wrapper.library, EntryPoint = "PhysXVehicle_Create", CallingConvention = Wrapper.convention )]
		public unsafe static extern IntPtr/*PhysXVehicle*/ Create( IntPtr/*PhysXScene*/ scene, IntPtr/*PhysXBody*/ baseBody,
			IntPtr/*PhysXNativeVehicleInitData*/ generalData,
			IntPtr/*PhysXNativeVehicleInitData*/ wheelFrontLeftData, IntPtr/*PhysXNativeVehicleInitData*/ wheelFrontRightData,
			IntPtr/*PhysXNativeVehicleInitData*/ wheelRearLeftData, IntPtr/*PhysXNativeVehicleInitData*/ wheelRearRightData );

		[DllImport( Wrapper.library, EntryPoint = "PhysXVehicle_Destroy", CallingConvention = Wrapper.convention )]
		public unsafe static extern void Destroy( IntPtr/*PhysXVehicle*/ vehicle );

		[DllImport( Wrapper.library, EntryPoint = "PhysXVehicle_Update", CallingConvention = Wrapper.convention )]
		public unsafe static extern void Update( IntPtr/*PhysXVehicle*/ vehicle, float delta );

		[DllImport( Wrapper.library, EntryPoint = "PhysXVehicle_SetInputData", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetInputData( IntPtr/*PhysXVehicle*/ vehicle,
			[MarshalAs( UnmanagedType.U1 )] bool digitalInput, float* smoothingSettings, int steerVsForwardSpeedTablePairCount,
			float* steerVsForwardSpeedTable, float accel, float brake, float steer, float handbrake );

		[DllImport( Wrapper.library, EntryPoint = "PhysXVehicle_SetToRestState", CallingConvention = Wrapper.convention )]
		public unsafe static extern void SetToRestState( IntPtr/*PhysXVehicle*/ vehicle );

		[DllImport( Wrapper.library, EntryPoint = "PhysXVehicle_ForceGearChange", CallingConvention = Wrapper.convention )]
		public unsafe static extern void ForceGearChange( IntPtr/*PhysXVehicle*/ vehicle, int gear );

		[DllImport( Wrapper.library, EntryPoint = "PhysXVehicle_StartGearChange", CallingConvention = Wrapper.convention )]
		public unsafe static extern void StartGearChange( IntPtr/*PhysXVehicle*/ vehicle, int gear );

		[DllImport( Wrapper.library, EntryPoint = "PhysXVehicle_CallMethod", CallingConvention = Wrapper.convention )]
		public unsafe static extern void CallMethod( IntPtr/*PhysXVehicle*/ vehicle,
			[MarshalAs( UnmanagedType.LPWStr )] string message, int parameter1, double parameter2, double parameter3,
			out int result1, out double result2, out double result3 );

		[DllImport( Wrapper.library, EntryPoint = "PhysXVehicle_GetCurrentGear", CallingConvention = Wrapper.convention )]
		public unsafe static extern int GetCurrentGear( IntPtr/*PhysXVehicle*/ vehicle );

		[DllImport( Wrapper.library, EntryPoint = "PhysXVehicle_GetTargetGear", CallingConvention = Wrapper.convention )]
		public unsafe static extern int GetTargetGear( IntPtr/*PhysXVehicle*/ vehicle );

		[DllImport( Wrapper.library, EntryPoint = "PhysXVehicle_GetEngineRotationSpeed", CallingConvention = Wrapper.convention )]
		public unsafe static extern float GetEngineRotationSpeed( IntPtr/*PhysXVehicle*/ vehicle );

		[DllImport( Wrapper.library, EntryPoint = "PhysXVehicle_GetWheelRotationSpeed", CallingConvention = Wrapper.convention )]
		public unsafe static extern float GetWheelRotationSpeed( IntPtr/*PhysXVehicle*/ vehicle, int wheelIndex );

		[DllImport( Wrapper.library, EntryPoint = "PhysXVehicle_GetWheelSteer", CallingConvention = Wrapper.convention )]
		public unsafe static extern float GetWheelSteer( IntPtr/*PhysXVehicle*/ vehicle, int wheelIndex );

		[DllImport( Wrapper.library, EntryPoint = "PhysXVehicle_GetWheelRotationAngle", CallingConvention = Wrapper.convention )]
		public unsafe static extern float GetWheelRotationAngle( IntPtr/*PhysXVehicle*/ vehicle, int wheelIndex );

		[DllImport( Wrapper.library, EntryPoint = "PhysXVehicle_GetWheelSuspensionJounce", CallingConvention = Wrapper.convention )]
		public unsafe static extern float GetWheelSuspensionJounce( IntPtr/*PhysXVehicle*/ vehicle, int wheelIndex );

		[DllImport( Wrapper.library, EntryPoint = "PhysXVehicle_GetWheelTireLongitudinalSlip", CallingConvention = Wrapper.convention )]
		public unsafe static extern float GetWheelTireLongitudinalSlip( IntPtr/*PhysXVehicle*/ vehicle, int wheelIndex );

		[DllImport( Wrapper.library, EntryPoint = "PhysXVehicle_GetWheelTireLateralSlip", CallingConvention = Wrapper.convention )]
		public unsafe static extern float GetWheelTireLateralSlip( IntPtr/*PhysXVehicle*/ vehicle, int wheelIndex );

		[DllImport( Wrapper.library, EntryPoint = "PhysXVehicle_GetWheelTireFriction", CallingConvention = Wrapper.convention )]
		public unsafe static extern float GetWheelTireFriction( IntPtr/*PhysXVehicle*/ vehicle, int wheelIndex );

		[return: MarshalAs( UnmanagedType.U1 )]
		[DllImport( Wrapper.library, EntryPoint = "PhysXVehicle_IsWheelInAir", CallingConvention = Wrapper.convention )]
		public unsafe static extern bool IsWheelInAir( IntPtr/*PhysXVehicle*/ vehicle, int wheelIndex );
	}

	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	class PhysXPhysicsVehicle : PhysicsVehicle
	{
		IntPtr nativeVehicle;

		//

		public PhysXPhysicsVehicle( PhysXPhysicsScene scene, PhysXBody baseBody )
			: base( scene, baseBody )
		{
		}

		protected override void OnUpdatePushedToWorld()
		{
			if( PushedToWorld )
				PushToWorld();
			else
				PopFromWorld();
		}

		void InitWheelData( IntPtr data, InitDataClass.WheelItem source )
		{
			PhysXNativeVehicleInitData.SetParameter( data, "mass", source.Mass );

			PhysXNativeVehicleInitData.SetParameter( data, "wheelDampingRate", source.WheelDampingRate );
			PhysXNativeVehicleInitData.SetParameter( data, "wheelMaxBrakeTorque", source.WheelMaxBrakeTorque );
			PhysXNativeVehicleInitData.SetParameter( data, "wheelMaxHandBrakeTorque", source.WheelMaxHandBrakeTorque );
			PhysXNativeVehicleInitData.SetParameter( data, "wheelMaxSteer", source.WheelMaxSteer );
			PhysXNativeVehicleInitData.SetParameter( data, "wheelToeAngle", source.WheelToeAngle );
			PhysXNativeVehicleInitData.SetParameter( data, "suspensionSpringStrength", source.SuspensionSpringStrength );
			PhysXNativeVehicleInitData.SetParameter( data, "suspensionSpringDamperRate", source.SuspensionSpringDamperRate );
			PhysXNativeVehicleInitData.SetParameter( data, "suspensionMaxCompression", source.SuspensionMaxCompression );
			PhysXNativeVehicleInitData.SetParameter( data, "suspensionMaxDroop", source.SuspensionMaxDroop );
			PhysXNativeVehicleInitData.SetParameter( data, "suspensionSprungMassCoefficient", source.SuspensionSprungMassCoefficient );

			PhysXNativeVehicleInitData.SetParameter( data, "suspensionForceApplicationPointOffset.X", source.SuspensionForceApplicationPointOffset.X );
			PhysXNativeVehicleInitData.SetParameter( data, "suspensionForceApplicationPointOffset.Y", source.SuspensionForceApplicationPointOffset.Y );
			PhysXNativeVehicleInitData.SetParameter( data, "suspensionForceApplicationPointOffset.Z", source.SuspensionForceApplicationPointOffset.Z );

			PhysXNativeVehicleInitData.SetParameter( data, "tireForceApplicationPointOffset.X", source.TireForceApplicationPointOffset.X );
			PhysXNativeVehicleInitData.SetParameter( data, "tireForceApplicationPointOffset.Y", source.TireForceApplicationPointOffset.Y );
			PhysXNativeVehicleInitData.SetParameter( data, "tireForceApplicationPointOffset.Z", source.TireForceApplicationPointOffset.Z );

			PhysXNativeVehicleInitData.SetParameter( data, "tireLatStiffX", source.TireLatStiffX );
			PhysXNativeVehicleInitData.SetParameter( data, "tireLatStiffY", source.TireLatStiffY );
			PhysXNativeVehicleInitData.SetParameter( data, "tireLongitudinalStiffnessPerUnitGravity", source.TireLongitudinalStiffnessPerUnitGravity );
			PhysXNativeVehicleInitData.SetParameter( data, "tireCamberStiffness", source.TireCamberStiffness );
			PhysXNativeVehicleInitData.SetParameter( data, "frictionVsSlipGraphZeroLongitudinalSlip", source.FrictionVsSlipGraphZeroLongitudinalSlip );
			PhysXNativeVehicleInitData.SetParameter( data, "frictionVsSlipGraphLongitudinalSlipWithMaximumFriction", source.FrictionVsSlipGraphLongitudinalSlipWithMaximumFriction );
			PhysXNativeVehicleInitData.SetParameter( data, "frictionVsSlipGraphMaximumFriction", source.FrictionVsSlipGraphMaximumFriction );
			PhysXNativeVehicleInitData.SetParameter( data, "frictionVsSlipGraphEndPointOfGraph", source.FrictionVsSlipGraphEndPointOfGraph );
			PhysXNativeVehicleInitData.SetParameter( data, "frictionVsSlipGraphValueOfFrictionForSlipsGreaterThanEndPointOfGraph", source.FrictionVsSlipGraphValueOfFrictionForSlipsGreaterThanEndPointOfGraph );
		}

		void PushToWorld()
		{
			PhysXPhysicsScene scene = (PhysXPhysicsScene)Scene;
			PhysXBody baseBody = (PhysXBody)BaseBody;

			IntPtr generalData = PhysXNativeVehicleInitData.Create();
			IntPtr wheelFrontLeftData = PhysXNativeVehicleInitData.Create();
			IntPtr wheelFrontRightData = PhysXNativeVehicleInitData.Create();
			IntPtr wheelRearLeftData = PhysXNativeVehicleInitData.Create();
			IntPtr wheelRearRightData = PhysXNativeVehicleInitData.Create();

			//Chassis mass
			PhysXNativeVehicleInitData.SetParameter( generalData, "massChassis", InitData.MassChassis );

			//Differential
			PhysXNativeVehicleInitData.SetParameter( generalData, "differentialType", (float)(int)InitData.DifferentialType );
			PhysXNativeVehicleInitData.SetParameter( generalData, "differentialFrontRearSplit", InitData.DifferentialFrontRearSplit );
			PhysXNativeVehicleInitData.SetParameter( generalData, "differentialFrontLeftRightSplit", InitData.DifferentialFrontLeftRightSplit );
			PhysXNativeVehicleInitData.SetParameter( generalData, "differentialRearLeftRightSplit", InitData.DifferentialRearLeftRightSplit );
			PhysXNativeVehicleInitData.SetParameter( generalData, "differentialCenterBias", InitData.DifferentialCenterBias );
			PhysXNativeVehicleInitData.SetParameter( generalData, "differentialFrontBias", InitData.DifferentialFrontBias );
			PhysXNativeVehicleInitData.SetParameter( generalData, "differentialRearBias", InitData.DifferentialRearBias );

			//Engine
			{
				PhysXNativeVehicleInitData.SetParameter( generalData, "enginePeakTorque", InitData.EnginePeakTorque );
				PhysXNativeVehicleInitData.SetParameter( generalData, "engineMaxRPM", InitData.EngineMaxRPM );
				PhysXNativeVehicleInitData.SetParameter( generalData, "engineDampingRateFullThrottle",
					InitData.EngineDampingRateFullThrottle );
				PhysXNativeVehicleInitData.SetParameter( generalData, "engineDampingRateZeroThrottleClutchEngaged",
					InitData.EngineDampingRateZeroThrottleClutchEngaged );
				PhysXNativeVehicleInitData.SetParameter( generalData, "engineDampingRateZeroThrottleClutchDisengaged",
					InitData.EngineDampingRateZeroThrottleClutchDisengaged );

				if( InitData.EngineTorqueCurve.Count == 0 )
					Log.Fatal( "PhysXPhysicsVehicle: PushToWorld: EngineTorqueCurve is not configured." );
				if( InitData.EngineTorqueCurve.Count > 8 )
					Log.Fatal( "PhysXPhysicsVehicle: PushToWorld: EngineTorqueCurve can't have more than 8 values." );
				for( int n = 0; n < InitData.EngineTorqueCurve.Count; n++ )
				{
					InitDataClass.EngineTorqueCurveItem item = InitData.EngineTorqueCurve[ n ];
					PhysXNativeVehicleInitData.SetParameter( generalData, string.Format( "engineTorqueCurveTorque{0}", n ),
						item.NormalizedTorque );
					PhysXNativeVehicleInitData.SetParameter( generalData, string.Format( "engineTorqueCurveRev{0}", n ),
						item.NormalizedRev );
				}
			}

			//Gears
			{
				if( InitData.Gears.Count == 0 )
					Log.Fatal( "PhysXPhysicsVehicle: PushToWorld: The gears are not defined." );
				if( InitData.FindGearByNumber( 0 ) == null )
					Log.Fatal( "PhysXPhysicsVehicle: PushToWorld: Neutral gear is not defined." );
				if( InitData.FindGearByNumber( -1 ) == null )
					Log.Fatal( "PhysXPhysicsVehicle: PushToWorld: Reverse gear is not defined." );

				foreach( PhysicsVehicle.InitDataClass.GearItem gearItem in InitData.Gears )
				{
					if( gearItem.Number < -1 )
						Log.Fatal( "PhysXPhysicsVehicle: PushToWorld: Gear number can't be less than -1." );
					if( gearItem.Number > 30 )
						Log.Fatal( "PhysXPhysicsVehicle: PushToWorld: Gear number can't be more than 30." );

					if( gearItem.Number < 0 && gearItem.Ratio >= 0 )
						Log.Fatal( "PhysXPhysicsVehicle: PushToWorld: Reverse gear ratio must be less than zero." );
					if( gearItem.Number == 0 && gearItem.Ratio != 0 )
						Log.Fatal( "PhysXPhysicsVehicle: PushToWorld: Neutral gear ratio must be zero." );
					if( gearItem.Number > 0 && gearItem.Ratio < 0 )
						Log.Fatal( "PhysXPhysicsVehicle: PushToWorld: Forward gear ratios must be greater than zero." );

					if( gearItem.Number >= 2 )
					{
						PhysicsVehicle.InitDataClass.GearItem nextGearItem = InitData.FindGearByNumber( gearItem.Number - 1 );
						if( nextGearItem == null || gearItem.Ratio > nextGearItem.Ratio )
							Log.Fatal( "PhysXPhysicsVehicle: PushToWorld: Forward gear ratios must be a descending sequence of gear ratios." );
					}

					PhysXNativeVehicleInitData.SetParameter( generalData, string.Format( "gear{0}", gearItem.Number ), gearItem.Ratio );
				}
				PhysXNativeVehicleInitData.SetParameter( generalData, "gearsSwitchTime", InitData.GearsSwitchTime );
			}

			//Clutch
			PhysXNativeVehicleInitData.SetParameter( generalData, "clutchStrength", InitData.ClutchStrength );

			//Ackermann steer accuracy
			PhysXNativeVehicleInitData.SetParameter( generalData, "ackermannSteerAccuracy", InitData.AckermannSteerAccuracy );

			//Wheels
			InitWheelData( wheelFrontLeftData, InitData.WheelFrontLeft );
			InitWheelData( wheelFrontRightData, InitData.WheelFrontRight );
			InitWheelData( wheelRearLeftData, InitData.WheelRearLeft );
			InitWheelData( wheelRearRightData, InitData.WheelRearRight );

			//Tire settings
			{
				int n = 0;
				foreach( InitDataClass.TireFrictionMultiplierItem frictionItem in InitData.TireFrictionMultipliers )
				{
					PhysXNativeVehicleInitData.SetParameter( generalData,
						string.Format( "tireFrictionMaterial{0}", n ), frictionItem.SurfaceMaterialName );
					PhysXNativeVehicleInitData.SetParameter( generalData,
						string.Format( "tireFrictionValue{0}", n ), frictionItem.Value );
					n++;
				}
			}

			nativeVehicle = PhysXNativeVehicle.Create( scene.nativeScene, baseBody.nativeBody, generalData,
				wheelFrontLeftData, wheelFrontRightData, wheelRearLeftData, wheelRearRightData );

			PhysXNativeVehicleInitData.Destroy( generalData );
			PhysXNativeVehicleInitData.Destroy( wheelFrontLeftData );
			PhysXNativeVehicleInitData.Destroy( wheelFrontRightData );
			PhysXNativeVehicleInitData.Destroy( wheelRearLeftData );
			PhysXNativeVehicleInitData.Destroy( wheelRearRightData );

			OnUpdateAutoGearSettings();
		}

		void PopFromWorld()
		{
			if( nativeVehicle != IntPtr.Zero )
			{
				PhysXNativeVehicle.Destroy( nativeVehicle );
				nativeVehicle = IntPtr.Zero;
			}
		}

		public unsafe override void SetInputData( InputDataTypes inputType, InputSmoothingSettings smoothingSettings,
			Pair<float, float>[] steerVsForwardSpeedTable, float accel, float brake, float steer, float handbrake )
		{
			if( steerVsForwardSpeedTable.Length > 8 )
				Log.Fatal( "PhysXPhysicsVehicle: SetInputData: The amount of steer vs forward speed table item can't be more than 8." );

			if( nativeVehicle != IntPtr.Zero )
			{
				float[] smoothingSettings2 = new float[]
				{
					smoothingSettings.RiseRateAccel,
					smoothingSettings.RiseRateBrake,
					smoothingSettings.RiseRateSteer,
					smoothingSettings.RiseRateHandbrake,
					smoothingSettings.FallRateAccel,
					smoothingSettings.FallRateBrake,
					smoothingSettings.FallRateSteer,
					smoothingSettings.FallRateHandbrake
				};

				float[] steerVsForwardSpeedTable2 = new float[ steerVsForwardSpeedTable.Length * 2 ];
				for( int n = 0; n < steerVsForwardSpeedTable.Length; n++ )
				{
					steerVsForwardSpeedTable2[ n * 2 + 0 ] = steerVsForwardSpeedTable[ n ].First;
					steerVsForwardSpeedTable2[ n * 2 + 1 ] = steerVsForwardSpeedTable[ n ].Second;
				}

				fixed( float* pSmoothingSettings = smoothingSettings2, pSteerVsForwardSpeedTable = steerVsForwardSpeedTable2 )
				{
					PhysXNativeVehicle.SetInputData( nativeVehicle, inputType == InputDataTypes.Digital, pSmoothingSettings,
						steerVsForwardSpeedTable.Length, pSteerVsForwardSpeedTable, accel, brake, steer, handbrake );
				}
			}
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			if( nativeVehicle != IntPtr.Zero )
				PhysXNativeVehicle.Update( nativeVehicle, Scene.StepSize );
		}

		int GetMaxGearNumber()
		{
			int max = 0;
			for( int n = 0; n < InitData.Gears.Count; n++ )
			{
				if( InitData.Gears[ n ].Number > max )
					max = InitData.Gears[ n ].Number;
			}
			return max;
		}

		public override void SetToRestState()
		{
			if( nativeVehicle != IntPtr.Zero )
				PhysXNativeVehicle.SetToRestState( nativeVehicle );
		}

		public override void ForceGearChange( int gear )
		{
			if( gear < -1 )
				gear = -1;
			int maxNumber = GetMaxGearNumber();
			if( gear > maxNumber )
				gear = maxNumber;

			if( nativeVehicle != IntPtr.Zero )
				PhysXNativeVehicle.ForceGearChange( nativeVehicle, gear );
		}

		public override void StartGearChange( int gear )
		{
			if( gear < -1 )
				gear = -1;
			int maxNumber = GetMaxGearNumber();
			if( gear > maxNumber )
				gear = maxNumber;

			if( nativeVehicle != IntPtr.Zero )
				PhysXNativeVehicle.StartGearChange( nativeVehicle, gear );
		}

		public override object CallCustomMethod( string message, object param )
		{
			return null;
		}

		public override int GetCurrentGear()
		{
			if( nativeVehicle != IntPtr.Zero )
				return PhysXNativeVehicle.GetCurrentGear( nativeVehicle );
			return 0;
		}

		public override int GetTargetGear()
		{
			if( nativeVehicle != IntPtr.Zero )
				return PhysXNativeVehicle.GetTargetGear( nativeVehicle );
			return 0;
		}

		public override Radian GetEngineRotationSpeed()
		{
			if( nativeVehicle != IntPtr.Zero )
				return PhysXNativeVehicle.GetEngineRotationSpeed( nativeVehicle );
			return 0;
		}

		void CallMethod( string message, int parameter1, double parameter2, double parameter3, out int result1,
			out double result2, out double result3 )
		{
			PhysXNativeVehicle.CallMethod( nativeVehicle, message, parameter1, parameter2, parameter3, out result1, out result2,
				out result3 );
		}

		void CallMethod( string message, int parameter1 )
		{
			int result1;
			double result2;
			double result3;
			CallMethod( message, parameter1, 0, 0, out result1, out result2, out result3 );
		}

		protected override void OnUpdateAutoGearSettings()
		{
			if( nativeVehicle != IntPtr.Zero )
			{
				CallMethod( "SetAutoGear", AutoGear ? 1 : 0 );
			}
		}

		public override Radian[] GetWheelsRotationSpeed()
		{
			Radian[] result = new Radian[ WheelCount ];
			if( nativeVehicle != IntPtr.Zero )
			{
				for( int n = 0; n < WheelCount; n++ )
					result[ n ] = PhysXNativeVehicle.GetWheelRotationSpeed( nativeVehicle, n );
			}
			return result;
		}

		public override Radian[] GetWheelsSteer()
		{
			Radian[] result = new Radian[ WheelCount ];
			if( nativeVehicle != IntPtr.Zero )
			{
				for( int n = 0; n < WheelCount; n++ )
					result[ n ] = PhysXNativeVehicle.GetWheelSteer( nativeVehicle, n );
			}
			return result;
		}

		public override Radian[] GetWheelsRotationAngle()
		{
			Radian[] result = new Radian[ WheelCount ];
			if( nativeVehicle != IntPtr.Zero )
			{
				for( int n = 0; n < WheelCount; n++ )
					result[ n ] = PhysXNativeVehicle.GetWheelRotationAngle( nativeVehicle, n );
			}
			return result;
		}

		public override float[] GetWheelsSuspensionJounce()
		{
			float[] result = new float[ WheelCount ];
			if( nativeVehicle != IntPtr.Zero )
			{
				for( int n = 0; n < WheelCount; n++ )
					result[ n ] = PhysXNativeVehicle.GetWheelSuspensionJounce( nativeVehicle, n );
			}
			return result;
		}

		public override float[] GetWheelsTireLongitudinalSlip()
		{
			float[] result = new float[ WheelCount ];
			if( nativeVehicle != IntPtr.Zero )
			{
				for( int n = 0; n < WheelCount; n++ )
					result[ n ] = PhysXNativeVehicle.GetWheelTireLongitudinalSlip( nativeVehicle, n );
			}
			return result;
		}

		public override float[] GetWheelsTireLateralSlip()
		{
			float[] result = new float[ WheelCount ];
			if( nativeVehicle != IntPtr.Zero )
			{
				for( int n = 0; n < WheelCount; n++ )
					result[ n ] = PhysXNativeVehicle.GetWheelTireLateralSlip( nativeVehicle, n );
			}
			return result;
		}

		public override float[] GetWheelsTireFriction()
		{
			float[] result = new float[ WheelCount ];
			if( nativeVehicle != IntPtr.Zero )
			{
				for( int n = 0; n < WheelCount; n++ )
					result[ n ] = PhysXNativeVehicle.GetWheelTireFriction( nativeVehicle, n );
			}
			return result;
		}

		public override bool[] AreWheelsInAir()
		{
			bool[] result = new bool[ WheelCount ];
			if( nativeVehicle != IntPtr.Zero )
			{
				for( int n = 0; n < WheelCount; n++ )
					result[ n ] = PhysXNativeVehicle.IsWheelInAir( nativeVehicle, n );
			}
			return result;
		}
	}
}
