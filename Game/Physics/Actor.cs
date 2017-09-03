﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using FarseerPhysics.Dynamics;
using Game.Common;
using Game.Portals;
using Game.Serialization;
using OpenTK;
using Vector2 = OpenTK.Vector2;
using Vector3 = OpenTK.Vector3;
using Xna = Microsoft.Xna.Framework;

namespace Game.Physics
{
    /// <summary>A SceneNode with rigid body physics.</summary>
    [DataContract, DebuggerDisplay(nameof(Actor) + " {" + nameof(Name) + "}")]
    public class Actor : SceneNode, IWall, IPortalable
    {
        public delegate void OnCollisionHandler(Actor collidingWith, bool firstEvent);
        public event OnCollisionHandler OnCollision;

        public Transform2 Transform
        {
            get { return GetTransform(); }
            set { SetTransform(value); }
        }
        public Transform2 Velocity
        {
            get { return GetVelocity(); }
            set { BodyEx.SetVelocity(Body, value); }
        }
        /// <summary>
        /// Physics rigid body associated with this Actor.
        /// </summary>
        public Body Body { get; private set; }
        [DataMember]
        public bool IsSensor { get; private set; }
        [DataMember]
        public BodyType BodyType { get; private set; }
        [DataMember]
        Vector2 _scale = new Vector2(1, 1);
        /// <summary>
        /// Used for storing body data when serialized.
        /// </summary>
        [DataMember]
        BodyMemento _body;
        [DataMember]
        Vector2[] _vertices;
        /// <summary>Copy of local coordinates for collision mask.</summary>
        public IList<Vector2> Vertices => _vertices.ToList();

        [DataMember]
        public Action<EnterCallbackData, Transform2, Transform2> EnterPortal { get; set; }

        public Actor(Scene scene, IList<Vector2> vertices)
            : this(scene, vertices, new Transform2())
        {
        }

        public Actor(Scene scene, IList<Vector2> vertices, Transform2 transform)
            : base(scene)
        {
            _vertices = vertices.ToArray();
            _scale = transform.Scale;
            Body = Factory.CreatePolygon(Scene.World, transform, Vertices);
            BodyEx.SetData(Body, this);
            SetBodyType(BodyType.Dynamic);
        }

        //TODO: Fix serialization for scenes.
        //[OnDeserialized]
        //public void Deserialize(StreamingContext context)
        //{
        //    Body = Factory.CreatePolygon(Scene.World, _body.Transform, Vertices);
        //    BodyExt.SetData(Body, this);
        //    BodyExt.SetVelocity(Body, _body.Velocity);
        //}

        //[OnSerializing]
        //public void Serialize(StreamingContext context)
        //{
        //    _body = new BodyMemento(Body);
        //}

        public override IDeepClone ShallowClone()
        {
            var clone = new Actor(Scene, Vertices, GetTransform());
            ShallowClone(clone);
            return clone;
        }

        protected void ShallowClone(Actor destination)
        {
            base.ShallowClone(destination);
            BodyEx.SetData(destination.Body, destination);
            foreach (Fixture f in destination.Body.FixtureList)
            {
                FixtureEx.SetData(f);
            }
        }

        public override void SetParent(SceneNode parent)
        {
            DebugEx.Assert(parent == null, "Actor must be the root SceneNode.");
            base.SetParent(parent);
        }

        public void SetCollisionCategory(Category category)
        {
            foreach (BodyData data in Tree<BodyData>.GetAll(BodyEx.GetData(Body)))
            {
                data.Body.CollisionCategories = category;
            }
        }

        public void SetCollidesWith(Category category)
        {
            foreach (BodyData data in Tree<BodyData>.GetAll(BodyEx.GetData(Body)))
            {
                data.Body.CollidesWith = category;
            }
        }

        public void CallOnCollision(Actor collidingWith, bool firstEvent)
        {
            OnCollision?.Invoke(collidingWith, firstEvent);
        }

        public float GetMass()
        {
            float mass = 0;
            foreach (BodyData data in Tree<BodyData>.GetAll(BodyEx.GetData(Body)))
            {
                mass += BodyEx.GetLocalMassData(data.Body).Mass;
            }
            return mass;
        }

        /// <summary>
        /// Returns the center of mass in world coordinates.
        /// </summary>
        /// <returns></returns>
        public Vector2 GetCentroid()
        {
            var centroid = new Vector2();
            float massTotal = 0;
            foreach (BodyData data in Tree<BodyData>.GetAll(BodyEx.GetData(Body)))
            {
                var massData = BodyEx.GetLocalMassData(data.Body);
                centroid += UndoPortalTransform(data, new Transform2(massData.Centroid)).Position * massData.Mass;
                massTotal += massData.Mass;
            }
            DebugEx.Assert(massTotal == GetMass());
            centroid /= massTotal;
            return centroid;
        }

        public void SetBodyType(BodyType type)
        {
            BodyType = type;
            _setBodyType(Body, type);
        }

        void _setBodyType(Body body, BodyType type)
        {
            DebugEx.Assert(!Scene.InWorldStep);
            body.BodyType = type;
            foreach (var b in BodyEx.GetData(body).BodyChildren)
            {
                _setBodyType(b.Body, BodyType == BodyType.Dynamic ? BodyType.Dynamic : BodyType.Kinematic);
            }
        }

        public void Update()
        {
            BodyEx.GetData(Body).Update();
        }

        public override void Remove()
        {
            Scene sceneTemp = Scene;
            base.Remove();
            if (Body != null)
            {
                sceneTemp.World.RemoveBody(Body);
                Body = null;
            }
        }

