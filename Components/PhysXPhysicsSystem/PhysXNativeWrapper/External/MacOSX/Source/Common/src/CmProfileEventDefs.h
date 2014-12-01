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

#define THERE_IS_NO_INCLUDE_GUARD_HERE_FOR_A_REASON

// This file should only be included by CmEventProfiler.h. It is included there multiple times

#define PX_PROFILE_TASK_PRIORITY Detail
// simulation task graph - mostly coarse function-level profiling seems preferable, since
// the aggregation of functionality into tasks isn't very insightful unless you're trying
// to debug scheduling issues

PX_PROFILE_BEGIN_SUBSYSTEM( SimTask)
PX_PROFILE_EVENT(SimTask, Anonymous, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, ScSceneExecution, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, ScSceneCompletion, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, ScPreBroadPhase, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, ScBroadPhase, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, ScPostBroadPhase, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, ScPreRigidBodyNarrowPhase, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, ScRigidBodyNarrowPhase, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, ScPostRigidBodyNarrowPhase, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, ScTransformVaultUpdate, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, ScRigidBodySolver, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, ScPostSolver, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, ScCCDBroadPhase, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, ScCCDBroadPhaseComplete, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, ScCCDSinglePass, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, ScCCDMultiPass, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, ScPostCCDPass, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, ScSceneFinalization, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxsPostCCDSweep, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxsPostCCDAdvance, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxsPostCCDDepenetrate, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxsDynamicsMerge, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxsDynamicsSolver, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxsAtomIntegration, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxsParallelSolve, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxsAABBManagerFinalize, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxsConstraintPartitioning, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxsContextNarrowPhase, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxsContextNarrowPhasePrepare, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxsContextNarrowPhaseMerge, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxsContextSweep, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxsContextSweepMerge, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxsFluidCollision, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxsFluidDynamics, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxsFluidCollisionMerge, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, ScParticleSystemCollisionUpdate, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, ScParticleSystemPostShapesUpdate, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, ScClothPreprocessing, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxgSceneGpu_updateRigidMirroring, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxgParticleSystemSim_packetShapesUpdate, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxgParticleSystemSim_collisionPreparation, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxgParticleSystemBatcher_startParticlePipeline, PX_PROFILE_TASK_PRIORITY)
PX_PROFILE_EVENT(SimTask, PxgParticleSystemBatcher_cudaCompletion, PX_PROFILE_TASK_PRIORITY)

PX_PROFILE_END_SUBSYSTEM( SimTask )

PX_PROFILE_BEGIN_SUBSYSTEM(SimAPI)

PX_PROFILE_EVENT(SimAPI, pvdFrameStart, Coarse)

PX_PROFILE_EVENT(SimAPI, simulate, Coarse)
PX_PROFILE_EVENT(SimAPI, checkResults, Coarse)
PX_PROFILE_EVENT(SimAPI, fetchResults, Coarse)

PX_PROFILE_EVENT(SimAPI, addActor, Medium)
PX_PROFILE_EVENT(SimAPI, removeActor, Medium)
PX_PROFILE_EVENT(SimAPI, addAggregate, Medium)
PX_PROFILE_EVENT(SimAPI, removeAggregate, Medium)

#define PX_PROFILE_ADDREMOVE_DETAIL_PRIORITY Never
PX_PROFILE_EVENT(SimAPI, addActorToSim, PX_PROFILE_ADDREMOVE_DETAIL_PRIORITY)
PX_PROFILE_EVENT(SimAPI, removeActorFromSim, PX_PROFILE_ADDREMOVE_DETAIL_PRIORITY)
PX_PROFILE_EVENT(SimAPI, addShapesToSim, PX_PROFILE_ADDREMOVE_DETAIL_PRIORITY)
PX_PROFILE_EVENT(SimAPI, removeShapesFromSim, PX_PROFILE_ADDREMOVE_DETAIL_PRIORITY)
PX_PROFILE_EVENT(SimAPI, addShapesToSQ, PX_PROFILE_ADDREMOVE_DETAIL_PRIORITY)
PX_PROFILE_EVENT(SimAPI, removeShapesFromSQ, PX_PROFILE_ADDREMOVE_DETAIL_PRIORITY)
PX_PROFILE_EVENT(SimAPI, simAddShapeToBroadPhase, PX_PROFILE_ADDREMOVE_DETAIL_PRIORITY)
PX_PROFILE_EVENT(SimAPI, findAndReplaceWithLast, PX_PROFILE_ADDREMOVE_DETAIL_PRIORITY)
PX_PROFILE_END_SUBSYSTEM(SimAPI)


PX_PROFILE_BEGIN_SUBSYSTEM(Sim)

// scene initialization

