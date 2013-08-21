using System;
using Fusee.Engine;
using Fusee.SceneManagement;
using Fusee.Math;

namespace Examples.BlendingTest
{
    public class BlendingTest : RenderCanvas
    {
        // angle variables
        private float _angleHorz, _angleVert, _angleVelHorz, _angleVelVert;

        private const float RotationSpeed = 1f;
        private const float Damping = 0.92f;

        // model variables
        private Mesh _meshTea;

        // variables for shader
        private ShaderProgram _spColor;
        private IShaderParam _colorParam;

        private ShaderEffect _shaderEffect = new ShaderEffect( new EffectPassDeclaration[]
            {
               new EffectPassDeclaration(){
                   VS = @"
            /* Copies incoming vertex color without change.
             * Applies the transformation matrix to vertex position.
             */

            attribute vec4 fuColor;
            attribute vec3 fuVertex;
            attribute vec3 fuNormal;
            attribute vec2 fuUV;
                    
            varying vec4 vColor;
            varying vec3 vNormal;
            varying vec2 vUV;
        
            uniform mat4 FUSEE_MVP;
            uniform mat4 FUSEE_ITMV;

            void main()
            {
                gl_Position = FUSEE_MVP * vec4(fuVertex, 1.0);
                vNormal = mat3(FUSEE_ITMV[0].xyz, FUSEE_ITMV[1].xyz, FUSEE_ITMV[2].xyz) * fuNormal;
                vUV = fuUV;
            }",

        PS = @"
            /* Copies incoming fragment color without change. */
            #ifdef GL_ES
                precision highp float;
            #endif
        
            uniform vec4 vColor;
            varying vec3 vNormal;

            void main()
            {
                gl_FragColor = vColor * dot(vNormal, vec3(0, 0, 1));
            }",

         StateSet = new RenderStateSet()
               {
                    AlphaBlendEnable = true,
                    BlendFactor = new float4(0.5f, 0.5f, 0.5f, 0.5f),
                    BlendOperation = BlendOperation.Add,
                    SourceBlend = Blend.BlendFactor,
                    DestinationBlend = Blend.InverseBlendFactor
                }
             },
         }); 


        // is called on startup
        public override void Init()
        {
            // ColorUint constructor test
            ColorUint ui1 = new ColorUint((uint) 4711);
            ColorUint ui2 = new ColorUint((byte)42, (byte)43, (byte)44, (byte)45);
            ColorUint u3 = new ColorUint((float)1, (float)43, (float)44, (float)45);




            RC.ClearColor = new float4(1f, 1f, 1f, 1);

            // initialize the variables
            _meshTea = MeshReader.LoadMesh(@"Assets/Teapot.obj.model");

            _shaderEffect.AttachToContext(RC);


            _spColor = MoreShaders.GetShader("simple", RC);
            _colorParam = _spColor.GetShaderParam("vColor");

            /*
            RC.SetRenderState(RenderState.ZEnable, (uint) 1);            
            RC.SetRenderState(RenderState.AlphaBlendEnable, (uint) 1);
            RC.SetRenderState(RenderState.BlendFactor, (uint)new ColorUint(0.25f, 0.25f, 0.25f, 0.25f));
            RC.SetRenderState(RenderState.BlendOperation, (uint)(BlendOperation.Add));
            RC.SetRenderState(RenderState.SourceBlend, (uint)(Blend.BlendFactor));
            RC.SetRenderState(RenderState.DestinationBlend, (uint)(Blend.InverseBlendFactor));
            */

            RC.SetRenderState(new RenderStateSet
                {
                    AlphaBlendEnable = true,
                    BlendFactor = new float4(0.5f, 0.5f, 0.5f, 0.5f),
                    BlendOperation = BlendOperation.Add,
                    SourceBlend = Blend.BlendFactor,
                    DestinationBlend = Blend.InverseBlendFactor
                });
        }

        // is called once a frame
        public override void RenderAFrame()
        {
           RC.Clear(ClearFlags.Color | ClearFlags.Depth);
 
            // move per mouse
            if (Input.Instance.IsButtonDown(MouseButtons.Left))
            {
                _angleVelHorz = RotationSpeed * Input.Instance.GetAxis(InputAxis.MouseX);
                _angleVelVert = RotationSpeed * Input.Instance.GetAxis(InputAxis.MouseY);
            }
            else
            {
                var curDamp = (float)Math.Exp(-Damping * Time.Instance.DeltaTime);

                _angleVelHorz *= curDamp;
                _angleVelVert *= curDamp;
            }

            _angleHorz += _angleVelHorz;
            _angleVert += _angleVelVert;

            // move per keyboard
            if (Input.Instance.IsKeyDown(KeyCodes.Left))
                _angleHorz -= RotationSpeed * (float)Time.Instance.DeltaTime;

            if (Input.Instance.IsKeyDown(KeyCodes.Right))
                _angleHorz += RotationSpeed * (float)Time.Instance.DeltaTime;

            if (Input.Instance.IsKeyDown(KeyCodes.Up))
                _angleVert -= RotationSpeed * (float)Time.Instance.DeltaTime;

            if (Input.Instance.IsKeyDown(KeyCodes.Down))
                _angleVert += RotationSpeed * (float)Time.Instance.DeltaTime;

            var mtxRot = float4x4.CreateRotationY(_angleHorz) * float4x4.CreateRotationX(_angleVert);
            var mtxCam = float4x4.LookAt(0, 200, 500, 0, 0, 0, 0, 1, 0);

            // first mesh
            RC.ModelView = float4x4.CreateTranslation(0, -50, 0) * mtxRot* mtxCam;

            _shaderEffect.RenderMesh(_meshTea);

            /*
            RC.SetShader(_spColor);
            // RC.SetShaderParam(_colorParam, new float4(0.5f, 0.8f, 0, 1));
            RC.Render(_meshTea);
            */

            // swap buffers
            Present();
        }

        public override void Resize()
        {
            // is called when the window is resized
            RC.Viewport(0, 0, Width, Height);

            var aspectRatio = Width / (float)Height;
            RC.Projection = float4x4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 1, 10000);
        }

        public static void Main()
        {
            var app = new BlendingTest();
            app.Run();
        }

    }
}
