//
// Copyright (c) 2009-2010 Mikko Mononen memon@inside.org
//
// This software is provided 'as-is', without any express or implied
// warranty.  In no event will the authors be held liable for any damages
// arising from the use of this software.
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
// 1. The origin of this software must not be misrepresented; you must not
//    claim that you wrote the original software. If you use this software
//    in a product, an acknowledgment in the product documentation would be
//    appreciated but is not required.
// 2. Altered source versions must be plainly marked as such, and must not be
//    misrepresented as being the original software.
// 3. This notice may not be removed or altered from any source distribution.
//

#define _USE_MATH_DEFINES
#include <math.h>
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include "InputGeom.h"
#include "NeoAxis_TileMesh.h"
#include "Recast.h"
#include "RecastDebugDraw.h"
#include "RecastDump.h"
#include "DetourNavMesh.h"
#include "DetourNavMeshBuilder.h"
#include "DetourDebugDraw.h"

#ifdef WIN32
#	define snprintf _snprintf
#endif


inline unsigned int nextPow2(unsigned int v)
{
	v--;
	v |= v >> 1;
	v |= v >> 2;
	v |= v >> 4;
	v |= v >> 8;
	v |= v >> 16;
	v++;
	return v;
}

inline unsigned int ilog2(unsigned int v)
{
	unsigned int r;
	unsigned int shift;
	r = (v > 0xffff) << 4; v >>= r;
	shift = (v > 0xff) << 3; v >>= shift; r |= shift;
	shift = (v > 0xf) << 2; v >>= shift; r |= shift;
	shift = (v > 0x3) << 1; v >>= shift; r |= shift;
	r |= (v >> 1);
	return r;
}

NeoAxis_TileMesh::NeoAxis_TileMesh() :
	m_geom(0),
	m_navMesh(0),
	m_navQuery(0),
	m_ctx(0),
	m_keepInterResults(false),
	m_triareas(0),
	m_solid(0),
	m_chf(0),
	m_cset(0),
	m_pmesh(0),
	m_dmesh(0),
	m_maxTiles(0),
	m_maxPolysPerTile(0),
	m_tileSize(32),
	m_tileBuildTime(0),
	m_tileMemUsage(0),
	m_tileTriCount(0)
{
	resetCommonSettings();
	memset(m_tileBmin, 0, sizeof(m_tileBmin));
	memset(m_tileBmax, 0, sizeof(m_tileBmax));
	m_navQuery = dtAllocNavMeshQuery();
}

NeoAxis_TileMesh::~NeoAxis_TileMesh()
{
	cleanup();
	dtFreeNavMesh(m_navMesh);
	dtFreeNavMeshQuery(m_navQuery);
	m_navMesh = 0;
}

void NeoAxis_TileMesh::cleanup()
{
	delete [] m_triareas;
	m_triareas = 0;
	rcFreeHeightField(m_solid);
	m_solid = 0;
	rcFreeCompactHeightfield(m_chf);
	m_chf = 0;
	rcFreeContourSet(m_cset);
	m_cset = 0;
	rcFreePolyMesh(m_pmesh);
	m_pmesh = 0;
	rcFreePolyMeshDetail(m_dmesh);
	m_dmesh = 0;
}

void NeoAxis_TileMesh::resetCommonSettings()
{
	m_cellSize = 0.3f;
	m_cellHeight = 0.2f;
	m_agentHeight = 2.0f;
	m_agentRadius = 0.6f;
	m_agentMaxClimb = 0.9f;
	m_agentMaxSlope = 45.0f;
	m_regionMinSize = 8;
	m_regionMergeSize = 20;
	m_monotonePartitioning = false;
	m_edgeMaxLen = 12.0f;
	m_edgeMaxError = 1.3f;
	m_vertsPerPoly = 6.0f;
	m_detailSampleDist = 6.0f;
	m_detailSampleMaxError = 1.0f;
}

