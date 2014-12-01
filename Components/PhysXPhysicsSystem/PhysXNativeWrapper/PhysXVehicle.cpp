// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#include "precompiled.h"
#include "PhysXNativeWrapper.h"
#include "PhysXVehicle.h"
#include "PhysXBody.h"
#include "PhysXWorld.h"
#include "StringUtils.h"

float DegToRad( float a )
{
	return (float)0.01745329251994329547 * a;
}

//PxVehicleKeySmoothingData gKeySmoothingData =
//{
//	{
//		3.0f,	//rise rate eANALOG_INPUT_ACCEL		
//		3.0f,	//rise rate eANALOG_INPUT_BRAKE		
//		10.0f,	//rise rate eANALOG_INPUT_HANDBRAKE	
//		2.5f,	//rise rate eANALOG_INPUT_STEER_LEFT	
//		2.5f,	//rise rate eANALOG_INPUT_STEER_RIGHT	
//	},
//	{
//		5.0f,	//fall rate eANALOG_INPUT__ACCEL		
//		5.0f,	//fall rate eANALOG_INPUT__BRAKE		
//		10.0f,	//fall rate eANALOG_INPUT__HANDBRAKE	
//		5.0f,	//fall rate eANALOG_INPUT_STEER_LEFT	
//		5.0f	//fall rate eANALOG_INPUT_STEER_RIGHT	
//	}
//};
//

//PxVehiclePadSmoothingData gPadSmoothingData =
//{
//	{
//		3.0f,	//rise rate eANALOG_INPUT_ACCEL		
//		3.0f,	//rise rate eANALOG_INPUT_BRAKE		
//		10.0f,	//rise rate eANALOG_INPUT_HANDBRAKE	
//		2.5f,	//rise rate eANALOG_INPUT_STEER_LEFT	
//		2.5f,	//rise rate eANALOG_INPUT_STEER_RIGHT	
//	},
//	{
//		5.0f,	//fall rate eANALOG_INPUT__ACCEL		
//		5.0f,	//fall rate eANALOG_INPUT__BRAKE		
//		10.0f,	//fall rate eANALOG_INPUT__HANDBRAKE	
//		5.0f,	//fall rate eANALOG_INPUT_STEER_LEFT	
//		5.0f	//fall rate eANALOG_INPUT_STEER_RIGHT	
//	}
//};

//PxF32 gSteerVsForwardSpeedData[ 2 * 8 ] =
//{
//	0.0f,		0.75f,
//	5.0f,		0.75f,
//	30.0f,		0.125f,
//	120.0f,		0.1f,
//	PX_MAX_F32, PX_MAX_F32,
//	PX_MAX_F32, PX_MAX_F32,
//	PX_MAX_F32, PX_MAX_F32,
//	PX_MAX_F32, PX_MAX_F32
//};
//
//PxFixedSizeLookupTable<8> gSteerVsForwardSpeedTable(gSteerVsForwardSpeedData,4);

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void ComputeWheelWidthsAndRadii(PxConvexMesh** wheelConvexMeshes, PxF32* wheelWidths, PxF32* wheelRadii)
{
	for(PxU32 i = 0; i < 4; i++)
	{
		const PxU32 numWheelVerts = wheelConvexMeshes[i]->getNbVertices();
		const PxVec3* wheelVerts = wheelConvexMeshes[i]->getVertices();
		PxVec3 wheelMin(PX_MAX_F32, PX_MAX_F32, PX_MAX_F32);
		PxVec3 wheelMax(-PX_MAX_F32, -PX_MAX_F32, -PX_MAX_F32);
		for(PxU32 j = 0; j < numWheelVerts; j++)
		{
			wheelMin.x = PxMin(wheelMin.x, wheelVerts[j].x);
			wheelMin.y = PxMin(wheelMin.y, wheelVerts[j].y);
			wheelMin.z = PxMin(wheelMin.z, wheelVerts[j].z);
			wheelMax.x = PxMax(wheelMax.x, wheelVerts[j].x);
			wheelMax.y = PxMax(wheelMax.y, wheelVerts[j].y);
			wheelMax.z = PxMax(wheelMax.z, wheelVerts[j].z);
		}
		wheelWidths[i] = wheelMax.y - wheelMin.y;
		wheelRadii[i] = PxMax(wheelMax.x, wheelMax.z) * 0.975f;
	}
}