PX_PROFILE_EVENT(Sim, updateShaders, Coarse)
PX_PROFILE_EVENT(Sim, taskFrameworkSetup, Coarse)
PX_PROFILE_EVENT(Sim, visualize, Coarse)
PX_PROFILE_EVENT(Sim, queueTasks, Coarse)
PX_PROFILE_EVENT(Sim, stepSetup, Coarse)

// broad phase 
PX_PROFILE_EVENT(Sim, startBroadPhase, Coarse)
PX_PROFILE_EVENT(Sim, updateVolumes, Coarse)
PX_PROFILE_EVENT(Sim, broadPhase, Coarse)

// handle broad phase outputs
PX_PROFILE_EVENT(Sim, processNewOverlaps, Coarse)
PX_PROFILE_EVENT(Sim, processLostOverlaps, Coarse)
PX_PROFILE_EVENT(Sim, generateIslands, Coarse)

// handle narrow phase
PX_PROFILE_EVENT(Sim, processTriggers, Coarse)
PX_PROFILE_EVENT(Sim, queueNarrowPhase, Coarse)
PX_PROFILE_EVENT(Sim, narrowPhase, Coarse)
PX_PROFILE_EVENT(Sim, narrowPhaseMerge, Coarse)
PX_PROFILE_EVENT(Sim, finishModifiablePairs, Coarse)

// RB Dynamics solver

PX_PROFILE_EVENT(Sim, updateForces, Coarse)
PX_PROFILE_EVENT(Sim, refineIslands, Coarse)
PX_PROFILE_EVENT(Sim, queueSolverTasks, Coarse)
PX_PROFILE_EVENT(Sim, solveGroup, Coarse)
PX_PROFILE_EVENT(Sim, solverMerge, Coarse)
PX_PROFILE_EVENT(Sim, solver, Coarse)

PX_PROFILE_EVENT(Sim, updateKinematics, Medium)
PX_PROFILE_EVENT(Sim, contactThresholds, Medium)
PX_PROFILE_EVENT(Sim, updateVelocities, Medium)
PX_PROFILE_EVENT(Sim, runConstraintShaders, Medium)
PX_PROFILE_EVENT(Sim, finalizeContacts, Medium)
PX_PROFILE_EVENT(Sim, updatePositions, Medium)


// CCD
PX_PROFILE_EVENT(Sim, projectContacts, Coarse)
PX_PROFILE_EVENT(Sim, ccdSweep, Coarse)
PX_PROFILE_EVENT(Sim, ccdSweepMerge, Coarse)
PX_PROFILE_EVENT(Sim, ccdPair, Coarse)
PX_PROFILE_EVENT(Sim, ccdIsland, Coarse)

// reports & finalization
PX_PROFILE_EVENT(Sim, getSimEvents, Coarse)
PX_PROFILE_EVENT(Sim, syncBodies, Coarse)
PX_PROFILE_EVENT(Sim, projectConstraints, Coarse)
PX_PROFILE_EVENT(Sim, checkConstraintBreakage, Coarse)
PX_PROFILE_EVENT(Sim, processCallbacks, Coarse)

PX_PROFILE_EVENT(Sim, firePreSyncCallbacks, Coarse)
PX_PROFILE_EVENT(Sim, updatePruningTrees, Coarse)
PX_PROFILE_EVENT(Sim, firePostSyncCallbacks, Coarse)
PX_PROFILE_EVENT(Sim, syncState, Coarse)
PX_PROFILE_EVENT(Sim, buildActiveTransforms, Coarse)


PX_PROFILE_EVENT(Sim, sceneFinalization, Coarse)
PX_PROFILE_EVENT(Sim, ParticleSystemSim_startStep, Coarse)
PX_PROFILE_EVENT(Sim, ParticleSystemSim_endStep, Coarse)
PX_PROFILE_EVENT(Sim, ParticleSystemSim_shapesUpdateProcessing, Coarse)
PX_PROFILE_EVENT(Sim, ParticleSystemSim_updateCollision, Coarse)
PX_PROFILE_END_SUBSYSTEM(Sim)


#define PX_PROFILE_SCENEQUERY_PRIORITY Detail
PX_PROFILE_BEGIN_SUBSYSTEM(SceneQuery)
PX_PROFILE_EVENT(SceneQuery, raycastAny, PX_PROFILE_SCENEQUERY_PRIORITY)
PX_PROFILE_EVENT(SceneQuery, raycastSingle, PX_PROFILE_SCENEQUERY_PRIORITY)
PX_PROFILE_EVENT(SceneQuery, raycastMultiple, PX_PROFILE_SCENEQUERY_PRIORITY)
PX_PROFILE_EVENT(SceneQuery, overlapMultiple, PX_PROFILE_SCENEQUERY_PRIORITY)
PX_PROFILE_EVENT(SceneQuery, sweepAny, PX_PROFILE_SCENEQUERY_PRIORITY)
PX_PROFILE_EVENT(SceneQuery, sweepAnyList, PX_PROFILE_SCENEQUERY_PRIORITY)
PX_PROFILE_EVENT(SceneQuery, sweepSingle, PX_PROFILE_SCENEQUERY_PRIORITY)
PX_PROFILE_EVENT(SceneQuery, sweepSingleList, PX_PROFILE_SCENEQUERY_PRIORITY)
PX_PROFILE_EVENT(SceneQuery, sweepMultiple, PX_PROFILE_SCENEQUERY_PRIORITY)
PX_PROFILE_EVENT(SceneQuery, sweepMultipleList, PX_PROFILE_SCENEQUERY_PRIORITY)
PX_PROFILE_EVENT(SceneQuery, flushUpdates, PX_PROFILE_SCENEQUERY_PRIORITY)
PX_PROFILE_END_SUBSYSTEM(SceneQuery)



