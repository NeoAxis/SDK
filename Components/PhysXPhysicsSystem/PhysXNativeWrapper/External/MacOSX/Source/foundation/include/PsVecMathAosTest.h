// This code contains NVIDIA Confidential Information and is disclosed to you 
// under a form of NVIDIA software license agreement provided separately to you.
//
// Notice
// NVIDIA Corporation and its licensors retain all intellectual property and
// proprietary rights in and to this software and related documentation and 
// any modifications thereto. Any use, reproduction, disclosure, or 
// distribution of this software and related documentation without an express 
// license agreement from NVIDIA Corporation is strictly prohibited.
// 
// ALL NVIDIA DESIGN SPECIFICATIONS, CODE ARE PROVIDED "AS IS.". NVIDIA MAKES
// NO WARRANTIES, EXPRESSED, IMPLIED, STATUTORY, OR OTHERWISE WITH RESPECT TO
// THE MATERIALS, AND EXPRESSLY DISCLAIMS ALL IMPLIED WARRANTIES OF NONINFRINGEMENT,
// MERCHANTABILITY, AND FITNESS FOR A PARTICULAR PURPOSE.
//
// Information and code furnished is believed to be accurate and reliable.
// However, NVIDIA Corporation assumes no responsibility for the consequences of use of such
// information or for any infringement of patents or other rights of third parties that may
// result from its use. No license is granted by implication or otherwise under any patent
// or patent rights of NVIDIA Corporation. Details are subject to change without notice.
// This code supersedes and replaces all information previously supplied.
// NVIDIA Corporation products are not authorized for use as critical
// components in life support devices or systems without express written approval of
// NVIDIA Corporation.
//
// Copyright (c) 2008-2012 NVIDIA Corporation. All rights reserved.
// Copyright (c) 2004-2008 AGEIA Technologies, Inc. All rights reserved.
// Copyright (c) 2001-2004 NovodeX AG. All rights reserved.  

#ifndef PX_PHYSICS_COMMON_VECMATH_AOS_TEST
#define PX_PHYSICS_COMMON_VECMATH_AOS_TEST

#include "PsVecMath.h"
using namespace Ps::aos;