void CreateVehicle4WSimulationData( PxConvexMesh* chassisConvexMesh, PxConvexMesh** wheelConvexMeshes, 
	const PxVec3* wheelCenterOffsets, PxVehicleWheelsSimData& wheelsData, PxVehicleDriveSimData4W& driveData, 
	PhysXVehicleInitData* generalData, PhysXVehicleInitData** wheelDatas )
{
	float chassisMass = generalData->GetFloatParameter( "massChassis" );

	PxF32 wheelWidths[ 4 ];
	PxF32 wheelRadii[ 4 ];
	ComputeWheelWidthsAndRadii( wheelConvexMeshes, wheelWidths, wheelRadii );

	//Wheels
	for( int n = 0; n < 4; n++ )
	{
		PhysXVehicleInitData* wheelInitData = wheelDatas[ n ];

		PxVehicleWheelData wheelData;
		wheelData.mRadius = wheelRadii[ n ];
		wheelData.mMass = wheelInitData->GetFloatParameter( "mass" );
		wheelData.mMOI = 0.5f * wheelData.mMass * wheelRadii[ n ] * wheelRadii[ n ];
		wheelData.mWidth = wheelWidths[ n ];
		wheelData.mDampingRate = wheelInitData->GetFloatParameter( "wheelDampingRate" );
		wheelData.mMaxBrakeTorque = wheelInitData->GetFloatParameter( "wheelMaxBrakeTorque" );
		wheelData.mMaxHandBrakeTorque = wheelInitData->GetFloatParameter( "wheelMaxHandBrakeTorque" );
		wheelData.mMaxSteer = DegToRad( wheelInitData->GetFloatParameter("wheelMaxSteer") );
		wheelData.mToeAngle = DegToRad( wheelInitData->GetFloatParameter("wheelToeAngle") );
		wheelsData.setWheelData( n, wheelData );

		PxVehicleTireData tireData;
		tireData.mLatStiffX = wheelInitData->GetFloatParameter( "tireLatStiffX" );
		tireData.mLatStiffY = wheelInitData->GetFloatParameter( "tireLatStiffY" );
		tireData.mLongitudinalStiffnessPerUnitGravity = wheelInitData->GetFloatParameter( "tireLongitudinalStiffnessPerUnitGravity" );
		tireData.mCamberStiffness = wheelInitData->GetFloatParameter( "tireCamberStiffness" );
		tireData.mFrictionVsSlipGraph[ 0 ][ 1 ] = wheelInitData->GetFloatParameter( "frictionVsSlipGraphZeroLongitudinalSlip" );
		tireData.mFrictionVsSlipGraph[ 1 ][ 0 ] = wheelInitData->GetFloatParameter( "frictionVsSlipGraphLongitudinalSlipWithMaximumFriction" );
		tireData.mFrictionVsSlipGraph[ 1 ][ 1 ] = wheelInitData->GetFloatParameter( "frictionVsSlipGraphMaximumFriction" );
		tireData.mFrictionVsSlipGraph[ 2 ][ 0 ] = wheelInitData->GetFloatParameter( "frictionVsSlipGraphEndPointOfGraph" );
		tireData.mFrictionVsSlipGraph[ 2 ][ 1 ] = wheelInitData->GetFloatParameter( "frictionVsSlipGraphValueOfFrictionForSlipsGreaterThanEndPointOfGraph" );
		wheelsData.setTireData( n, tireData );

		PxVehicleSuspensionData suspensionData;
		suspensionData.mMaxCompression = wheelInitData->GetFloatParameter( "suspensionMaxCompression" );
		suspensionData.mMaxDroop = wheelInitData->GetFloatParameter( "suspensionMaxDroop" );
		suspensionData.mSpringStrength = wheelInitData->GetFloatParameter( "suspensionSpringStrength" );
		suspensionData.mSpringDamperRate = wheelInitData->GetFloatParameter( "suspensionSpringDamperRate" );
		suspensionData.mSprungMass = chassisMass * wheelInitData->GetFloatParameter( "suspensionSprungMassCoefficient" );
		wheelsData.setSuspensionData( n, suspensionData );

		PxVec3 suspensionForceApplicationPointOffset = PxVec3(
			wheelInitData->GetFloatParameter( "suspensionForceApplicationPointOffset.X" ),
			wheelInitData->GetFloatParameter( "suspensionForceApplicationPointOffset.Y" ),
			wheelInitData->GetFloatParameter( "suspensionForceApplicationPointOffset.Z" ) );
		PxVec3 tireForceApplicationPointOffset = PxVec3(
			wheelInitData->GetFloatParameter( "tireForceApplicationPointOffset.X" ),
			wheelInitData->GetFloatParameter( "tireForceApplicationPointOffset.Y" ),
			wheelInitData->GetFloatParameter( "tireForceApplicationPointOffset.Z" ) );
		wheelsData.setSuspTravelDirection( n, PxVec3( 0, 0, -1 ) );
		wheelsData.setWheelCentreOffset( n, wheelCenterOffsets[ n ] );
		wheelsData.setSuspForceAppPointOffset( n, wheelCenterOffsets[ n ] + suspensionForceApplicationPointOffset );
		wheelsData.setTireForceAppPointOffset( n, wheelCenterOffsets[ n ] + tireForceApplicationPointOffset );
	}

	//Differential
	{
		PxVehicleDifferential4WData differential;
		differential.mType = (int)( generalData->GetFloatParameter( "differentialType" ) + .0001f );
		differential.mFrontRearSplit = generalData->GetFloatParameter( "differentialFrontRearSplit" );
		differential.mFrontLeftRightSplit = generalData->GetFloatParameter( "differentialFrontLeftRightSplit" );
		differential.mRearLeftRightSplit = generalData->GetFloatParameter( "differentialRearLeftRightSplit" );
		differential.mCentreBias = generalData->GetFloatParameter( "differentialCenterBias" );
		differential.mFrontBias = generalData->GetFloatParameter( "differentialFrontBias" );
		differential.mRearBias = generalData->GetFloatParameter( "differentialRearBias" );
		driveData.setDiffData( differential );
	}
	
	//Engine
	{
		PxVehicleEngineData engine;
		engine.mPeakTorque = generalData->GetFloatParameter("enginePeakTorque");// 500.0f;
		engine.mMaxOmega = generalData->GetFloatParameter("engineMaxRPM") * PI * 2.0f / 60.0f;//600.0f;//approx 6000 rpm
		engine.mDampingRateFullThrottle = generalData->GetFloatParameter("engineDampingRateFullThrottle");
		engine.mDampingRateZeroThrottleClutchEngaged = generalData->GetFloatParameter("engineDampingRateZeroThrottleClutchEngaged");
		engine.mDampingRateZeroThrottleClutchDisengaged = generalData->GetFloatParameter("engineDampingRateZeroThrottleClutchDisengaged");

		PxFixedSizeLookupTable<PxVehicleEngineData::eMAX_NUM_ENGINE_TORQUE_CURVE_ENTRIES> torqueCurve;
		for( int n = 0; n < PxVehicleEngineData::eMAX_NUM_ENGINE_TORQUE_CURVE_ENTRIES; n++ )
		{
			char s[50];
			sprintf(s, "engineTorqueCurveTorque%d", n);
			if(!generalData->IsFloatParameterExist(s))
				break;
			float torque = generalData->GetFloatParameter(s);
			sprintf(s, "engineTorqueCurveRev%d", n);
			float rev = generalData->GetFloatParameter(s);
			torqueCurve.addPair(torque, rev);
		}
		engine.mTorqueCurve = torqueCurve;

		driveData.setEngineData( engine );
	}

	//Gears
	{
		PxVehicleGearsData gears;
		for( int n = 0; n < PxVehicleGearsData::eMAX_NUM_GEAR_RATIOS; n++)
			gears.mRatios[ n ] = 0;

		int minNumber = 100000;
		int maxNumber = -100000;
		for( int number = -1; number <= 30; number++ )
		{
			char s[25];
			sprintf(s, "gear%d", number);
			if(generalData->IsFloatParameterExist(s))
			{
				float ratio = generalData->GetFloatParameter(s);
				gears.mRatios[ number + 1 ] = ratio;

				if(number < minNumber)
					minNumber = number;
				if(number > maxNumber)
					maxNumber = number;
			}
		}
		gears.mFinalRatio = 1;
		gears.mNumRatios = maxNumber - minNumber + 1;
		gears.mSwitchTime = generalData->GetFloatParameter(L"gearsSwitchTime");//0.5f;

		driveData.setGearsData(gears);
	}

	//Clutch
	PxVehicleClutchData clutch;
	clutch.mStrength = generalData->GetFloatParameter(L"clutchStrength");
	driveData.setClutchData(clutch);

	//Ackermann steer accuracy
	PxVehicleAckermannGeometryData ackermann;
	ackermann.mAccuracy = generalData->GetFloatParameter(L"ackermannSteerAccuracy");
	ackermann.mAxleSeparation = fabs(
		wheelCenterOffsets[PxVehicleDrive4W::eFRONT_LEFT_WHEEL].x -
		wheelCenterOffsets[PxVehicleDrive4W::eREAR_LEFT_WHEEL].x);
	ackermann.mFrontWidth = fabs(
		wheelCenterOffsets[PxVehicleDrive4W::eFRONT_RIGHT_WHEEL].y -
		wheelCenterOffsets[PxVehicleDrive4W::eFRONT_LEFT_WHEEL].y);
	ackermann.mRearWidth = fabs(
		wheelCenterOffsets[PxVehicleDrive4W::eREAR_LEFT_WHEEL].y -
		wheelCenterOffsets[PxVehicleDrive4W::eREAR_RIGHT_WHEEL].y);
	driveData.setAckermannGeometryData(ackermann);
}

