/* Copyright (c) 2011 Khaled Mamou (kmamou at gmail dot com)
 All rights reserved.
 
 
 Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
 
 1. Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
 
 2. Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
 
 3. The names of the contributors may not be used to endorse or promote products derived from this software without specific prior written permission.
 
 THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

#define _CRT_SECURE_NO_WARNINGS
#include <time.h>
#include <stdlib.h>
#include <stdio.h>
#include <iostream>
#include <fstream>
#include <string>
#include <hacdHACD.h>
#include <hacdMicroAllocator.h>

#ifdef WIN32
#define PATH_SEP "\\"
#else
#define PATH_SEP "/"
#endif


void CallBack(const char * msg, double progress, double concavity, size_t nVertices)
{
	std::cout << msg;
}
bool LoadOFF(const std::string & fileName, std::vector< HACD::Vec3<HACD::Real> > & points, std::vector< HACD::Vec3<long> > & triangles, bool invert);
bool SaveVRML2(const std::string & fileName, const std::vector< HACD::Vec3< HACD::Real > > & points, 
               const std::vector< HACD::Vec3<long> > & triangles, const HACD::Vec3<HACD::Real> * colors= 0);
bool SaveVRML2(std::ofstream & fout, const std::vector< HACD::Vec3< HACD::Real > > & points, 
               const std::vector< HACD::Vec3<long> > & triangles,
               const HACD::Material & material, const HACD::Vec3<HACD::Real> * colors = 0);
bool SaveOFF(const std::string & fileName, const std::vector< HACD::Vec3<HACD::Real> > & points, const std::vector< HACD::Vec3<long> > & triangles);
bool SaveOFF(const std::string & fileName, size_t nV, size_t nT, const HACD::Vec3<HACD::Real> * const points, const HACD::Vec3<long> * const triangles);
bool SavePartition(const std::string & fileName, const std::vector< HACD::Vec3<HACD::Real> > & points, 
                   const std::vector< HACD::Vec3<long> > & triangles,
                   const long * partition, const size_t nClusters);
int main(int argc, char * argv[])
{
    if (argc != 9)
    { 
        std::cout << "Usage: ./testHACD fileName.off minNClusters maxConcavity invertInputFaces addExtraDistPoints addFacesPoints ConnectedComponentsDist targetNTrianglesDecimatedMesh"<< std::endl;
		std::cout << "Recommended parameters: ./testHACD fileName.off 2 100 0 1 1 30 2000"<< std::endl;
        return -1;
    }
    
	const std::string fileName(argv[1]);
    size_t nClusters = atoi(argv[2]);
    double concavity = atof(argv[3]);
	bool invert = (atoi(argv[4]) == 0)?false:true;
	bool addExtraDistPoints = (atoi(argv[5]) == 0)?false:true;
    bool addFacesPoints = (atoi(argv[6]) == 0)?false:true;
    double ccConnectDist = atof(argv[7]);
	size_t targetNTrianglesDecimatedMesh = atoi(argv[8]);
    
    /*
    const std::string fileName("/Users/khaledmammou/Dev/HACD/data/Sketched-Brunnen.off");
    size_t nClusters = 1;
    double concavity = 100.0;
	bool invert = false;
	bool addExtraDistPoints = true;
    bool addNeighboursDistPoints = false;
    bool addFacesPoints = true;
    double ccConnectDist = 30.0;
	size_t targetNTrianglesDecimatedMesh = 1000;
    */
    
	std::string folder;
	int found = fileName.find_last_of(PATH_SEP);
	if (found != -1)
	{
		folder = fileName.substr(0,found);
	}
	if (folder == "")
    {
        folder = ".";
    }

	std::string file(fileName.substr(found+1));
	std::string outWRLFileName = folder + PATH_SEP + file.substr(0, file.find_last_of(".")) + ".wrl";
	std::string outOFFFileName = folder + PATH_SEP + file.substr(0, file.find_last_of(".")) + ".off";
    std::string outOFFFileNameDecimated = folder + PATH_SEP + file.substr(0, file.find_last_of(".")) + "_decimated.off";
	std::vector< HACD::Vec3<HACD::Real> > points;
	std::vector< HACD::Vec3<long> > triangles;
    LoadOFF(fileName, points, triangles, invert);
	SaveVRML2(outWRLFileName.c_str(), points, triangles);
	SaveOFF(outOFFFileName.c_str(), points, triangles);

	std::cout << "invert " << invert << std::endl;

	HACD::HeapManager * heapManager = HACD::createHeapManager(65536*(1000));

	HACD::HACD * const myHACD = HACD::CreateHACD(heapManager);
	myHACD->SetPoints(&points[0]);
	myHACD->SetNPoints(points.size());
	myHACD->SetTriangles(&triangles[0]);
	myHACD->SetNTriangles(triangles.size());
	myHACD->SetCompacityWeight(0.0001);
    myHACD->SetVolumeWeight(0.0);
    myHACD->SetConnectDist(ccConnectDist);               // if two connected components are seperated by distance < ccConnectDist
                                                        // then create a virtual edge between them so the can be merged during 
                                                        // the simplification process
	      
    myHACD->SetNClusters(nClusters);                     // minimum number of clusters
    myHACD->SetNVerticesPerCH(100);                      // max of 100 vertices per convex-hull
	myHACD->SetConcavity(concavity);                     // maximum concavity
	myHACD->SetSmallClusterThreshold(0.25);				 // threshold to detect small clusters
	myHACD->SetNTargetTrianglesDecimatedMesh(targetNTrianglesDecimatedMesh); // # triangles in the decimated mesh
	myHACD->SetCallBack(&CallBack);
    myHACD->SetAddExtraDistPoints(addExtraDistPoints);   
    myHACD->SetAddFacesPoints(addFacesPoints); 
    
    clock_t start, end;
    double elapsed;
    start = clock();
    {
	    myHACD->Compute();
    }
    end = clock();
    elapsed = static_cast<double>(end - start) / CLOCKS_PER_SEC;
    std::cout << "Time " << elapsed << " s"<< std::endl;
    nClusters = myHACD->GetNClusters();
        
    bool printfInfo = false;
	if (printfInfo)
	{
		
		std::cout << "Output" << std::endl;
		for(size_t c = 0; c < nClusters; ++c)
		{
			std::cout << std::endl << "Convex-Hull " << c << std::endl;
			size_t nPoints = myHACD->GetNPointsCH(c);
			size_t nTriangles = myHACD->GetNTrianglesCH(c);
			HACD::Vec3<HACD::Real> * pointsCH = new HACD::Vec3<HACD::Real>[nPoints];
			HACD::Vec3<long> * trianglesCH = new HACD::Vec3<long>[nTriangles];
			myHACD->GetCH(c, pointsCH, trianglesCH);
			std::cout << "Points " << nPoints << std::endl;
			for(size_t v = 0; v < nPoints; ++v)
			{
				std::cout << v << "\t" 
						  << pointsCH[v].X() << "\t" 
						  << pointsCH[v].Y() << "\t" 
						  << pointsCH[v].Z() << std::endl;
			}
			std::cout << "Triangles " << nTriangles << std::endl;
			for(size_t f = 0; f < nTriangles; ++f)
			{
				std::cout   << f << "\t" 
							<< trianglesCH[f].X() << "\t" 
							<< trianglesCH[f].Y() << "\t" 
							<< trianglesCH[f].Z() << std::endl;
			}
			delete [] pointsCH;
			delete [] trianglesCH;
		}
	}
	std::string outFileName = folder + PATH_SEP + file.substr(0, file.find_last_of(".")) + "_hacd.wrl";
	myHACD->Save(outFileName.c_str(), false);
    
    const HACD::Vec3<HACD::Real> * const decimatedPoints = myHACD->GetDecimatedPoints();
    const HACD::Vec3<long> * const decimatedTriangles    = myHACD->GetDecimatedTriangles();
    if (decimatedPoints && decimatedTriangles)
    {
        SaveOFF(outOFFFileNameDecimated, myHACD->GetNDecimatedPoints(), 
                                         myHACD->GetNDecimatedTriangles(), decimatedPoints, decimatedTriangles);
    }
    
    
    
    bool exportSepFiles = false;
    if (exportSepFiles)
    {
        char outFileName[1024];
        for(size_t c = 0; c < nClusters; c++)
        {
            sprintf(outFileName, "%s%s%s_hacd_%lu.wrl", folder.c_str(), PATH_SEP, file.substr(0, file.find_last_of(".")).c_str(), static_cast<unsigned long>(c));
            myHACD->Save(outFileName, false, c);
        }
    }
    
