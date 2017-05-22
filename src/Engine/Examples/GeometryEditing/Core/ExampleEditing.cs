﻿using System.Collections.Generic;
using System.Xml;
using Fusee.Engine.Common;
using Fusee.Engine.Core;
using Fusee.Jometri.DCEL;
using Fusee.Jometri.Manipulation;
using Fusee.Jometri.Triangulation;
using Fusee.Math.Core;
using Fusee.Serialization;
using Fusee.Xene;
using static Fusee.Engine.Core.Input;
using static Fusee.Engine.Core.Time;
using Geometry = Fusee.Jometri.DCEL.Geometry;


namespace Fusee.Engine.Examples.MeshingAround.Core
{

    [FuseeApplication(Name = "Geometry Editing", Description = "Example App to show basic geometry editing in FUSEE")]
    public class ExampleEditing : RenderCanvas
    {
        private float _alpha;
        private float _beta;

        // angle variables
        private static float _angleHorz = M.PiOver6 * 2.0f, _angleVert = -M.PiOver6 * 0.5f,
                             _angleVelHorz, _angleVelVert, _angleRoll, _angleRollInit, _zoomVel, _zoom=8, _xPos, _yPos;
        private static float2 _offset;
        private static float2 _offsetInit;

        private const float RotationSpeed = 7;
        private const float Damping = 0.8f;

        private SceneNodeContainer _parentNode;
        private float4x4 _sceneScale = float4x4.CreateScale(1);
        private float4x4 _projection;
        private bool _twoTouchRepeated;
        
        private SceneRenderer _renderer;

        // Init is called on startup. 
        public override void Init()
        {

            ////////////////// Fill SceneNodeContainer ////////////////////////////////
            _parentNode = new SceneNodeContainer
            {
                Components = new List<SceneComponentContainer>(),
                Children = new List<SceneNodeContainer>()
            };

            var parentTrans = new TransformComponent
            {
                Rotation = float3.Zero,
                Scale = float3.One,
                Translation = new float3(0, 0, 0)
            };
            _parentNode.Components.Add(parentTrans);
            //////////////////////////////////////////////////////////////////////////

            Geometry sphere = CreateGeometry.CreateSpehreGeometry(2,22,11);
            sphere = SubdivisionSurface.CatmullClarkSubdivision(sphere);
            AddGeometryToSceneNode(sphere, new float3(0,0,0));

            Geometry cuboid = CreateGeometry.CreateCuboidGeometry(5, 2, 5);
            AddGeometryToSceneNode(cuboid, new float3(-5,0,0));
            

            var sc = new SceneContainer { Children = new List<SceneNodeContainer> { _parentNode } };            
            _renderer = new SceneRenderer(sc);
           
            SelectGeometry();            
            RC.ClearColor = new float4(.7f, .7f, .7f, 1);

        }

        private void AddGeometryToSceneNode(Geometry geometry, float3 position)
        {
            geometry.Triangulate();
            var geometryMesh = new JometriMesh(geometry);

            var sceneNodeContainer = new SceneNodeContainer { Components = new List<SceneComponentContainer>() };

            var meshComponent = new MeshComponent
            {
                Vertices = geometryMesh.Vertices,
                Triangles = geometryMesh.Triangles,
                Normals = geometryMesh.Normals,
            };
            var translationComponent = new TransformComponent
            {
                Rotation = float3.Zero,
                Scale = new float3(1, 1, 1),
                Translation = position
            };
            var materialComponent = new MaterialComponent
            {                
                Diffuse = new MatChannelContainer()
            };

            sceneNodeContainer.Components.Add(translationComponent);
            sceneNodeContainer.Components.Add(meshComponent);
            sceneNodeContainer.Components.Add(materialComponent);

            _parentNode.Children.Add(sceneNodeContainer);
        }

        // RenderAFrame is called once a frame
        public override void RenderAFrame()
        {

            // Clear the backbuffer
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            HandleCamera();
            _renderer.Render(RC);            

            Present();
        }