PhysXVehicle::PhysXVehicle( PhysXScene* scene, PhysXBody* baseBody, PhysXVehicleInitData* generalData,
	PhysXVehicleInitData* wheelFrontLeftData, PhysXVehicleInitData* wheelFrontRightData, 
	PhysXVehicleInitData* wheelRearLeftData, PhysXVehicleInitData* wheelRearRightData )
{
	PhysXVehicleInitData* wheelDatas[ 4 ];
	wheelDatas[ 0 ] = wheelFrontLeftData;
	wheelDatas[ 1 ] = wheelFrontRightData;
	wheelDatas[ 2 ] = wheelRearLeftData;
	wheelDatas[ 3 ] = wheelRearRightData;

	this->scene = scene;
	this->baseBody = baseBody;
	this->wheelCount = 4;
	vehicleDrive = NULL;
	surfaceTirePairs = NULL;
	surfaceTirePairsUsedMaterialsVersion = 0;
	ResetInputData();

	this->baseBody->ownerVehicle = this;

	//Scene query data for to allow raycasts for all suspensions of all vehicles.
	sceneQueryData = new PhysXVehicleSceneQuery(scene->mScene, wheelCount);

	PxVehicleWheelsSimData* wheelsSimData = PxVehicleWheelsSimData::allocate(wheelCount);
	PxVehicleDriveSimData4W driveSimData;

	PxConvexMesh* wheelConvexMeshes[ 4 ];
	PxVec3 wheelCenterOffsets[ 4 ];
	for( int n = 0; n < 4; n++ )
	{
		PhysXShape* shape = baseBody->mShapes[ n ];
		shape->isWheel = true;
		//PhysXShape* shape = baseBody->mShapes[ n + 1 ];
		wheelConvexMeshes[ n ] = shape->mShape->getGeometry().convexMesh().convexMesh;
		wheelCenterOffsets[ n ] = shape->mShape->getLocalPose().p;
	}
	PxConvexMesh* chassisConvexMesh = baseBody->mShapes[ 4 ]->mShape->getGeometry().convexMesh().convexMesh;
	//PxConvexMesh* chassisConvexMesh = baseBody->mShapes[0]->mShape->getGeometry().convexMesh().convexMesh;

	CreateVehicle4WSimulationData( chassisConvexMesh, wheelConvexMeshes, wheelCenterOffsets, *wheelsSimData, driveSimData, 
		generalData, wheelDatas );

	vehicleDrive = PxVehicleDrive4W::allocate( wheelCount );
	vehicleDrive->setup( world->mPhysics, (PxRigidDynamic*)baseBody->mActor, *wheelsSimData, driveSimData, 0 );

	//vehicleDrive->setWheelShapeMapping(0, 1);
	//vehicleDrive->setWheelShapeMapping(1, 2);
	//vehicleDrive->setWheelShapeMapping(2, 3);
	//vehicleDrive->setWheelShapeMapping(3, 4);

	//Free the sim data because we don't need that any more.
	wheelsSimData->free();

	//configure tireFrictionMultipliers
	{
		tireFrictionMultipliers[ L"" ] = 1;
		for( int n = 0; ; n++ )
		{
			char s[50];
			sprintf( s, "tireFrictionMaterial%d", n );
			if(!generalData->IsStringParameterExist(s))
				break;
			WString materialName = generalData->GetStringParameter( s );
			sprintf( s, "tireFrictionValue%d", n );
			float value = generalData->GetFloatParameter( s );
			tireFrictionMultipliers[ materialName ] = value;
		}
	}

	//Set the transform and the instantiated car and set it be to be at rest.
	SetToRestState();
	ForceGearChange( 0 );
}