#define PX_PROFILE_ARTICULATION_PRIORITY Medium
PX_PROFILE_BEGIN_SUBSYSTEM(Articulations)
PX_PROFILE_EVENT(Articulations, setup, PX_PROFILE_ARTICULATION_PRIORITY)
PX_PROFILE_EVENT(Articulations, setupProject, PX_PROFILE_ARTICULATION_PRIORITY)
PX_PROFILE_EVENT(Articulations, prepareFsData, PX_PROFILE_ARTICULATION_PRIORITY)
PX_PROFILE_EVENT(Articulations, setupDrives, PX_PROFILE_ARTICULATION_PRIORITY)
PX_PROFILE_EVENT(Articulations, jointLoads, PX_PROFILE_ARTICULATION_PRIORITY)
PX_PROFILE_EVENT(Articulations, propagateDrivenInertia, PX_PROFILE_ARTICULATION_PRIORITY)
PX_PROFILE_EVENT(Articulations, computeJointDrives, PX_PROFILE_ARTICULATION_PRIORITY)
PX_PROFILE_EVENT(Articulations, applyJointDrives, PX_PROFILE_ARTICULATION_PRIORITY)
PX_PROFILE_EVENT(Articulations, applyExternalImpulses, PX_PROFILE_ARTICULATION_PRIORITY)
PX_PROFILE_EVENT(Articulations, setupConstraints, PX_PROFILE_ARTICULATION_PRIORITY)
PX_PROFILE_EVENT(Articulations, integrate, PX_PROFILE_ARTICULATION_PRIORITY)
PX_PROFILE_END_SUBSYSTEM(Articulations)



PX_PROFILE_BEGIN_SUBSYSTEM(PVD)
PX_PROFILE_EVENT(PVD, updateContacts, Medium)
PX_PROFILE_EVENT(PVD, updateDynamicBodies, Medium)
PX_PROFILE_EVENT(PVD, updateJoints, Medium)
PX_PROFILE_EVENT(PVD, updateCloths, Medium)
PX_PROFILE_EVENT(PVD, updateSleeping, Medium)
PX_PROFILE_EVENT(PVD, updatePariclesAndFluids, Medium)
PX_PROFILE_EVENT(PVD, sceneUpdate, Medium)
PX_PROFILE_EVENT(PVD, CREATE_PVD_INSTANCE, Medium)
PX_PROFILE_EVENT(PVD, RELEASE_PVD_INSTANCE, Medium)
PX_PROFILE_EVENT(PVD, UPDATE_PVD_PROPERTIES, Medium)
PX_PROFILE_EVENT(PVD, SEND_PVD_ARRAYS, Medium)
PX_PROFILE_END_SUBSYSTEM(PVD)


// Old SDK Profile Zones 
PX_PROFILE_BEGIN_SUBSYSTEM(Legacy)

PX_PROFILE_EVENT(Legacy, PXS_PROFILE_ZONE_FL_PU, Detail)
PX_PROFILE_EVENT(Legacy, PXS_PROFILE_ZONE_FL_PU_FIN, Detail)
PX_PROFILE_EVENT(Legacy, PXS_PROFILE_ZONE_FL_DYN, Detail)
PX_PROFILE_EVENT(Legacy, PXS_PROFILE_ZONE_FL_DYN_MERGE_DENSITY, Detail)
PX_PROFILE_EVENT(Legacy, PXS_PROFILE_ZONE_FL_DYN_MERGE_FORCE, Detail)
PX_PROFILE_EVENT(Legacy, PXS_PROFILE_ZONE_FL_DYN_FIN, Detail)
PX_PROFILE_EVENT(Legacy, PXS_PROFILE_ZONE_FL_COLL, Detail)
PX_PROFILE_EVENT(Legacy, PXS_PROFILE_ZONE_FL_COLL_FIN, Detail)
PX_PROFILE_EVENT(Legacy, PXS_PROFILE_ZONE_FL_HSH_SEC, Detail)
PX_PROFILE_END_SUBSYSTEM(Legacy)



#undef THERE_IS_NO_INCLUDE_GUARD_HERE_FOR_A_REASON
