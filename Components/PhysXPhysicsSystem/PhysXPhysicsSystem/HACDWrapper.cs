// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Engine;
using Engine.MathEx;
using Engine.FileSystem;
using Engine.PhysicsSystem;

namespace PhysXPhysicsSystem
{
	public static class HACDWrapper
	{
		const string library = "PhysXNativeWrapper"; //"hacd";
		const CallingConvention convention = CallingConvention.Cdecl;

		///////////////////////////////////////////

		[DllImport( library, CallingConvention = convention )]
		static extern IntPtr HACD_Init();

		[DllImport( library, CallingConvention = convention )]
		static extern void HACD_Shutdown( IntPtr instance );

		[DllImport( library, CallingConvention = convention )]
		[return: MarshalAs( UnmanagedType.U1 )]
		static extern unsafe bool HACD_Compute( IntPtr instance, double* points, int pointCount,
			int* triangles, int triangleCount, int maxTrianglesInDecimatedMesh, int maxVerticesPerConvexHull );

		[DllImport( library, CallingConvention = convention )]
		static extern int HACD_GetClusterCount( IntPtr instance );

		[DllImport( library, CallingConvention = convention )]
		static extern void HACD_GetBufferSize( IntPtr instance, int cluster, out int pointCount,
			out int triangleCount );

		[DllImport( library, CallingConvention = convention )]
		static extern unsafe void HACD_GetBuffer( IntPtr instance, int cluster, double* points,
			int* triangles );

		[DllImport( library, CallingConvention = convention )]
		static extern void HACD_ClearComputed( IntPtr instance );

		///////////////////////////////////////////

		public class Instance
		{
			internal IntPtr nativeObject;
		}

		///////////////////////////////////////////

		public static Instance Init()
		{
			//NativeLibraryManager.PreLoadLibrary( "hacd" );

			Instance instance = new Instance();
			instance.nativeObject = HACD_Init();
			if( instance.nativeObject == IntPtr.Zero )
				return null;
			return instance;
		}

		public static void Shutdown( Instance instance )
		{
			if( instance.nativeObject != IntPtr.Zero )
			{
				HACD_Shutdown( instance.nativeObject );
				instance.nativeObject = IntPtr.Zero;
			}
		}

		public unsafe static bool Compute( Instance instance, double* points, int pointCount, int* triangles,
			int triangleCount, int maxTrianglesInDecimatedMesh, int maxVerticesPerConvexHull )
		{
			bool result = HACD_Compute( instance.nativeObject, points, pointCount, triangles, triangleCount,
				maxTrianglesInDecimatedMesh, maxVerticesPerConvexHull );
			return result;
		}

		public static int GetClusterCount( Instance instance )
		{
			return HACD_GetClusterCount( instance.nativeObject );
		}

		public static unsafe void GetBufferSize( Instance instance, int cluster, out int pointCount,
			out int triangleCount )
		{
			HACD_GetBufferSize( instance.nativeObject, cluster, out pointCount, out triangleCount );
		}

		public static unsafe void GetBuffer( Instance instance, int cluster, double* points, int* triangles )
		{
			HACD_GetBuffer( instance.nativeObject, cluster, points, triangles );
		}

		public static void ClearComputed( Instance instance )
		{
			HACD_ClearComputed( instance.nativeObject );
		}
	}
}
