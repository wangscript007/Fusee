﻿using System;
using System.Collections.Generic;
using System.Linq;
using Fusee.Base.Core;
using Fusee.Math.Core;

namespace Fusee.Jometri
{
    /// <summary>
    /// Stores geometry in a DCEL (doubly conneted edge list).
    /// </summary>
    public class Geometry
    {
        #region Members

        /// <summary>
        /// Contains handles to half edges. Use this to find adjecant half edges or the origin vertex.
        /// </summary>
        public IList<HalfEdgeHandle> HalfEdgeHandles;

        /// <summary>
        /// Contains handles to outlines. Use this to find the first half edge.
        /// </summary>
        public IList<FaceHandle> FaceHandles;

        /// <summary>
        /// Contains handles to outline. Use this to get the vertexes coordinates.
        /// </summary>
        public IList<VertHandle> VertHandles;

        private readonly List<Vertex> _vertices;
        private readonly List<HalfEdge> _halfEdges;
        private readonly List<Face> _faces;

        #endregion

        /// <summary>
        /// Stores geometry in a DCEL (doubly conneted edge list).
        /// </summary>
        /// <param name="outlines">A collection of the geometrys' outlines, each containing the geometric information as a list of float3 in ccw order</param>
        public Geometry(IEnumerable<Outline> outlines)
        {
            _vertices = new List<Vertex>();
            _halfEdges = new List<HalfEdge>();
            _faces = new List<Face>();

            HalfEdgeHandles = new List<HalfEdgeHandle>();
            FaceHandles = new List<FaceHandle>();
            VertHandles = new List<VertHandle>();

            CreateHalfEdgesForGeometry(outlines);
        }

        #region Structs

        /// <summary>
        /// Each face contains:
        /// A handle to assign a abstract reference to it.
        /// A handle to the first half edge that belongs to this face.
        /// </summary>
        internal struct Face
        {
            internal FaceHandle Handle;
            internal HalfEdgeHandle FirstHalfEdge;
            internal List<HalfEdgeHandle> InnerHalfEdges;
        }

        /// <summary>
        /// Each vertex contains:
        /// A handle to assign a abstract reference to it.
        /// The vertex' coordinates.
        /// </summary>
        public struct Vertex
        {
            /// <summary>
            /// The vertex' reference.
            /// </summary>
            public VertHandle Handle;

            /// <summary>
            /// The geometric data of the vertex
            /// </summary>
            public float3 Coord;

            /// <summary>
            /// The handle to the half edge with this vertex as origin
            /// </summary>
            public HalfEdgeHandle IncidentHalfEdge;

            
            /// <summary>
            /// The vertex' constuctor.
            /// </summary>
            /// <param name="coord">The new vertex' coordinates</param>
            public Vertex(float3 coord)
            {
                Handle = new VertHandle();
                IncidentHalfEdge = new HalfEdgeHandle();
                Coord = coord;
            }
        }

        /// <summary>
        /// Represents a half edge.
        /// Each half edge contains:
        /// A handle to assign a abstract reference to it.
        /// A handle to the half edges' origin vertex.
        /// A handle to the next half edge (in ccw order).
        /// A handle to the previous half edge (in ccw order).
        /// A handle to the face it belongs to.
        /// </summary>
        internal struct HalfEdge
        {
            internal HalfEdgeHandle Handle;

            internal VertHandle Origin;
            internal HalfEdgeHandle Twin;
            internal HalfEdgeHandle Next;
            internal HalfEdgeHandle Prev;
            internal FaceHandle IncidentFace;
        }

        /// <summary>
        /// Represents a outer or inner boundary of a face
        /// </summary>
        public struct Outline
        {
            /// <summary>
            /// The geometric information of the vertices which belong to a boundary
            /// </summary>
            public IList<float3> Points;

            /// <summary>
            /// Determines wheather a boundary is a outer bondary or a inner boundary (which forms a hole in the face).
            /// </summary>
            public bool IsOuter;
        }

        #endregion

        /*Insert methods like:
            >InsertVertex
            >InsertFace
            >Get all edges adjecant to a vertex
            >Get all edges that belong to a face
            >etc.
        */

        #region public Methods