PhysXVehicle::~PhysXVehicle()
{
	for( int n = 0; n < 4; n++ )
	{
		PhysXShape* shape = baseBody->mShapes[ n ];
		shape->isWheel = false;
	}
	baseBody->ownerVehicle = NULL;

	if(vehicleDrive)
	{
		vehicleDrive->free();
		vehicleDrive = NULL;
	}

	if(sceneQueryData)
	{
		delete sceneQueryData;
		sceneQueryData = NULL;
	}

	if(surfaceTirePairs)
	{
		surfaceTirePairs->release();
		surfaceTirePairs = NULL;
	}
}

void PhysXVehicle::UpdateSurfaceTirePairs()
{
	//set up the friction values arising from combinations of tire type and surface type.

	if(surfaceTirePairs)
	{
		surfaceTirePairs->release();
		surfaceTirePairs = NULL;
	}

	int materialCount = world->vehicleDrivableMaterials.size();

	if(tireFrictionMultipliers.size() > PxVehicleDrivableSurfaceToTireFrictionPairs::eMAX_NUM_SURFACE_TYPES)
	{
		char s[300];
		sprintf(s, "PhysXVehicle: UpdateSurfaceTirePairs: Amount of tire friction multipliers for specified tire type is %d. Maximally supported amount is %d.", (int)tireFrictionMultipliers.size(), 
			(int)PxVehicleDrivableSurfaceToTireFrictionPairs::eMAX_NUM_SURFACE_TYPES);
		Fatal(s);
	}
	if(materialCount > PxVehicleDrivableSurfaceToTireFrictionPairs::eMAX_NUM_SURFACE_TYPES)
	{
		char s[300];
		sprintf(s, "PhysXVehicle: UpdateSurfaceTirePairs: Amount of different vehicle drivable materials is %d. Maximally supported amount is %d.", 
			materialCount, (int)PxVehicleDrivableSurfaceToTireFrictionPairs::eMAX_NUM_SURFACE_TYPES);
		Fatal(s);
	}

	PxMaterial** pxMaterials = new PxMaterial*[materialCount];
	{
		int n = 0;
		for(std::set<PhysXMaterial*>::iterator it = world->vehicleDrivableMaterials.begin(); 
			it != world->vehicleDrivableMaterials.end(); it++) 
		{
			pxMaterials[ n ] = (*it)->material;
			n++;
		}
	}

	//sense of mType? ok..
	PxVehicleDrivableSurfaceType drivableSurfaceTypes[PxVehicleDrivableSurfaceToTireFrictionPairs::eMAX_NUM_SURFACE_TYPES];
	for(int n = 0; n < PxVehicleDrivableSurfaceToTireFrictionPairs::eMAX_NUM_SURFACE_TYPES; n++)
		drivableSurfaceTypes[n].mType = n;

	//mNumTireTypes must be a multiple of 4. why? ok..
	surfaceTirePairs = PxVehicleDrivableSurfaceToTireFrictionPairs::create(4, materialCount, (const PxMaterial**)pxMaterials, 
		drivableSurfaceTypes);
	{
		int n = 0;
		for(std::set<PhysXMaterial*>::iterator it = world->vehicleDrivableMaterials.begin(); 
			it != world->vehicleDrivableMaterials.end(); it++) 
		{
			PhysXMaterial* material = *it;

			std::map<WString, float>::iterator it2 = tireFrictionMultipliers.find(material->materialName);
			if(it2 == tireFrictionMultipliers.end())
				it2 = tireFrictionMultipliers.find(L"");
			float multiplier = 1;
			if(it2 != tireFrictionMultipliers.end())
				multiplier = it2->second;

			surfaceTirePairs->setTypePairFriction(n, 0, multiplier);

			n++;
		}
	}

	delete[] pxMaterials;
}

