﻿using System;
using System.Collections.Generic;
using System.Linq;
using Fusee.Base.Core;
using Fusee.Jometri.DCEL;
using Fusee.Math.Core;

namespace Fusee.Jometri.Triangulation
{
    /// <summary>
    /// Contains the triangulation of some geometry, stored in a doubly connected halfe edge list.
    /// </summary>
    internal static class Triangulation
    {
        private static Geometry2D _geometry;
        private static VertexType _vertType;
        private static SweepLineStatus _sweepLineStatus;

        internal static void Triangulate(this Geometry2D geometry)
        {
            //TODO: Both, MakeMonotone and TriangulateMonotone need 2D coordinates instead of 3D. It is possibly more effective to call Reduce2D for the vertices of the whole face in those methods than in the sub methods

            _geometry = geometry;

            var originalFaces = new List<FaceHandle>();
            originalFaces.AddRange(_geometry.FaceHandles);

            var firstFace = _geometry.GetFaceByHandle(_geometry.FaceHandles[0]);
            var isFirstFaceUnbounded = firstFace.OuterHalfEdge == default(HalfEdgeHandle);

            for (var i = 0; i < originalFaces.Count; i++)
            {
                var fHandle = originalFaces[i];

                //If face has no OuterHalfEdge it's unbounded and can be ignored - if there is a unbounded face its always the first in _faces
                if (i == 0 && isFirstFaceUnbounded) { continue; }

                if (!IsMonotone(fHandle))
                    MakeMonotone(fHandle);
            }

            var monotoneFaces = new List<FaceHandle>();
            monotoneFaces.AddRange(_geometry.FaceHandles);

            for (var i = 0; i < monotoneFaces.Count; i++)
            {
                var fHandle = monotoneFaces[i];

                if (i == 0 && isFirstFaceUnbounded) { continue; }

                TriangulateMonotone(fHandle);
            }
        }

        #region Triangulate monotone polygone

        private static void TriangulateMonotone(FaceHandle fHandle)
        {
            var faceVertices = new List<Vertex>();
            foreach (var vHandle in _geometry.GetFaceVertices(fHandle))
            {
                faceVertices.Add(_geometry.GetVertexByHandle(vHandle));
            }

            if (faceVertices.Count.Equals(3)) return;

            var sortedVerts = GetSortedVertices(faceVertices);
            var vertStack = new Stack<Vertex>();
            var leftChain = GetLeftChain(sortedVerts, fHandle).ToList();

            vertStack.Push(sortedVerts[0]);
            vertStack.Push(sortedVerts[1]);

            for (var i = 2; i < sortedVerts.Count - 1; i++)
            {
                var current = sortedVerts[i];

                if (!IsLeftChain(leftChain, current) && IsLeftChain(leftChain, vertStack.Peek()) ||
                    IsLeftChain(leftChain, current) && !IsLeftChain(leftChain, vertStack.Peek()))
                {
                    while (vertStack.Count > 0)
                    {
                        var popped = vertStack.Pop();

                        if (vertStack.Count > 0)
                            _geometry.InsertDiagonal(current.Handle, popped.Handle);
                    }
                    vertStack.Push(sortedVerts[i - 1]);
                    vertStack.Push(current);
                }
                else
                {
                    var popped = vertStack.Pop();

                    float3 v1;
                    float3 v2;

                    Vertex next;
                    Vertex prev;

                    if (IsLeftChain(leftChain, popped))
                    {
                        next = sortedVerts[i];
                        prev = vertStack.Peek();
                    }
                    else
                    {
                        next = vertStack.Peek();
                        prev = sortedVerts[i];
                    }

                    v1 = next.Coord - popped.Coord;
                    v2 = prev.Coord - popped.Coord;

                    while (vertStack.Count > 0 && !IsAngleGreaterOrEqualPi(v1, v2))
                    {
                        popped = vertStack.Pop();

                        if (vertStack.Count > 0)
                        {
                            if (IsLeftChain(leftChain, popped))
                            {
                                next = sortedVerts[i];
                                prev = vertStack.Peek();
                            }
                            else
                            {
                                next = vertStack.Peek();
                                prev = sortedVerts[i];
                            }

                            v1 = next.Coord - popped.Coord;
                            v2 = prev.Coord - popped.Coord;
                        }

                        _geometry.InsertDiagonal(current.Handle, popped.Handle);
                    }
                    vertStack.Push(popped);
                    vertStack.Push(current);
                }
            }

            var count = vertStack.Count;

            for (var j = 0; j < count; j++)
            {
                var popped = vertStack.Pop();

                if (j == 0) continue;
                if (j != count - 1)
                    _geometry.InsertDiagonal(sortedVerts.LastItem().Handle, popped.Handle);
            }
        }

