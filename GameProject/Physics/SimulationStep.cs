﻿using Game.Portals;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public static class SimulationStep
    {
        private class PortalableMovement
        {
            public Transform2 Previous;
            public Line StartEnd;
            public IPortalable Instance;
            public PortalableMovement(IPortalable instance, Line startEnd, Transform2 previous)
            {
                Instance = instance;
                StartEnd = startEnd;
                Previous = previous;
            }
        }

        private class PortalMovement
        {
            public Line Start;
            public Line End;
            public IPortal Portal;
            public PortalMovement(IPortal portal, Line start, Line end)
            {
                Portal = portal;
                Start = start;
                End = end;
            }
        }

        private class PortalableSweep
        {
            public GeometryUtil.Sweep Sweep;
            public PortalableMovement Portalable;
            public PortalMovement Portal;
            public PortalableSweep(GeometryUtil.Sweep sweep, PortalableMovement portalable, PortalMovement portal)
            {
                Sweep = sweep;
                Portalable = portalable;
                Portal = portal;
            }
        }

        public static void Step(IEnumerable<IPortalable> moving, IEnumerable<IPortal> portals, double stepSize, Action<EnterCallbackData> portalEnter)
        {
            Step(moving, portals, stepSize, portalEnter, null);
        }

        private static void Step(IEnumerable<IPortalable> moving, IEnumerable<IPortal> portals, double stepSize, Action<EnterCallbackData> portalEnter, PortalableSweep previous)
        {
            List<PortalableMovement> pointMovement = new List<PortalableMovement>();
            List<PortalMovement> lineMovement = new List<PortalMovement>();

            //Get the start positions and initial end positions for all portals and portalables.
            {
                foreach (IPortal p in portals)
                {
                    if (!Portal.IsValid(p))
                    {
                        continue;
                    }
                    Line lineStart = new Line(Portal.GetWorldVerts(p));
                    lineMovement.Add(new PortalMovement(p, lineStart, new Line()));
                }

                foreach (IPortalable p in moving)
                {
                    Transform2 transform = p.GetTransform();
                    Vector2 pointStart = transform.Position;
                    p.SetTransform(transform.Add(p.GetVelocity().Multiply((float)stepSize)));
                    Vector2 pointEnd = transform.Position;

                    pointMovement.Add(new PortalableMovement(p, new Line(pointStart, pointEnd), transform));
                }

                foreach (PortalMovement line in lineMovement)
                {
                    line.End = new Line(Portal.GetWorldVerts(line.Portal));
                }

                //Move portalable instance back to their start positions.
                foreach (PortalableMovement p in pointMovement)
                {
                    p.Instance.SetTransform(p.Previous);
                }
            }

            PortalableSweep earliest = GetEarliestCollision(pointMovement, lineMovement, previous.Sweep.TimeProportion, previous);

            if (earliest == null)
            {
                return;
            }

            double tDelta = earliest.Sweep.TimeProportion;
            foreach (PortalableMovement move in pointMovement)
            {
                Transform2 velocity = move.Instance.GetVelocity().Multiply((float)(stepSize * tDelta));
                move.Instance.SetTransform(move.Instance.GetTransform().Add(velocity));
            }
            float intersectT = (float)earliest.Sweep.AcrossProportion;
            Portal.Enter(earliest.Portal.Portal, earliest.Portalable.Instance, intersectT);
            Step(moving, portals, stepSize * (1 - tDelta), portalEnter, earliest);
        }

        private static PortalableSweep GetEarliestCollision(List<PortalableMovement> pointMovement, List<PortalMovement> lineMovement, double tCurrent, PortalableSweep previous)
        {
            double tMin = 1;
            PortalableSweep earliest = null;
            foreach (PortalableMovement move in pointMovement)
            {
                if (move.Instance.IsPortalable)
                {
                    Debug.Assert(!(move is IPortal), "Portals cannot do portal teleporation with other portals.");
                    foreach (PortalMovement portal in lineMovement)
                    {
                        //Prevent rounding errors from allowing the same portal/portalable collision to happen twice.
                        if (previous?.Portal == portal.Portal && previous?.Portalable == move.Instance)
                        {
                            continue;
                        }
                        if (move.Instance == portal.Portal)
                        {
                            continue;
                        }
                        var collisionList = MathExt.MovingPointLineIntersect(move.StartEnd, portal.Start, portal.End);
                        if (collisionList.Count > 0 && collisionList[0].TimeProportion >= tCurrent && collisionList[0].TimeProportion < tMin)
                        {
                            earliest = new PortalableSweep(collisionList[0], move, portal);
                            tMin = collisionList[0].TimeProportion;
                        }
                    }
                }
            }
            return earliest;
        }

        public static void Step(IList<ProxyPortal> portals, IList<ProxyPortalable> portalables, int iterations, float stepSize)
        {
            Debug.Assert(iterations > 0);
            Debug.Assert(portals != null);
            Debug.Assert(portalables != null);

            float iterationLength = stepSize / iterations;
            for (int i = 0; i < iterations; i++)
            {
                foreach (ProxyPortalable portalable in portalables)
                {
                    //Note that at very high iterations, portalable instance can fail to enter a portal 
                    //due to their velocity being less than Portal.EnterMinDistance.
                    portalable.Transform.Rotation += portalable.Velocity.Rotation * iterationLength;
                    portalable.Transform.Size += portalable.Velocity.Size * iterationLength;
                    Ray.Settings settings = new Ray.Settings();
                    settings.TimeScale = iterationLength;
                    Ray.RayCast(portalable, portals, settings, (EnterCallbackData data, double movementLeft) =>
                    {
                        portalable.TrueVelocity = Portal.EnterVelocity(data.EntrancePortal, (float)data.PortalT, portalable.TrueVelocity);
                        portalable.Portalable.EnterPortal?.Invoke(data, null, null);
                    });
                }
                foreach (ProxyPortal p in portals)
                {
                    if (!Portal.IsValid(p))
                    {
                        continue;
                    }
                    if (p.WorldVelocity == new Transform2())
                    {
                        continue;
                    }
                    List<Vector2> quad = new List<Vector2>();
                    quad.AddRange(Portal.GetWorldVerts(p));

                    p.WorldTransform = p.WorldTransform.Add(p.WorldVelocity.Multiply(iterationLength));

                    //Make the size of the quad slightly larger than it really is to prevent a situation where the portal 
                    //"pushes" away a portalable instance.
                    //Note that there is still a failure case if the portal is rotating slowly but not translating.
                    Vector2 margin = Vector2.Zero;
                    if (p.WorldVelocity.Position != Vector2.Zero)
                    {
                        margin = p.WorldVelocity.Position.Normalized() * Portal.EnterMinDistance * 2;
                    }
                    p.WorldTransform.Position += margin;
                    //Reverse the order of the next vertice pair so that this forms a quadrilateral.
                    quad.AddRange(Portal.GetWorldVerts(p).Reverse());

                    foreach (ProxyPortalable portalable in portalables)
                    {
                        if (MathExt.PointInPolygon(portalable.Transform.Position, quad))
                        {
                            Portal.Enter(p, portalable, 0.5f);
                            portalable.TrueVelocity = Portal.EnterVelocity(p, 0.5f, portalable.TrueVelocity);
                        }
                    }
                    p.WorldTransform.Position -= margin;
                }
            }

            foreach (ProxyPortalable portalable in portalables)
            {
                portalable.Portalable.SetTransform(portalable.Transform);
                if (portalable.Portalable is Actor)
                {
                    portalable.Portalable.SetVelocity(portalable.TrueVelocity);
                }
                else
                {
                    portalable.Portalable.SetVelocity(portalable.Velocity);
                }
            }
        }
    }
}
