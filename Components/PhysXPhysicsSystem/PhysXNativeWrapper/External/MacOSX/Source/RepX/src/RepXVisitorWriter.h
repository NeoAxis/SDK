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
#ifndef REPX_VISITOR_WRITER_H
#define REPX_VISITOR_WRITER_H
#include "RepX.h"
#include "PsArray.h"
#include "PsInlineArray.h"
#include "CmPhysXCommon.h"
#include "RepXMetaDataPropertyVisitor.h"
#include "PxStreamOperators.h"
#include "MemoryPoolStreams.h"
#include "RepXMetaDataPropertyVisitor.h"
#include "PxProfileFoundationWrapper.h"
#include "RepXWriter.h"
#include "RepXImpl.h"
#include "RepXMemoryAllocator.h"
#include "RepXCoreExtensions.h"

namespace physx { namespace repx {

	template<typename TDataType>
	inline void writeReference( RepXWriter& writer, RepXIdToRepXObjectMap& inMap, const char* inPropName, const TDataType* inDatatype )
	{
		TRepXId theId = inMap.getIdForLiveObject( getBasePtr( inDatatype ) );
		bool ok = !inDatatype || (inDatatype && 0 != theId);
		REPX_REPORT_ERROR_IF( ok, RepXErrorCode::eReferenceNotFound, inPropName );
		writer.write( inPropName, createRepXObject( inDatatype, theId ) );
	}

	inline void writeProperty( RepXWriter& inWriter, MemoryBuffer& inBuffer, const char* inProp )
	{
		PxU8 data = 0;
		inBuffer.write( &data, sizeof(PxU8) );
		inWriter.write( inProp, reinterpret_cast<const char*>( inBuffer.mBuffer ) );
		inBuffer.clear();
	}

	template<typename TDataType>
	inline void writeProperty( RepXWriter& inWriter, RepXIdToRepXObjectMap&, MemoryBuffer& inBuffer, const char* inPropName, TDataType inValue )
	{
		inBuffer << inValue;
		writeProperty( inWriter, inBuffer, inPropName );
	}

	inline void writeProperty( RepXWriter& writer, RepXIdToRepXObjectMap& idMap, MemoryBuffer& inBuffer, const char* inPropName, const PxConvexMesh* inDatatype )
	{
		writeReference( writer, idMap, inPropName, inDatatype );
	}
	
	inline void writeProperty( RepXWriter& writer, RepXIdToRepXObjectMap& idMap, MemoryBuffer& inBuffer, const char* inPropName, PxConvexMesh* inDatatype )
	{
		writeReference( writer, idMap, inPropName, inDatatype );
	}

	inline void writeProperty( RepXWriter& writer, RepXIdToRepXObjectMap& idMap, MemoryBuffer& inBuffer, const char* inPropName, const PxTriangleMesh* inDatatype )
	{
		writeReference( writer, idMap, inPropName, inDatatype );
	}
	
	inline void writeProperty( RepXWriter& writer, RepXIdToRepXObjectMap& idMap, MemoryBuffer& inBuffer, const char* inPropName, PxTriangleMesh* inDatatype )
	{
		writeReference( writer, idMap, inPropName, inDatatype );
	}

	inline void writeProperty( RepXWriter& writer, RepXIdToRepXObjectMap& idMap, MemoryBuffer& inBuffer, const char* inPropName, const PxHeightField* inDatatype )
	{
		writeReference( writer, idMap, inPropName, inDatatype );
	}
	
	inline void writeProperty( RepXWriter& writer, RepXIdToRepXObjectMap& idMap, MemoryBuffer& inBuffer, const char* inPropName, PxHeightField* inDatatype )
	{
		writeReference( writer, idMap, inPropName, inDatatype );
	}

	inline void writeProperty( RepXWriter& writer, RepXIdToRepXObjectMap& idMap, MemoryBuffer& inBuffer, const char* inPropName, const PxRigidActor* inDatatype )
	{
		writeReference( writer, idMap, inPropName, inDatatype );
	}