        private static IEnumerable<Vertex> GetLeftChain(IList<Vertex> sortedVerts, FaceHandle fHandle)
        {
            var heHandle = new HalfEdgeHandle();
            var endOfChain = sortedVerts.LastItem();

            var startingAtFirstV = _geometry.GetVertexStartingHalfEdges(sortedVerts[0].Handle).ToList();
            if (startingAtFirstV.Count > 1)
            {
                foreach (var heh in startingAtFirstV)
                {
                    var he = _geometry.GetHalfEdgeByHandle(heh);
                    if (he.IncidentFace == fHandle)
                        heHandle = heh;
                }
            }
            else
            { heHandle = sortedVerts[0].IncidentHalfEdge; }

            do
            {
                var halfEdge = _geometry.GetHalfEdgeByHandle(heHandle);
                yield return _geometry.GetVertexByHandle(halfEdge.Origin);
                heHandle = halfEdge.Next;

            } while (_geometry.GetHalfEdgeByHandle(heHandle).Origin != endOfChain.Handle);
        }

        private static bool IsLeftChain(IEnumerable<Vertex> leftChain, Vertex vert)
        {
            foreach (var v in leftChain)
            {
                if (v.Handle == vert.Handle)
                    return true;
            }
            return false;
        }

        //Vertices need to be reduced to 2D
        private static bool IsAngleGreaterOrEqualPi(float3 first, float3 second)
        {
            var redFirst = first.Reduce2D();
            var redSecond = second.Reduce2D();

            var cross = redFirst.x * redSecond.y - redFirst.y * redSecond.x; //z component of the cross product
            var dot = float3.Dot(first, second);

            var angle = (float)System.Math.Atan2(cross, dot);
            var deg = M.RadiansToDegrees(angle);
            return angle <= 0;
        }

        #endregion

        #region Test face for y monotony

        private static bool IsMonotone(FaceHandle fHandle)
        {
            var face = (Face2D)_geometry.GetFaceByHandle(fHandle);
            var noSplitOrMerge = HasNoSplitOrMerge(fHandle);

            return noSplitOrMerge && face.InnerHalfEdges.Count == 0;
        }

        private static bool HasNoSplitOrMerge(FaceHandle fHandle)
        {
            var verts = _geometry.GetFaceVertices(fHandle).ToList();

            foreach (var vHandle in verts)
            {
                TestVertexType(vHandle, fHandle);
                if (_vertType.Equals(VertexType.SPLIT_VERTEX) || _vertType.Equals(VertexType.MERGE_VERTEX))
                    return false;
            }
            return true;
        }

        private static void TestVertexType(VertHandle vHandle, FaceHandle fHandle)
        {
            var vert = _geometry.GetVertexByHandle(vHandle);
            var heStartingAtFace = _geometry.GetVertexStartingHalfEdges(vHandle).ToList();

            var incidentHalfEdge = new HalfEdge();
            foreach (var he in heStartingAtFace)
            {
                var incidentFace = _geometry.GetHalfEdgeByHandle(he).IncidentFace;
                if (!incidentFace.Equals(fHandle)) continue;
                incidentHalfEdge = _geometry.GetHalfEdgeByHandle(he);
                break;
            }

            var nextHalfEdge = _geometry.GetHalfEdgeByHandle(incidentHalfEdge.Next);
            var nextVert = _geometry.GetVertexByHandle(nextHalfEdge.Origin);

            var prevHalfEdge = _geometry.GetHalfEdgeByHandle(incidentHalfEdge.Prev);
            var prevVert = _geometry.GetVertexByHandle(prevHalfEdge.Origin);

            var v2 = new float3(prevVert.Coord - vert.Coord);
            var v1 = new float3(nextVert.Coord - vert.Coord);

            if (IsUnderVert(vert, nextVert) && IsUnderVert(vert, prevVert))
            {
                if (IsAngleGreaterPi(v1, v2))
                    _vertType = VertexType.SPLIT_VERTEX;
                else
                {
                    _vertType = VertexType.START_VERTEX;
                }
            }
            else if (IsOverVert(vert, nextVert) && IsOverVert(vert, prevVert))
            {
                if (IsAngleGreaterPi(v1, v2))
                    _vertType = VertexType.MERGE_VERTEX;
                else
                {
                    _vertType = VertexType.END_VERTEX;
                }
            }
            else
            {
                _vertType = VertexType.REGULAR_VERTEX;
            }
        }

