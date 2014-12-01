// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
#include "precompiled.h"
#include "RecastWrapper.h"
#include "NeoAxis_TileMesh.h"
#include "InputGeom.h"
#include "DetourDebugDraw.h"
#include "DetourCommon.h"

#ifdef __APPLE_CC__
	#import <Carbon/Carbon.h>
#endif

/////////////////////////////////////////////////////////////////////////////////////////////////////////////

void Fatal(const char* text)
{
#ifdef PLATFORM_WINDOWS
	MessageBoxA(NULL, text, "Fatal", MB_OK | MB_ICONEXCLAMATION);
#elif defined(PLATFORM_MACOS)
	CFStringRef textRef = CFStringCreateWithCString(NULL, text, kCFStringEncodingUTF8);
	CFUserNotificationDisplayAlert(0, kCFUserNotificationStopAlertLevel, NULL, NULL, NULL, 
		CFSTR("Fatal"), textRef, CFSTR("OK"), NULL, NULL, NULL);
	CFRelease(textRef);
#elif defined(PLATFORM_ANDROID)
//!!!!!!dr
	char tempBuffer[4096];
	sprintf(tempBuffer, "Recast: Fatal: %s\n", text);
	__android_log_write(ANDROID_LOG_ERROR, "NeoAxis Engine", tempBuffer);
#endif
	exit(0);
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////

struct Vec3
{
	float x, y, z;
};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////

class RecastWorld
{
public:

	NeoAxis_TileMesh* tileMesh;
	InputGeom* inputGeometry;
	BuildContext ctx;

	int findPathPolyListSize;
	dtPolyRef* findPathPolyList;
	int findPathSmoothListSize;
	float* findPathSmoothList;

	int findPathSteerSize;
	float* findPathSteerPath;
	unsigned char* findPathSteerPathFlags;
	dtPolyRef* findPathSteerPathPolys;

	//

	RecastWorld();
	bool Initialize( Vec3 bmin, Vec3 bmax,
		float tileSize, float cellSize, float cellHeight,
		int minRegionSize, int mergeRegionSize, bool monotonePartitioning,
		float maxEdgeLength, float maxEdgeError, 
		int vertsPerPoly, float detailSampleDistance, float detailMaxSampleError, 
		float agentHeight, float agentRadius, float agentMaxClimb, float agentMaxSlope);
	void Destroy();
	bool NavQueryInit(int maxNodes);
	bool GetNavigationMesh(float** vertices, int* vertexCount);

	bool getSteerTarget(dtNavMeshQuery* navQuery, const float* startPos, const float* endPos,
		const float minTargetDist, const dtPolyRef* path, const int pathSize,
		float* steerPos, unsigned char& steerPosFlag, dtPolyRef& steerPosRef, int maxSteerPoints);
	bool FindPath( const Vec3& start, const Vec3& end, float stepSize, const Vec3& polygonPickExtents, 
		int maxPolygonPath, int maxSmoothPath, int maxSteerPoints, Vec3** outPath, int* outPathCount );

	void SetGeometry(float* vertices, int vertexCount, int* indices, int indexCount, int trianglesPerChunk);
};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////

EXPORT RecastWorld* Recast_Initialize( const Vec3& bmin, const Vec3& bmax,
	float tileSize, float cellSize, float cellHeight,
	int minRegionSize, int mergeRegionSize, bool monotonePartitioning,
	float maxEdgeLength, float maxEdgeError, 
	int vertsPerPoly, float detailSampleDistance, float detailMaxSampleError, 
	float agentHeight, float agentRadius, float agentMaxClimb, float agentMaxSlope)
{
	RecastWorld* world = new RecastWorld();
	if(!world->Initialize(
		bmin, bmax,
		tileSize, cellSize, cellHeight,
		minRegionSize, mergeRegionSize, monotonePartitioning,
		maxEdgeLength, maxEdgeError,
		vertsPerPoly, detailSampleDistance, detailMaxSampleError,
		agentHeight, agentRadius, agentMaxClimb, agentMaxSlope))
	{
		world->Destroy();
		delete world;
		return NULL;
	}
	return world;
}

EXPORT bool Recast_NavQueryInit(RecastWorld* world, int maxNodes)
{
	return world->NavQueryInit(maxNodes);
}

EXPORT void Recast_BuildAllTiles(RecastWorld* world)
{
	if (world->tileMesh)
		world->tileMesh->buildAllTiles();
}

EXPORT void Recast_DestroyAllTiles(RecastWorld* world)
{
	if (world->tileMesh)
		world->tileMesh->removeAllTiles();
}

EXPORT void Recast_Destroy(RecastWorld* world)
{
	world->Destroy();
	delete world;
}

EXPORT bool Recast_GetNavigationMesh(RecastWorld* world, float** vertices, int* vertexCount)
{
	return world->GetNavigationMesh(vertices, vertexCount);
}

EXPORT bool Recast_FindPath( RecastWorld* world, const Vec3& start, const Vec3& end, float stepSize, 
	const Vec3& polygonPickExtents, int maxPolygonPath, int maxSmoothPath, int maxSteerPoints, 
	Vec3** outPath, int* outPathCount )
{
	return world->FindPath(start, end, stepSize, polygonPickExtents, maxPolygonPath, maxSmoothPath, 
		maxSteerPoints, outPath, outPathCount );
}

EXPORT void Recast_FreeMemory(void* pointer)
{
	free(pointer);
}

EXPORT void Recast_GetSizes(RecastWorld* world, int* maxTiles, int* maxPolysPerTile)
{
	if (world->tileMesh)
		world->tileMesh->getMaximums(maxTiles, maxPolysPerTile);
}

EXPORT bool Recast_LoadNavMesh(RecastWorld* world, void* data, int dataSize)
{
	if(world->tileMesh && world->tileMesh->m_navMesh)
		return world->tileMesh->loadNavMesh(data, dataSize);
	else
		return false;
}

EXPORT void Recast_SaveNavMesh(RecastWorld* world, void** data, int* dataSize)
{
	*data = NULL;
	*dataSize = 0;
	if(world->tileMesh && world->tileMesh->m_navMesh)
		return world->tileMesh->saveNavMesh(world->tileMesh->m_navMesh, data, dataSize);
	return;
}

EXPORT void Recast_BuildTile(RecastWorld* world, const Vec3& position)
{
	if(world->tileMesh)
		world->tileMesh->buildTile((float*)&position);
}

EXPORT void Recast_RemoveTile(RecastWorld* world, const Vec3& position)
{
	if (world->tileMesh)
		world->tileMesh->removeTile((float*)&position);
}

EXPORT void Recast_SetGeometry(RecastWorld* world, float* vertices, int vertexCount, int* indices, 
	int indexCount, int trianglesPerChunk)
{
	world->SetGeometry(vertices, vertexCount, indices, indexCount, trianglesPerChunk);
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////

RecastWorld::RecastWorld()
{
	tileMesh = NULL;
	inputGeometry = NULL;
	findPathPolyListSize = 0;
	findPathPolyList = NULL;
	findPathSmoothListSize = 0;
	findPathSmoothList = NULL;

	findPathSteerSize = 0;
	findPathSteerPath = NULL;
	findPathSteerPathFlags = NULL;
	findPathSteerPathPolys = NULL;
}

bool RecastWorld::Initialize( Vec3 bmin, Vec3 bmax,
	float tileSize, float cellSize, float cellHeight,
	int minRegionSize, int mergeRegionSize, bool monotonePartitioning,
	float maxEdgeLength, float maxEdgeError, 
	int vertsPerPoly, float detailSampleDistance, float detailMaxSampleError, 
	float agentHeight, float agentRadius, float agentMaxClimb, float agentMaxSlope)
{
	tileMesh = new NeoAxis_TileMesh();

	rcVcopy(tileMesh->m_bmin, (float*)&bmin);
	rcVcopy(tileMesh->m_bmax, (float*)&bmax);

	//SodanKerjuu: get true minmax corners of the given bounding box (user can give false orientation)
	tileMesh->fixMinMaxCorners();

	tileMesh->m_tileSize = tileSize;
	tileMesh->m_cellSize = cellSize;
	tileMesh->m_cellHeight = cellHeight;

	tileMesh->m_regionMinSize = (float)minRegionSize;
	tileMesh->m_regionMergeSize = (float)mergeRegionSize;
	tileMesh->m_monotonePartitioning = monotonePartitioning;
	
	tileMesh->m_edgeMaxLen = maxEdgeLength;
	tileMesh->m_edgeMaxError = maxEdgeError;
	
	tileMesh->m_vertsPerPoly = (float)vertsPerPoly;
	tileMesh->m_detailSampleDist = detailSampleDistance;
	tileMesh->m_detailSampleMaxError = detailMaxSampleError;

	tileMesh->m_agentHeight = agentHeight;
	tileMesh->m_agentRadius = agentRadius;
	tileMesh->m_agentMaxClimb = agentMaxClimb;
	tileMesh->m_agentMaxSlope = agentMaxSlope;

	tileMesh->setContext(&ctx);

	//tile and polygon maximums
	tileMesh->calculateSize();
	
	return tileMesh->init();
}

bool RecastWorld::NavQueryInit(int maxNodes)
{
	dtStatus status = tileMesh->m_navQuery->init(tileMesh->m_navMesh, maxNodes, true);
	if (dtStatusFailed(status))
	{
		tileMesh->m_ctx->log(RC_LOG_ERROR, "buildTiledNavigation: Could not init Detour navmesh query");
		return false;
	}
	return true;
}

void RecastWorld::SetGeometry(float* vertices, int vertexCount, int* indices, int indexCount, 
	int trianglesPerChunk)
{
	//SodanKerjuu: need to delete previous or fills up memory really fast
	delete inputGeometry;
	tileMesh->cleanup();
	
	inputGeometry = new InputGeom();
	if(!inputGeometry->loadMesh(&ctx, vertices, vertexCount, indices, indexCount, trianglesPerChunk))
		Fatal("Recast: !geom->loadMesh(&ctx, path)");

	tileMesh->m_geom = inputGeometry;
}

class DebugDraw : public duDebugDraw
{
public:

	bool writingTriangles;
	//bool writingLines;

	std::vector<float> vertices;
	//std::vector<float> lines;

	//

	DebugDraw()
	{
		writingTriangles = false;
	}

	virtual void depthMask(bool state)
	{
	}

	virtual void texture(bool state)
	{
	}

	virtual void begin(duDebugDrawPrimitives prim, float size = 1.0f)
	{
		if(prim == DU_DRAW_TRIS)
			writingTriangles = true;
		//if(prim == DU_DRAW_LINES)
		//	writingLines = true;
	}

	virtual void vertex(const float* pos, unsigned int color)
	{
		if(writingTriangles)
		{
			vertices.push_back(pos[0]);
			vertices.push_back(pos[1]);
			vertices.push_back(pos[2]);
		}

		//if(writingLines)
		//{
		//	lines.push_back(pos[0]);
		//	lines.push_back(pos[1]);
		//	lines.push_back(pos[2]);
		//}
	}

	virtual void vertex(const float x, const float y, const float z, unsigned int color)
	{
	}

	virtual void vertex(const float* pos, unsigned int color, const float* uv)
	{
	}

	virtual void vertex(const float x, const float y, const float z, unsigned int color, const float u, const float v)
	{
	}

	virtual void end()
	{
		writingTriangles = false;
		//writingLines = false;
	}
};

bool RecastWorld::GetNavigationMesh(float** outVertices, int* outVertexCount)
{
	//SodanKerjuu: no need to fatal out, it's perfectly cool not having a navmesh
	if(tileMesh->m_navMesh == NULL)
		return false;
	if(tileMesh->m_navQuery == NULL)
		return false;

	DebugDraw debugDraw;
	duDebugDrawNavMeshWithClosedList(&debugDraw, *tileMesh->m_navMesh, *tileMesh->m_navQuery, 
		tileMesh->m_navMeshDrawFlags);

	if(debugDraw.vertices.size() != 0)
	{
		float* vertices = new float[ debugDraw.vertices.size() ];
		int vertexCount = (int)debugDraw.vertices.size() / 3;
		for(int n = 0; n < (int)debugDraw.vertices.size(); n++)
			vertices[n] = debugDraw.vertices[n];
		*outVertices = vertices;
		*outVertexCount = vertexCount;

		//float* lines = new float[ debugDraw.lines.size() ];
		//int lineCount = debugDraw.lines.size() / 2;
		//for(int n = 0; n < (int)debugDraw.lines.size(); n++)
		//	lines[n] = debugDraw.lines[n];
		//*outLines = lines;
		//*outLineCount = lineCount;

		return true;
	}

	*outVertices = NULL;
	*outVertexCount = NULL;
	//*outLines = NULL;
	//*outLineCount = NULL;
	return false;
}

void RecastWorld::Destroy()
{
	//!!!!!!leaks?

	findPathPolyListSize = 0;
	if(findPathPolyList)
	{
		delete[] findPathPolyList;
		findPathPolyList = NULL;
	}

	findPathSmoothListSize = 0;
	if(findPathSmoothList)
	{
		delete[] findPathSmoothList;
		findPathSmoothList = NULL;
	}

	findPathSteerSize = 0;
	if(findPathSteerPath)
	{
		delete[] findPathSteerPath;
		delete[] findPathSteerPathFlags;
		delete[] findPathSteerPathPolys;
		findPathSteerPath = NULL;
		findPathSteerPathFlags = NULL;
		findPathSteerPathPolys = NULL;
	}

	if(tileMesh)
	{
		delete tileMesh;
		tileMesh = NULL;
	}

	if(inputGeometry)
	{
		delete inputGeometry;
		inputGeometry = NULL;
	}
}

inline bool inRange(const float* v1, const float* v2, const float r, const float h)
{
	const float dx = v2[0] - v1[0];
	const float dy = v2[1] - v1[1];
	const float dz = v2[2] - v1[2];
	return (dx*dx + dz*dz) < r*r && fabsf(dy) < h;
}

static int fixupCorridor(dtPolyRef* path, const int npath, const int maxPath,
	const dtPolyRef* visited, const int nvisited)
{
	int furthestPath = -1;
	int furthestVisited = -1;
	
	// Find furthest common polygon.
	for (int i = npath-1; i >= 0; --i)
	{
		bool found = false;
		for (int j = nvisited-1; j >= 0; --j)
		{
			if (path[i] == visited[j])
			{
				furthestPath = i;
				furthestVisited = j;
				found = true;
			}
		}
		if (found)
			break;
	}

	// If no intersection found just return current path. 
	if (furthestPath == -1 || furthestVisited == -1)
		return npath;
	
	// Concatenate paths.	

	// Adjust beginning of the buffer to include the visited.
	const int req = nvisited - furthestVisited;
	const int orig = rcMin(furthestPath+1, npath);
	int size = rcMax(0, npath-orig);
	if (req+size > maxPath)
		size = maxPath-req;
	if (size)
		memmove(path+req, path+orig, size*sizeof(dtPolyRef));
	
	// Store visited
	for (int i = 0; i < req; ++i)
		path[i] = visited[(nvisited-1)-i];				
	
	return req+size;
}

bool RecastWorld::getSteerTarget(dtNavMeshQuery* navQuery, const float* startPos, const float* endPos,
   const float minTargetDist, const dtPolyRef* path, const int pathSize,
   float* steerPos, unsigned char& steerPosFlag, dtPolyRef& steerPosRef, int maxSteerPoints)
   /*float* outPoints = 0, int* outPointCount = 0)*/
{
	// Find steer target.

	if(findPathSteerPath == NULL || findPathSteerSize < maxSteerPoints)
	{
		if(findPathSteerPath)
		{
			delete[] findPathSteerPath;
			delete[] findPathSteerPathFlags;
			delete[] findPathSteerPathPolys;
		}
		findPathSteerSize = maxSteerPoints;
		findPathSteerPath = new float[maxSteerPoints * 3];
		findPathSteerPathFlags = new unsigned char[maxSteerPoints];
		findPathSteerPathPolys = new dtPolyRef[maxSteerPoints];
	}
	float* steerPath = findPathSteerPath;
	unsigned char* steerPathFlags = findPathSteerPathFlags;
	dtPolyRef* steerPathPolys = findPathSteerPathPolys;
	//static const int MAX_STEER_POINTS = 3;
	//float steerPath[MAX_STEER_POINTS*3];
	//unsigned char steerPathFlags[MAX_STEER_POINTS];
	//dtPolyRef steerPathPolys[MAX_STEER_POINTS];

	int nsteerPath = 0;
	navQuery->findStraightPath(startPos, endPos, path, pathSize,
		steerPath, steerPathFlags, steerPathPolys, &nsteerPath, maxSteerPoints);
	if (!nsteerPath)
		return false;
		
	//if (outPoints && outPointCount)
	//{
	//	*outPointCount = nsteerPath;
	//	for (int i = 0; i < nsteerPath; ++i)
	//		dtVcopy(&outPoints[i*3], &steerPath[i*3]);
	//}

	
	// Find vertex far enough to steer to.
	int ns = 0;
	while (ns < nsteerPath)
	{
		// Stop at Off-Mesh link or when point is further than slop away.
		if ((steerPathFlags[ns] & DT_STRAIGHTPATH_OFFMESH_CONNECTION) ||
			!inRange(&steerPath[ns*3], startPos, minTargetDist, 1000.0f))
			break;
		ns++;
	}
	// Failed to find good point to steer to.
	if (ns >= nsteerPath)
		return false;
	
	dtVcopy(steerPos, &steerPath[ns*3]);
	steerPos[1] = startPos[1];
	steerPosFlag = steerPathFlags[ns];
	steerPosRef = steerPathPolys[ns];
	
	return true;
}

bool RecastWorld::FindPath( const Vec3& start, const Vec3& end, float stepSize, 
	const Vec3& polygonPickExtents, int maxPolygonPath, int maxSmoothPath, int maxSteerPoints, 
	Vec3** outPath, int* outPathCount )
{
	*outPath = NULL;
	*outPathCount = 0;

	dtStatus status;

	float m_polyPickExt[3];
	m_polyPickExt[0] = polygonPickExtents.x;
	m_polyPickExt[1] = polygonPickExtents.y;
	m_polyPickExt[2] = polygonPickExtents.z;

	dtQueryFilter m_filter;
	m_filter.setIncludeFlags(NeoAxis_TileMesh::POLYFLAGS_ALL);
	m_filter.setExcludeFlags(0);
	
	for(int n = 0;n < DT_MAX_AREAS; n++)
		m_filter.setAreaCost(n, 1);
	
	//m_filter.setAreaCost(NeoAxis_TileMesh::POLYAREA_GROUND, 1.0f);
	//m_filter.setAreaCost(NeoAxis_TileMesh::POLYAREA_ROUGH, 1.25f);
	//m_filter.setAreaCost(NeoAxis_TileMesh::POLYAREA_SWAMP, 2.0f);
	//m_filter.setAreaCost(NeoAxis_TileMesh::POLYAREA_WATER, 10.0f);
	//m_filter.setAreaCost(NeoAxis_TileMesh::POLYAREA_ROAD, 0.8f);
	//m_filter.setAreaCost(NeoAxis_TileMesh::POLYAREA_DOOR, 1.0f);
	//m_filter.setAreaCost(NeoAxis_TileMesh::POLYAREA_JUMP, 1.5f);
	//m_filter.setIncludeFlags(m_filter.getIncludeFlags() ^ NeoAxis_TileMesh::POLYFLAGS_WALK);

	dtPolyRef m_startRef;
	status = tileMesh->m_navQuery->findNearestPoly((float*)&start, m_polyPickExt, &m_filter, &m_startRef, 0);
	if(!dtStatusSucceed(status))
		return false;

	dtPolyRef m_endRef;
	status = tileMesh->m_navQuery->findNearestPoly((float*)&end, m_polyPickExt, &m_filter, &m_endRef, 0);
	if(!dtStatusSucceed(status))
		return false;

	//static const int MAX_POLYS = 256 * 4;
	//static const int MAX_SMOOTH = 2048 * 4;

	if(findPathPolyList == NULL || findPathPolyListSize < maxPolygonPath)
	{
		if(findPathPolyList)
			delete[] findPathPolyList;
		findPathPolyListSize = maxPolygonPath;
		findPathPolyList = new dtPolyRef[findPathPolyListSize];
	}
	dtPolyRef* m_polys = findPathPolyList;
	//dtPolyRef m_polys[MAX_POLYS];
	int m_npolys = 0;

	status = tileMesh->m_navQuery->findPath(m_startRef, m_endRef, (float*)&start, (float*)&end, 
		&m_filter, m_polys, &m_npolys, maxPolygonPath);
	if(!dtStatusSucceed(status))
		return false;

	if(m_npolys == 0)
		return false;

	float m_prevIterPos[3], m_iterPos[3], m_steerPos[3], m_targetPos[3];
	tileMesh->m_navQuery->closestPointOnPolyBoundary(m_startRef, (float*)&start, m_iterPos);
	tileMesh->m_navQuery->closestPointOnPolyBoundary(m_polys[m_npolys-1], (float*)&end, m_targetPos);

	if(findPathSmoothList == NULL || findPathSmoothListSize < maxSmoothPath)
	{
		if(findPathSmoothList)
			delete[] findPathSmoothList;
		findPathSmoothListSize = maxSmoothPath;
		findPathSmoothList = new float[findPathSmoothListSize * 3];
	}
	float* m_smoothPath = findPathSmoothList;
	//float m_smoothPath[MAX_SMOOTH*3];
	int m_nsmoothPath = 0;

	dtVcopy(&m_smoothPath[m_nsmoothPath*3], m_iterPos);
	m_nsmoothPath++;

	dtVcopy(m_prevIterPos, m_iterPos);

	int m_pathIterNum = 0;

	while(true)
	{
		m_pathIterNum++;

		if (m_nsmoothPath >= maxSmoothPath)
			return false;

		// Move towards target a small advancement at a time until target reached or
		// when ran out of memory to store the path.

		const float SLOP = 0.01f;

		// Find location to steer towards.
		float steerPos[3];
		unsigned char steerPosFlag;
		dtPolyRef steerPosRef;

		//static const int MAX_STEER_POINTS = 10;
		//float m_steerPoints[MAX_STEER_POINTS*3];
		//int m_steerPointCount;

		if (!getSteerTarget(tileMesh->m_navQuery, m_iterPos, m_targetPos, SLOP,
			m_polys, m_npolys, steerPos, steerPosFlag, steerPosRef, maxSteerPoints)/*,
			m_steerPoints, &m_steerPointCount)*/)
		{
			goto end;
		}
			
		dtVcopy(m_steerPos, steerPos);
		
		bool endOfPath = (steerPosFlag & DT_STRAIGHTPATH_END) ? true : false;
		bool offMeshConnection = (steerPosFlag & DT_STRAIGHTPATH_OFFMESH_CONNECTION) ? true : false;
			
		// Find movement delta.
		float delta[3], len;
		dtVsub(delta, steerPos, m_iterPos);
		len = sqrtf(dtVdot(delta,delta));
		// If the steer target is end of path or off-mesh link, do not move past the location.
		if ((endOfPath || offMeshConnection) && len < stepSize)
			len = 1;
		else
			len = stepSize / len;
		float moveTgt[3];
		dtVmad(moveTgt, m_iterPos, delta, len);
			
		// Move
		float result[3];
		dtPolyRef visited[16];
		int nvisited = 0;
		tileMesh->m_navQuery->moveAlongSurface(m_polys[0], m_iterPos, moveTgt, &m_filter,
			result, visited, &nvisited, 16);
		m_npolys = fixupCorridor(m_polys, m_npolys, maxPolygonPath, visited, nvisited);
		float h = 0;
		tileMesh->m_navQuery->getPolyHeight(m_polys[0], result, &h);
		result[1] = h;
		dtVcopy(m_iterPos, result);
		
		// Handle end of path and off-mesh links when close enough.
		if (endOfPath && inRange(m_iterPos, steerPos, SLOP, 1.0f))
		{
			// Reached end of path.
			dtVcopy(m_iterPos, m_targetPos);
			if (m_nsmoothPath < maxSmoothPath)
			{
				dtVcopy(&m_smoothPath[m_nsmoothPath*3], m_iterPos);
				m_nsmoothPath++;
			}
			goto end;
		}
		else if (offMeshConnection && inRange(m_iterPos, steerPos, SLOP, 1.0f))
		{
			// Reached off-mesh connection.
			float startPos[3], endPos[3];
			
			// Advance the path up to and over the off-mesh connection.
			dtPolyRef prevRef = 0, polyRef = m_polys[0];
			int npos = 0;
			while (npos < m_npolys && polyRef != steerPosRef)
			{
				prevRef = polyRef;
				polyRef = m_polys[npos];
				npos++;
			}
			for (int i = npos; i < m_npolys; ++i)
				m_polys[i-npos] = m_polys[i];
			m_npolys -= npos;
					
			// Handle the connection.
			dtStatus status = tileMesh->m_navMesh->getOffMeshConnectionPolyEndPoints(prevRef, polyRef, startPos, endPos);
			if (dtStatusSucceed(status))
			{
				if (m_nsmoothPath < maxSmoothPath)
				{
					dtVcopy(&m_smoothPath[m_nsmoothPath*3], startPos);
					m_nsmoothPath++;
					// Hack to make the dotted path not visible during off-mesh connection.
					if (m_nsmoothPath & 1)
					{
						dtVcopy(&m_smoothPath[m_nsmoothPath*3], startPos);
						m_nsmoothPath++;
					}
				}
				// Move position at the other side of the off-mesh link.
				dtVcopy(m_iterPos, endPos);
				float h;
				tileMesh->m_navQuery->getPolyHeight(m_polys[0], m_iterPos, &h);
				m_iterPos[1] = h;
			}
		}
		
		// Store results.
		if (m_nsmoothPath < maxSmoothPath)
		{
			dtVcopy(&m_smoothPath[m_nsmoothPath*3], m_iterPos);
			m_nsmoothPath++;
		}

	}

	end:;

	Vec3* path = (Vec3*)malloc(m_nsmoothPath * sizeof(Vec3));
	for(int n = 0;n < m_nsmoothPath;n++)
	{
		Vec3 point;
		point.x = m_smoothPath[n * 3 + 0];
		point.y = m_smoothPath[n * 3 + 1];
		point.z = m_smoothPath[n * 3 + 2];
		path[n] = point;
	}

	*outPath = path;
	*outPathCount = m_nsmoothPath;
	return true;
}