static const int NAVMESHSET_MAGIC = 'M'<<24 | 'S'<<16 | 'E'<<8 | 'T'; //'MSET';
static const int NAVMESHSET_VERSION = 1;

struct NavMeshSetHeader
{
	int magic;
	int version;
	int numTiles;
	dtNavMeshParams params;
};

struct NavMeshTileHeader
{
	dtTileRef tileRef;
	int dataSize;
};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////

class ReadBuffer
{
	void* buffer;
	int bufferSize;
	int position;

public:

	ReadBuffer(void* buffer, int bufferSize)
	{
		this->buffer = buffer;
		this->bufferSize = bufferSize;
		position = 0;
	}

	bool Read(void* data, int size)
	{
		if(position + size > bufferSize)
			return false;
		memcpy(data, (char*)buffer + position, size);
		position += size;
		return true;
	}
};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////

class WriteBuffer
{
public:
	void* buffer;
	int bufferCapacity;
	int bufferSize;

	WriteBuffer(int initialCapacity)
	{
		buffer = malloc(initialCapacity);
		bufferCapacity = initialCapacity;
		bufferSize = 0;
	}

	void Write(void* data, int size)
	{
		int neededSize = bufferSize + size;

		if(neededSize > bufferCapacity)
		{
			while(neededSize > bufferCapacity)
				bufferCapacity *= 2;
			void* oldBuffer = buffer;
			buffer = malloc(bufferCapacity);
			memcpy(buffer, oldBuffer, bufferSize);
			free(oldBuffer);
		}

		memcpy((char*)buffer + bufferSize, data, size);
		bufferSize += size;
	}
};

/////////////////////////////////////////////////////////////////////////////////////////////////////////////

bool NeoAxis_TileMesh::loadNavMesh(void* data, int dataSize)
{
	ReadBuffer buffer(data, dataSize);

	// Read header.
	NavMeshSetHeader header;
	if(!buffer.Read(&header, sizeof(NavMeshSetHeader)))
		return false;
	if (header.magic != NAVMESHSET_MAGIC)
		return false;
	if (header.version != NAVMESHSET_VERSION)
		return false;
	
	dtNavMesh* mesh = dtAllocNavMesh();
	if (!mesh)
		return false;

	dtStatus status = mesh->init(&header.params);
	if (dtStatusFailed(status))
		return false;
		
	// Read tiles.
	for (int i = 0; i < header.numTiles; ++i)
	{
		NavMeshTileHeader tileHeader;
		buffer.Read(&tileHeader, sizeof(tileHeader));

		if (!tileHeader.tileRef || !tileHeader.dataSize)
			break;

		unsigned char* data = (unsigned char*)dtAlloc(tileHeader.dataSize, DT_ALLOC_PERM);
		if (!data)
			break;
		memset(data, 0, tileHeader.dataSize);
		buffer.Read(data, tileHeader.dataSize);
		
		mesh->addTile(data, tileHeader.dataSize, DT_TILE_FREE_DATA, tileHeader.tileRef, 0);
	}
	
	m_navMesh = mesh;

	return true;
}

void NeoAxis_TileMesh::saveNavMesh(const dtNavMesh* mesh, void** outData, int* outDataSize)
{
	*outData = NULL;
	*outDataSize = 0;

	if (!mesh)
		return;

	WriteBuffer buffer(16384);

	// Store header.
	NavMeshSetHeader header;
	header.magic = NAVMESHSET_MAGIC;
	header.version = NAVMESHSET_VERSION;
	header.numTiles = 0;
	for (int i = 0; i < mesh->getMaxTiles(); ++i)
	{
		const dtMeshTile* tile = mesh->getTile(i);
		if (!tile || !tile->header || !tile->dataSize)
			continue;
		header.numTiles++;
	}
	memcpy(&header.params, mesh->getParams(), sizeof(dtNavMeshParams));
	buffer.Write(&header, sizeof(NavMeshSetHeader));

	// Store tiles.
	for (int i = 0; i < mesh->getMaxTiles(); ++i)
	{
		const dtMeshTile* tile = mesh->getTile(i);
		if (!tile || !tile->header || !tile->dataSize)
			continue;

		NavMeshTileHeader tileHeader;
		tileHeader.tileRef = mesh->getTileRef(tile);
		tileHeader.dataSize = tile->dataSize;
		buffer.Write(&tileHeader, sizeof(tileHeader));

		buffer.Write(tile->data, tile->dataSize);
	}

	*outData = buffer.buffer;
	*outDataSize = buffer.bufferSize;
}