        //Vertices need to be reduced to 2D
        private static bool IsUnderVert(Vertex middle, Vertex neighbour)
        {
            var redMiddle = middle.Coord.Reduce2D();
            var redNeighbour = neighbour.Coord.Reduce2D();

            if (redMiddle.y > redNeighbour.y)
                return true;
            if (redMiddle.y.Equals(redNeighbour.y) && redMiddle.x < redNeighbour.x)
            {
                return true;
            }
            return false;
        }

        //Vertices need to be reduced to 2D
        private static bool IsOverVert(Vertex middle, Vertex neighbour)
        {
            var redMiddle = middle.Coord.Reduce2D();
            var redNeighbour = neighbour.Coord.Reduce2D();

            if (redMiddle.y < redNeighbour.y)
                return true;
            if (redMiddle.y.Equals(redNeighbour.y) && redMiddle.x > redNeighbour.x)
            {
                return true;
            }
            return false;
        }

        //Vertices need to be reduced to 2D
        private static bool IsAngleGreaterPi(float3 first, float3 second)
        {
            var redFirst = first.Reduce2D();
            var redSecond = second.Reduce2D();

            var cross = redFirst.x * redSecond.y - redFirst.y * redSecond.x; //z component of the cross product
            var dot = float3.Dot(first, second);

            var angle = (float)System.Math.Atan2(cross, dot);
            var deg = M.RadiansToDegrees(angle);
            if ((angle * -1).Equals(M.Pi))
                return false;
            return angle < 0;
        }
        #endregion