        /// <summary>
        /// Applies a force at the center of mass.
        /// </summary>
        /// <param name="force">The force.</param>
        public void ApplyForce(Vector2 force)
        {
            _applyForce(force, (Vector2)Body.GetWorldPoint(new Xna.Vector2()));
        }

        /// <summary>
        /// Apply a force at a world point. If the force is not
        /// applied at the center of mass, it will generate a torque and
        /// affect the angular velocity. This wakes up the body.
        /// </summary>
        /// <param name="force">The world force vector, usually in Newtons (N).</param>
        /// <param name="point">The world position of the point of application.</param>
        public void ApplyForce(Vector2 force, Vector2 point)
        {
            _applyForce(force, point);
        }

        void _applyForce(Vector2 force, Vector2 point)
        {
            Body.ApplyForce((Xna.Vector2)force, (Xna.Vector2)point);
        }

        public void ApplyTorque(float torque)
        {
            Body.ApplyTorque(torque);
        }

        public void ApplyGravity(Vector2 force)
        {
            foreach (BodyData data in Tree<BodyData>.GetAll(BodyEx.GetData(Body)))
            {
                var massData = BodyEx.GetLocalMassData(data.Body);
                data.Body.ApplyForce(
                    (Xna.Vector2)force * massData.Mass,
                    (Xna.Vector2)massData.Centroid);
            }
        }

        public void SetSensor(bool isSensor)
        {
            IsSensor = isSensor;
            foreach (BodyData data in Tree<BodyData>.GetAll(BodyEx.GetData(Body)))
            {
                data.Body.IsSensor = isSensor;
            }
        }

        public override Transform2 GetTransform()
        {
            return BodyEx.GetTransform(Body).SetScale(_scale);
        }

        public override void SetTransform(Transform2 transform)
        {
            _setTransform(Body, transform);
            _scale = transform.Scale;
            base.SetTransform(transform);
        }

        void _setTransform(Body body, Transform2 transform, bool checkScale = true)
        {
            if (checkScale && _scale != transform.Scale)
            {
                DebugEx.Assert(!Scene.InWorldStep, "Scale cannot change during a physics step.");

                BodyEx.ScaleFixtures(body, transform.Scale);
            }
            BodyEx.SetTransform(body, transform);

            foreach (BodyData data in BodyEx.GetData(body).Children)
            {
                _setTransform(data.Body, Portal.Enter(data.BodyParent.Portal, transform));
            }
        }

        public override Transform2 GetVelocity()
        {
            return BodyEx.GetVelocity(Body);
        }

        /// <summary>
        /// Set Actor's velocity.  The scale component is ignored.
        /// </summary>
        public override void SetVelocity(Transform2 velocity)
        {
            BodyEx.SetVelocity(Body, velocity);
            base.SetVelocity(velocity);
        }

        /// <summary>
        /// Get world coordinates for collision mask.
        /// </summary>
        public IList<Vector2> GetWorldVertices()
        {
            Vector2[] worldVertices = Vector2Ex.Transform(Vertices, WorldTransform.GetMatrix()).ToArray();
            return worldVertices;
        }

        /// <summary>
        /// Returns polygon that is the local polygon with only the local transforms Scale component applied. 
        /// This is useful because the vertices should match up with vertices in the physics fixtures for this Actor's body (within rounding errors).
        /// </summary>
        public static List<Vector2> GetFixtureContour(Actor actor)
        {
            return GetFixtureContour(actor.Vertices, actor.GetTransform().Scale);
        }

        public static List<Vector2> GetFixtureContour(IList<Vector2> vertices, Vector2 scale)
        {
            DebugEx.Assert(scale.X != 0 && scale.Y != 0);
            Matrix4 scaleMat = Matrix4.CreateScale(new Vector3(scale));
            List<Vector2> contour = Vector2Ex.Transform(vertices, scaleMat);
            if (Math.Sign(scale.X) != Math.Sign(scale.Y))
            {
                contour.Reverse();
            }
            return contour;
        }

        public static void AssertTransform(Actor actor)
        {
            /*Bodies don't have a scale component so we use the default scale when comparing the Actor's
             * scale to that of the child bodies.*/
            Transform2 actorTransform = actor.WorldTransform.SetScale(Vector2.One);

            /*foreach (BodyData data in Tree<BodyData>.GetAll(BodyExt.GetData(actor.Body)))
            {
                Transform2 bodyTransform = UndoPortalTransform(data, BodyExt.GetTransform(data.Body));
                bodyTransform.SetScale(Vector2.One);
                DebugEx.Assert(bodyTransform.AlmostEqual(actorTransform, 0.01f, 0.01f));
            }*/
        }

        static Transform2 UndoPortalTransform(BodyData data, Transform2 transform)
        {
            if (data.Parent == null)
            {
                return transform;
            }
            return UndoPortalTransform(
                data.Parent,
                transform.Transform(Portal.GetLinkedTransform(data.BodyParent.Portal).Inverted()));
        }

        /// <summary>
        /// Verifies the BodyType for Actor bodies is correct.
        /// </summary>
        /// <returns></returns>
        public static void AssertBodyType(Actor actor)
        {
            if (actor.Body.BodyType != actor.BodyType)
            {
                DebugEx.Fail("");
            }
            foreach (BodyData data in BodyEx.GetData(actor.Body).Children)
            {
                _assertBodyType(data);
            }
        }

        static void _assertBodyType(BodyData bodyData)
        {
            DebugEx.Assert(
                (bodyData.Body.BodyType == BodyType.Dynamic && bodyData.Actor.BodyType == BodyType.Dynamic) ||
                (bodyData.Body.BodyType == BodyType.Kinematic && bodyData.Actor.BodyType != BodyType.Dynamic));
            foreach (BodyData data in bodyData.Children)
            {
                _assertBodyType(data);
            }
        }
    }
}