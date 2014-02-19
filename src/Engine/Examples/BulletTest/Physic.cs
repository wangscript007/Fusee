﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using Fusee.Engine;
using Fusee.Math;
using Microsoft.Win32;

namespace Examples.BulletTest
{
    internal class Physic
    {
        private DynamicWorld _world;

        public DynamicWorld World
        {
            get { return _world; }
            set { _world = value; }
        }

        internal BoxShape MyBoxCollider;
        internal SphereShape MySphereCollider;
        internal CylinderShape MyCylinderCollider;
        internal ConvexHullShape MyConvHull;
        internal ConvexHullShape TeaPotHull;

        internal Mesh BoxMesh, TeaPotMesh, PlatonicMesh;

        internal RigidBody _PRigidBody;

        public RigidBody PRbBody
        {
            get { return _PRigidBody; }
            set { _PRigidBody = value; }
        }


        public Physic()
        {
            Debug.WriteLine("Physic: Constructor");
            //InitCollisionCallback();
            InitScene1();
            //InitDfo6Constraint();
            //Tester();
        }


        public void InitWorld()
        {
            _world = new DynamicWorld();
        }

        public void InitColliders()
        {
            MyBoxCollider = _world.AddBoxShape(2);
            MySphereCollider = _world.AddSphereShape(2);
            MyCylinderCollider = _world.AddCylinderShape(new float3(2, 4, 2));

            BoxMesh = MeshReader.LoadMesh(@"Assets/Cube.obj.model");
            TeaPotMesh = MeshReader.LoadMesh(@"Assets/Teapot.obj.model");
            PlatonicMesh = MeshReader.LoadMesh(@"Assets/Platonic.obj.model");
            float3[] verts = PlatonicMesh.Vertices;

            MyConvHull = _world.AddConvexHullShape(verts, true);
            //var vertices = BoxMesh.Vertices;

            float3[] vertsTeaPot = TeaPotMesh.Vertices;
            TeaPotHull = _world.AddConvexHullShape(vertsTeaPot, true);
            TeaPotHull.LocalScaling = new float3(0.05f, 0.05f,0.05f);
            //var verticesTea = TeaPotMesh.Vertices;

        }


        public void InitCollisionCallback()
        {
            InitWorld();
            InitColliders();
            GroundPlane(float3.Zero, float3.Zero);

            var box1 = _world.AddRigidBody(1, new float3(0,20,0), float3.Zero, MyBoxCollider);
            
            //var box2 = _world.AddRigidBody(1, new float3(20, 20, 0), float3.Zero, MyBoxCollider);
            
           // var box3 = _world.AddRigidBody(1, new float3(10, 50, 0), float3.Zero, MyBoxCollider);
            
        }




        public void InitScene1()
        {
            InitWorld();
            InitColliders();
            GroundPlane(float3.Zero, float3.Zero);
            FallingTower1();
        }
        public void InitScene2()
        {
            InitWorld();
            InitColliders();
            //InitHull();
           // FallingTower3();
            GroundPlane(new float3(30, 15, 0), new float3(0, 0, (float)Math.PI / 6));
            GroundPlane(new float3(-20, 0, 0), float3.Zero);
            FallingPlatonics();
            InitPoint2PointConstraint();
            InitHingeConstraint();
        }
        public void InitScene3()
        {
            InitWorld();
            InitColliders();
            GroundPlane(new float3(30, 15, 0), new float3(0,0,(float)Math.PI/6));
            GroundPlane(new float3(-20, 0, 0), float3.Zero);
            FallingSpheres();
            InitKegel();
        }

        public void InitScene4()
        {
            InitWorld();
            InitColliders();
            GroundPlane(float3.Zero, float3.Zero);
            FallingTeaPots();
        }

        public void GroundPlane(float3 pos, float3 rot)
        {
            //var plane = _world.AddStaticPlaneShape(float3.UnitY, 1);
            //var groundPlane = _world.AddRigidBody(0, new float3(0,0,0), float3.Zero, plane);
            //groundPlane.Bounciness = 1;
            var groundShape = _world.AddBoxShape(30, 0.1f, 30);
            var ground = _world.AddRigidBody(0, pos, rot, groundShape);
           
            ground.Restitution = 1f;
            ground.Friction = 1;
        }

        public void InitKegel()
        {
            var shape = _world.AddCylinderShape(2f, 4, 2f);
            _world.AddRigidBody(1, new float3(-20, 3, 15), float3.Zero, shape);
            _world.AddRigidBody(1, new float3(-25, 3, 10), float3.Zero, shape);
            _world.AddRigidBody(1, new float3(-10, 3, -5), float3.Zero, shape);
            _world.AddRigidBody(1, new float3(-15, 3, 0), float3.Zero, shape);
            _world.AddRigidBody(1, new float3(-10, 3, -10), float3.Zero, shape);
            
            
        }