	inline void writeProperty( RepXWriter& writer, RepXIdToRepXObjectMap& idMap, MemoryBuffer& inBuffer, const char* inPropName, PxArticulation* inDatatype )
	{
		writeReference( writer, idMap, inPropName, inDatatype );
	}
	
	inline void writeProperty( RepXWriter& writer, RepXIdToRepXObjectMap& idMap, MemoryBuffer& inBuffer, const char* inPropName, PxRigidActor* inDatatype )
	{
		writeReference( writer, idMap, inPropName, inDatatype );
	}

	inline void writeFlagsProperty( RepXWriter& inWriter, MemoryBuffer& tempBuf, const char* inPropName, PxU32 inFlags, const PxU32ToName* inTable )
	{
		if ( inTable )
		{
			PxU32 flagValue( inFlags );
			if ( flagValue )
			{
				for ( PxU32 idx =0; inTable[idx].mName != NULL; ++idx )
				{
					if ( (inTable[idx].mValue & flagValue) != 0 )
					{
						if ( tempBuf.mWriteOffset != 0 )
							tempBuf << "|";
						tempBuf << inTable[idx].mName;
					}
				}
				writeProperty( inWriter, tempBuf, inPropName );
			}
		}
	}

	inline void writePxVec3( PxOutputStream& inStream, const PxVec3& inVec ) { inStream << inVec; }
	

	template<typename TDataType>
	inline const TDataType& PtrAccess( const TDataType* inPtr, PxU32 inIndex )
	{
		return inPtr[inIndex];
	}
	
	template<typename TDataType>
	inline void BasicDatatypeWrite( PxOutputStream& inStream, const TDataType& item ) { inStream << item; }
	
	template<typename TObjType, typename TAccessOperator, typename TWriteOperator>
	inline void writeBuffer( RepXWriter& inWriter, MemoryBuffer& inTempBuffer
							, PxU32 inObjPerLine, const TObjType* inObjType, TAccessOperator inAccessOperator
							, PxU32 inBufSize, const char* inPropName, TWriteOperator inOperator )
	{
		if ( inBufSize && inObjType )
		{
			for ( PxU32 idx = 0; idx < inBufSize; ++idx )
			{
				if ( idx && ( idx % inObjPerLine == 0 ) )
					inTempBuffer << "\n\t\t\t";
				else
					inTempBuffer << " ";
				inOperator( inTempBuffer, inAccessOperator( inObjType, idx ) );
			}
			writeProperty( inWriter, inTempBuffer, inPropName );
		}
	}

	template<typename TDataType, typename TAccessOperator, typename TWriteOperator>
	inline void writeStrideBuffer( RepXWriter& inWriter, MemoryBuffer& inTempBuffer
							, PxU32 inObjPerLine, PxStrideIterator<const TDataType>& inData, TAccessOperator inAccessOperator
							, PxU32 inBufSize, const char* inPropName, PxU32 inStride, TWriteOperator inOperator )
	{
		if ( inBufSize && &inData[0])
		{
			for ( PxU32 idx = 0; idx < inBufSize; ++idx )
			{
				if ( idx && ( idx % inObjPerLine == 0 ) )
					inTempBuffer << "\n\t\t\t";
				else
					inTempBuffer << " ";
				
				inOperator( inTempBuffer, inAccessOperator( &inData[idx], 0  ) );
			}
			writeProperty( inWriter, inTempBuffer, inPropName );
		}
	}
	

	template<typename TDataType, typename TWriteOperator>
	inline void writeBuffer( RepXWriter& inWriter, MemoryBuffer& inTempBuffer
							, PxU32 inObjPerLine, const TDataType* inBuffer
							, PxU32 inBufSize, const char* inPropName, TWriteOperator inOperator )
	{
		writeBuffer( inWriter, inTempBuffer, inObjPerLine, inBuffer, PtrAccess<TDataType>, inBufSize, inPropName, inOperator );
	}

