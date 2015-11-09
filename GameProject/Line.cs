﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using System.Diagnostics;
using Xna = Microsoft.Xna.Framework;

namespace Game
{
    public class Line
    {
        public enum Side
        {
            IsLeftOf,
            IsRightOf,
            IsNeither
        }

        public float Length
        {
            get
            {
                return (Vertices[1] - Vertices[0]).Length;
            }
        }
        public Vector2[] Vertices = new Vector2[2];
        public Vector2 Center { get { return (Vertices[0] + Vertices[1]) / 2; } }

        #region constructors
        public Line()
        {
        }

        public Line(Vector2 lineStart, Vector2 lineEnd)
        {
            Vertices[0] = lineStart;
            Vertices[1] = lineEnd;
        }

        public Line(Xna.Vector2 lineStart, Xna.Vector2 lineEnd)
        {
            Vertices[0] = Vector2Ext.ConvertTo(lineStart);
            Vertices[1] = Vector2Ext.ConvertTo(lineEnd);
        }

        public Line(Vector2[] line)
        {
            Vertices = line;
        }

        public Line(Vector2 center, float rotation, float length)
        {
            Vector2 offset = new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation)) * length;
            Vertices[0] = center + offset;
            Vertices[1] = center - offset;
        }
        #endregion

        public Vector2 this[int index]
        {
            get
            {
                return Vertices[index];
            }
            set
            {
                Vertices[index] = value;
            }
        }

        /// <summary>
        /// Returns whether a point is left or right of the line
        /// </summary>
        public Side GetSideOf(Vector2 point)
        {
            if ((Vertices[1].X - Vertices[0].X) * (point.Y - Vertices[0].Y) - (Vertices[1].Y - Vertices[0].Y) * (point.X - Vertices[0].X) > 0)
            {
                return Side.IsLeftOf;
            }
            return Side.IsRightOf;
        }

        /// <summary>
        /// Returns whether a point is left or right of the line
        /// </summary>
        public Side GetSideOf(Xna.Vector2 point)
        {
            return GetSideOf(Vector2Ext.ConvertTo(point));
        }

        /// <summary>
        /// returns whether a line is left, right, or inbetween the line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public Side GetSideOf(Line line)
        {
            Side side0 = GetSideOf(line[0]);
            Side side1 = GetSideOf(line[1]);
            if (side0 == side1)
            {
                return side0;
            }
            return Side.IsNeither;
        }

        public Vector2 GetNormal()
        {
            return (Vertices[1] - Vertices[0]).PerpendicularRight.Normalized();
        }

        public bool IsInsideFOV(Vector2 viewPoint, Vector2 lookPoint)
        {
            //check if the lookPoint is on the opposite side of the line from the viewPoint
            if (GetSideOf(viewPoint) == GetSideOf(lookPoint))
            {
                return false;
            }
            //check if the lookPoint is within the FOV angles
            double Angle0 = MathExt.AngleVector(Vertices[0] - viewPoint);
            double Angle1 = MathExt.AngleVector(Vertices[1] - viewPoint);
            double AngleDiff = MathExt.AngleDiff(Angle0, Angle1);
            double AngleLook = MathExt.AngleVector(lookPoint - viewPoint);
            double AngleLookDiff = MathExt.AngleDiff(Angle0, AngleLook);
            if (Math.Abs(AngleDiff) >= Math.Abs(AngleLookDiff) && Math.Sign(AngleDiff) == Math.Sign(AngleLookDiff))
            {
                return true;
            }
            return false;
        }

        public bool IsInsideFOV(Vector2 viewPoint, Line lookLine)
        {
            //check if the lookPoint is on the opposite side of the line from the viewPoint
            if (GetSideOf(viewPoint) == GetSideOf(lookLine[0]) && GetSideOf(viewPoint) == GetSideOf(lookLine[1]))
            {
                return false;
            }
            //check if the lookPoint is within the FOV angles
            double Angle0 = MathExt.AngleVector(Vertices[0] - viewPoint);
            double Angle1 = MathExt.AngleVector(Vertices[1] - viewPoint);
            double AngleDiff = MathExt.AngleDiff(Angle0, Angle1);
            double AngleLook = MathExt.AngleVector(lookLine[0] - viewPoint);
            double AngleLookDiff = MathExt.AngleDiff(Angle0, AngleLook);
            double AngleLook2 = MathExt.AngleVector(lookLine[1] - viewPoint);
            double AngleLookDiff2 = MathExt.AngleDiff(Angle0, AngleLook2);
            //check if the first point is in the FOV
            if (Math.Abs(AngleDiff) >= Math.Abs(AngleLookDiff) && Math.Sign(AngleDiff) == Math.Sign(AngleLookDiff))
            {
                return true;
            }
            //check if the second point is in the FOV
            if (Math.Abs(AngleDiff) >= Math.Abs(AngleLookDiff2) && Math.Sign(AngleDiff) == Math.Sign(AngleLookDiff2))
            {
                return true;
            }
            //check if the two points are on opposite sides of the FOV
            if (Math.Sign(AngleLookDiff) != Math.Sign(AngleLookDiff2))
            {
                return true;
            }
            return false;
        }

        public IntersectPoint Intersects(Line line, bool segmentOnly)
        {
            return MathExt.LineIntersection(Vertices[0], Vertices[1], line[0], line[1], segmentOnly);
        }

        /// <summary>
        /// Checks if the line intersects the edges of a polygon.  This does not test if the line is contained in the polygon
        /// </summary>
        /// <param name="polygon">A closed polygon</param>
        /// <returns>An intersection point</returns>
        public IntersectPoint Intersects(Vector2[] polygon)
        {
            IntersectPoint point;
            for (int i0 = 0; i0 < polygon.Length; i0++)
            {
                int i1 = (i0 + 1) % polygon.Length;
                point = MathExt.LineIntersection(Vertices[0], Vertices[1], polygon[i0], polygon[i1], true);
                if (point.Exists)
                {
                    return point;
                }
            }
            point = new IntersectPoint();
            point.Exists = false;
            return point;
        }

        /// <summary>
        /// Checks if the line is at least partially contained in a polygon
        /// </summary>
        /// <param name="polygon">A closed polygon</param>
        /// <returns></returns>
        public bool IsInsideOfPolygon(Vector2[] polygon)
        {
            if (MathExt.IsPointInPolygon(polygon, Vertices[0]))
            {
                return true;
            }
            if (MathExt.IsPointInPolygon(polygon, Vertices[1]))
            {
                return true;
            }
            return Intersects(polygon).Exists;
        }

        public float PointDistance(Vector2 point, bool isSegment)
        {
            Vector2 V;
            Vector2 VDelta = Vertices[1] - Vertices[0];
            if ((VDelta.X == 0) && (VDelta.Y == 0))
            {
                V = Vertices[0];
            }
            else
            {
                float t = ((point.X - Vertices[0].X) * VDelta.X + (point.Y - Vertices[0].Y) * VDelta.Y) / (float)(Math.Pow(VDelta.X, 2) + Math.Pow(VDelta.Y, 2));
                Debug.Assert(float.IsNaN(t) == false);
                if (isSegment) { t = (float)Math.Min(Math.Max(0, t), 1); }
                V = Vertices[0] + Vector2.Multiply(VDelta, t);
            }
            float distance = (point - V).Length;
            Debug.Assert(distance >= 0);
            return distance;
        }

        public float PointDistance(Xna.Vector2 point, bool isSegment)
        {
            return PointDistance(Vector2Ext.ConvertTo(point), isSegment);
        }

        public float GetOffset()
        {
            return PointDistance(new Vector2(), false);
        }

        /// <summary>
        /// Swaps the start and end vertice for the line.
        /// </summary>
        public void Reverse()
        {
            Vector2 temp = Vertices[0];
            Vertices[0] = Vertices[1];
            Vertices[1] = temp;
        }

        public void Transform(Matrix4 transformMatrix)
        {
            Vertices = Vector2Ext.Transform(Vertices, transformMatrix);
        }

        public Vector2 Lerp(float t)
        {
            return MathExt.Lerp(Vertices[0], Vertices[1], t);
        }

        /// <summary>
        /// Returns the T value of the nearest point on this line to a vector.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public float NearestT(Vector2 v)
        {
            return Vector2.Dot(v - Vertices[0], Vertices[1] - Vertices[0]);
        }

        public float NearestT(Xna.Vector2 v)
        {
            return NearestT(Vector2Ext.ConvertTo(v));
        }

        public float Angle()
        {
            return (float)MathExt.AngleLine(Vertices[0], Vertices[1]);
        }

        public Line Copy()
        {
            return new Line(Vertices);
        }

        /// <summary>
        /// Approximates the time at which a moving line and a moving point intersect or if they don't intersect at all.
        /// </summary>
        /// <param name="velocity"></param>
        /// <param name="rotVelocity"></param>
        /// <param name="pointMotion"></param>
        /// <returns></returns>
        public IntersectPoint IntersectsParametric(Vector2 velocity, float rotVelocity, Line pointMotion, int detail)
        {
            Matrix4 transform = Matrix4.CreateTranslation(new Vector3(velocity.X, velocity.Y, 0) / detail);
            transform = Matrix4.CreateRotationZ(rotVelocity / detail) * transform;
            Line line = Copy();
            IntersectPoint intersect = new IntersectPoint();
            for (int i = 0; i < detail; i++ )
            {
                Line lineNext = line.Copy();
                lineNext.Transform(transform);

                Vector2[] verts = new Vector2[] {
                    line[0],
                    line[1],
                    lineNext[1],
                    lineNext[0]
                };

                Line pointLine = new Line(pointMotion.Lerp(i/detail), pointMotion.Lerp((i+1)/detail));
                if (pointLine.IsInsideOfPolygon(verts))
                {
                    
                    intersect.T = (i + 0.5f) / detail;
                    intersect.Exists = true;
                    Vector2 pos = pointMotion.Lerp((float)intersect.T);
                    intersect.Position = new Vector2d(pos.X, pos.Y);
                    return intersect;
                }
                line = lineNext;
            }
            intersect.Exists = false;
            return intersect;
        }
    }
}