//void PhysXVehicle::ProcessGearsAutoReverse()
//{
//	if( !vehicleDrive->isInAir() )
//	{
//		const PxF32 forwardSpeed = vehicleDrive->computeForwardSpeed();
//		const PxU32 currentGear = vehicleDrive->mDriveDynData.getCurrentGear();
//
//		const float forwardSpeedTheshold = .1f;
//
//		if( ( inputDigitalBrake || inputAnalogBrake > .05f ) && forwardSpeed < forwardSpeedTheshold && 
//			( currentGear == PxVehicleGearsData::eNEUTRAL || currentGear == PxVehicleGearsData::eFIRST ) )
//		{
//			vehicleDrive->mDriveDynData.startGearChange( currentGear - 1 );
//		}
//
//		if( ( inputDigitalAccel || inputAnalogAccel > .05f ) && forwardSpeed < forwardSpeedTheshold &&
//			( currentGear == PxVehicleGearsData::eREVERSE || currentGear == PxVehicleGearsData::eNEUTRAL ) )
//		{
//			vehicleDrive->mDriveDynData.startGearChange( currentGear + 1 );
//		}
//	}
//}

void PhysXVehicle::UpdateController( float delta )
{
	////process auto reverse mode
	//if( vehicleDrive->mDriveDynData.getUseAutoGears() && autoGearAutoReverse )
	//	ProcessGearsAutoReverse();

	//bool swapAccelBrake = false;
	//if( vehicleDrive->mDriveDynData.getUseAutoGears() && autoGearAutoReverse && 
	//	vehicleDrive->mDriveDynData.getCurrentGear() == PxVehicleGearsData::eREVERSE )
	//{
	//	swapAccelBrake = true;
	//}

	if( digitalInput )
	{
		//digital input

		rawInputData.setDigitalAccel( inputAccel != 0 );
		rawInputData.setDigitalBrake( inputBrake != 0 );
		//need swap left and right. why? ok...
		rawInputData.setDigitalSteerLeft( inputSteer > 0 );
		rawInputData.setDigitalSteerRight( inputSteer < 0 );
		//rawInputData.setDigitalSteerLeft( inputDigitalSteerLeft );
		//rawInputData.setDigitalSteerRight( inputDigitalSteerRight );
		rawInputData.setDigitalHandbrake( inputHandbrake != 0 );

		PxVehicleDrive4WSmoothDigitalRawInputsAndSetAnalogInputs( inputKeySmoothingData, inputSteerVsForwardSpeedTable, 
			rawInputData, delta, *vehicleDrive );
	}
	else
	{
		//analog input

		rawInputData.setAnalogAccel( inputAccel );
		rawInputData.setAnalogBrake( inputBrake );
		//need swap left and right. why? ok...
		rawInputData.setAnalogSteer( -inputSteer );
		rawInputData.setAnalogHandbrake( inputHandbrake );

		PxVehicleDrive4WSmoothAnalogRawInputsAndSetAnalogInputs( inputPadSmoothingData, inputSteerVsForwardSpeedTable, 
			rawInputData, delta, *vehicleDrive );
	}
}