        #region MakeMonotone
        private static void MakeMonotone(FaceHandle fHandle)
        {
            var vertices = new List<Vertex>();

            foreach (var vHandle in _geometry.GetFaceVertices(fHandle))
            {
                vertices.Add(_geometry.GetVertexByHandle(vHandle));
            }

            var sortedVertices = GetSortedVertices(vertices.ToList());
            var faceHalfEdges = _geometry.GetFaceHalfEdges(fHandle).ToList();

            var newFaces = new List<FaceHandle>();

            _sweepLineStatus = new SweepLineStatus();

            while (sortedVertices.Count != 0)
            {
                var current = sortedVertices[0];

                TestVertexType(current, fHandle, newFaces);

                switch (_vertType)
                {
                    case VertexType.START_VERTEX:
                        HandleStartVertex(current, faceHalfEdges);
                        break;
                    case VertexType.END_VERTEX:
                        HandleEndVertex(current, faceHalfEdges, newFaces);
                        break;
                    case VertexType.SPLIT_VERTEX:
                        HandleSplitVertex(current, newFaces);
                        break;
                    case VertexType.MERGE_VERTEX:
                        HandleMergeVertex(current, faceHalfEdges, newFaces);
                        break;
                    case VertexType.REGULAR_VERTEX:
                        HandleRegularVertex(current, faceHalfEdges, newFaces);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                sortedVertices.RemoveAt(0);
            }
        }

        private static void TestVertexType(Vertex vert, FaceHandle fHandle, ICollection<FaceHandle> newFaces)
        {
            var heStartingAtVert = _geometry.GetVertexStartingHalfEdges(vert.Handle).ToList();
            var incidentHalfEdge = new HalfEdge();

            foreach (var he in heStartingAtVert)
            {
                var incidentFace = _geometry.GetHalfEdgeByHandle(he).IncidentFace;
                if (incidentFace.Equals(fHandle))
                {
                    incidentHalfEdge = _geometry.GetHalfEdgeByHandle(he);
                    break;
                }
                foreach (var fh in newFaces)
                {
                    if (incidentFace.Equals(fh))
                        incidentHalfEdge = _geometry.GetHalfEdgeByHandle(he);
                }
            }

            var nextHalfEdge = _geometry.GetHalfEdgeByHandle(incidentHalfEdge.Next);
            var nextVert = _geometry.GetVertexByHandle(nextHalfEdge.Origin);

            var prevHalfEdge = _geometry.GetHalfEdgeByHandle(incidentHalfEdge.Prev);
            var prevVert = _geometry.GetVertexByHandle(prevHalfEdge.Origin);

            var v2 = new float3(prevVert.Coord - vert.Coord);
            var v1 = new float3(nextVert.Coord - vert.Coord);

            if (IsUnderVert(vert, nextVert) && IsUnderVert(vert, prevVert))
            {
                if (IsAngleGreaterPi(v1, v2))
                    _vertType = VertexType.SPLIT_VERTEX;
                else
                {
                    _vertType = VertexType.START_VERTEX;
                }
            }
            else if (IsOverVert(vert, nextVert) && IsOverVert(vert, prevVert))
            {
                if (IsAngleGreaterPi(v1, v2))
                    _vertType = VertexType.MERGE_VERTEX;
                else
                {
                    _vertType = VertexType.END_VERTEX;
                }
            }
            else
            {
                _vertType = VertexType.REGULAR_VERTEX;
            }
        }

        private static void HandleStartVertex(Vertex vert, IEnumerable<HalfEdgeHandle> faceHalfEdges)
        {
            foreach (var halfEdge in faceHalfEdges)
            {
                var he = _geometry.GetHalfEdgeByHandle(halfEdge);

                if (he.Origin != vert.Handle) continue;

                _sweepLineStatus.UpdateNodes(vert);
                var origin = _geometry.GetVertexByHandle(he.Origin);
                var targetH = _geometry.GetHalfEdgeByHandle(he.Next).Origin;
                var target = _geometry.GetVertexByHandle(targetH);

                var ei = new StatusEdge(origin, target, vert);
                ei.HalfEdge = he.Handle;
                ei.Helper = vert.Handle;
                ei.IsMergeVertex = false;

                _sweepLineStatus.InsertNode(ei.IntersectionPointX, ei);
                break;
            }
        }

        private static void HandleEndVertex(Vertex vert, IEnumerable<HalfEdgeHandle> faceHalfEdges, ICollection<FaceHandle> newFaces)
        {
            foreach (var heh in faceHalfEdges)
            {
                var he = _geometry.GetHalfEdgeByHandle(heh);

                if (he.Origin != vert.Handle) continue;

                _sweepLineStatus.UpdateNodes(vert);

                var eMinOne = _sweepLineStatus.FindStatusEdgeWithHandle(he.Prev);

                if (eMinOne.IsMergeVertex)
                {
                    _geometry.InsertDiagonal(vert.Handle, eMinOne.Helper);
                    newFaces.Add(_geometry.FaceHandles.LastItem());
                }

                _sweepLineStatus.DeleteNode(eMinOne.IntersectionPointX);
                break;
            }
        }

        private static void HandleSplitVertex(Vertex vert, ICollection<FaceHandle> newFaces)
        {
            _sweepLineStatus.UpdateNodes(vert);
            _sweepLineStatus.BalanceTree();

            var ej = _sweepLineStatus.FindLargestSmallerThanInBalanced(vert.Coord.x);

            _geometry.InsertDiagonal(vert.Handle, ej.Helper);
            newFaces.Add(_geometry.FaceHandles.LastItem());

            _sweepLineStatus.FindNode(ej.IntersectionPointX).Helper = vert.Handle;
            _sweepLineStatus.FindNode(ej.IntersectionPointX).IsMergeVertex = false;

            var he = _geometry.GetHalfEdgeByHandle(vert.IncidentHalfEdge);
            var origin = _geometry.GetVertexByHandle(he.Origin);
            var targetH = _geometry.GetHalfEdgeByHandle(he.Next).Origin;
            var target = _geometry.GetVertexByHandle(targetH);

            var ei = new StatusEdge(origin, target, vert);
            ei.HalfEdge = vert.IncidentHalfEdge;
            ei.Helper = vert.Handle;
            ei.IsMergeVertex = false;

            _sweepLineStatus.InsertNode(ei.IntersectionPointX, ei);
        }

        private static void HandleMergeVertex(Vertex vert, IEnumerable<HalfEdgeHandle> faceHalfEdges, ICollection<FaceHandle> newFaces)
        {
            var he = new HalfEdge();
            foreach (var heh in faceHalfEdges)
            {
                he = _geometry.GetHalfEdgeByHandle(heh);

                if (he.Origin == vert.Handle) break;
            }

            _sweepLineStatus.UpdateNodes(vert);

            var eMinOne = _sweepLineStatus.FindStatusEdgeWithHandle(he.Prev);

            if (eMinOne.IsMergeVertex)
            {
                _geometry.InsertDiagonal(vert.Handle, eMinOne.Helper);
                newFaces.Add(_geometry.FaceHandles.LastItem());
            }

            _sweepLineStatus.DeleteNode(eMinOne.IntersectionPointX);
            _sweepLineStatus.BalanceTree();

            var ej = _sweepLineStatus.FindLargestSmallerThanInBalanced(vert.Coord.x);

            if (ej.IsMergeVertex)
            {
                _geometry.InsertDiagonal(vert.Handle, ej.Helper);
                newFaces.Add(_geometry.FaceHandles.LastItem());
            }

            _sweepLineStatus.FindNode(ej.IntersectionPointX).Helper = vert.Handle;
            _sweepLineStatus.FindNode(ej.IntersectionPointX).IsMergeVertex = true;
        }

        private static void HandleRegularVertex(Vertex vert, IEnumerable<HalfEdgeHandle> faceHalfEdges, ICollection<FaceHandle> newFaces)
        {
            if (IsPolygonRightOfVert(vert))
            {
                foreach (var heh in faceHalfEdges)
                {
                    var he = _geometry.GetHalfEdgeByHandle(heh);

                    if (he.Origin != vert.Handle) continue;

                    _sweepLineStatus.UpdateNodes(vert);

                    var eMinOne = _sweepLineStatus.FindStatusEdgeWithHandle(he.Prev);

                    if (eMinOne.IsMergeVertex)
                    {
                        _geometry.InsertDiagonal(vert.Handle, eMinOne.Helper);
                        newFaces.Add(_geometry.FaceHandles.LastItem());
                    }

                    _sweepLineStatus.DeleteNode(eMinOne.IntersectionPointX);

                    var halfEdge = _geometry.GetHalfEdgeByHandle(vert.IncidentHalfEdge);
                    var origin = _geometry.GetVertexByHandle(halfEdge.Origin);
                    var targetH = _geometry.GetHalfEdgeByHandle(halfEdge.Next).Origin;
                    var target = _geometry.GetVertexByHandle(targetH);

                    var ei = new StatusEdge(origin, target, vert);
                    ei.HalfEdge = vert.IncidentHalfEdge;
                    ei.Helper = vert.Handle;
                    ei.IsMergeVertex = false;

                    _sweepLineStatus.InsertNode(ei.IntersectionPointX, ei);

                    break;
                }
            }
            else
            {
                _sweepLineStatus.UpdateNodes(vert);
                _sweepLineStatus.BalanceTree();

                var ej = _sweepLineStatus.FindLargestSmallerThanInBalanced(vert.Coord.x);

                if (ej.IsMergeVertex)
                {
                    _geometry.InsertDiagonal(vert.Handle, ej.Helper);
                    newFaces.Add(_geometry.FaceHandles.LastItem());
                }

                _sweepLineStatus.FindNode(ej.IntersectionPointX).Helper = vert.Handle;
                _sweepLineStatus.FindNode(ej.IntersectionPointX).IsMergeVertex = false;
            }
        }

        private static bool IsPolygonRightOfVert(Vertex vert)
        {
            var prevV = GetPrevVertex(vert);
            var nextV = GetNextVertex(vert);

            return prevV.Coord.y > nextV.Coord.y;
        }

        private static Vertex GetNextVertex(Vertex currentVert)
        {
            var currentHe = _geometry.GetHalfEdgeByHandle(currentVert.IncidentHalfEdge);
            var nextHe = _geometry.GetHalfEdgeByHandle(currentHe.Next);

            return _geometry.GetVertexByHandle(nextHe.Origin);
        }

        private static Vertex GetPrevVertex(Vertex currentVert)
        {
            var currentHe = _geometry.GetHalfEdgeByHandle(currentVert.IncidentHalfEdge);
            var prevHe = _geometry.GetHalfEdgeByHandle(currentHe.Prev);

            return _geometry.GetVertexByHandle(prevHe.Origin);
        }

        #endregion

        //Vertices need to be reduced to 2D.
        //Can be optimized by implementing a priority queue data structure and use it insted of sorting a list
        private static IList<Vertex> GetSortedVertices(IEnumerable<Vertex> unsortedVerts)
        {
            var sorted = new List<Vertex>();
            sorted.AddRange(unsortedVerts);
            sorted.Sort(delegate (Vertex a, Vertex b)
            {
                var redA = a.Coord.Reduce2D();
                var redB = b.Coord.Reduce2D();

                var ydiff = -1 * redA.y.CompareTo(redB.y);
                if (ydiff != 0) return ydiff;
                return redA.x.CompareTo(redB.x);
            });

            return sorted;
        }
    }
}