bool FloatVTestFunctions()
{
	const PxF32 fa=1.5f;
	const PxF32 fb=2.5f;
	const FloatV a=FloatV_From_F32(fa);
	const FloatV b=FloatV_From_F32(fb);
	FloatV ftwo=ITwo();
	PxI32 itwo;
	PxF32_From_FloatV(ftwo, PX_FPTR(&itwo));
	PxI32 utwo;
	PxF32_From_FloatV(ftwo, PX_FPTR(&utwo));

	if(!_VecMathTests::allElementsEqualFloatV(FNeg(a),FloatV_From_F32(-fa)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualFloatV(FAdd(a,b),FloatV_From_F32(fa+fb)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualFloatV(FSub(a,b),FloatV_From_F32(fa-fb)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualFloatV(FMul(a,b),FloatV_From_F32(fa*fb)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualFloatV(FDiv(a,b),FloatV_From_F32(fa/fb)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualFloatV(FDivFast(a,b),FloatV_From_F32(fa/fb)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualFloatV(FAbs(FloatV_From_F32(-fb)),FloatV_From_F32(fb)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualFloatV(FSel(BTTTT(),a,b),FloatV_From_F32(fa)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualFloatV(FSel(BFFFF(),a,b),FloatV_From_F32(fb)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualBoolV(FIsGrtr(a,b),BoolV_From_Bool32(fa>fb)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualBoolV(FIsGrtrOrEq(a,b),BoolV_From_Bool32(fa>=fb)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualBoolV(FIsGrtr(b,a),BoolV_From_Bool32(fb>fa)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualBoolV(FIsGrtrOrEq(b,a),BoolV_From_Bool32(fb>=fa)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualFloatV(FMax(a,b),FloatV_From_F32(fa > fb ? fa : fb)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualFloatV(FMin(a,b),FloatV_From_F32(fa < fb ? fa : fb)))
	{
		return false;
	}
	if(!FInBounds(a,b) || !FInBounds(a, FNeg(b), b))
	{
		return false;
	}
	if(FOutOfBounds(a,b) || FOutOfBounds(a, FNeg(b), b))
	{
		return false;
	}
	if(FInBounds(b,a) || FInBounds(b, FNeg(a), a))
	{
		return false;
	}
	if(!FOutOfBounds(b,a) || !FOutOfBounds(b, FNeg(a), a))
	{
		return false;
	}
	if(itwo!=2)
	{
		return false;
	}
	if(utwo!=2)
	{
		return false;
	}

	return true;
}

bool Vec3VTestFunctions()
{
	const PxF32 fax=1.5f;
	const PxF32 fay=2.5f;
	const PxF32 faz=3.5f;
	const PxVec3 fa(fax,fay,faz);
	const PxF32 fbx=4.5f;
	const PxF32 fby=5.5f;
	const PxF32 fbz=-1.5f;
	const PxVec3 fb(fbx,fby,fbz);
	const Vec3V a=Vec3V_From_PxVec3(fa);
	const Vec3V b=Vec3V_From_PxVec3(fb);
	const PxF32 fx=2.0f;
	const PxF32 fy=3.0f;
	const PxF32 fz=4.0f;
	const FloatV x=FloatV_From_F32(fx);
	const FloatV y=FloatV_From_F32(fy);
	const FloatV z=FloatV_From_F32(fz);
	const Vec3V xyz=V3Merge(x,y,z);
	const bool aIsGreaterThanB[4]={fax>fbx,fay>fby,faz>fbz,0>0};
	const bool aIsGreaterThanOrEqualB[4]={fax>fbx,fay>fby,faz>fbz,0>=0};

	if(!_VecMathTests::allElementsEqualVec3V(xyz,Vec3V_From_PxVec3(PxVec3(fx,fy,fz))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualFloatV(V3GetX(xyz),FloatV_From_F32(fx)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualFloatV(V3GetY(xyz),FloatV_From_F32(fy)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualFloatV(V3GetZ(xyz),FloatV_From_F32(fz)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3Neg(xyz),Vec3V_From_PxVec3(PxVec3(-fx,-fy,-fz))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3Add(a,b),Vec3V_From_PxVec3(PxVec3(fax+fbx,fay+fby,faz+fbz))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3Sub(a,b),Vec3V_From_PxVec3(PxVec3(fax-fbx,fay-fby,faz-fbz))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3Scale(a,x),Vec3V_From_PxVec3(PxVec3(fax*fx,fay*fx,faz*fx))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3Mul(a,b),Vec3V_From_PxVec3(PxVec3(fax*fbx,fay*fby,faz*fbz))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualVec3V(V3ScaleInv(a,x),Vec3V_From_PxVec3(PxVec3(fax/fx,fay/fx,faz/fx))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualVec3V(V3Div(a,b),Vec3V_From_PxVec3(PxVec3(fax/fbx,fay/fby,faz/fbz))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualVec3V(V3ScaleInvFast(a,x),Vec3V_From_PxVec3(PxVec3(fax/fx,fay/fx,faz/fx))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualVec3V(V3DivFast(a,b),Vec3V_From_PxVec3(PxVec3(fax/fbx,fay/fby,faz/fbz))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualFloatV(V3Dot(a,b),FloatV_From_F32(fax*fbx + fay*fby + faz*fbz)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3Cross(a,b),Vec3V_From_PxVec3(PxVec3(fay*fbz-faz*fby, -fax*fbz+faz*fbx, fax*fby-fay*fbx))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualFloatV(V3Length(a),FloatV_From_F32(sqrtf(fax*fax + fay*fay + faz*faz))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualFloatV(V3LengthSq(a),FloatV_From_F32(fax*fax + fay*fay + faz*faz)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualVec3V(V3Normalize(a),Vec3V_From_PxVec3(PxVec3(fax/sqrtf(fax*fax + fay*fay + faz*faz),fay/sqrtf(fax*fax + fay*fay + faz*faz),faz/sqrtf(fax*fax + fay*fay + faz*faz)))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualVec3V(V3NormalizeSafe(a),Vec3V_From_PxVec3(PxVec3(fax/sqrtf(fax*fax + fay*fay + faz*faz),fay/sqrtf(fax*fax + fay*fay + faz*faz),faz/sqrtf(fax*fax + fay*fay + faz*faz)))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualVec3V(V3NormalizeFast(a),Vec3V_From_PxVec3(PxVec3(fax/sqrtf(fax*fax + fay*fay + faz*faz),fay/sqrtf(fax*fax + fay*fay + faz*faz),faz/sqrtf(fax*fax + fay*fay + faz*faz)))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3Sel(BTFTF(),a,b),Vec3V_From_PxVec3(PxVec3(fax,fby,faz))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3Sel(BFTFT(),a,b),Vec3V_From_PxVec3(PxVec3(fbx,fay,fbz))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualBoolV(V3IsGrtr(a,b),BoolV_From_Bool32Array(aIsGreaterThanB)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualBoolV(V3IsGrtrOrEq(a,b),BoolV_From_Bool32Array(aIsGreaterThanOrEqualB)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3Max(a,b),Vec3V_From_PxVec3(PxVec3(fax > fbx ? fax : fbx, fay > fby ? fay : fby, faz > fbz ? faz : fbz))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3Min(a,b),Vec3V_From_PxVec3(PxVec3(fax < fbx ? fax : fbx, fay < fby ? fay : fby, faz < fbz ? faz : fbz))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3PermYZZ(a), Vec3V_From_PxVec3(PxVec3(fa.y, fa.z, fa.z))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3PermXYX(a), Vec3V_From_PxVec3(PxVec3(fa.x, fa.y, fa.x))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3PermYZX(a), Vec3V_From_PxVec3(PxVec3(fa.y, fa.z, fa.x))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3PermZXY(a), Vec3V_From_PxVec3(PxVec3(fa.z, fa.x, fa.y))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3PermZZY(a), Vec3V_From_PxVec3(PxVec3(fa.z, fa.z, fa.y))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3PermYXX(a), Vec3V_From_PxVec3(PxVec3(fa.y, fa.x, fa.x))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3Perm_Zero_1Z_0Y(a,b), Vec3V_From_PxVec3(PxVec3(0, fb.z, fa.y))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3Perm_0Z_Zero_1X(a,b), Vec3V_From_PxVec3(PxVec3(fa.z, 0, fb.x))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3Perm_1Y_0X_Zero(a,b), Vec3V_From_PxVec3(PxVec3(fb.y, fa.x, 0))))
	{
		return false;
	}
	if(!V3InBounds(a,xyz) || !V3InBounds(a, V3Neg(xyz), xyz))
	{
		return false;
	}
	if(V3OutOfBounds(a,xyz) || V3OutOfBounds(a, V3Neg(xyz), xyz))
	{
		return false;
	}
	if(V3InBounds(xyz,a) || V3InBounds(xyz, V3Neg(a), a))
	{
		return false;
	}
	if(!V3OutOfBounds(xyz,a) || !V3OutOfBounds(xyz, V3Neg(a), a))
	{
		return false;
	}
	if(PxF32_From_FloatV(V3SumElems(xyz)) != fx+fy+fz)
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3ColX(a,b,xyz), Vec3V_From_PxVec3(PxVec3(fa.x,fb.x,fx))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3ColY(a,b,xyz), Vec3V_From_PxVec3(PxVec3(fa.y,fb.y,fy))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(V3ColZ(a,b,xyz), Vec3V_From_PxVec3(PxVec3(fa.z,fb.z,fz))))
	{
		return false;
	}

	return true;
}

bool Vec4VTestFunctions()
{
	const PxF32 fax=1.5f;
	const PxF32 fay=2.5f;
	const PxF32 faz=3.5f;
	const PxF32 faw=4.5f;
	const PX_ALIGN(16, PxF32 fa[4])={fax,fay,faz,faw};
	const PxF32 fbx=4.5f;
	const PxF32 fby=5.5f;
	const PxF32 fbz=-1.5f;
	const PxF32 fbw=-2.5f;
	const PX_ALIGN(16, PxF32 fb[4])={fbx,fby,fbz,fbw};
	const Vec4V a=Vec4V_From_F32Array_Aligned(fa);
	const Vec4V b=Vec4V_From_F32Array_Aligned(fb);
	const PxF32 fx=2.0f;
	const PxF32 fy=3.0f;
	const PxF32 fz=4.0f;
	const PxF32 fw=5.0f;
	const PX_ALIGN(16, PxF32 fxyzw[4])={fx,fy,fz,fw};
	const PX_ALIGN(16, PxF32 fxyzwNegated[4])={-fx,-fy,-fz,-fw};
	const FloatV x=FloatV_From_F32(fx);
	const FloatV y=FloatV_From_F32(fy);
	const FloatV z=FloatV_From_F32(fz);
	const FloatV w=FloatV_From_F32(fw);
	const PX_ALIGN(16, FloatV xyzwArray[4])={x,y,z,w};
	const Vec4V xyzw=V4Merge(xyzwArray);
	const PX_ALIGN(16, PxF32 aPlusB[4])={fax+fbx,fay+fby,faz+fbz,faw+fbw};
	const PX_ALIGN(16, PxF32 aMinusB[4])={fax-fbx,fay-fby,faz-fbz,faw-fbw};
	const PX_ALIGN(16, PxF32 aTimesB[4])={fax*fbx,fay*fby,faz*fbz,faw*fbw};
	const PX_ALIGN(16, PxF32 aDividedByB[4])={fax/fbx,fay/fby,faz/fbz,faw/fbw};
	const PX_ALIGN(16, PxF32 aTimesX[4])={fax*fx,fay*fx,faz*fx,faw*fx};
	const PX_ALIGN(16, PxF32 aDividedByX[4])={fax/fx,fay/fx,faz/fx,faw/fx};
	const PX_ALIGN(16, PxF32 aNormalised[4])={fax/sqrtf(fax*fax+fay*fay+faz*faz+faw*faw),fay/sqrtf(fax*fax+fay*fay+faz*faz+faw*faw),faz/sqrtf(fax*fax+fay*fay+faz*faz+faw*faw),faw/sqrtf(fax*fax+fay*fay+faz*faz+faw*faw)};
	const PX_ALIGN(16, PxF32 tftfab[4])={fax,fby,faz,fbw};
	const PX_ALIGN(16, PxF32 ftftab[4])={fbx,fay,fbz,faw};
	const bool aGreaterThanB[4]={fax>fbx,fay>fby,faz>fbz,faw>fbw}; 
	const bool aGreaterThanOrEqualB[4]={fax>=fbx,fay>=fby,faz>=fbz,faw>=fbw}; 
	const PX_ALIGN(16, PxF32 maxab[4])={fax>fbx ? fax : fbx, fay>fby ? fay : fby, faz>fbz ? faz : fbz, faw>fbw ? faw : fbw};
	const PX_ALIGN(16, PxF32 minab[4])={fax<fbx ? fax : fbx, fay<fby ? fay : fby, faz<fbz ? faz : fbz, faw<fbw ? faw : fbw};

	if(!_VecMathTests::allElementsEqualVec4V(V4Merge(xyzwArray),Vec4V_From_F32Array_Aligned(fxyzw)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualFloatV(V4GetX(xyzw),FloatV_From_F32(fx)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualFloatV(V4GetY(xyzw),FloatV_From_F32(fy)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualFloatV(V4GetZ(xyzw),FloatV_From_F32(fz)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualFloatV(V4GetW(xyzw),FloatV_From_F32(fw)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec4V(V4Neg(xyzw),Vec4V_From_F32Array_Aligned(fxyzwNegated)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec4V(V4Add(a,b),Vec4V_From_F32Array_Aligned(aPlusB)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec4V(V4Sub(a,b),Vec4V_From_F32Array_Aligned(aMinusB)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec4V(V4Scale(a,x),Vec4V_From_F32Array_Aligned(aTimesX)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec4V(V4Mul(a,b),Vec4V_From_F32Array_Aligned(aTimesB)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualVec4V(V4ScaleInv(a,x),Vec4V_From_F32Array_Aligned(aDividedByX)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualVec4V(V4Div(a,b),Vec4V_From_F32Array_Aligned(aDividedByB)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualVec4V(V4ScaleInvFast(a,x),Vec4V_From_F32Array_Aligned(aDividedByX)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualVec4V(V4DivFast(a,b),Vec4V_From_F32Array_Aligned(aDividedByB)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualFloatV(V4Dot(a,b),FloatV_From_F32(fax*fbx + fay*fby + faz*fbz + faw*fbw)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualFloatV(V4Length(a),FloatV_From_F32(sqrtf(fax*fax + fay*fay + faz*faz + faw*faw))))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualFloatV(V4LengthSq(a),FloatV_From_F32(fax*fax + fay*fay + faz*faz + faw*faw)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualVec4V(V4Normalize(a),Vec4V_From_F32Array_Aligned(aNormalised)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualVec4V(V4NormalizeSafe(a),Vec4V_From_F32Array_Aligned(aNormalised)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualVec4V(V4NormalizeFast(a),Vec4V_From_F32Array_Aligned(aNormalised)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec4V(V4Sel(BTFTF(),a,b),Vec4V_From_F32Array_Aligned(tftfab)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec4V(V4Sel(BFTFT(),a,b),Vec4V_From_F32Array_Aligned(ftftab)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualBoolV(V4IsGrtr(a,b),BoolV_From_Bool32Array(aGreaterThanB)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualBoolV(V4IsGrtrOrEq(a,b),BoolV_From_Bool32Array(aGreaterThanOrEqualB)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec4V(V4Max(a,b),Vec4V_From_F32Array_Aligned(maxab)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec4V(V4Min(a,b),Vec4V_From_F32Array_Aligned(minab)))
	{
		return false;
	}

	return true;
}

bool Mat33VTestFunctions()
{
	const PX_ALIGN(16, PxVec3 faCol0)=PxVec3(2.0f,3.0f,4.0f);
	const PX_ALIGN(16, PxVec3 faCol1) =PxVec3(6.0f,7.0f,8.0f);
	const PX_ALIGN(16, PxVec3 faCol2)=PxVec3(10.0f,11.0f,13.0f);
	Mat33V a;
	a.col0=Vec3V_From_PxVec3(faCol0);
	a.col1=Vec3V_From_PxVec3(faCol1);
	a.col2=Vec3V_From_PxVec3(faCol2);
	const PX_ALIGN(16, PxVec3 fbCol0)=PxVec3(1.0f,2.0f,3.0f);
	const PX_ALIGN(16, PxVec3 fbCol1)=PxVec3(4.0f,5.0f,6.0f);
	const PX_ALIGN(16, PxVec3 fbCol2)=PxVec3(7.0f,8.0f,9.0f);
	Mat33V b;
	b.col0=Vec3V_From_PxVec3(fbCol0);
	b.col1=Vec3V_From_PxVec3(fbCol1);
	b.col2=Vec3V_From_PxVec3(fbCol2);

	const PX_ALIGN(16, PxVec3 fv) =PxVec3(1.0f,2.0f,3.0f);
	const Vec3V v=Vec3V_From_PxVec3(fv);
	const PX_ALIGN(16, PxVec3 ftr) = PxVec3(8.0f, 10.0f, 17.0f);
	const Vec3V tr= Vec3V_From_PxVec3(ftr);

	const PxVec3 aTimesV(44.0f,50.0f,59.0f);
	const PxVec3 aTimesVPlusTr = aTimesV + ftr;
	const PxVec3 aTransposeTimesV(20.0f,44.0f,71.0f);
	const Mat33V aTimesB(
		Vec3V_From_PxVec3(PxVec3(44.0f,50.0f,59.0f)),
		Vec3V_From_PxVec3(PxVec3(98.0f,113.0f,134.0f)),
		Vec3V_From_PxVec3(PxVec3(152.0f,176.0f,209.0f)));
	const Mat33V aPlusB(
		Vec3V_From_PxVec3(PxVec3(3.0f,5.0f,7.0f)),
		Vec3V_From_PxVec3(PxVec3(10.0f,12.0f,14.0f)),
		Vec3V_From_PxVec3(PxVec3(17.0f,19.0f,22.0f)));
	const Mat33V id(
		Vec3V_From_PxVec3(PxVec3(1.0f,0.0f,0.0f)),
		Vec3V_From_PxVec3(PxVec3(0.0f,1.0f,0.0f)),
		Vec3V_From_PxVec3(PxVec3(0.0f,0.0f,1.0f)));
	const Mat33V aTranspose(
		Vec3V_From_PxVec3(PxVec3(2.0f,6.0f,10.0f)),
		Vec3V_From_PxVec3(PxVec3(3.0f,7.0f,11.0f)),
		Vec3V_From_PxVec3(PxVec3(4.0f,8.0f,13.0f)));

	if(!_VecMathTests::allElementsEqualVec3V(M33MulV3(a,v),Vec3V_From_PxVec3(aTimesV)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(M33TrnspsMulV3(a,v),Vec3V_From_PxVec3(aTransposeTimesV)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualMat33V(M33MulM33(a,b),aTimesB))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualMat33V(M33Add(a,b),aPlusB))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualMat33V(M33Trnsps(a),aTranspose))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(M33MulV3AddV3(a,v,tr), Vec3V_From_PxVec3(aTimesVPlusTr)))
	{
		return false;
	}

	return true;
}

bool Mat34VTestFunctions()
{
	const PX_ALIGN(16, PxVec3 faCol0)=PxVec3(2.0f,3.0f,4.0f);
	const PX_ALIGN(16, PxVec3 faCol1)=PxVec3(6.0f,7.0f,8.0f);
	const PX_ALIGN(16, PxVec3 faCol2)=PxVec3(10.0f,11.0f,13.0f);
	const PX_ALIGN(16, PxVec3 faCol3)=PxVec3(1.0f,2.0f,3.0f);
	Mat34V a;
	a.col0=Vec3V_From_PxVec3(faCol0);
	a.col1=Vec3V_From_PxVec3(faCol1);
	a.col2=Vec3V_From_PxVec3(faCol2);
	a.col3=Vec3V_From_PxVec3(faCol3);
	const PX_ALIGN(16, PxVec3 fbCol0)=PxVec3(1.0f,2.0f,3.0f);
	const PX_ALIGN(16, PxVec3 fbCol1)=PxVec3(4.0f,5.0f,6.0f);
	const PX_ALIGN(16, PxVec3 fbCol2)=PxVec3(7.0f,8.0f,9.0f);
	const PxVec3 fbCol3 =PxVec3(-1.0f,-2.0f,-3.0f);
	Mat34V b;
	b.col0=Vec3V_From_PxVec3(fbCol0);
	b.col1=Vec3V_From_PxVec3(fbCol1);
	b.col2=Vec3V_From_PxVec3(fbCol2);
	b.col3=Vec3V_From_PxVec3(fbCol3);

	const PX_ALIGN(16, PxVec3 fv)=PxVec3(1.0f,2.0f,3.0f);
	const Vec3V v=Vec3V_From_PxVec3(fv);

	const PxVec3 aTimesV(45.0f,52.0f,62.0f);
	const PxVec3 a3x3TimesV(44.0f,50.0f,59.0f);
	const PxVec3 a3x3TransposeTimesV(20.0f,44.0f,71.0f);

	const Mat33V a3x3TimesB(
		Vec3V_From_PxVec3(PxVec3(44.0f,50.0f,59.0f)),
		Vec3V_From_PxVec3(PxVec3(98.0f,113.0f,134.0f)),
		Vec3V_From_PxVec3(PxVec3(152.0f,176.0f,209.0f)));
	const Mat34V aTimesB(
		Vec3V_From_PxVec3(PxVec3(44.0f,50.0f,59.0f)),
		Vec3V_From_PxVec3(PxVec3(98.0f,113.0f,134.0f)),
		Vec3V_From_PxVec3(PxVec3(152.0f,176.0f,209.0f)),
		Vec3V_From_PxVec3(PxVec3(-43.0f,-48.0f,-56.0f)));
	const Mat34V aPlusB(
		Vec3V_From_PxVec3(PxVec3(3.0f,5.0f,7.0f)),
		Vec3V_From_PxVec3(PxVec3(10.0f,12.0f,14.0f)),
		Vec3V_From_PxVec3(PxVec3(17.0f,19.0f,22.0f)),
		Vec3V_From_PxVec3(PxVec3(0.0f,0.0f,0.0f)));
	const Mat34V id(
		Vec3V_From_PxVec3(PxVec3(1.0f,0.0f,0.0f)),
		Vec3V_From_PxVec3(PxVec3(0.0f,1.0f,0.0f)),
		Vec3V_From_PxVec3(PxVec3(0.0f,0.0f,1.0f)),
		Vec3V_From_PxVec3(PxVec3(0.0f,0.0f,0.0f)));
	const Mat33V aTranspose(
		Vec3V_From_PxVec3(PxVec3(2.0f,6.0f,10.0f)),
		Vec3V_From_PxVec3(PxVec3(3.0f,7.0f,11.0f)),
		Vec3V_From_PxVec3(PxVec3(4.0f,8.0f,13.0f)));
	const Mat33V a3x3Transpose(
		Vec3V_From_PxVec3(PxVec3(2.0f,6.0f,10.0f)),
		Vec3V_From_PxVec3(PxVec3(3.0f,7.0f,11.0f)),
		Vec3V_From_PxVec3(PxVec3(4.0f,8.0f,13.0f)));

	if(!_VecMathTests::allElementsEqualVec3V(M34MulV3(a,v),Vec3V_From_PxVec3(aTimesV)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(M34Mul33V3(a,v),Vec3V_From_PxVec3(a3x3TimesV)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec3V(M34TrnspsMul33V3(a,v),Vec3V_From_PxVec3(a3x3TransposeTimesV)))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualMat34V(M34MulM34(a,b),aTimesB))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualMat33V(M34Mul33MM34(a,b),a3x3TimesB))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualMat34V(M34Add(a,b),aPlusB))
	{
		return false;
	}

	if(!_VecMathTests::allElementsEqualMat33V(M34Trnsps33(a),a3x3Transpose))
	{
		return false;
	}

	return true;
}

bool Mat44VTestFunctions()
{
	const PX_ALIGN(16, PxF32 faCol0[4]) ={2.0f,2.0f,4.0f,5.0f};
	const PX_ALIGN(16, PxF32 faCol1[4])={6.0f,7.0f,8.0f,9.0f};
	const PX_ALIGN(16, PxF32 faCol2[4])={10.0f,11.0f,13.0f,14.0f};
	const PX_ALIGN(16, PxF32 faCol3[4])={15.0f,16.0f,17.0f,17.0f};
	Mat44V a;
	a.col0=Vec4V_From_F32Array_Aligned(faCol0);
	a.col1=Vec4V_From_F32Array_Aligned(faCol1);
	a.col2=Vec4V_From_F32Array_Aligned(faCol2);
	a.col3=Vec4V_From_F32Array_Aligned(faCol3);
	const PX_ALIGN(16, PxF32 fbCol0[4])={1.0f,2.0f,3.0f,2.0f};
	const PX_ALIGN(16, PxF32 fbCol1[4])={4.0f,5.0f,6.0f,2.0f};
	const PX_ALIGN(16, PxF32 fbCol2[4])={7.0f,8.0f,9.0f,1.0f};
	const PX_ALIGN(16, PxF32 fbCol3[4])={-5.0f,-6.0f,-5.0f,2.0f};
	Mat44V b;
	b.col0=Vec4V_From_F32Array_Aligned(fbCol0);
	b.col1=Vec4V_From_F32Array_Aligned(fbCol1);
	b.col2=Vec4V_From_F32Array_Aligned(fbCol2);
	b.col3=Vec4V_From_F32Array_Aligned(fbCol3);

	const PX_ALIGN(16, PxF32 fv[4])={1.0f,2.0f,3.0f,4.0f};
	const Vec4V v=Vec4V_From_F32Array_Aligned(fv);

	const PX_ALIGN(16, PxF32 fATimesV[4])={104.0f,113.0f,127.0f,133.0f};
	const Vec4V aTimesV=Vec4V_From_F32Array_Aligned(fATimesV);

	const PX_ALIGN(16, PxF32 fATransposeTimesV[4])={38.0f,80.0f,127.0f,166.0f};
	const Vec4V aTransposeTimesV=Vec4V_From_F32Array_Aligned(fATransposeTimesV);

	const PX_ALIGN(16, PxF32 fATimesBCol0[4])={74.0f,81.0f,93.0f,99.0f};
	const PX_ALIGN(16, PxF32 fATimesBCol1[4])={128.0f,141.0f,168.0f,183.0f};
	const PX_ALIGN(16, PxF32 fATimesBCol2[4])={167.0f,185.0f,226.0f,250.0f};
	const PX_ALIGN(16, PxF32 fATimesBCol3[4])={-66.0f,-75.0f,-99.0f,-115.0f};
	const Mat44V aTimesB(
		Vec4V_From_F32Array_Aligned(fATimesBCol0),
		Vec4V_From_F32Array_Aligned(fATimesBCol1),
		Vec4V_From_F32Array_Aligned(fATimesBCol2),
		Vec4V_From_F32Array_Aligned(fATimesBCol3));
	const PX_ALIGN(16, PxF32 fAPlusCol0[4])={3.0f,4.0f,7.0f,7.0f};
	const PX_ALIGN(16, PxF32 fAPlusCol1[4])={10.0f,12.0f,14.0f,11.0f};
	const PX_ALIGN(16, PxF32 fAPlusCol2[4])={17.0f,19.0f,22.0f,15.0f};
	const PX_ALIGN(16, PxF32 fAPlusCol3[4])={10.0f,10.0f,12.0f,19.0f};
	const Mat44V aPlusB(
		Vec4V_From_F32Array_Aligned(fAPlusCol0),
		Vec4V_From_F32Array_Aligned(fAPlusCol1),
		Vec4V_From_F32Array_Aligned(fAPlusCol2),
		Vec4V_From_F32Array_Aligned(fAPlusCol3));

	const Mat44V id(
		V4UnitX(),
		V4UnitY(),
		V4UnitZ(),
		V4UnitW());

	const PX_ALIGN(16, PxF32 faTrCol0[4]) ={2.0f,6.0f,10.0f,15.0f};
	const PX_ALIGN(16, PxF32 faTrCol1[4])={2.0f,7.0f,11.0f,16.0f};
	const PX_ALIGN(16, PxF32 faTrCol2[4])={4.0f,8.0f,13.0f,17.0f};
	const PX_ALIGN(16, PxF32 faTrCol3[4])={5.0f,9.0f,14.0f,17.0f};
	Mat44V aTr;
	aTr.col0=Vec4V_From_F32Array_Aligned(faTrCol0);
	aTr.col1=Vec4V_From_F32Array_Aligned(faTrCol1);
	aTr.col2=Vec4V_From_F32Array_Aligned(faTrCol2);
	aTr.col3=Vec4V_From_F32Array_Aligned(faTrCol3);

	if(!_VecMathTests::allElementsEqualVec4V(M44MulV4(a,v),aTimesV))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualVec4V(M44TrnspsMulV4(a,v),aTransposeTimesV))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualMat44V(M44MulM44(a,b),aTimesB))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualMat44V(M44Add(a,b),aPlusB))
	{
		return false;
	}
	if(!_VecMathTests::allElementsNearEqualMat44V(M44MulM44(a,M44Inverse(a)),id))
	{
		return false;
	}
	if(!_VecMathTests::allElementsEqualMat44V(M44Trnsps(a),aTr))
	{
		return false;
	}

	return true;
}


#endif //PX_PHYSICS_COMMON_VECMATH_AOS_TEST