void PhysXVehicle::Update( float delta )
{
	UpdateController(delta);

	//optimization: update all cars by one call. make sense?

	PxVehicleWheels* vehicles[1];
	vehicles[0] = vehicleDrive;
	PxVehicleSuspensionRaycasts(sceneQueryData->batchQuery, 1, vehicles, wheelCount, sceneQueryData->raycastQueryResults);

	if(surfaceTirePairsUsedMaterialsVersion != world->vehicleDrivableMaterialsVersionCounter)
	{
		UpdateSurfaceTirePairs();
		surfaceTirePairsUsedMaterialsVersion = world->vehicleDrivableMaterialsVersionCounter;
	}

	//Update the vehicle for which we want to record debug data.
	//PxVehicleUpdateSingleVehicleAndStoreTelemetryData(delta, scene->mScene->getGravity(), *mSurfaceTirePairs, 
	//	vehicleDrive, *telemetryData);
	PxVehicleUpdates(delta, scene->mScene->getGravity(), *surfaceTirePairs, 1, vehicles);
}

void PhysXVehicle::SetInputData( bool digitalInput, float* smoothingSettings, int steerVsForwardSpeedTablePairCount, 
	float* steerVsForwardSpeedTable, float accel, float brake, float steer, float handbrake )
{
	this->digitalInput = digitalInput;
	inputAccel = accel;
	inputBrake = brake;
	inputSteer = steer;
	inputHandbrake = handbrake;

	inputKeySmoothingData.mRiseRates[ PxVehicleDrive4W::eANALOG_INPUT_ACCEL ] = smoothingSettings[ 0 ];
	inputKeySmoothingData.mRiseRates[ PxVehicleDrive4W::eANALOG_INPUT_BRAKE ] = smoothingSettings[ 1 ];
	inputKeySmoothingData.mRiseRates[ PxVehicleDrive4W::eANALOG_INPUT_HANDBRAKE ] = smoothingSettings[ 3 ];
	inputKeySmoothingData.mRiseRates[ PxVehicleDrive4W::eANALOG_INPUT_STEER_LEFT ] = smoothingSettings[ 2 ];
	inputKeySmoothingData.mRiseRates[ PxVehicleDrive4W::eANALOG_INPUT_STEER_RIGHT ] = smoothingSettings[ 2 ];
	inputKeySmoothingData.mFallRates[ PxVehicleDrive4W::eANALOG_INPUT_ACCEL ] = smoothingSettings[ 4 ];
	inputKeySmoothingData.mFallRates[ PxVehicleDrive4W::eANALOG_INPUT_BRAKE ] = smoothingSettings[ 5 ];
	inputKeySmoothingData.mFallRates[ PxVehicleDrive4W::eANALOG_INPUT_HANDBRAKE ] = smoothingSettings[ 7 ];
	inputKeySmoothingData.mFallRates[ PxVehicleDrive4W::eANALOG_INPUT_STEER_LEFT ] = smoothingSettings[ 6 ];
	inputKeySmoothingData.mFallRates[ PxVehicleDrive4W::eANALOG_INPUT_STEER_RIGHT ] = smoothingSettings[ 6 ];

	inputPadSmoothingData.mRiseRates[ PxVehicleDrive4W::eANALOG_INPUT_ACCEL ] = smoothingSettings[ 0 ];
	inputPadSmoothingData.mRiseRates[ PxVehicleDrive4W::eANALOG_INPUT_BRAKE ] = smoothingSettings[ 1 ];
	inputPadSmoothingData.mRiseRates[ PxVehicleDrive4W::eANALOG_INPUT_HANDBRAKE ] = smoothingSettings[ 3 ];
	inputPadSmoothingData.mRiseRates[ PxVehicleDrive4W::eANALOG_INPUT_STEER_LEFT ] = smoothingSettings[ 2 ];
	inputPadSmoothingData.mRiseRates[ PxVehicleDrive4W::eANALOG_INPUT_STEER_RIGHT ] = smoothingSettings[ 2 ];
	inputPadSmoothingData.mFallRates[ PxVehicleDrive4W::eANALOG_INPUT_ACCEL ] = smoothingSettings[ 4 ];
	inputPadSmoothingData.mFallRates[ PxVehicleDrive4W::eANALOG_INPUT_BRAKE ] = smoothingSettings[ 5 ];
	inputPadSmoothingData.mFallRates[ PxVehicleDrive4W::eANALOG_INPUT_HANDBRAKE ] = smoothingSettings[ 7 ];
	inputPadSmoothingData.mFallRates[ PxVehicleDrive4W::eANALOG_INPUT_STEER_LEFT ] = smoothingSettings[ 6 ];
	inputPadSmoothingData.mFallRates[ PxVehicleDrive4W::eANALOG_INPUT_STEER_RIGHT ] = smoothingSettings[ 6 ];

	inputSteerVsForwardSpeedTable = PxFixedSizeLookupTable<8>( steerVsForwardSpeedTable, steerVsForwardSpeedTablePairCount );
}