        /// <summary>
        /// Inserts a pair of half edges between two outline.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="q"></param>
        /// <exception cref="Exception"></exception>
        public void InsertHalfEdge(VertHandle p, VertHandle q)
        {
            var vertP = GetVertexByHandle(p);
            var vertQ = GetVertexByHandle(q);

            var heStartingAtP = HalfEdgesStartingAtV(vertP).ToList();
            var heStaringAtQ = HalfEdgesStartingAtV(vertQ).ToList();

            var face = new Face();
            var pStartHe = new HalfEdge();
            var qStartHe = new HalfEdge();

            foreach (var heP in heStartingAtP)
            {
                var faceHeP = GetHalfEdgeByHandle(heP).IncidentFace;

                foreach (var heQ in heStaringAtQ)
                {
                    var faceHeQ = GetHalfEdgeByHandle(heQ).IncidentFace;

                    if (faceHeP.Id == faceHeQ.Id)
                    {
                        face = GetFaceByHandle(faceHeP);
                        pStartHe = GetHalfEdgeByHandle(vertP.IncidentHalfEdge);
                        qStartHe = GetHalfEdgeByHandle(vertQ.IncidentHalfEdge);
                    }
                    else
                    {
                        throw new ArgumentException("Vertex " + p + " vertex " + q + " have no common Face!");
                    }
                }
            }

            var newFromP = new HalfEdge();
            var newFromQ = new HalfEdge();
            var newFace = new Face
            {
                Handle = new FaceHandle(_faces.Count + 1),
                InnerHalfEdges = new List<HalfEdgeHandle>()
            };
            FaceHandles.Add(newFace.Handle);

            newFromP.Origin = p;
            newFromP.Next = qStartHe.Handle;
            newFromP.Prev = pStartHe.Prev;
            newFromP.IncidentFace = newFace.Handle;
            newFromP.Handle = new HalfEdgeHandle(HalfEdgeHandles.Count + 1);

            newFromQ.Origin = q;
            newFromQ.Next = pStartHe.Handle;
            newFromQ.Prev = qStartHe.Prev;
            newFromQ.IncidentFace = face.Handle;
            newFromQ.Handle = new HalfEdgeHandle(HalfEdgeHandles.Count + 1);

            newFromP.Twin = newFromQ.Handle;
            newFromQ.Twin = newFromP.Handle;

            newFace.FirstHalfEdge = newFromP.Handle;
            _faces.Add(newFace);

            HalfEdgeHandles.Add(newFromP.Handle);
            HalfEdgeHandles.Add(newFromQ.Handle);

            _halfEdges.Add(newFromP);
            _halfEdges.Add(newFromQ);

            //Assign new Next to previous HalfEdges from p and q  //Assign new prev for qStartHe and pStartHe
            var prevHeP = GetHalfEdgeByHandle(pStartHe.Prev);
            var prevHeQ = GetHalfEdgeByHandle(qStartHe.Prev);
            var count = 0;
            for (var i = 0; i < _halfEdges.Count; i++)
            {
                var he = _halfEdges[i];
                if (he.Handle.Id == prevHeP.Handle.Id)
                {
                    he.Next = newFromP.Handle;
                    _halfEdges[i] = he;
                    count++;
                }
                else if (he.Handle.Id == prevHeQ.Handle.Id)
                {
                    he.Next = newFromQ.Handle;
                    _halfEdges[i] = he;
                    count++;
                }
                else if (_halfEdges[i].Handle.Id == pStartHe.Handle.Id)
                {
                    he.Prev = newFromQ.Handle;
                    _halfEdges[i] = he;
                    count++;
                }
                else if (_halfEdges[i].Handle.Id == qStartHe.Handle.Id)
                {
                    he.Prev = newFromP.Handle;
                    _halfEdges[i] = he;
                    count++;
                }
                if (count == 4) break;
            }

            //Assign the handle of the new face to its half edges
            var currentHe = qStartHe;
            do
            {
                currentHe.IncidentFace = newFace.Handle;

                for (var i = 0; i < _halfEdges.Count; i++)
                {
                    if (_halfEdges[i].Handle.Id != currentHe.Handle.Id) continue;
                    currentHe.IncidentFace = newFace.Handle;
                    _halfEdges[i] = currentHe;
                    break;
                }
                currentHe = GetHalfEdgeByHandle(currentHe.Next);
                

            } while (currentHe.Handle.Id != qStartHe.Handle.Id);
        }