	template<typename TEnumType>
	inline void writeEnumProperty( RepXWriter& inWriter, const char* inPropName, TEnumType inEnumValue, const PxU32ToName* inConversions )
	{
		PxU32 theValue = static_cast<PxU32>( inEnumValue );
		for ( const PxU32ToName* conv = inConversions; conv->mName != NULL; ++conv )
			if ( conv->mValue == theValue ) inWriter.write( inPropName, conv->mName );
	}
	
	
		
	template<typename TObjType, typename TWriterType, typename TInfoType>
	inline void handleComplexObj( TWriterType& oldVisitor, const TObjType* inObj, TInfoType& info);

	template<typename TCollectionType, typename TVisitor, typename TPropType, typename TInfoType >
	void handleComplexCollection( TVisitor& visitor, const TPropType& inProp, const char* childName, TInfoType& inInfo )
	{
		PxU32 count( inProp.size( visitor.mObj ) );
		if ( count )
		{
			InlineArray<TCollectionType*,5> theData;
			theData.resize( count );
			inProp.get( visitor.mObj, theData.begin(), count );
			for( PxU32 idx =0; idx < count; ++idx )
			{
				visitor.pushName( childName );
				handleComplexObj( visitor, theData[idx], inInfo );
				visitor.popName();
			}
		}
	}
	template<typename TVisitor>
	void handleShapes( TVisitor& visitor, const PxRigidActorShapeCollection& inProp )
	{
		PxShapeGeneratedInfo theInfo;
		handleComplexCollection<PxShape>( visitor, inProp, "PxShape", theInfo );
	}

	template<typename TVisitor>
	void handleShapeMaterials( TVisitor& visitor, const PxShapeMaterialsProperty& inProp )
	{
		PxU32 count( inProp.size( visitor.mObj ) );
		if ( count )
		{
			InlineArray<PxMaterial*,5> theData;
			theData.resize( count );
			inProp.get( visitor.mObj, theData.begin(), count );
			visitor.pushName( "PxMaterialRef" );
			for( PxU32 idx =0; idx < count; ++idx )
				writeReference( visitor.mWriter, visitor.mIdMap, "PxMaterialRef", theData[idx] );
			visitor.popName();
		}
	}

	template<typename TObjType>
	struct RepXVisitorWriterBase
	{
		TNameStack&	mNameStack;
		RepXWriter&					mWriter;
		const TObjType*				mObj;
		MemoryBuffer&				mTempBuffer;
		RepXIdToRepXObjectMap&		mIdMap;


		RepXVisitorWriterBase( TNameStack& ns, RepXWriter& writer, const TObjType* obj, MemoryBuffer& buf, RepXIdToRepXObjectMap& map )
			: mNameStack( ns )
			, mWriter( writer )
			, mObj( obj )
			, mTempBuffer( buf )
			, mIdMap( map )
		{
		}

		RepXVisitorWriterBase( const RepXVisitorWriterBase<TObjType>& other )
			: mNameStack( other.mNameStack )
			, mWriter( other.mWriter )
			, mObj( other.mObj )
			, mTempBuffer( other.mTempBuffer )
			, mIdMap( other.mIdMap )
		{
		}

		RepXVisitorWriterBase& operator=( const RepXVisitorWriterBase& ){ PX_ASSERT( false ); return *this; }

		void gotoTopName()
		{
			if ( mNameStack.size() && mNameStack.back().mOpen == false ) 
			{
				mWriter.addAndGotoChild( mNameStack.back().mName );
				mNameStack.back().mOpen = true;
			}
		}

		void pushName( const char* inName ) 
		{ 
			gotoTopName();
			mNameStack.pushBack( inName ); 
		}

		void pushBracketedName( const char* inName ) { pushName( inName ); }
		void popName() 
		{ 
			if ( mNameStack.size() )
			{
				if ( mNameStack.back().mOpen )
					mWriter.leaveChild();
				mNameStack.popBack(); 
			}
		}

		const char* topName() const
		{
			if ( mNameStack.size() ) return mNameStack.back().mName;
			PX_ASSERT( false );
			return "bad__repx__name";
		}