/*   
	// to do: fix this in the case the simplification is on
    std::string outFileNamePartition = folder + PATH_SEP + file.substr(0, file.find_last_of(".")) + "_partition.wrl";
	const long * const partition = myHACD->GetPartition();
    SavePartition(outFileNamePartition, points, triangles, partition, nClusters);
*/

    HACD::DestroyHACD(myHACD);
    HACD::releaseHeapManager(heapManager);
	return 0;
}

bool SaveVRML2(const std::string & fileName, const std::vector< HACD::Vec3<HACD::Real> > & points, 
               const std::vector< HACD::Vec3<long> > & triangles,
               const HACD::Vec3<HACD::Real> * colors)
{
    std::cout << "Saving " <<  fileName << std::endl;
    std::ofstream fout(fileName.c_str());
    if (fout.is_open()) 
    {
        const HACD::Material material;
        
        if (SaveVRML2(fout, points, triangles, material, colors))
        {
            fout.close();
            return true;
        }
        return false;
    }
    return false;
}

bool SaveVRML2(std::ofstream & fout, const std::vector< HACD::Vec3<HACD::Real> > & points, 
               const std::vector< HACD::Vec3<long> > & triangles, 
               const HACD::Material & material, const HACD::Vec3<HACD::Real> * colors)
{
    if (fout.is_open()) 
    {
        size_t nV = points.size();
        size_t nT = triangles.size();            
        fout <<"#VRML V2.0 utf8" << std::endl;	    	
        fout <<"" << std::endl;
        fout <<"# Vertices: " << nV << std::endl;		
        fout <<"# Triangles: " << nT << std::endl;		
        fout <<"" << std::endl;
        fout <<"Group {" << std::endl;
        fout <<"	children [" << std::endl;
        fout <<"		Shape {" << std::endl;
        fout <<"			appearance Appearance {" << std::endl;
        fout <<"				material Material {" << std::endl;
        fout <<"					diffuseColor "      << material.m_diffuseColor.X()      << " " 
                                                        << material.m_diffuseColor.Y()      << " "
                                                        << material.m_diffuseColor.Z()      << std::endl;  
        fout <<"					ambientIntensity "  << material.m_ambientIntensity      << std::endl;
        fout <<"					specularColor "     << material.m_specularColor.X()     << " " 
                                                        << material.m_specularColor.Y()     << " "
                                                        << material.m_specularColor.Z()     << std::endl; 
        fout <<"					emissiveColor "     << material.m_emissiveColor.X()     << " " 
                                                        << material.m_emissiveColor.Y()     << " "
                                                        << material.m_emissiveColor.Z()     << std::endl; 
        fout <<"					shininess "         << material.m_shininess             << std::endl;
        fout <<"					transparency "      << material.m_transparency          << std::endl;
        fout <<"				}" << std::endl;
        fout <<"			}" << std::endl;
        fout <<"			geometry IndexedFaceSet {" << std::endl;
        fout <<"				ccw TRUE" << std::endl;
        fout <<"				solid TRUE" << std::endl;
        fout <<"				convex TRUE" << std::endl;
        if (colors && nT>0)
        {
            fout <<"				colorPerVertex FALSE" << std::endl;
            fout <<"				color Color {" << std::endl;
            fout <<"					color [" << std::endl;
            for(size_t c = 0; c < nT; c++)
            {
                fout <<"						" << colors[c].X() << " " 
                                                  << colors[c].Y() << " " 
                                                  << colors[c].Z() << "," << std::endl;
            }
            fout <<"					]" << std::endl;
            fout <<"				}" << std::endl;
                    }
        if (nV > 0) 
        {
            fout <<"				coord DEF co Coordinate {" << std::endl;
            fout <<"					point [" << std::endl;
            for(size_t v = 0; v < nV; v++)
            {
                fout <<"						" << points[v].X() << " " 
                                                  << points[v].Y() << " " 
                                                  << points[v].Z() << "," << std::endl;
            }
            fout <<"					]" << std::endl;
            fout <<"				}" << std::endl;
        }
        if (nT > 0) 
        {
            fout <<"				coordIndex [ " << std::endl;
            for(size_t f = 0; f < nT; f++)
            {
                fout <<"						" << triangles[f].X() << ", " 
                                                  << triangles[f].Y() << ", "                                                  
                                                  << triangles[f].Z() << ", -1," << std::endl;
            }
            fout <<"				]" << std::endl;
        }
        fout <<"			}" << std::endl;
        fout <<"		}" << std::endl;
        fout <<"	]" << std::endl;
        fout <<"}" << std::endl;	
	    return true;
    }
    return false;
}

