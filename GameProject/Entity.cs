﻿using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using FarseerPhysics.Dynamics;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace Game
{
    /// <summary>
    /// An object that exists within the world space and can be drawn
    /// </summary>
    public class Entity : Placeable2D
    {
        private ResourceID<Entity> _id = new ResourceID<Entity>();
        public ResourceID<Entity> ID
        {
            get { return _id; }
        }
        
        private Transform2D _velocity = new Transform2D();
        private List<Model> _models = new List<Model>();
        private List<ClipModel> ClipModels = new List<ClipModel>();
        private bool _isPortalable = false;

        public int BodyId = -1;

        //public Body Body;
        public Body Body
        {
            get 
            {
                if (BodyId == -1)
                {
                    return null;
                }
                Debug.Assert(Scene != null, "Entity must be assigned to a scene.");
                Debug.Assert(Scene.PhysWorld.BodyList.Exists(item => (item.BodyId == BodyId)), "Body id does not exist.");
                return Scene.PhysWorld.BodyList.Find(item => (item.BodyId == BodyId)); 
            }
        }
        /// <summary>
        /// Represents the size of the cutLines array within the fragment shader
        /// </summary>
        private const int CUT_LINE_ARRAY_MAX_LENGTH = 16;
        /// <summary>
        /// Whether or not this entity will interact with portals when intersecting them
        /// </summary>
        public bool IsPortalable
        {
            get { return _isPortalable; }
            set { _isPortalable = value; }
        }
        public virtual Transform2D Velocity { get { return _velocity; } set { _velocity = value; } }
        //[IgnoreDataMemberAttribute]
        public virtual List<Model> Models { get { return _models; } set { _models = value; } }
        
        public class ClipModel
        {
            private Line[] _clipLines;
            public Line[] ClipLines { get { return _clipLines; } }
            private Model _model;
            public Model Model { get { return _model; } }
            private Matrix4 _transform;
            public Matrix4 Transform { get { return _transform; } }

            public ClipModel (Model model, Line[] clipLines, Matrix4 transform)
            {
                _model = model;
                _clipLines = clipLines;
                _transform = transform;
            }
        }

        private Entity()
        {
        }

        public Entity(Scene scene)
            : base(scene)
        {
        }

        public Entity(Vector2 position)
        {
            Transform.Position = position;
        }

        public Entity(Scene scene, Vector2 position) : this(scene)
        {
            Transform.Position = position;
        }

        public Entity(Scene scene, Transform2D transform) : this(scene)
        {
            Transform.SetLocal(transform);
        }

        public void LinkBody(Body body)
        {
            Transform.UniformScale = true;
            BodyUserData userData = new BodyUserData(this);
            Debug.Assert(body.UserData == null, "This body has UserData already assigned to it.");
            body.UserData = userData;
            BodyId = body.BodyId;
            //Body = body;
        }

        public virtual void Step()
        {
            if (Body != null)
            {
                Transform.Position = VectorExt2.ConvertTo(Body.Position);
                Transform.Rotation = Body.Rotation;
            }
        }

        public void PositionUpdate()
        {
            foreach (Portal portal in Scene.PortalList)
            {
                //position the entity slightly outside of the exit portal to avoid precision issues with portal collision checking
                Line exitLine = new Line(portal.GetWorldVerts());
                float distanceToPortal = exitLine.PointDistance(Transform.Position, true);
                if (distanceToPortal < Portal.EntityMinDistance)
                {
                    Vector2 exitNormal = portal.Transform.GetNormal();
                    if (exitLine.GetSideOf(Transform.Position) != exitLine.GetSideOf(exitNormal + portal.Transform.Position))
                    {
                        exitNormal = -exitNormal;
                    }

                    Vector2 pos = exitNormal * (Portal.EntityMinDistance - distanceToPortal);
                    /*if (Transform.Parent != null)
                    {
                        pos = Transform.Parent.WorldToLocal(pos);
                    }*/
                    Transform.Position += pos;
                    break;
                }
            }
        }

        public virtual void Render(Matrix4 viewMatrix, float timeDelta)
        {
            foreach (Model v in Models)
            {
                List<Vector3> verts = new List<Vector3>();
                List<int> inds = new List<int>();
                List<Vector3> colors = new List<Vector3>();
                List<Vector2> texcoords = new List<Vector2>();

                // Assemble vertex and indice data for all volumes
                int vertcount = 0;
                
                verts.AddRange(v.GetVerts().ToList());
                inds.AddRange(v.GetIndices().ToList());
                colors.AddRange(v.GetColorData().ToList());
                texcoords.AddRange(v.GetTextureCoords());
                vertcount += v.Vertices.Count;

                Vector3[] vertdata;
                Vector3[] coldata;
                Vector2[] texcoorddata;
                int[] indicedata;
                int indiceat = 0;

                vertdata = verts.ToArray();
                indicedata = inds.ToArray();
                coldata = colors.ToArray();
                texcoorddata = texcoords.ToArray();

                GL.BindBuffer(BufferTarget.ArrayBuffer, v.Shader.GetBuffer("vPosition"));

                GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(vertdata.Length * Vector3.SizeInBytes), vertdata, BufferUsageHint.StreamDraw);
                GL.VertexAttribPointer(v.Shader.GetAttribute("vPosition"), 3, VertexAttribPointerType.Float, false, 0, 0);

                // Buffer vertex color if shader supports it
                if (v.Shader.GetAttribute("vColor") != -1)
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, v.Shader.GetBuffer("vColor"));
                    GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(coldata.Length * Vector3.SizeInBytes), coldata, BufferUsageHint.StreamDraw);
                    GL.VertexAttribPointer(v.Shader.GetAttribute("vColor"), 3, VertexAttribPointerType.Float, true, 0, 0);
                }

                // Buffer texture coordinates if shader supports it
                if (v.Shader.GetAttribute("texcoord") != -1)
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, v.Shader.GetBuffer("texcoord"));
                    GL.BufferData<Vector2>(BufferTarget.ArrayBuffer, (IntPtr)(texcoorddata.Length * Vector2.SizeInBytes), texcoorddata, BufferUsageHint.StreamDraw);
                    GL.VertexAttribPointer(v.Shader.GetAttribute("texcoord"), 2, VertexAttribPointerType.Float, true, 0, 0);
                }

                GL.UseProgram(v.Shader.ProgramID);

                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

                // Buffer index data
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, v.IboElements);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(indicedata.Length * sizeof(int)), indicedata, BufferUsageHint.StreamDraw);

                GL.BindTexture(TextureTarget.Texture2D, v.TextureId);
                
                Matrix4 UVMatrix = v.TransformUv.GetMatrix();
                GL.UniformMatrix4(v.Shader.GetUniform("UVMatrix"), false, ref UVMatrix);

                if (v.Shader.GetAttribute("maintexture") != -1)
                {
                    GL.Uniform1(v.Shader.GetAttribute("maintexture"), v.TextureId);
                }

                if (v.Wireframe)
                {
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                }
                
                if (IsPortalable)
                {
                    UpdatePortalClipping(4);
                    _RenderClipModels(ClipModels, viewMatrix);
                }
                else
                {
                    GL.Uniform1(v.Shader.GetUniform("cutLinesLength"), 0);
                    _RenderSetTransformMatrix(v, viewMatrix);
                    GL.DrawElements(BeginMode.Triangles, v.Indices.Count, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                }
                
                if (v.Wireframe)
                {
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                }
                //indiceat += v.IndiceCount;
            }
        }

        private void _RenderSetTransformMatrix(Model model, Matrix4 viewMatrix)
        {
            Matrix4 modelMatrix = model.Transform.GetMatrix() * Transform.GetWorldMatrix() * viewMatrix;
            GL.UniformMatrix4(model.Shader.GetUniform("modelMatrix"), false, ref modelMatrix);    
        }

        private void UpdatePortalClipping(int depth)
        {
            ClipModels.Clear();
            foreach (Model m in Models)
            {
                _ModelPortalClipping(m, Transform.WorldPosition, null, Matrix4.Identity, 4, ref ClipModels);
            }
        }

        /// <param name="depth">Number of iterations.</param>
        /// <param name="clipModels">Adds the ClipModel instances to this list.</param>
        private void _ModelPortalClipping(Model model, Vector2 centerPoint, Portal portalEnter, Matrix4 modelMatrix, int depth, ref List<ClipModel> clipModels)
        {
            if (depth <= 0)
            {
                return;
            }
            List<float> cutLines = new List<float>();
            List<Portal> collisions = new List<Portal>();
            foreach (Portal portal in Scene.PortalList)
            {
                Line portalLine = new Line(portal.GetWorldVerts());
                Vector2[] convexHull = VectorExt2.Transform(model.GetWorldConvexHull(), this.Transform.GetWorldMatrix() * modelMatrix);

                if (portalLine.IsInsideOfPolygon(convexHull))
                {
                    collisions.Add(portal);
                }
            }

            collisions = collisions.OrderBy(item => (item.Transform.WorldPosition - centerPoint).Length).ToList();
            for (int i = 0; i < collisions.Count; i++)
            {
                Portal portal = collisions[i];
                for (int j = collisions.Count - 1; j > i; j--)
                {
                    Line currentLine = new Line(collisions[i].GetWorldVerts());
                    Line checkLine = new Line(collisions[j].GetWorldVerts());
                    Line.Side checkSide = currentLine.GetSideOf(checkLine);
                    if (checkSide != currentLine.GetSideOf(centerPoint))
                    {
                        collisions.RemoveAt(j);
                    }
                }
            }
            
            List<Line> clipLines = new List<Line>();
            foreach (Portal portal in collisions)
            {
                Vector2[] pv = portal.GetWorldVerts();
                Line clipLine = new Line(pv);

                Line portalLine = new Line(pv);
                Vector2 normal = portal.Transform.GetWorldNormal();
                if (portal.Transform.WorldIsMirrored())
                {
                    normal = -normal;
                }

                Vector2 portalNormal = portal.Transform.WorldPosition + normal;
                if (portalLine.GetSideOf(centerPoint) != portalLine.GetSideOf(portalNormal))
                {
                    normal *= Portal.EntityMinDistance;
                }
                else
                {
                    clipLine.Reverse();
                    normal *= -Portal.EntityMinDistance;
                }

                clipLines.Add(clipLine);
                if (portalEnter == null || portal != portalEnter.Linked)
                {
                    Vector2 centerPointNext = VectorExt2.Transform(portal.Transform.WorldPosition + normal, portal.GetPortalMatrix());
                    _ModelPortalClipping(model, centerPointNext, portal, modelMatrix * portal.GetPortalMatrix(), depth - 1, ref clipModels);
                }
            }
            
            ClipModels.Add(new ClipModel(model, clipLines.ToArray(), modelMatrix));
            
        }

        private void _RenderClipModels(List<ClipModel> clipModels, Matrix4 viewMatrix)
        {
            Matrix4 ScaleMatrix;
            ScaleMatrix = viewMatrix * Matrix4.CreateTranslation(new Vector3(1, 1, 0)) * Matrix4.CreateScale(new Vector3(Controller.ClientSize.Width / (float)2, Controller.ClientSize.Height / (float)2, 0));

            Vector2[] mirrorTest = new Vector2[3] {
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(0, 0)
            };
            bool isMirrored;
            mirrorTest = VectorExt2.Transform(mirrorTest, viewMatrix);
            isMirrored = MathExt.AngleDiff(MathExt.AngleVector(mirrorTest[0] - mirrorTest[2]), MathExt.AngleVector(mirrorTest[1] - mirrorTest[2])) > 0;
            foreach (ClipModel cm in clipModels)
            {
                List<float> cutLines = new List<float>();
                foreach (Line l in cm.ClipLines)
                {
                    if (isMirrored)
                    {
                        l.Reverse();
                    }
                    l.Transform(ScaleMatrix);
                    cutLines.AddRange(new float[4] {
                        l.Vertices[0].X,
                        l.Vertices[0].Y,
                        l.Vertices[1].X,
                        l.Vertices[1].Y
                    });
                }

                GL.Uniform1(cm.Model.Shader.GetUniform("cutLinesLength"), cutLines.Count);
                //GL.Uniform1(model.Shader.GetUniform("cutLines"), cutLines.Count, cutLines.ToArray());
                GL.Uniform1(GL.GetUniformLocation(cm.Model.Shader.ProgramID, "cutLines[0]"), cutLines.Count, cutLines.ToArray());
                _RenderSetTransformMatrix(cm.Model, cm.Transform * viewMatrix);
                GL.DrawElements(BeginMode.Triangles, cm.Model.Indices.Count, DrawElementsType.UnsignedInt, 0);
            }
        }
    }
}