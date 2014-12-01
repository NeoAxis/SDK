//// Copyright (C) NeoAxis Group Ltd. This is part of NeoAxis 3D Engine SDK.
//using System;
//using Engine;
//using Engine.MathEx;
//using Engine.PhysicsSystem;
//using PhysXNativeWrapper;

//namespace PhysXPhysicsSystem
//{
//   class PhysXDistanceJoint : DistanceJoint, IPhysXJoint
//   {
//      IntPtr nativeJoint;

//      //////////////////////////////////////////////////////////

//      public PhysXDistanceJoint( Body body1, Body body2 )
//         : base( body1, body2 )
//      {
//      }

//      void UpdatePhysXJoint()
//      {
//         bool needCreate = PushedToWorld && !Broken;
//         bool created = nativeJoint != IntPtr.Zero;

//         if( needCreate == created )
//            return;

//         if( needCreate )
//         {
//            PhysXBody physXBody0 = (PhysXBody)Body1;
//            PhysXBody physXBody1 = (PhysXBody)Body2;

//            if( ( !physXBody0.Static || !physXBody1.Static ) &&
//               ( physXBody0.nativeBody != IntPtr.Zero && physXBody1.nativeBody != IntPtr.Zero ) )
//            {
//               good?
//               Vec3 localPosition0 = physXBody0.Rotation.GetInverse() * ( Anchor - physXBody0.Position );
//               Quat localRotation0 = physXBody0.Rotation.GetInverse().GetNormalize();
//               Vec3 localPosition1 = physXBody1.Rotation.GetInverse() * ( Anchor - physXBody1.Position );
//               Quat localRotation1 = physXBody1.Rotation.GetInverse().GetNormalize();

//               nativeJoint = PhysXNativeDistanceJoint.Create(
//                  physXBody0.nativeBody, ref localPosition0, ref localRotation0,
//                  physXBody1.nativeBody, ref localPosition1, ref localRotation1 );

//               PhysXNativeDistanceJoint.SetMinDistanceEnabled( nativeJoint, MinDistanceEnabled );
//               PhysXNativeDistanceJoint.SetMinDistance( nativeJoint, MinDistance );
//               PhysXNativeDistanceJoint.SetMaxDistanceEnabled( nativeJoint, MaxDistanceEnabled );
//               PhysXNativeDistanceJoint.SetMaxDistance( nativeJoint, MaxDistance );
//               PhysXNativeDistanceJoint.SetSpringEnabled( nativeJoint, SpringEnabled );
//               PhysXNativeDistanceJoint.SetSpring( nativeJoint, Spring );
//               PhysXNativeDistanceJoint.SetTolerance( nativeJoint, Tolerance );
//               PhysXNativeDistanceJoint.SetDamping( nativeJoint, Damping );

//               if( ContactsEnabled )
//                  PhysXNativeWrapper.PhysXJoint.SetCollisionEnable( nativeJoint, true );
//               UpdatePhysXBreakData();
//               if( Scene._EnableDebugVisualization )
//                  SetVisualizationEnable( true );
//            }
//         }
//         else
//         {
//            DestroyPhysXJoint();
//         }
//      }

//      internal void DestroyPhysXJoint()
//      {
//         if( nativeJoint != IntPtr.Zero )
//         {
//            PhysXJoint.Destroy( nativeJoint );
//            nativeJoint = IntPtr.Zero;
//         }
//      }

//      protected override void OnUpdatePushedToWorld()
//      {
//         UpdatePhysXJoint();
//      }

//      public void UpdateDataFromLibrary()
//      {
//         //if( nativeJoint == IntPtr.Zero )
//         //   return;
//      }

//      protected override void OnSetBroken()
//      {
//         UpdatePhysXJoint();
//      }

//      protected override bool OnUpdateBreakState()
//      {
//         return nativeJoint != IntPtr.Zero && PhysXJoint.IsBroken( nativeJoint );
//      }

//      protected void UpdatePhysXBreakData()
//      {
//         if( nativeJoint == IntPtr.Zero )
//            return;
//         PhysXJoint.SetBreakForce( nativeJoint,
//            ( BreakMaxForce > 0 ) ? BreakMaxForce * 0.2161234981132075f : PhysXNativeWorld.MAX_REAL,
//            ( BreakMaxTorque > 0 ) ? BreakMaxTorque * 0.0493827160493827f : PhysXNativeWorld.MAX_REAL );
//      }

//      protected override void OnSetBreakProperties()
//      {
//         base.OnSetBreakProperties();
//         UpdatePhysXBreakData();
//      }

//      public void SetVisualizationEnable( bool enable )
//      {
//         if( nativeJoint != IntPtr.Zero )
//            PhysXJoint.SetVisualizationEnable( nativeJoint, enable );
//      }

//      protected override void OnUpdateContactsEnabled()
//      {
//         base.OnUpdateContactsEnabled();
//         if( nativeJoint != IntPtr.Zero )
//            PhysXNativeWrapper.PhysXJoint.SetCollisionEnable( nativeJoint, ContactsEnabled );
//      }

//      protected override void OnUpdateParameter( Parameters parameter )
//      {
//         switch( parameter )
//         {
//         case Parameters.MinDistanceEnabled:
//            PhysXNativeDistanceJoint.SetMinDistanceEnabled( nativeJoint, MinDistanceEnabled );
//            break;
//         case Parameters.MinDistance:
//            PhysXNativeDistanceJoint.SetMinDistance( nativeJoint, MinDistance );
//            break;
//         case Parameters.MaxDistanceEnabled:
//            PhysXNativeDistanceJoint.SetMaxDistanceEnabled( nativeJoint, MaxDistanceEnabled );
//            break;
//         case Parameters.MaxDistance:
//            PhysXNativeDistanceJoint.SetMaxDistance( nativeJoint, MaxDistance );
//            break;
//         case Parameters.SpringEnabled:
//            PhysXNativeDistanceJoint.SetSpringEnabled( nativeJoint, SpringEnabled );
//            break;
//         case Parameters.Spring:
//            PhysXNativeDistanceJoint.SetSpring( nativeJoint, Spring );
//            break;
//         case Parameters.Tolerance:
//            PhysXNativeDistanceJoint.SetTolerance( nativeJoint, Tolerance );
//            break;
//         case Parameters.Damping:
//            PhysXNativeDistanceJoint.SetDamping( nativeJoint, Damping );
//            break;
//         }
//      }
//   }
//}