		template<typename TAccessorType>
		void simpleProperty( PxU32 key, TAccessorType& inProp )
		{
			typedef typename TAccessorType::prop_type TPropertyType;
			TPropertyType propVal = inProp.get( mObj );
			writeProperty( mWriter, mIdMap, mTempBuffer, topName(), propVal );
		}
		
		template<typename TAccessorType>
		void enumProperty( PxU32 key, TAccessorType& inProp, const PxU32ToName* inConversions )
		{
			writeEnumProperty( mWriter, topName(),  inProp.get( mObj ), inConversions );
		}

		template<typename TAccessorType>
		void flagsProperty( PxU32 key, const TAccessorType& inProp, const PxU32ToName* inConversions )
		{
			writeFlagsProperty( mWriter, mTempBuffer, topName(), inProp.get( mObj ), inConversions );
		}

		template<typename TAccessorType, typename TInfoType>
		void complexProperty( PxU32* key, const TAccessorType& inProp, TInfoType& inInfo )
		{
			typedef typename TAccessorType::prop_type TPropertyType;
			TPropertyType propVal = inProp.get( mObj );
			handleComplexObj( *this, &propVal, inInfo );
		}

		void handleShapes( const PxRigidActorShapeCollection& inProp )
		{
			physx::repx::handleShapes( *this, inProp );
		}

		void handleShapeMaterials( const PxShapeMaterialsProperty& inProp )
		{
			physx::repx::handleShapeMaterials( *this, inProp );
		}
	};

	template<typename TObjType>
	struct RepXVisitorWriter : RepXVisitorWriterBase<TObjType>
	{
		RepXVisitorWriter( TNameStack& ns, RepXWriter& writer, const TObjType* obj, MemoryBuffer& buf, RepXIdToRepXObjectMap& map )
			: RepXVisitorWriterBase<TObjType>( ns, writer, obj, buf, map )
		{
		}

		RepXVisitorWriter( const RepXVisitorWriter<TObjType>& other )
			: RepXVisitorWriterBase<TObjType>( other )
		{
		}
	};

	template<>
	struct RepXVisitorWriter<PxArticulationLink> : RepXVisitorWriterBase<PxArticulationLink>
	{
		RepXVisitorWriter( TNameStack& ns, RepXWriter& writer, const PxArticulationLink* obj, MemoryBuffer& buf, RepXIdToRepXObjectMap& map )
			: RepXVisitorWriterBase<PxArticulationLink>( ns, writer, obj, buf, map )
		{
		}

		RepXVisitorWriter( const RepXVisitorWriter<PxArticulationLink>& other )
			: RepXVisitorWriterBase<PxArticulationLink>( other )
		{
		}

		void handleIncomingJoint( const TIncomingJointPropType& prop )
		{
			const PxArticulationJoint* theJoint( prop.get( mObj ) );
			if ( theJoint )
			{
				PxArticulationJointGeneratedInfo info;
				pushName( "Joint" );
				handleComplexObj( *this, theJoint, info );
				popName();
			}
		}
	};
	
	typedef ProfileHashMap< const TRepXId, const PxArticulationLink* > TArticulationLinkLinkMap;
	
	template<>
	struct RepXVisitorWriter<PxArticulation> : RepXVisitorWriterBase<PxArticulation>
	{
		TArticulationLinkLinkMap& mArticulationLinkParents;

		RepXVisitorWriter( TNameStack& ns, RepXWriter& writer, const PxArticulation* inArticulation, MemoryBuffer& buf, RepXIdToRepXObjectMap& map, TArticulationLinkLinkMap* artMap = NULL )
			: RepXVisitorWriterBase<PxArticulation>( ns, writer, inArticulation, buf, map )
			, mArticulationLinkParents( *artMap )
		{
			InlineArray<PxArticulationLink*, 64, WrapperReflectionAllocator<PxArticulationLink*> > linkList( WrapperReflectionAllocator<PxArticulationLink*>( buf.mManager->getWrapper() ) );
			PxU32 numLinks = inArticulation->getNbLinks();
			linkList.resize( numLinks );
			inArticulation->getLinks( linkList.begin(), numLinks );
			for ( PxU32 idx = 0; idx < numLinks; ++idx )
			{
				const PxArticulationLink* theLink( linkList[idx] );
				map.addLiveObject( createRepXObject( linkList[idx]) );
				InlineArray<PxArticulationLink*, 64> theChildList;
				PxU32 numChildren = theLink->getNbChildren();
				theChildList.resize( numChildren );
				theLink->getChildren( theChildList.begin(), numChildren );
				for ( PxU32 childIdx = 0; childIdx < numChildren; ++childIdx )
					mArticulationLinkParents.insert( PX_PROFILE_POINTER_TO_U64(theChildList[childIdx]), theLink );
			}
		}

