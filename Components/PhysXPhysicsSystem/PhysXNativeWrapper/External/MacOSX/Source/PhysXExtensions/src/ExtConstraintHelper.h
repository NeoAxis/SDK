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


#ifndef NP_CONSTRAINT_HELPER_H
#define NP_CONSTRAINT_HELPER_H

#include "PxTransform.h"
#include "PxJointLimit.h"
#include "PxMat33.h"
#include "ExtJoint.h"

namespace physx
{
namespace Ext
{
	namespace joint
	{
		PX_INLINE void computeDerived(const JointData& data, 
									  const PxTransform& bA2w, 
									  const PxTransform& bB2w,
									  PxTransform& cA2w,
									  PxTransform& cB2w,
									  PxTransform& cB2cA)
		{
			PX_ASSERT(bA2w.isValid() && bB2w.isValid());

			cA2w = bA2w.transform(data.c2b[0]);
			cB2w = bB2w.transform(data.c2b[1]);

			if(cA2w.q.dot(cB2w.q)<0)	// minimum error quat
				cB2w.q = -cB2w.q;

			cB2cA = cA2w.transformInv(cB2w);

			PX_ASSERT(cA2w.isValid() && cB2w.isValid() && cB2cA.isValid());
		}

		PX_INLINE PxVec3 truncateLinear(const PxVec3& in, PxReal tolerance, bool& truncated)
		{		
			PxReal m = in.magnitudeSquared();
			truncated = m>tolerance * tolerance;
			return truncated ? in * PxRecipSqrt(m) * tolerance : in;
		}

		PX_INLINE PxQuat truncateAngular(const PxQuat& in, PxReal sinHalfTol, PxReal cosHalfTol, bool& truncated)
		{
			truncated = false;

			if(sinHalfTol > 0.9999f)	// fixes numerical tolerance issue of projecting because quat is not exactly normalized
				return in;

			PxQuat q = in.w>=0 ? in : -in;
					
			const PxVec3& im = q.getImaginaryPart();
			PxReal m = im.magnitudeSquared();
			truncated =  m>sinHalfTol*sinHalfTol;
			if(!truncated)
				return in;

			PxVec3 outV = im * sinHalfTol * PxRecipSqrt(m);			
			return PxQuat(outV.x, outV.y, outV.z, cosHalfTol);
		}

		PX_FORCE_INLINE void projectTransforms(PxTransform& bA2w, PxTransform& bB2w, 
											   const PxTransform& cA2w, const PxTransform& cB2w, 
											   const PxTransform& cB2cA, const JointData& data, bool projectToA)
		{
			PX_ASSERT(cB2cA.isValid());

			// normalization here is unfortunate: long chains of projected constraints can result in
			// accumulation of error in the quaternion which eventually leaves the quaternion
			// magnitude outside the validation range. The approach here is slightly overconservative
			// in that we could just normalize the quaternions which are out of range, but since we
			// regard projection as an occasional edge case it shouldn't be perf-sensitive, and
			// this way we maintain the invariant (also maintained by the dynamics integrator) that
			// body quats are properly normalized up to FP error.

			if (projectToA)
			{
				bB2w = cA2w.transform(cB2cA.transform(data.c2b[1].getInverse()));
				bB2w.q.normalize();
			}
			else
			{
				bA2w = cB2w.transform(cB2cA.transformInv(data.c2b[0].getInverse()));
				bA2w.q.normalize();
			}


			PX_ASSERT(bA2w.isValid());
			PX_ASSERT(bB2w.isValid());
		}



		PX_INLINE void computeJacobianAxes(PxVec3 row[3], const PxQuat& qa, const PxQuat& qb)
		{
			// Compute jacobian matrix for (qa* qb)  [[* means conjugate in this expr]]
			// d/dt (qa* qb) = 1/2 L(qa*) R(qb) (omega_b - omega_a)
			// result is L(qa*) R(qb), where L(q) and R(q) are left/right q multiply matrix

			PxReal wa = qa.w, wb = qb.w;
			const PxVec3 va(qa.x,qa.y,qa.z), vb(qb.x,qb.y,qb.z);

			const PxVec3 c = vb*wa + va*wb;
			const PxReal d = wa*wb - va.dot(vb);

			row[0] = va * vb.x + vb * va.x + PxVec3(d,     c.z, -c.y);
			row[1] = va * vb.y + vb * va.y + PxVec3(-c.z,  d,    c.x);
			row[2] = va * vb.z + vb * va.z + PxVec3(c.y,   -c.x,   d);
		}

		class ConstraintHelper
		{
			Px1DConstraint* mConstraints;
			Px1DConstraint* mCurrent;
			PxVec3 mRa, mRb;

		public:
			ConstraintHelper(Px1DConstraint* c, const PxVec3& ra, const PxVec3& rb)
				: mConstraints(c), mCurrent(c), mRa(ra), mRb(rb)	{}

			// hard linear & angular
			void linear(const PxVec3& axis, PxReal posErr)
			{
				Px1DConstraint *c = linear(axis, posErr, 256);
				c->flags |= Px1DConstraintFlag::eOUTPUT_FORCE;
			}

			void angular(const PxVec3& axis, PxReal posErr)
			{
				Px1DConstraint *c = angular(axis, posErr, 256);
				c->flags |= Px1DConstraintFlag::eOUTPUT_FORCE;
			}

			// limited linear & angular
			void linear(const PxVec3& axis, PxReal error, const PxJointLimitParameters& limit)
			{
				addLimit(linear(axis,error, 0),limit);
			}