const float* NeoAxis_TileMesh::getBoundsMin()
{
	if (!m_geom) return 0;
	return m_geom->getMeshBoundsMin();
}

const float* NeoAxis_TileMesh::getBoundsMax()
{
	if (!m_geom) return 0;
	return m_geom->getMeshBoundsMax();
}

//SodanKerjuu: this function finds which corners of the bounding box is truly minimum and maximum
void NeoAxis_TileMesh::fixMinMaxCorners()
{
	float truMin[3], truMax[3];
	{	
		truMin[0] = rcMin(m_bmin[0], m_bmax[0]);
		truMin[1] = rcMin(m_bmin[1], m_bmax[1]);
		truMin[2] = rcMin(m_bmin[2], m_bmax[2]);

		truMax[0] = rcMax(m_bmin[0], m_bmax[0]);
		truMax[1] = rcMax(m_bmin[1], m_bmax[1]);
		truMax[2] = rcMax(m_bmin[2], m_bmax[2]);
	}
	
	rcVcopy(m_bmin, truMin);
	rcVcopy(m_bmax, truMax);
}

void NeoAxis_TileMesh::calculateSize()
{
	//!!!!!!when we set tileSize = 4, we can got "max tiles reached" error.

	int gw = 0, gh = 0;
	rcCalcGridSize(m_bmin, m_bmax, m_cellSize, &gw, &gh);
	const int ts = (int)m_tileSize;
	const int tw = (gw + ts-1) / ts;
	const int th = (gh + ts-1) / ts;

	// Max tiles and max polys affect how the tile IDs are caculated.
	// There are 22 bits available for identifying a tile and a polygon.
	int tileBits = rcMin((int)ilog2(nextPow2(tw*th)), 14);
	if (tileBits > 14) tileBits = 14;
	int polyBits = 22 - tileBits;
	m_maxTiles = 1 << tileBits;
	m_maxPolysPerTile = 1 << polyBits;

	//debug
	//m_ctx->log(RC_LOG_ERROR, "CellSize: %f  TileSize: %f", m_cellSize, m_tileSize);
	//m_ctx->log(RC_LOG_ERROR, "Min: %f %f %f , Max: %f %f %f", m_bmin[0], m_bmin[1], m_bmin[2], m_bmax[0], m_bmax[1], m_bmax[2]);
	//m_ctx->log(RC_LOG_ERROR, "MaxTiles: %d  MaxPPT: %d", m_maxTiles, m_maxPolysPerTile);
}

void NeoAxis_TileMesh::getMaximums(int* maxTiles, int* maxPolysPerTile)
{
	*maxTiles = m_maxTiles;
	*maxPolysPerTile = m_maxPolysPerTile;
}

bool NeoAxis_TileMesh::init()
{
	dtFreeNavMesh(m_navMesh);
	
	m_navMesh = dtAllocNavMesh();
	if (!m_navMesh)
	{
		m_ctx->log(RC_LOG_ERROR, "buildTiledNavigation: Could not allocate navmesh.");
		return false;
	}

	dtNavMeshParams params;
	rcVcopy(params.orig, m_bmin);
	params.tileWidth = m_tileSize * m_cellSize;
	params.tileHeight = m_tileSize * m_cellSize;
	params.maxTiles = m_maxTiles;
	params.maxPolys = m_maxPolysPerTile;
	
	dtStatus status;
	
	status = m_navMesh->init(&params);
	if (dtStatusFailed(status))
	{
		m_ctx->log(RC_LOG_ERROR, "buildTiledNavigation: Could not init navmesh.");
		return false;
	}

	return true;
}

