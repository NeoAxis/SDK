// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#pragma once
#include "PhysXBody.h"
#include "PhysXScene.h"
#include "PhysXVehicleSceneQuery.h"
#include "StringUtils.h"

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

class PhysXVehicleInitData
{
public:
	std::map<WString, float> floatParameters;
	std::map<WString, WString> stringParameters;

	bool IsFloatParameterExist(const wchar_t* name)
	{
		return floatParameters.find(name) != floatParameters.end();
	}

	float GetFloatParameter(const wchar_t* name)
	{
		std::map<WString, float>::iterator it = floatParameters.find(name);
		if(it == floatParameters.end())
		{
			char str[256];
			sprintf(str, "PhysXVehicleInitData: GetParameter: The parameter \"%s\" is not defined.",
				ConvertStringToUTF8(name).c_str());
			Fatal(str);
		}
		return it->second;
	}

	bool IsFloatParameterExist(const char* name)
	{
		return IsFloatParameterExist(ConvertStringToUTFWide(name).c_str());
	}

	float GetFloatParameter(const char* name)
	{
		return GetFloatParameter(ConvertStringToUTFWide(name).c_str());
	}

	bool IsStringParameterExist(const wchar_t* name)
	{
		return stringParameters.find(name) != stringParameters.end();
	}

	WString GetStringParameter(const wchar_t* name)
	{
		std::map<WString, WString>::iterator it = stringParameters.find(name);
		if(it == stringParameters.end())
		{
			char str[256];
			sprintf(str, "PhysXVehicleInitData: GetParameter: The parameter \"%s\" is not defined.",
				ConvertStringToUTF8(name).c_str());
			Fatal(str);
		}
		return it->second;
	}

	bool IsStringParameterExist(const char* name)
	{
		return IsStringParameterExist(ConvertStringToUTFWide(name).c_str());
	}

	WString GetStringParameter(const char* name)
	{
		return GetStringParameter(ConvertStringToUTFWide(name).c_str());
	}
};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

class PhysXVehicle
{
public:
	PhysXScene* scene;
	PhysXBody* baseBody;
	int wheelCount;
	std::map<WString, float> tireFrictionMultipliers;

	PhysXVehicleSceneQuery* sceneQueryData;
	PxVehicleDrive4W* vehicleDrive;

	//Friction from combinations of tire and surface types.
	PxVehicleDrivableSurfaceToTireFrictionPairs* surfaceTirePairs;
	int surfaceTirePairsUsedMaterialsVersion;

	PxVehicleDrive4WRawInputData rawInputData;

	//input data
	bool digitalInput;
	float inputAccel;
	float inputBrake;
	float inputSteer;
	float inputHandbrake;
	PxVehicleKeySmoothingData inputKeySmoothingData;
	PxVehiclePadSmoothingData inputPadSmoothingData;
	PxFixedSizeLookupTable<8> inputSteerVsForwardSpeedTable;

	//

	PhysXVehicle( PhysXScene* scene, PhysXBody* baseBody, PhysXVehicleInitData* generalData, 
		PhysXVehicleInitData* wheelFrontLeftData, PhysXVehicleInitData* wheelFrontRightData, 
		PhysXVehicleInitData* wheelRearLeftData, PhysXVehicleInitData* wheelRearRightData );
	~PhysXVehicle();
	
	void UpdateSurfaceTirePairs();
	//void ProcessGearsAutoReverse();
	void UpdateController(float delta);
	void Update(float delta);
	void SetInputData( bool digitalInput, float* smoothingSettings, int steerVsForwardSpeedTablePairCount, 
		float* steerVsForwardSpeedTable, float accel, float brake, float steer, float handbrake );
	void ResetInputData();
	void SetToRestState();
	void ForceGearChange( int gear );
};