        /// <summary>
        /// Gets a vertex by its handle
        /// </summary>
        /// <param name="vertexHandle">The vertex' reference</param>
        /// <returns></returns>
        public Vertex GetVertexByHandle(VertHandle vertexHandle)
        {
            foreach (var e in _vertices)
            {
                if (e.Handle.Id == vertexHandle.Id)
                    return e;
            }
            throw new HandleNotFoundException("HalfEdge with id " + vertexHandle.Id + " not found!");
        }

        /// <summary>
        /// This collection contains all Vertices of a certain face.
        /// </summary>
        /// <param name="face">The faces reference</param>
        /// <returns></returns>
        public IEnumerable<Vertex> GetVeticesFromFace(FaceHandle face)
        {
            //Outer Outline
            var fistHalfEdgeHandle = GetFaceByHandle(face).FirstHalfEdge;
            var halfEdgeOuter = GetHalfEdgeByHandle(fistHalfEdgeHandle);

            do
            {
                var originVert = halfEdgeOuter.Origin;
                yield return GetVertexByHandle(originVert);
                halfEdgeOuter = GetHalfEdgeByHandle(halfEdgeOuter.Next);

            } while (halfEdgeOuter.Handle.Id != fistHalfEdgeHandle.Id);

            //Inner Outlines
            var innerComponents = GetFaceByHandle(face).InnerHalfEdges;

            if (innerComponents.Count == 0) yield break;

            foreach (var comp in innerComponents)
            {
                var halfEdgeInner = GetHalfEdgeByHandle(comp);

                do
                {
                    var originVert = halfEdgeInner.Origin;
                    yield return GetVertexByHandle(originVert);
                    halfEdgeInner = GetHalfEdgeByHandle(halfEdgeInner.Next);

                } while (halfEdgeInner.Handle.Id != comp.Id);

            }
        }
        #endregion

        #region internal Methods

        /// <summary>
        /// This collection contains all handles to HalfEdges which are starting at a certain vertex.
        /// </summary>
        /// <param name="v">The start vertex.</param>
        /// <returns></returns>
        internal IEnumerable<HalfEdgeHandle> HalfEdgesStartingAtV(Vertex v)
        {
            var origin = v.IncidentHalfEdge;
            var halfEdge = GetHalfEdgeByHandle(origin);
            do
            {
                if (halfEdge.Twin.Id != 0)
                {
                    var twin = GetHalfEdgeByHandle(halfEdge.Twin);
                    halfEdge = GetHalfEdgeByHandle(twin.Next);
                    yield return halfEdge.Handle;
                }
                else
                {
                    yield return halfEdge.Handle;
                    break;
                }
            } while (halfEdge.Handle.Id != origin.Id);
        }

        /// <summary>
        /// Gets a half edge by its handle
        /// </summary>
        /// <param name="halfEdgeHandle">The half edges' reference</param>
        /// <returns></returns>
        internal HalfEdge GetHalfEdgeByHandle(HalfEdgeHandle halfEdgeHandle)
        {
            foreach (var e in _halfEdges)
            {
                if (e.Handle.Id == halfEdgeHandle.Id)
                    return e;
            }
            throw new HandleNotFoundException("HalfEdge with id " + halfEdgeHandle.Id + " not found!");
        }

        /// <summary>
        /// Gets a face by its handle
        /// </summary>
        /// <param name="faceHandle">The faces' reference</param>
        /// <returns></returns>
        internal Face GetFaceByHandle(FaceHandle faceHandle)
        {
            foreach (var e in _faces)
            {
                if (e.Handle.Id == faceHandle.Id)
                    return e;
            }
            throw new HandleNotFoundException("HalfEdge with id " + faceHandle.Id + " not found!");
        }

        #endregion

        #region private Methods

