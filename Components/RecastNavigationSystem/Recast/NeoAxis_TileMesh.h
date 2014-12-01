// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
// based on the original RecastSampleTileMesh.h by Mikko Mononen

#ifndef RECASTSAMPLETILEMESH_H
#define RECASTSAMPLETILEMESH_H

//#include "Sample.h"
#include "SampleInterfaces.h"
#include "DetourNavMesh.h"
#include "Recast.h"
#include "ChunkyTriMesh.h"

class NeoAxis_TileMesh
{
protected:
	bool m_keepInterResults;
	bool m_buildAll;

	unsigned char* m_triareas;
	rcHeightfield* m_solid;
	rcCompactHeightfield* m_chf;
	rcContourSet* m_cset;
	rcPolyMesh* m_pmesh;
	rcPolyMeshDetail* m_dmesh;
	rcConfig m_cfg;	
	
	int m_maxTiles;
	int m_maxPolysPerTile;
	
	float m_tileBmin[3];
	float m_tileBmax[3];
	float m_tileBuildTime;
	float m_tileMemUsage;//in floats in MB?!
	int m_tileTriCount;

	unsigned char* buildTileMesh(const int tx, const int ty, const float* bmin, const float* bmax, int& dataSize);
	
public:
	NeoAxis_TileMesh();
	virtual ~NeoAxis_TileMesh();

	bool loadNavMesh(void* data, int dataSize);
	void saveNavMesh(const dtNavMesh* mesh, void** outData, int* outDataSize);

	float m_tileSize;

	float m_bmin[3];
	float m_bmax[3];

	void calculateSize();
	void fixMinMaxCorners();
	virtual bool init();
	
	void getTilePos(const float* pos, int& tx, int& ty);

	void getMaximums(int* maxTiles, int* maxPolysPerTile);

	void buildTile(const float* pos);
	void removeTile(const float* pos);
	void buildAllTiles();
	void removeAllTiles();

	void cleanup();

	//------------------
	//from the sample.h
	//------------------
	class InputGeom* m_geom;
	class dtNavMesh* m_navMesh;
	class dtNavMeshQuery* m_navQuery;
	unsigned char m_navMeshDrawFlags;

	enum PolyAreas
	{
		POLYAREA_GROUND,
		POLYAREA_ROUGH,
		POLYAREA_SWAMP,
		POLYAREA_WATER,
		POLYAREA_ROAD,
		POLYAREA_DOOR,
		POLYAREA_JUMP,
	};
	enum PolyFlags
	{
		POLYFLAGS_WALK = 0x01,		///< Ability to walk (ground, road)
		POLYFLAGS_SWIM = 0x02,		///< Ability to swim (water).
		POLYFLAGS_DOOR = 0x04,		///< Ability to move through doors.
		POLYFLAGS_JUMP = 0x08,		///< Ability to jump.
		POLYFLAGS_ALL = 0xffff		///< All abilities.
	};

	float m_cellSize;
	float m_cellHeight;
	float m_agentHeight;
	float m_agentRadius;
	float m_agentMaxClimb;
	float m_agentMaxSlope;
	float m_regionMinSize;
	float m_regionMergeSize;
	bool m_monotonePartitioning;
	float m_edgeMaxLen;
	float m_edgeMaxError;
	float m_vertsPerPoly;
	float m_detailSampleDist;
	float m_detailSampleMaxError;

	BuildContext* m_ctx;
	void setContext(BuildContext* ctx) { m_ctx = ctx; }

	virtual class InputGeom* getInputGeom() { return m_geom; }
	virtual class dtNavMesh* getNavMesh() { return m_navMesh; }
	virtual class dtNavMeshQuery* getNavMeshQuery() { return m_navQuery; }
	virtual float getAgentRadius() { return m_agentRadius; }
	virtual float getAgentHeight() { return m_agentHeight; }
	virtual float getAgentClimb() { return m_agentMaxClimb; }
	virtual const float* getBoundsMin();
	virtual const float* getBoundsMax();

	void resetCommonSettings();
};


#endif