		RepXVisitorWriter( const RepXVisitorWriter<PxArticulation>& other )
			: RepXVisitorWriterBase<PxArticulation>( other )
			, mArticulationLinkParents( other.mArticulationLinkParents )
		{
		}
		template<typename TAccessorType, typename TInfoType>
		void complexProperty( PxU32* key, const TAccessorType& inProp, TInfoType& inInfo )
		{
			typedef typename TAccessorType::prop_type TPropertyType;
			TPropertyType propVal = inProp.get( mObj );
			handleComplexObj( *this, &propVal, inInfo );
		}

		void writeArticulationLink( const PxArticulationLink* inLink )
		{
			pushName( "PxArticulationLink" );
			gotoTopName();
			const TArticulationLinkLinkMap::Entry* theParentPtr = mArticulationLinkParents.find( PX_PROFILE_POINTER_TO_U64(inLink) );
			if ( theParentPtr != NULL )
				writeProperty( mWriter, mIdMap, mTempBuffer, "Parent",  theParentPtr->second );
			writeProperty( mWriter, mIdMap, mTempBuffer, "Id", inLink  );

			PxArticulationLinkGeneratedInfo info;
			handleComplexObj( *this, inLink, info );
			popName();
		}

		void recurseAddLinkAndChildren( const PxArticulationLink* inLink, InlineArray<const PxArticulationLink*, 64>& ioLinks )
		{
			ioLinks.pushBack( inLink );
			InlineArray<PxArticulationLink*, 8> theChildren;
			PxU32 childCount( inLink->getNbChildren() );
			theChildren.resize( childCount );
			inLink->getChildren( theChildren.begin(), childCount );
			for ( PxU32 idx = 0; idx < childCount; ++idx )
				recurseAddLinkAndChildren( theChildren[idx], ioLinks );
		}

		void handleArticulationLinks( const PxArticulationLinkCollectionProp& inProp )
		{
			//topologically sort the links as per my discussion with Dilip because
			//links aren't guaranteed to have the parents before the children in the
			//overall link list and it is unlikely to be done by beta 1.
			PxU32 count( inProp.size( mObj ) );
			if ( count )
			{
				InlineArray<PxArticulationLink*, 64> theLinks;
				theLinks.resize( count );
				inProp.get( mObj, theLinks.begin(), count );
				
				InlineArray<const PxArticulationLink*, 64> theSortedLinks;
				for ( PxU32 idx = 0; idx < count; ++idx )
				{
					const PxArticulationLink* theLink( theLinks[idx] );
					if ( mArticulationLinkParents.find( PX_PROFILE_POINTER_TO_U64(theLink) ) == NULL )
						recurseAddLinkAndChildren( theLink, theSortedLinks );
				}	
				PX_ASSERT( theSortedLinks.size() == count );
				for ( PxU32 idx = 0; idx < count; ++idx )
					writeArticulationLink( theSortedLinks[idx] );
				popName();
			}
		}
	};
	
	template<>
	struct RepXVisitorWriter<PxShape> : RepXVisitorWriterBase<PxShape>
	{
		RepXVisitorWriter( TNameStack& ns, RepXWriter& writer, const PxShape* obj, MemoryBuffer& buf, RepXIdToRepXObjectMap& map )
			: RepXVisitorWriterBase<PxShape>( ns, writer, obj, buf, map )
		{
		}