        private void CreateHalfEdgesForGeometry(IEnumerable<Outline> outlines)
        {
            var count = 0;
            foreach (var o in outlines)
            {
                var outlineHalfEdges = CreateHalfEdgesForBoundary(o);

                for (var i = 0; i < outlineHalfEdges.Count; i++)
                {
                    var current = outlineHalfEdges[i];

                    //Assign Twins. There can only be twins if another outline was already processed.
                    if (count == 0)
                    {
                        outlineHalfEdges[i] = current;
                        continue;
                    }

                    //Find Twin by checking for existing half edges with opposit direction of the origin and target vertices.
                    var origin = current.Origin;
                    var target = new VertHandle();
                    foreach (var he in outlineHalfEdges)
                    {
                        if (he.Handle.Id == current.Next.Id)
                            target = he.Origin;
                    }

                    foreach (var halfEdge in _halfEdges)
                    {
                        var compOrigin = halfEdge.Origin;
                        var compTarget = GetHalfEdgeByHandle(halfEdge.Next).Origin;

                        if (origin.Equals(compTarget) && target.Equals(compOrigin))
                        {
                            current.Twin = halfEdge.Handle;
                        }
                    }
                    outlineHalfEdges[i] = current;
                }
                count++;
                _halfEdges.AddRange(outlineHalfEdges);
            }
        }

        private List<HalfEdge> CreateHalfEdgesForBoundary(Outline outline)
        {
            var outlineHalfEdges = new List<HalfEdge>();
            var faceHandle = new FaceHandle();

            for (var i = 0; i < outline.Points.Count; i++)
            {
                var coord = outline.Points[i];

                Vertex vert;
                var vertHandle = CreateAndAssignVertex(coord, out vert);

                var halfEdgeHandle = new HalfEdgeHandle(HalfEdgeHandles.Count + 1);

                if (vert.Handle.Id != 0)
                {
                    vert.IncidentHalfEdge = halfEdgeHandle;
                    _vertices.Add(vert);
                }

                HalfEdgeHandles.Add(halfEdgeHandle);
                var halfEdge = new HalfEdge
                {
                    Origin = vertHandle,
                    Handle = halfEdgeHandle,
                    Twin = new HalfEdgeHandle()
                };

                //Assumption: outlines are processed from outer to inner for every face, therfore faceHandle will never has its default value if else is hit.
                if (outline.IsOuter)
                {
                    if (faceHandle.Id == default(FaceHandle).Id)
                    {
                        Face face;
                        faceHandle = AddFace(halfEdge.Handle, out face);
                        FaceHandles.Add(faceHandle);
                        _faces.Add(face);
                    }
                }
                else
                {
                    if (i == 0)
                        _faces.LastItem().InnerHalfEdges.Add(halfEdge.Handle);
                }
                halfEdge.IncidentFace = faceHandle;

                outlineHalfEdges.Add(halfEdge);
            }

            for (var i = 0; i < outlineHalfEdges.Count; i++)
            {
                var he = outlineHalfEdges[i];

                //Assumption: a boundary is always closed!
                if (i + 1 < outlineHalfEdges.Count)
                    he.Next.Id = outlineHalfEdges[i + 1].Handle.Id;
                else { he.Next.Id = outlineHalfEdges[0].Handle.Id; }

                if (i - 1 < 0)
                    he.Prev.Id = outlineHalfEdges.LastItem().Handle.Id;
                else { he.Prev.Id = outlineHalfEdges[i - 1].Handle.Id; }

                outlineHalfEdges[i] = he;
            }
            return outlineHalfEdges;
        }

        private FaceHandle AddFace(HalfEdgeHandle firstHalfEdge, out Face face)
        {
            var faceHandle = new FaceHandle { Id = FaceHandles.Count + 1 };

            face = new Face
            {
                Handle = faceHandle,
                FirstHalfEdge = firstHalfEdge,
                InnerHalfEdges = new List<HalfEdgeHandle>()
            };
            return faceHandle;
        }

        private VertHandle CreateAndAssignVertex(float3 pointCoord, out Vertex vert)
        {
            var vertHandle = new VertHandle();
            vert = new Vertex();

            //Check if a Vertex already exists and assign it to the HalfEdge instead of createing a new
            if (_vertices.Count != 0)
            {
                foreach (var v in _vertices)
                {
                    if (pointCoord.Equals(v.Coord))
                        vertHandle.Id = v.Handle.Id;
                    else
                    {
                        //Create Vertice and VertHandle
                        vertHandle.Id = VertHandles.Count + 1;
                        VertHandles.Add(vertHandle);
                        vert = new Vertex(pointCoord) { Handle = vertHandle };
                        break;
                    }
                }
            }
            else
            {
                //Create Vertices and VertHandle
                vertHandle.Id = VertHandles.Count + 1;
                VertHandles.Add(vertHandle);
                vert = new Vertex(pointCoord) { Handle = vertHandle };
            }
            return vertHandle;
        }
        #endregion
    }
}



