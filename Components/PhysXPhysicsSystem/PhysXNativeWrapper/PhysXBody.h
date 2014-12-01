// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#pragma once
#include "PhysXShape.h"

#pragma pack(push)  /* push current alignment to stack */
#pragma pack(1)     /* set alignment to 1 byte boundary */
struct HeightFieldShape_Sample
{
	ushort height;
	byte materialIndex0;
	byte materialIndex1;
	//byte hole; //bool
};
#pragma pack(pop)   /* restore original alignment from stack */

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

class PhysXBody
{
public:
	PhysXScene* mScene;
	bool mIsStatic;
	PxRigidActor* mActor;
	std::vector<PhysXShape*> mShapes;
	PhysXVehicle* ownerVehicle;

	PhysXBody(PhysXScene* pScene, bool isStatic, const PxVec3& globalPosition, const PxQuat& globalRotation);
	~PhysXBody();
};