void PhysXVehicle::ResetInputData()
{
	digitalInput = false;
	inputAccel = 0;
	inputBrake = 0;
	inputSteer = 0;
	inputHandbrake = 0;

	inputKeySmoothingData.mRiseRates[ PxVehicleDrive4W::eANALOG_INPUT_ACCEL ] = 3;
	inputKeySmoothingData.mRiseRates[ PxVehicleDrive4W::eANALOG_INPUT_BRAKE ] = 3;
	inputKeySmoothingData.mRiseRates[ PxVehicleDrive4W::eANALOG_INPUT_HANDBRAKE ] = 10;
	inputKeySmoothingData.mRiseRates[ PxVehicleDrive4W::eANALOG_INPUT_STEER_LEFT ] = 2.5f;
	inputKeySmoothingData.mRiseRates[ PxVehicleDrive4W::eANALOG_INPUT_STEER_RIGHT ] = 2.5f;
	inputKeySmoothingData.mFallRates[ PxVehicleDrive4W::eANALOG_INPUT_ACCEL ] = 5;
	inputKeySmoothingData.mFallRates[ PxVehicleDrive4W::eANALOG_INPUT_BRAKE ] = 5;
	inputKeySmoothingData.mFallRates[ PxVehicleDrive4W::eANALOG_INPUT_HANDBRAKE ] = 10;
	inputKeySmoothingData.mFallRates[ PxVehicleDrive4W::eANALOG_INPUT_STEER_LEFT ] = 5;
	inputKeySmoothingData.mFallRates[ PxVehicleDrive4W::eANALOG_INPUT_STEER_RIGHT ] = 5;

	inputPadSmoothingData.mRiseRates[ PxVehicleDrive4W::eANALOG_INPUT_ACCEL ] = 3;
	inputPadSmoothingData.mRiseRates[ PxVehicleDrive4W::eANALOG_INPUT_BRAKE ] = 3;
	inputPadSmoothingData.mRiseRates[ PxVehicleDrive4W::eANALOG_INPUT_HANDBRAKE ] = 10;
	inputPadSmoothingData.mRiseRates[ PxVehicleDrive4W::eANALOG_INPUT_STEER_LEFT ] = 2.5f;
	inputPadSmoothingData.mRiseRates[ PxVehicleDrive4W::eANALOG_INPUT_STEER_RIGHT ] = 2.5f;
	inputPadSmoothingData.mFallRates[ PxVehicleDrive4W::eANALOG_INPUT_ACCEL ] = 5;
	inputPadSmoothingData.mFallRates[ PxVehicleDrive4W::eANALOG_INPUT_BRAKE ] = 5;
	inputPadSmoothingData.mFallRates[ PxVehicleDrive4W::eANALOG_INPUT_HANDBRAKE ] = 10;
	inputPadSmoothingData.mFallRates[ PxVehicleDrive4W::eANALOG_INPUT_STEER_LEFT ] = 5;
	inputPadSmoothingData.mFallRates[ PxVehicleDrive4W::eANALOG_INPUT_STEER_RIGHT ] = 5;

	PxF32 steerVsForwardSpeedData[ 2 * 8 ] =
	{
		0.0f,		0.75f,
		5.0f,		0.75f,
		30.0f,		0.125f,
		120.0f,		0.1f,
		PX_MAX_F32, PX_MAX_F32,
		PX_MAX_F32, PX_MAX_F32,
		PX_MAX_F32, PX_MAX_F32,
		PX_MAX_F32, PX_MAX_F32
	};
	inputSteerVsForwardSpeedTable = PxFixedSizeLookupTable<8>( steerVsForwardSpeedData, 4 );
}

void PhysXVehicle::SetToRestState()
{
	vehicleDrive->setToRestState();
	ResetInputData();
}