void NeoAxis_TileMesh::getTilePos(const float* pos, int& tx, int& ty)
{
	const float ts = m_tileSize*m_cellSize;
	tx = (int)((pos[0] - m_bmin[0]) / ts);
	ty = (int)((pos[2] - m_bmin[2]) / ts);
}

void NeoAxis_TileMesh::buildTile(const float* pos)
{
	if (!m_navMesh) return;
		
	const float ts = m_tileSize*m_cellSize;
	const int tx = (int)((pos[0] - m_bmin[0]) / ts);
	const int ty = (int)((pos[2] - m_bmin[2]) / ts);
	
	m_tileBmin[0] = m_bmin[0] + tx*ts;
	m_tileBmin[1] = m_bmin[1];
	m_tileBmin[2] = m_bmin[2] + ty*ts;
	
	m_tileBmax[0] = m_bmin[0] + (tx+1)*ts;
	m_tileBmax[1] = m_bmax[1];
	m_tileBmax[2] = m_bmin[2] + (ty+1)*ts;
	
	m_ctx->resetLog();
	
	int dataSize = 0;
	unsigned char* data = buildTileMesh(tx, ty, m_tileBmin, m_tileBmax, dataSize);
	
	if (data)
	{
		// Remove any previous data (navmesh owns and deletes the data).
		m_navMesh->removeTile(m_navMesh->getTileRefAt(tx,ty,0),0,0);
		
		// Let the navmesh own the data.
		dtStatus status = m_navMesh->addTile(data,dataSize,DT_TILE_FREE_DATA,0,0);
		if (dtStatusFailed(status))
			dtFree(data);
	}
	
	m_ctx->dumpLog("Build Tile (%d,%d):", tx,ty);
}

void NeoAxis_TileMesh::removeTile(const float* pos)
{
	if (!m_navMesh) return;
	
	const float ts = m_tileSize*m_cellSize;
	const int tx = (int)((pos[0] - m_bmin[0]) / ts);
	const int ty = (int)((pos[2] - m_bmin[2]) / ts);
	
	m_tileBmin[0] = m_bmin[0] + tx*ts;
	m_tileBmin[1] = m_bmin[1];
	m_tileBmin[2] = m_bmin[2] + ty*ts;
	
	m_tileBmax[0] = m_bmin[0] + (tx+1)*ts;
	m_tileBmax[1] = m_bmax[1];
	m_tileBmax[2] = m_bmin[2] + (ty+1)*ts;
	
	m_navMesh->removeTile(m_navMesh->getTileRefAt(tx,ty,0),0,0);
}

