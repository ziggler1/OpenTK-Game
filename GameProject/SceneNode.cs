﻿using FarseerPhysics.Dynamics;
using Game.Portals;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace Game
{
    /// <summary>
    /// Scene graph node.  All derived classes MUST override ShallowClone() and return an instance of the derived class.
    /// </summary>
    [DataContract, DebuggerDisplay("SceneNode {Name}")]
    public class SceneNode : ITreeNode<SceneNode>, IDeepClone, ISceneObject, IPortalCommon
    {
        [DataMember]
        public PortalPath Path { get; set; } = new PortalPath();
        [DataMember]
        Transform2 _worldTransformPrevious = null;
        public Transform2 WorldTransform
        {
            get { return _worldTransformPrevious?.ShallowClone(); }
            set { _worldTransformPrevious = value?.ShallowClone(); }
        }
        [DataMember]
        Transform2 _worldVelocityPrevious = null;
        public Transform2 WorldVelocity
        {
            get { return _worldVelocityPrevious?.ShallowClone(); }
            set { _worldVelocityPrevious = value?.ShallowClone(); }
        }
        IPortalCommon ITreeNode<IPortalCommon>.Parent { get { return Parent; } }
        List<IPortalCommon> ITreeNode<IPortalCommon>.Children { get { return Children.ToList<IPortalCommon>(); } }

        [DataMember]
        public string Name { get; set; }
        [DataMember]
        HashSet<SceneNode> _children = new HashSet<SceneNode>();
        public List<SceneNode> Children { get { return new List<SceneNode>(_children); } }
        [DataMember]
        public SceneNode Parent { get; private set; }
        [DataMember]
        public Scene Scene { get; private set; }
        IScene IPortalCommon.Scene { get { return Scene; } }
        public virtual bool IsBackground { get { return false; } }

        #region Constructors
        public SceneNode(Scene scene)
        {
            Scene = scene;
            Scene.SceneNodes.Add(this);
            Name = "";
        }
        #endregion

        public virtual IDeepClone ShallowClone()
        {
            SceneNode clone = new SceneNode(Scene);
            ShallowClone(clone);
            return clone;
        }

        protected void ShallowClone(SceneNode destination)
        {
            //Remove the child pointer from the root node since the cloned instance is automatically parented to it.
            //Scene.Root._children.Remove(destination);
            destination.Parent = Parent;
            destination._children = new HashSet<SceneNode>(Children);
            destination.Name = Name + " Clone";
        }

        public virtual HashSet<IDeepClone> GetCloneableRefs()
        {
            return new HashSet<IDeepClone>(Children);
        }

        public virtual void UpdateRefs(IReadOnlyDictionary<IDeepClone, IDeepClone> cloneMap)
        {
            if (Parent != null)
            {
                if (cloneMap.ContainsKey(Parent))
                {
                    Parent = (SceneNode)cloneMap[Parent];
                }
                else
                {
                    SetParent(Parent);
                }
            }

            List<SceneNode> children = Children;
            _children.Clear();
            foreach (SceneNode e in children)
            {
                _children.Add((SceneNode)cloneMap[e]);
            }
        }

        private void RemoveParent()
        {
            if (Parent != null)
            {
                Parent._children.Remove(this);
            }
            Parent = null;
        }

        public virtual void SetParent(SceneNode parent)
        {
            if (parent != null && parent.Scene != Scene)
            {
                Scene.SceneNodes.Remove(this);
                Scene = parent.Scene;
                Scene.SceneNodes.Add(this);
            }

            RemoveParent();
            Parent = parent;

            if (parent != null)
            {
                parent._children.Add(this);
            }
            
            Debug.Assert(Scene.SceneNodes.FindAll(item => item == this).Count <= 1);
            Debug.Assert(!Tree<SceneNode>.ParentLoopExists(this), "Cannot have cycles in Parent tree.");
            PortalCommon.ResetWorldTransform(this);
        }

        public void RemoveChildren()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                Children[i].SetParent(null);
            }
        }

        /// <summary>Remove from scene.</summary>
        public virtual void Remove()
        {
            RemoveParent();
            Scene.SceneNodes.Remove(this);
            Scene = null;
        }

        public virtual Transform2 GetTransform()
        {
            return new Transform2();
        }

        /// <summary>
        /// Set transform and update children.  This method is expected to only be called by classes extending SceneNode.
        /// </summary>
        /// <param name="transform"></param>
        public virtual void SetTransform(Transform2 transform)
        {
            PortalCommon.ResetWorldTransform(this);
        }

        public virtual void SetVelocity(Transform2 transform)
        {
        }

        public virtual Transform2 GetVelocity()
        {
            return Transform2.CreateVelocity();
        }

        public Transform2 GetWorldTransform(bool ignorePortals = false)
        {
            return WorldTransform;
        }

        public Transform2 GetWorldVelocity(bool ignorePortals = false)
        {
            return WorldVelocity;
        }
    }
}