void PhysXVehicle::ForceGearChange( int gear )
{
	vehicleDrive->mDriveDynData.forceGearChange((uint)(gear + 1));
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

EXPORT PhysXVehicleInitData* PhysXNativeVehicleInitData_Create()
{
	return new PhysXVehicleInitData();
}

EXPORT void PhysXNativeVehicleInitData_Destroy(PhysXVehicleInitData* data)
{
	delete data;
}

EXPORT void PhysXNativeVehicleInitData_SetFloatParameter(PhysXVehicleInitData* data, wchar16* namePtr, float value)
{
	WString name = TO_WCHAR_T(namePtr);
	data->floatParameters[name] = value;
}

EXPORT void PhysXNativeVehicleInitData_SetStringParameter(PhysXVehicleInitData* data, wchar16* namePtr, wchar16* valuePtr)
{
	WString name = TO_WCHAR_T(namePtr);
	WString value = TO_WCHAR_T(valuePtr);
	data->stringParameters[name] = value;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

EXPORT PhysXVehicle* PhysXVehicle_Create( PhysXScene* scene, PhysXBody* baseBody, PhysXVehicleInitData* generalData,
	PhysXVehicleInitData* wheelFrontLeftData, PhysXVehicleInitData* wheelFrontRightData, 
	PhysXVehicleInitData* wheelRearLeftData, PhysXVehicleInitData* wheelRearRightData )
{
	return new PhysXVehicle( scene, baseBody, generalData, wheelFrontLeftData, wheelFrontRightData, wheelRearLeftData, 
		wheelRearRightData );
}

EXPORT void PhysXVehicle_Destroy( PhysXVehicle* vehicle )
{
	delete vehicle;
}

EXPORT void PhysXVehicle_Update( PhysXVehicle* vehicle, float delta )
{
	vehicle->Update( delta );
}

EXPORT void PhysXVehicle_SetInputData( PhysXVehicle* vehicle, bool digitalInput, float* smoothingSettings, 
	int steerVsForwardSpeedTablePairCount, float* steerVsForwardSpeedTable, float accel, float brake, float steer, 
	float handbrake )
{
	vehicle->SetInputData( digitalInput, smoothingSettings, steerVsForwardSpeedTablePairCount, steerVsForwardSpeedTable, 
		accel, brake, steer, handbrake );
}

EXPORT void PhysXVehicle_SetToRestState( PhysXVehicle* vehicle )
{
	vehicle->SetToRestState();
}

EXPORT void PhysXVehicle_ForceGearChange( PhysXVehicle* vehicle, int gear )
{
	vehicle->ForceGearChange(gear);
}

EXPORT void PhysXVehicle_StartGearChange( PhysXVehicle* vehicle, int gear )
{
	vehicle->vehicleDrive->mDriveDynData.startGearChange((uint)(gear + 1));
}

EXPORT void PhysXVehicle_CallMethod( PhysXVehicle* vehicle, wchar16* messagePtr, int parameter1, double parameter2, 
	double parameter3, int& result1, double& result2, double& result3 )
{
	result1 = 0;
	result2 = 0;
	result3 = 0;
	WString message = TO_WCHAR_T( messagePtr );

	if( message == L"SetAutoGear" )
	{
		vehicle->vehicleDrive->mDriveDynData.setUseAutoGears( parameter1 != 0 );
		return;
	}
	//if( message == L"SetAutoGearAutoReverse" )
	//{
	//	vehicle->autoGearAutoReverse = parameter1 != 0;
	//	return;
	//}
}

EXPORT int PhysXVehicle_GetCurrentGear( PhysXVehicle* vehicle )
{
	return vehicle->vehicleDrive->mDriveDynData.getCurrentGear() - 1;
}

EXPORT int PhysXVehicle_GetTargetGear( PhysXVehicle* vehicle )
{
	return vehicle->vehicleDrive->mDriveDynData.getTargetGear() - 1;
}

EXPORT float PhysXVehicle_GetEngineRotationSpeed( PhysXVehicle* vehicle )
{
	return vehicle->vehicleDrive->mDriveDynData.getEngineRotationSpeed();
}

EXPORT float PhysXVehicle_GetWheelRotationSpeed( PhysXVehicle* vehicle, int wheelIndex )
{
	return vehicle->vehicleDrive->mWheelsDynData.getWheelRotationSpeed( wheelIndex );
}

EXPORT float PhysXVehicle_GetWheelSteer( PhysXVehicle* vehicle, int wheelIndex )
{
	return vehicle->vehicleDrive->mWheelsDynData.getSteer( wheelIndex );
}

EXPORT float PhysXVehicle_GetWheelRotationAngle( PhysXVehicle* vehicle, int wheelIndex )
{
	return vehicle->vehicleDrive->mWheelsDynData.getWheelRotationAngle( wheelIndex );
}

EXPORT float PhysXVehicle_GetWheelSuspensionJounce( PhysXVehicle* vehicle, int wheelIndex )
{
	return vehicle->vehicleDrive->mWheelsDynData.getSuspJounce( wheelIndex );
}

EXPORT float PhysXVehicle_GetWheelTireLongitudinalSlip( PhysXVehicle* vehicle, int wheelIndex )
{
	return vehicle->vehicleDrive->mWheelsDynData.getTireLongSlip( wheelIndex );
}

EXPORT float PhysXVehicle_GetWheelTireLateralSlip( PhysXVehicle* vehicle, int wheelIndex )
{
	return vehicle->vehicleDrive->mWheelsDynData.getTireLatSlip( wheelIndex );
}

EXPORT float PhysXVehicle_GetWheelTireFriction( PhysXVehicle* vehicle, int wheelIndex )
{
	return vehicle->vehicleDrive->mWheelsDynData.getTireFriction( wheelIndex );
}

EXPORT bool PhysXVehicle_IsWheelInAir( PhysXVehicle* vehicle, int wheelIndex )
{
	return vehicle->vehicleDrive->isInAir( wheelIndex );
}