void NeoAxis_TileMesh::buildAllTiles()
{
	if (!m_geom) return;
	if (!m_navMesh) return;

	int gw = 0, gh = 0;
	rcCalcGridSize(m_bmin, m_bmax, m_cellSize, &gw, &gh);
	const int ts = (int)m_tileSize;
	const int tw = (gw + ts-1) / ts;
	const int th = (gh + ts-1) / ts;
	const float tcs = m_tileSize*m_cellSize;

	// Start the build process.
	//m_ctx->startTimer(RC_TIMER_TEMP);

	for (int y = 0; y < th; ++y)
	{
		for (int x = 0; x < tw; ++x)
		{
			m_tileBmin[0] = m_bmin[0] + x*tcs;
			m_tileBmin[1] = m_bmin[1];
			m_tileBmin[2] = m_bmin[2] + y*tcs;
			
			m_tileBmax[0] = m_bmin[0] + (x+1)*tcs;
			m_tileBmax[1] = m_bmax[1];
			m_tileBmax[2] = m_bmin[2] + (y+1)*tcs;
			
			int dataSize = 0;
			unsigned char* data = buildTileMesh(x, y, m_tileBmin, m_tileBmax, dataSize);
			if (data)
			{
				// Remove any previous data (navmesh owns and deletes the data).
				m_navMesh->removeTile(m_navMesh->getTileRefAt(x,y,0),0,0);
				// Let the navmesh own the data.
				dtStatus status = m_navMesh->addTile(data,dataSize,DT_TILE_FREE_DATA,0,0);
				if (dtStatusFailed(status))
				{
					dtFree(data);

					//!!!!
					//SodanKerjuu: stop calculating!!!
					if ((status & DT_OUT_OF_MEMORY) != 0)
					{
						m_ctx->log(RC_LOG_ERROR, "Max tiles reached! Please increase TileSize, CellSize properties.");
						return;
					}
				}
			}
		}
	}
	
	// Start the build process.	
	//m_ctx->stopTimer(RC_TIMER_TEMP);

	//m_totalBuildTimeMs = m_ctx->getAccumulatedTime(RC_TIMER_TEMP)/1000.0f;
	
}

void NeoAxis_TileMesh::removeAllTiles()
{
	int gw = 0, gh = 0;
	rcCalcGridSize(m_bmin, m_bmax, m_cellSize, &gw, &gh);
	const int ts = (int)m_tileSize;
	const int tw = (gw + ts-1) / ts;
	const int th = (gh + ts-1) / ts;
	
	for (int y = 0; y < th; ++y)
		for (int x = 0; x < tw; ++x)
			m_navMesh->removeTile(m_navMesh->getTileRefAt(x,y,0),0,0);
}