        public void InitHull()
        {
           float3[] verts = PlatonicMesh.Vertices;
          
           var shape = _world.AddConvexHullShape(verts);
           _world.AddRigidBody(1, new float3(20, 20,0), float3.Zero, shape);
          
        }

        public void Wippe()
        {
            //var groundShape = _world.AddBoxShape(150, 25, 150);
            //var ground = _world.AddRigidBody(0, new float3(0, 0, 0), Quaternion.Identity, groundShape);

            var boxShape = _world.AddBoxShape(30, 1, 10);

            //var brettShape = _world.AddBoxShape(50.0f, 0.1f, 10.0f);
            // var comp = _world.AddCompoundShape(true);
            // var brett = _world.AddRigidBody(1, new float3(0, 55, 0), brettShape, new float3(0, 0, 0));
            Quaternion rotA = new Quaternion(new float3(1, 0, 0), 30);
            Quaternion rotB = new Quaternion(new float3(1, 0, 0), -30);
            var box1 = _world.AddRigidBody(0, new float3(20, 100, 0), float3.Zero, boxShape);
            var box2 = _world.AddRigidBody(1, new float3(0, 250, 0), float3.Zero, MySphereCollider);
            var box3 = _world.AddRigidBody(0, new float3(-20, 50, 0), float3.Zero, boxShape);

        } 

        public void FallingTower1()
        {
            int num = 0;
            for (int k = 0; k < 5; k++)
            {
                for (int h = -2; h < 5; h++)
                {
                    for (int j = -2; j < 5; j++)
                    {
                        var pos = new float3((4 * h) , 20 + (k * 4), 4 * j);

                        //MyBoxCollider.LocalScaling = new float3(0.5f, 0.5f, 0.5f);
                        var cube = _world.AddRigidBody(1, pos, float3.Zero, MyBoxCollider);

                        cube.Friction = 1.0f;
                        cube.Restitution = 0.8f;
                        cube.SetDrag(0.0f, 0.05f);
                        num++;
                    }
                }
            }
            Debug.WriteLine("Number: " + num);
        }

        public void FallingSpheres()
        {
            for (int k = 0; k < 2; k++)
            {
                for (int h = -2; h < 2; h++)
                {
                    for (int j = -2; j < 5; j++)
                    {

                        var pos = new float3((4 * h)+25, 50 + (k * 4), 4 * j);

                        var sphere = _world.AddRigidBody(1, pos, float3.Zero, MySphereCollider);
                        sphere.Friction = 0.5f;
                        sphere.Restitution = 0.8f;
                    }
                }
            }
        }

        public void FallingPlatonics()
        {
            
            for (int k = 0; k < 5; k++)
            {
                for (int h = -2; h < 3; h++)
                {
                    for (int j = -2; j <3 ; j++)
                    {
                        var pos = new float3((4 * h) + 30, 50 + (k * 4), 4 * j);

                        var sphere = _world.AddRigidBody(1, pos, float3.Zero, MyConvHull);
                        
                        sphere.Friction = 0.5f;
                        sphere.Restitution = 0.2f;
                    }
                }
            }

            
        }

        public void FallingTeaPots()
        {
            int numTea = 0;
            for (int k = 0; k < 5; k++)
            {
                for (int h = -2; h < 4; h++)
                {
                    for (int j = -2; j < 4; j++)
                    {
                        var pos = new float3((10*h), 20 + (k*10), 10*j);
                        var cube = _world.AddRigidBody(1, pos, float3.Zero, TeaPotHull);
                        cube.Friction = 1.0f;
                        cube.SetDrag(0.0f, 0.05f);
                        numTea++;
                    }
                }
            }
            Debug.WriteLine("NumberTea: " + numTea);
        }

        public void InitPoint2PointConstraint()
        {
            
            var rbA = _world.AddRigidBody(1, new float3(-20, 15, 0), float3.Zero, MyBoxCollider);
            rbA.LinearFactor = new float3(0,0,0);
            rbA.AngularFactor = new float3(0, 0, 0);

            var rbB = _world.AddRigidBody(1, new float3(-21, 10, 0), float3.Zero, MyBoxCollider);
            var p2p = _world.AddPoint2PointConstraint(rbA, rbB, new float3(0, -3f, 0), new float3(0, 2.5f, 0));
            p2p.SetParam(PointToPointFlags.PointToPointFlagsCfm, 0.9f);

            var rbC = _world.AddRigidBody(1, new float3(-21, 5, 2), float3.Zero, MyBoxCollider);
            var p2p1 = _world.AddPoint2PointConstraint(rbB, rbC, new float3(0, -2.5f, 0), new float3(0, 2.5f, 0));
  
        }