			void angular(const PxVec3& axis, PxReal error, const PxJointLimitParameters& limit)
			{
				addLimit(angular(axis,error, 0),limit);
			}

			// driven linear & angular

			void linear(const PxVec3& axis, PxReal velTarget, PxReal error, const PxD6JointDrive& drive)
			{
				addDrive(linear(axis,error,0),velTarget,drive);
			}

			void angular(const PxVec3& axis, PxReal velTarget, PxReal error, const PxD6JointDrive& drive)
			{
				addDrive(angular(axis,error,0),velTarget,drive);
			}

			void linearLimitPair(PxReal ordinate, PxReal lower, PxReal upper, PxReal pad, const PxVec3& axis, const PxJointLimitParameters& limit)
			{
				if(ordinate < lower + pad)
					linear(-axis,-(lower - ordinate), limit);
				
				if(ordinate > upper - pad)
					linear(axis, (upper - ordinate), limit);
			}


			void halfAnglePair(PxReal halfAngle, PxReal lower, PxReal upper, PxReal pad, const PxVec3& axis, const PxJointLimitParameters& limit)
			{
				PX_ASSERT(lower<upper);
				if(halfAngle < lower+pad)
					angular(-axis, -(lower - halfAngle)*2,limit);
				if(halfAngle > upper-pad)
					angular(axis, (upper - halfAngle)*2, limit);
			}

			void quarterAnglePair(PxReal quarterAngle, PxReal lower, PxReal upper, PxReal pad, const PxVec3& axis, const PxJointLimitParameters& limit)
			{
				PX_ASSERT(lower<upper);
				if(quarterAngle < lower+pad)
					angular(-axis, -(lower - quarterAngle)*4,limit);
				if(quarterAngle > upper-pad)
					angular(axis, (upper - quarterAngle)*4, limit);
			}


			PxU32 getCount() { return PxU32(mCurrent - mConstraints); }

			void prepareLockedAxes(const PxQuat& qA, const PxQuat& qB, const PxVec3& cB2cAp, PxU32 lin, PxU32 ang)
			{
				Px1DConstraint* current = mCurrent;
				if(ang)
				{
					PxQuat qB2qA = qA.getConjugate() * qB;
					if(qB2qA.w<0)
						qB2qA = -qB2qA;

					PxVec3 row[3];
					computeJacobianAxes(row, qA, qB);
					PxVec3 imp = qB2qA.getImaginaryPart();
					if(ang&1) angular(row[0], -2.0f*imp.x);
					if(ang&2) angular(row[1], -2.0f*imp.y);
					if(ang&4) angular(row[2], -2.0f*imp.z);
				}

				if(lin)
				{
					PxMat33 axes(qA);
					if(lin&1) linear(axes[0], -cB2cAp[0]);
					if(lin&2) linear(axes[1], -cB2cAp[1]);
					if(lin&4) linear(axes[2], -cB2cAp[2]);
				}

				for(;current < mCurrent; current++)
					current->solveGroup = 256;
			}

			Px1DConstraint *getConstraintRow()
			{
				return mCurrent++;
			}

		private:
			Px1DConstraint* linear(const PxVec3& axis, PxReal posErr, PxU32 group)
			{
				Px1DConstraint* c = mCurrent++;

				c->linear0 = axis;					c->angular0	= mRa.cross(c->linear0);
				c->linear1 = axis;					c->angular1 = mRb.cross(c->linear1);
				PX_ASSERT(c->linear0.isFinite());
				PX_ASSERT(c->linear1.isFinite());
				PX_ASSERT(c->angular0.isFinite());
				PX_ASSERT(c->angular1.isFinite());

				c->geometricError	= posErr;		
				c->solveGroup		= group;

				return c;
			}

			Px1DConstraint* angular(const PxVec3& axis, PxReal posErr, PxU32 group)
			{
				Px1DConstraint* c = mCurrent++;

				c->linear0 = PxVec3(0);		c->angular0			= axis;
				c->linear1 = PxVec3(0);		c->angular1			= axis;

				c->geometricError	= posErr;
				c->solveGroup		= group;

				return c;
			}

			void addLimit(Px1DConstraint* c, const PxJointLimitParameters& limit)
			{
				c->minImpulse = 0;
				c->flags |= Px1DConstraintFlag::eOUTPUT_FORCE;

				c->restitution = limit.restitution;
				if(c->restitution>0)
					c->flags |= Px1DConstraintFlag::eRESTITUTION;

				c->spring = limit.spring;
				c->damping = limit.damping;

				if(c->spring>0 || c->damping>0) 
					c->flags |= Px1DConstraintFlag::eSPRING;
				else
					c->solveGroup = 257;

				if(c->geometricError>0)
					c->flags |= Px1DConstraintFlag::eKEEPBIAS;
			}

			void addDrive(Px1DConstraint* c, PxReal velTarget, const PxD6JointDrive& drive)
			{
				c->velocityTarget = velTarget;

				c->flags |= Px1DConstraintFlag::eSPRING;
				c->spring = drive.spring;
				c->damping = drive.damping;
				if(drive.flags & PxD6JointDriveFlag::eACCELERATION)
					c->flags |= Px1DConstraintFlag::eACCELERATION_SPRING;

				c->minImpulse = -drive.forceLimit;
				c->maxImpulse = drive.forceLimit;

				PX_ASSERT(c->linear0.isFinite());
				PX_ASSERT(c->angular0.isFinite());
			}
		};
	}
} // namespace

}

#endif