unsigned char* NeoAxis_TileMesh::buildTileMesh(const int tx, const int ty, const float* bmin, 
	const float* bmax, int& dataSize)
{
	if (!m_geom || !m_geom->getMesh() || !m_geom->getChunkyMesh())
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Input mesh is not specified.");
		return 0;
	}
	
	m_tileMemUsage = 0;
	m_tileBuildTime = 0;
	
	cleanup();
	
	const float* verts = m_geom->getMesh()->getVerts();
	const int nverts = m_geom->getMesh()->getVertCount();
	const int ntris = m_geom->getMesh()->getTriCount();
	const rcChunkyTriMesh* chunkyMesh = m_geom->getChunkyMesh();
	
	// Init build configuration from GUI
	memset(&m_cfg, 0, sizeof(m_cfg));
	m_cfg.cs = m_cellSize;
	m_cfg.ch = m_cellHeight;
	m_cfg.walkableSlopeAngle = m_agentMaxSlope;
	m_cfg.walkableHeight = (int)ceilf(m_agentHeight / m_cfg.ch);
	m_cfg.walkableClimb = (int)floorf(m_agentMaxClimb / m_cfg.ch);
	m_cfg.walkableRadius = (int)ceilf(m_agentRadius / m_cfg.cs);
	m_cfg.maxEdgeLen = (int)(m_edgeMaxLen / m_cellSize);
	m_cfg.maxSimplificationError = m_edgeMaxError;
	m_cfg.minRegionArea = (int)rcSqr(m_regionMinSize);		// Note: area = size*size
	m_cfg.mergeRegionArea = (int)rcSqr(m_regionMergeSize);	// Note: area = size*size
	m_cfg.maxVertsPerPoly = (int)m_vertsPerPoly;
	m_cfg.tileSize = (int)m_tileSize;
	m_cfg.borderSize = m_cfg.walkableRadius + 3; // Reserve enough padding.
	m_cfg.width = m_cfg.tileSize + m_cfg.borderSize*2;
	m_cfg.height = m_cfg.tileSize + m_cfg.borderSize*2;
	m_cfg.detailSampleDist = m_detailSampleDist < 0.9f ? 0 : m_cellSize * m_detailSampleDist;
	m_cfg.detailSampleMaxError = m_cellHeight * m_detailSampleMaxError;
	
	rcVcopy(m_cfg.bmin, bmin);
	rcVcopy(m_cfg.bmax, bmax);
	m_cfg.bmin[0] -= m_cfg.borderSize*m_cfg.cs;
	m_cfg.bmin[2] -= m_cfg.borderSize*m_cfg.cs;
	m_cfg.bmax[0] += m_cfg.borderSize*m_cfg.cs;
	m_cfg.bmax[2] += m_cfg.borderSize*m_cfg.cs;
	
	// Reset build times gathering.
	//m_ctx->resetTimers();
	
	// Start the build process.
	//m_ctx->startTimer(RC_TIMER_TOTAL);
	
	m_ctx->log(RC_LOG_PROGRESS, "Building navigation:");
	m_ctx->log(RC_LOG_PROGRESS, " - %d x %d cells", m_cfg.width, m_cfg.height);
	m_ctx->log(RC_LOG_PROGRESS, " - %.1fK verts, %.1fK tris", nverts/1000.0f, ntris/1000.0f);
	
	// Allocate voxel heightfield where we rasterize our input data to.
	m_solid = rcAllocHeightfield();
	if (!m_solid)
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Out of memory 'solid'.");
		return 0;
	}
	if (!rcCreateHeightfield(m_ctx, *m_solid, m_cfg.width, m_cfg.height, m_cfg.bmin, m_cfg.bmax, m_cfg.cs, m_cfg.ch))
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Could not create solid heightfield.");
		return 0;
	}
	
	// Allocate array that can hold triangle flags.
	// If you have multiple meshes you need to process, allocate
	// and array which can hold the max number of triangles you need to process.
	m_triareas = new unsigned char[chunkyMesh->maxTrisPerChunk];
	if (!m_triareas)
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Out of memory 'm_triareas' (%d).", chunkyMesh->maxTrisPerChunk);
		return 0;
	}
	
	float tbmin[2], tbmax[2];
	tbmin[0] = m_cfg.bmin[0];
	tbmin[1] = m_cfg.bmin[2];
	tbmax[0] = m_cfg.bmax[0];
	tbmax[1] = m_cfg.bmax[2];
	int cid[512];// TODO: Make grow when returning too many items.

	const int ncid = rcGetChunksOverlappingRect(chunkyMesh, tbmin, tbmax, cid, 512);
	if (!ncid)
		return 0;

	m_tileTriCount = 0;
	
	for (int i = 0; i < ncid; ++i)
	{
		const rcChunkyTriMeshNode& node = chunkyMesh->nodes[cid[i]];
		const int* tris = &chunkyMesh->tris[node.i*3];
		const int ntris = node.n;
		
		m_tileTriCount += ntris;
		
		memset(m_triareas, 0, ntris*sizeof(unsigned char));
		rcMarkWalkableTriangles(m_ctx, m_cfg.walkableSlopeAngle, verts, nverts, tris, ntris, m_triareas);
		
		rcRasterizeTriangles(m_ctx, verts, nverts, tris, m_triareas, ntris, *m_solid, m_cfg.walkableClimb);
	}
	
	if (!m_keepInterResults)
	{
		delete [] m_triareas;
		m_triareas = 0;
	}
	
	// Once all geometry is rasterized, we do initial pass of filtering to
	// remove unwanted overhangs caused by the conservative rasterization
	// as well as filter spans where the character cannot possibly stand.
	rcFilterLowHangingWalkableObstacles(m_ctx, m_cfg.walkableClimb, *m_solid);
	rcFilterLedgeSpans(m_ctx, m_cfg.walkableHeight, m_cfg.walkableClimb, *m_solid);
	rcFilterWalkableLowHeightSpans(m_ctx, m_cfg.walkableHeight, *m_solid);
	
	// Compact the heightfield so that it is faster to handle from now on.
	// This will result more cache coherent data as well as the neighbours
	// between walkable cells will be calculated.
	m_chf = rcAllocCompactHeightfield();
	if (!m_chf)
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Out of memory 'chf'.");
		return 0;
	}
	if (!rcBuildCompactHeightfield(m_ctx, m_cfg.walkableHeight, m_cfg.walkableClimb, *m_solid, *m_chf))
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Could not build compact data.");
		return 0;
	}
	
	if (!m_keepInterResults)
	{
		rcFreeHeightField(m_solid);
		m_solid = 0;
	}

	// Erode the walkable area by agent radius.
	if (!rcErodeWalkableArea(m_ctx, m_cfg.walkableRadius, *m_chf))
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Could not erode.");
		return 0;
	}

	// (Optional) Mark areas.
	const ConvexVolume* vols = m_geom->getConvexVolumes();
	for (int i  = 0; i < m_geom->getConvexVolumeCount(); ++i)
		rcMarkConvexPolyArea(m_ctx, vols[i].verts, vols[i].nverts, vols[i].hmin, vols[i].hmax, (unsigned char)vols[i].area, *m_chf);
	
	if (m_monotonePartitioning)
	{
		// Partition the walkable surface into simple regions without holes.
		if (!rcBuildRegionsMonotone(m_ctx, *m_chf, m_cfg.borderSize, m_cfg.minRegionArea, m_cfg.mergeRegionArea))
		{
			m_ctx->log(RC_LOG_ERROR, "buildNavigation: Could not build regions.");
			return 0;
		}
	}
	else
	{
		// Prepare for region partitioning, by calculating distance field along the walkable surface.
		if (!rcBuildDistanceField(m_ctx, *m_chf))
		{
			m_ctx->log(RC_LOG_ERROR, "buildNavigation: Could not build distance field.");
			return 0;
		}
		
		// Partition the walkable surface into simple regions without holes.
		if (!rcBuildRegions(m_ctx, *m_chf, m_cfg.borderSize, m_cfg.minRegionArea, m_cfg.mergeRegionArea))
		{
			m_ctx->log(RC_LOG_ERROR, "buildNavigation: Could not build regions.");
			return 0;
		}
	}
 	
	// Create contours.
	m_cset = rcAllocContourSet();
	if (!m_cset)
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Out of memory 'cset'.");
		return 0;
	}
	if (!rcBuildContours(m_ctx, *m_chf, m_cfg.maxSimplificationError, m_cfg.maxEdgeLen, *m_cset))
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Could not create contours.");
		return 0;
	}
	
	if (m_cset->nconts == 0)
	{
		return 0;
	}
	
	// Build polygon navmesh from the contours.
	m_pmesh = rcAllocPolyMesh();
	if (!m_pmesh)
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Out of memory 'pmesh'.");
		return 0;
	}
	if (!rcBuildPolyMesh(m_ctx, *m_cset, m_cfg.maxVertsPerPoly, *m_pmesh))
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Could not triangulate contours.");
		return 0;
	}
	
	// Build detail mesh.
	m_dmesh = rcAllocPolyMeshDetail();
	if (!m_dmesh)
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Out of memory 'dmesh'.");
		return 0;
	}
	
	if (!rcBuildPolyMeshDetail(m_ctx, *m_pmesh, *m_chf,
							   m_cfg.detailSampleDist, m_cfg.detailSampleMaxError,
							   *m_dmesh))
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Could build polymesh detail.");
		return 0;
	}
	
	if (!m_keepInterResults)
	{
		rcFreeCompactHeightfield(m_chf);
		m_chf = 0;
		rcFreeContourSet(m_cset);
		m_cset = 0;
	}
	
	unsigned char* navData = 0;
	int navDataSize = 0;
	if (m_cfg.maxVertsPerPoly <= DT_VERTS_PER_POLYGON)
	{
		if (m_pmesh->nverts >= 0xffff)
		{
			// The vertex indices are ushorts, and cannot point to more than 0xffff vertices.
			m_ctx->log(RC_LOG_ERROR, "Too many vertices per tile %d (max: %d).", m_pmesh->nverts, 0xffff);
			return false;
		}
		
		// Update poly flags from areas.
		for (int i = 0; i < m_pmesh->npolys; ++i)
		{
			if (m_pmesh->areas[i] == RC_WALKABLE_AREA)
				m_pmesh->areas[i] = POLYAREA_GROUND;
			
			if (m_pmesh->areas[i] == POLYAREA_GROUND ||
				m_pmesh->areas[i] == POLYAREA_ROAD)
			{
				m_pmesh->flags[i] = POLYFLAGS_WALK;
			}
			else if (m_pmesh->areas[i] == POLYAREA_WATER)
			{
				m_pmesh->flags[i] = POLYFLAGS_SWIM;
			}
			else if (m_pmesh->areas[i] == POLYAREA_DOOR)
			{
				m_pmesh->flags[i] = POLYFLAGS_WALK | POLYFLAGS_DOOR;
			}
		}
		
		dtNavMeshCreateParams params;
		memset(&params, 0, sizeof(params));
		params.verts = m_pmesh->verts;
		params.vertCount = m_pmesh->nverts;
		params.polys = m_pmesh->polys;
		params.polyAreas = m_pmesh->areas;
		params.polyFlags = m_pmesh->flags;
		params.polyCount = m_pmesh->npolys;
		params.nvp = m_pmesh->nvp;
		params.detailMeshes = m_dmesh->meshes;
		params.detailVerts = m_dmesh->verts;
		params.detailVertsCount = m_dmesh->nverts;
		params.detailTris = m_dmesh->tris;
		params.detailTriCount = m_dmesh->ntris;
		params.offMeshConVerts = m_geom->getOffMeshConnectionVerts();
		params.offMeshConRad = m_geom->getOffMeshConnectionRads();
		params.offMeshConDir = m_geom->getOffMeshConnectionDirs();
		params.offMeshConAreas = m_geom->getOffMeshConnectionAreas();
		params.offMeshConFlags = m_geom->getOffMeshConnectionFlags();
		params.offMeshConUserID = m_geom->getOffMeshConnectionId();
		params.offMeshConCount = m_geom->getOffMeshConnectionCount();
		params.walkableHeight = m_agentHeight;
		params.walkableRadius = m_agentRadius;
		params.walkableClimb = m_agentMaxClimb;
		params.tileX = tx;
		params.tileY = ty;
		params.tileLayer = 0;
		rcVcopy(params.bmin, m_pmesh->bmin);
		rcVcopy(params.bmax, m_pmesh->bmax);
		params.cs = m_cfg.cs;
		params.ch = m_cfg.ch;
		params.buildBvTree = true;
		
		if (!dtCreateNavMeshData(&params, &navData, &navDataSize))
		{
			m_ctx->log(RC_LOG_ERROR, "Could not build Detour navmesh.");
			return 0;
		}		
	}
	m_tileMemUsage = navDataSize/1024.0f;
	
	//m_ctx->stopTimer(RC_TIMER_TOTAL);
	
	// Show performance stats.
	//duLogBuildTimes(*m_ctx, m_ctx->getAccumulatedTime(RC_TIMER_TOTAL));
	m_ctx->log(RC_LOG_PROGRESS, ">> Polymesh: %d vertices  %d polygons", m_pmesh->nverts, m_pmesh->npolys);
	
	//m_tileBuildTime = m_ctx->getAccumulatedTime(RC_TIMER_TOTAL)/1000.0f;

	dataSize = navDataSize;
	return navData;
}