        private void SelectGeometry()
        {
            var material = _parentNode.Children[1].GetMaterial();
            material.Diffuse.Color = new float3(0,.5f,.5f);
        }

        private void HandleCamera()
        {
            var curDamp = (float)System.Math.Exp(-Damping * DeltaTime);

            //Camera Rotation
            if (Mouse.MiddleButton && !Keyboard.GetKey(KeyCodes.LShift))
            {
                _angleVelHorz = -RotationSpeed * Mouse.XVel * 0.00002f;
                _angleVelVert = RotationSpeed * Mouse.YVel * 0.00002f;
            }
            else if (Touch.GetTouchActive(TouchPoints.Touchpoint_0) && !Touch.TwoPoint)
            {
                float2 touchVel;
                touchVel = Touch.GetVelocity(TouchPoints.Touchpoint_0);
                _angleVelHorz = -RotationSpeed * touchVel.x * 0.00002f;
                _angleVelVert = RotationSpeed * touchVel.y * 0.00002f;
            }

            // Zoom & Roll
            if (Touch.TwoPoint)
            {
                if (!_twoTouchRepeated)
                {
                    _twoTouchRepeated = true;
                    _angleRollInit = Touch.TwoPointAngle - _angleRoll;
                    _offsetInit = Touch.TwoPointMidPoint - _offset;
                }
                _zoomVel = Touch.TwoPointDistanceVel * -0.001f;
                _angleRoll = Touch.TwoPointAngle - _angleRollInit;
                _offset = Touch.TwoPointMidPoint - _offsetInit;
            }
            else
            {
                _twoTouchRepeated = false;
                _zoomVel = Mouse.WheelVel * -0.005f;
                _angleRoll *= curDamp * 0.8f;
                _offset *= curDamp * 0.8f;
            }
            _zoom += _zoomVel;
            // Limit zoom
            if (_zoom < 2)
                _zoom = 2;

            _angleHorz += _angleVelHorz;
            // Wrap-around to keep _angleHorz between -PI and + PI
            _angleHorz = M.MinAngle(_angleHorz);

            _angleVert += _angleVelVert;
            // Limit pitch to the range between [-PI/2, + PI/2]
            _angleVert = M.Clamp(_angleVert, -M.PiOver2, M.PiOver2);

            // Wrap-around to keep _angleRoll between -PI and + PI
            _angleRoll = M.MinAngle(_angleRoll);

            //Camera Translation
            if (Keyboard.GetKey(KeyCodes.LShift) && Mouse.MiddleButton)
            {
                _xPos += -RotationSpeed * Mouse.XVel * 0.00002f;
                _yPos += RotationSpeed * Mouse.YVel * 0.00002f;
            }


            // Create the camera matrix and set it as the current ModelView transformation
            var mtxRot = float4x4.CreateRotationZ(_angleRoll) * float4x4.CreateRotationX(_angleVert) * float4x4.CreateRotationY(_angleHorz);
            var mtxCam = float4x4.LookAt(_xPos, _yPos, -_zoom, _xPos, _yPos, 0, 0, 1, 0);
            RC.ModelView = mtxCam * mtxRot * _sceneScale;
            //var mtxOffset = float4x4.CreateTranslation(2 * _offset.x / Width, -2 * _offset.y / Height, 0);
            RC.Projection = /*mtxOffset **/ _projection;
        }

        // Is called when the window was resized
        public override void Resize()
        {
            // Set the new rendering area to the entire new windows size
            RC.Viewport(0, 0, Width, Height);

            // Create a new projection matrix generating undistorted images on the new aspect ratio.
            var aspectRatio = Width / (float)Height;

            // 0.25*PI Rad -> 45° Opening angle along the vertical direction. Horizontal opening angle is calculated based on the aspect ratio
            // Front clipping happens at 1 (Objects nearer than 1 world unit get clipped)
            // Back clipping happens at 2000 (Anything further away from the camera than 2000 world units gets clipped, polygons will be cut)
            _projection = float4x4.CreatePerspectiveFieldOfView(M.PiOver4, aspectRatio, 1, 2000000);
        }

    }
}