        public void InitHingeConstraint()
        {
            var rot = new float3(0, (float) Math.PI/4, 0);
            //var mesh = MeshReader.LoadMesh(@"Assets/Cube.obj.model");
            var rbA = _world.AddRigidBody(1, new float3(0, 15, 0), float3.Zero, MyBoxCollider);
            rbA.LinearFactor = new float3(0, 0, 0);
            rbA.AngularFactor = new float3(0, 0, 0);

            var rbB = _world.AddRigidBody(1, new float3(0, 10, 0), float3.Zero, MyBoxCollider);
            
            var hc = _world.AddHingeConstraint(rbA, rbB, new float3(0, -5, 0), new float3(0, 2, 0), new float3(0, 0, 1), new float3(0, 0, 1), false);

            hc.SetLimit(-(float)Math.PI * 0.25f, (float)Math.PI * 0.25f);
        }

        public void InitSliderConstraint()
        {
            var mesh = MeshReader.LoadMesh(@"Assets/Cube.obj.model");
            var rbA = _world.AddRigidBody(1, new float3(400, 500, 0), float3.Zero, MyBoxCollider);
            rbA.LinearFactor = new float3(0, 0, 0);
            rbA.AngularFactor = new float3(0, 0, 0);

            var rbB = _world.AddRigidBody(1, new float3(200, 500, 0), float3.Zero, MyBoxCollider);

            var frameInA = float4x4.Identity;
            frameInA.Row3 = new float4(0,1,0,1);
            var frameInB = float4x4.Identity;
            frameInA.Row3 = new float4(0, 0, 0, 1);
            var sc = _world.AddSliderConstraint(rbA, rbB, frameInA, frameInB, true);

        }

        public void InitGearConstraint()
        {
            var mesh = MeshReader.LoadMesh(@"Assets/Cube.obj.model");
            var rbA = _world.AddRigidBody(0, new float3(0, 150, 0), float3.Zero, MyBoxCollider);
            //rbA.LinearFactor = new float3(0, 0, 0);
            //rbA.AngularFactor = new float3(0, 0, 0);

            var rbB = _world.AddRigidBody(1, new float3(0, 300, 0), float3.Zero, MyBoxCollider);
            //rbB.LinearFactor = new float3(0,0,0);
            ////var axisInB = new float3(0, 1, 0);
            // var gc = _world.AddGearConstraint(rbA, rbB, axisInA, axisInB);
        }

        public void InitDfo6Constraint()
        {
            InitWorld();
            GroundPlane(new float3(0, 0, 0), float3.Zero);
            var rbB = _world.AddRigidBody(1, new float3(0, 25, 0), float3.Zero, MyBoxCollider);
            var framInB = float4x4.CreateTranslation(new float3(0,-10,0));
            var dof6 = _world.AddGeneric6DofConstraint( rbB,  framInB, false);
            dof6.LinearLowerLimit = new float3(0,0,0);
            dof6.LinearUpperLimit = new float3(0,0,0);
            dof6.AngularLowerLimit = new float3(0,0,0);
            dof6.AngularUpperLimit = new float3(0,0,0);
        }

        public void CompoundShape()
        {
            var compShape = _world.AddCompoundShape(true);
            var box = _world.AddBoxShape(25);
            var sphere = _world.AddBoxShape(25);
            var matrixBox = float4x4.Identity;
            var matrixSphere = new float4x4(1, 0, 0, 2, 0, 1, 0, 2, 0, 0, 1, 2, 0, 0, 0, 1);
            compShape.AddChildShape(matrixBox, box);
            compShape.AddChildShape(matrixSphere, sphere);
            var rb = _world.AddRigidBody(1, new float3(0, 150, 0), float3.Zero, compShape);
        }

        public void InitGImpacShape()
        {
            var gimp = _world.AddGImpactMeshShape(TeaPotMesh);
            var rbB = _world.AddRigidBody(1, new float3(0, 10, 0), float3.Zero, gimp);
        }

        public void Tester()
        {
            InitWorld();
            InitColliders();
            GroundPlane(float3.Zero, new float3(0, 0, (float)Math.PI / 6));
            var compound = _world.AddCompoundShape(true);
         //   float4x4 pos4x4_1 = float4x4.CreateTranslation(new float3(0, 10, 10));
        //    float4x4 pos4x4_2 = float4x4.CreateTranslation(new float3(10, 0, 10));
          //  float4x4 pos4x4_3 = float4x4.CreateTranslation(new float3(0,0, 0));
          //  compound.AddChildShape(pos4x4_1, MyBoxCollider);
           // compound.AddChildShape(pos4x4_2, MySphereCollider);
           // compound.AddChildShape(pos4x4_3, MyCylinderCollider);
            var rb1 = _world.AddRigidBody(1, new float3(0, 10, 0), new float3(0, 0, -(float)Math.PI / 6), compound);
           
            
        }


        public void Shoot(float3 camPos, float3 target)
        {
            var ball = _world.AddRigidBody(1, camPos, float3.Zero, MySphereCollider);
            var impulse = target - camPos;
            impulse.Normalize();
            ball.ApplyCentralImpulse = impulse * 100;
        }


    }
}