		RepXVisitorWriter( const RepXVisitorWriter<PxShape>& other )
			: RepXVisitorWriterBase<PxShape>( other )
		{
		}

		template<typename GeometryType>
		inline void writeGeometryProperty( const PxShapeGeometryProperty& inProp, const char* inTypeName )
		{
			pushName( "Geometry" );
			pushName( inTypeName );
			GeometryType theType;
			inProp.getGeometry( mObj, theType );
			PxClassInfoTraits<GeometryType> theTraits;
			PxU32 count = theTraits.Info.totalPropertyCount();
			if(count)
			{				
				handleComplexObj( *this, &theType, theTraits.Info);
			}
			else
			{
				writeProperty(mWriter, mTempBuffer, inTypeName);
			}
			popName();
			popName();
		}

		void handleGeometryProperty( const PxShapeGeometryProperty& inProp )
		{
			switch( mObj->getGeometryType() )
			{
				case PxGeometryType::eSPHERE: writeGeometryProperty<PxSphereGeometry>( inProp, "PxSphereGeometry" ); break;
				case PxGeometryType::ePLANE: writeGeometryProperty<PxPlaneGeometry>( inProp, "PxPlaneGeometry" ); break;
				case PxGeometryType::eCAPSULE: writeGeometryProperty<PxCapsuleGeometry>( inProp, "PxCapsuleGeometry" ); break;
				case PxGeometryType::eBOX: writeGeometryProperty<PxBoxGeometry>( inProp, "PxBoxGeometry" ); break;
				case PxGeometryType::eCONVEXMESH: writeGeometryProperty<PxConvexMeshGeometry>( inProp, "PxConvexMeshGeometry" ); break;
				case PxGeometryType::eTRIANGLEMESH: writeGeometryProperty<PxTriangleMeshGeometry>( inProp, "PxTriangleMeshGeometry" ); break;
				case PxGeometryType::eHEIGHTFIELD: writeGeometryProperty<PxHeightFieldGeometry>( inProp, "PxHeightFieldGeometry" ); break;
				default: PX_ASSERT( false );
			}
		}
	};
	template<typename TObjType>
	inline void writeAllProperties( TNameStack& inNameStack, const TObjType* inObj, RepXWriter& writer, MemoryBuffer& buffer, RepXIdToRepXObjectMap& idMap )
	{
		RepXVisitorWriter<TObjType> newVisitor( inNameStack, writer, inObj, buffer, idMap );
		RepXPropertyFilter<RepXVisitorWriter<TObjType> > theOp( newVisitor );
		PxClassInfoTraits<TObjType> info;
		info.Info.visitBaseProperties( theOp );
		info.Info.visitInstanceProperties( theOp );
	}
	
	template<typename TObjType>
	inline void writeAllProperties( TNameStack& inNameStack, TObjType* inObj, RepXWriter& writer, MemoryBuffer& buffer, RepXIdToRepXObjectMap& idMap )
	{
		RepXVisitorWriter<TObjType> newVisitor( inNameStack, writer, inObj, buffer, idMap );
		RepXPropertyFilter<RepXVisitorWriter<TObjType> > theOp( newVisitor );
		PxClassInfoTraits<TObjType> info;
		info.Info.visitBaseProperties( theOp );
		info.Info.visitInstanceProperties( theOp );
	}
	template<typename TObjType>
	inline void writeAllProperties( const TObjType* inObj, RepXWriter& writer, MemoryBuffer& buffer, RepXIdToRepXObjectMap& idMap )
	{
		TNameStack theNames( buffer.mManager->getWrapper() );
		writeAllProperties( theNames, inObj, writer, buffer, idMap );
	}
		
	template<typename TObjType, typename TWriterType, typename TInfoType>
	inline void handleComplexObj( TWriterType& oldVisitor, const TObjType* inObj, TInfoType& info)
	{
		writeAllProperties( oldVisitor.mNameStack, inObj, oldVisitor.mWriter, oldVisitor.mTempBuffer, oldVisitor.mIdMap );
	}
	
}}
#endif