bool SaveOFF(const std::string & fileName, const std::vector< HACD::Vec3<HACD::Real> > & points, const std::vector< HACD::Vec3<long> > & triangles)
{
	return SaveOFF(fileName,points.size(), triangles.size(), &points[0], &triangles[0]);
}
bool SaveOFF(const std::string & fileName, size_t nV, size_t nT, const HACD::Vec3<HACD::Real> * const points, const HACD::Vec3<long> * const triangles)
{
    std::cout << "Saving " <<  fileName << std::endl;
    std::ofstream fout(fileName.c_str());
    if (fout.is_open()) 
    {           
        fout <<"OFF" << std::endl;	    	
        fout << nV << " " << nT << " " << 0<< std::endl;		
        for(size_t v = 0; v < nV; v++)
        {
            fout << points[v].X() << " " 
                 << points[v].Y() << " " 
                 << points[v].Z() << std::endl;
		}
        for(size_t f = 0; f < nT; f++)
        {
            fout <<"3 " << triangles[f].X() << " " 
                        << triangles[f].Y() << " "                                                  
                        << triangles[f].Z() << std::endl;
        }
        fout.close();
	    return true;
    }
    return false;
}

bool LoadOFF(const std::string & fileName, std::vector< HACD::Vec3<HACD::Real> > & points, std::vector< HACD::Vec3<long> > & triangles, bool invert) 
{    
	FILE * fid = fopen(fileName.c_str(), "r");
	if (fid) 
    {
		const std::string strOFF("OFF");
		char temp[1024];
		fscanf(fid, "%s", temp);
		if (std::string(temp) != strOFF)
		{
			printf( "Loading error: format not recognized \n");
            fclose(fid);

			return false;            
		}
		else
		{
			int nv = 0;
			int nf = 0;
			int ne = 0;
			fscanf(fid, "%i", &nv);
			fscanf(fid, "%i", &nf);
			fscanf(fid, "%i", &ne);
            points.resize(nv);
			triangles.resize(nf);
            HACD::Vec3<HACD::Real> coord;
			float x = 0;
			float y = 0;
			float z = 0;
			for (long p = 0; p < nv ; p++) 
            {
				fscanf(fid, "%f", &x);
				fscanf(fid, "%f", &y);
				fscanf(fid, "%f", &z);
				points[p].X() = x;
				points[p].Y() = y;
				points[p].Z() = z;
			}        
			int i = 0;
			int j = 0;
			int k = 0;
			int s = 0;
			for (long t = 0; t < nf ; ++t) {
				fscanf(fid, "%i", &s);
				if (s == 3)
				{
					fscanf(fid, "%i", &i);
					fscanf(fid, "%i", &j);
					fscanf(fid, "%i", &k);
					triangles[t].X() = i;
					if (invert)
					{
						triangles[t].Y() = k;
						triangles[t].Z() = j;
					}
					else
					{
						triangles[t].Y() = j;
						triangles[t].Z() = k;
					}
				}
				else			// Fix me: support only triangular meshes
				{
					for(long h = 0; h < s; ++h) fscanf(fid, "%i", &s);
				}
			}
            fclose(fid);
		}
	}
	else 
    {
		printf( "Loading error: file not found \n");
		return false;
    }
	return true;
}
bool SavePartition(const std::string & fileName, const std::vector< HACD::Vec3<HACD::Real> > & points, 
                                                 const std::vector< HACD::Vec3<long> > & triangles,
                                                 const long * partition, const size_t nClusters)
{
    if (!partition)
    {
        return false;
    }
    
    std::cout << "Saving " <<  fileName << std::endl;
    std::ofstream fout(fileName.c_str());
    if (fout.is_open()) 
    {
        HACD::Material mat;
        std::vector< HACD::Vec3<long> > triCluster;
        std::vector< HACD::Vec3<HACD::Real> > ptsCluster;
        std::vector< long > ptsMap;
        for(size_t c = 0; c < nClusters; c++)
        {
            ptsMap.resize(points.size());
            mat.m_diffuseColor.X() = mat.m_diffuseColor.Y() = mat.m_diffuseColor.Z() = 0.0;
            while (mat.m_diffuseColor.X() == mat.m_diffuseColor.Y() ||
                   mat.m_diffuseColor.Z() == mat.m_diffuseColor.Y() ||
                   mat.m_diffuseColor.Z() == mat.m_diffuseColor.X()  )
            {
                mat.m_diffuseColor.X() = (rand()%100) / 100.0;
                mat.m_diffuseColor.Y() = (rand()%100) / 100.0;
                mat.m_diffuseColor.Z() = (rand()%100) / 100.0;
            }
            long ver[3];
            long vCount = 1;
            for(size_t t = 0; t < triangles.size(); t++)
            {
                if (partition[t] == static_cast<long>(c))
                {
                    ver[0] = triangles[t].X();
                    ver[1] = triangles[t].Y();
                    ver[2] = triangles[t].Z();
                    for(int k = 0; k < 3; k++)
                    {
                        if (ptsMap[ver[k]] == 0)
                        {
                            ptsCluster.push_back(points[ver[k]]);
                            ptsMap[ver[k]] = vCount;
                            ver[k] = vCount-1;
                            vCount++;
                        }
                        else
                        {
                            ver[k] = ptsMap[ver[k]]-1;
                        }
                    }
                    triCluster.push_back(HACD::Vec3<long>(ver[0], ver[1], ver[2]));
                }
            }
            SaveVRML2(fout, ptsCluster, triCluster, mat);
            triCluster.clear();
            ptsCluster.clear();
            ptsMap.clear();
        }

        fout.close();
        return true;
    }
    return false;    
}
