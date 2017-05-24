﻿using Game.Common;
using Game.Serialization;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TimeLoopInc
{
    [DataContract]
    public class Transform2i : IShallowClone<Transform2i>
    {
        [DataMember]
        public Vector2i Position;
        [DataMember]
        public Direction Rotation;
        [DataMember]
        public bool MirrorX { get; set; }
        [DataMember]
        public int _size = 1;
        public int Size
        {
            get { return _size; }
            set
            {
                Debug.Assert(!double.IsNaN(value) && !double.IsPositiveInfinity(value) && !double.IsNegativeInfinity(value));
                _size = value;
            }
        }

        public Vector2i Scale => MirrorX ? new Vector2i(-Size, Size) : new Vector2i(Size, Size);

        public Transform2i(Vector2i position, Direction rotation = Direction.Right, int size = 1, bool mirrorX = false)
        {
            Position = position;
            Rotation = rotation;
            Size = size;
        }

        public Transform2 ToTransform2()
        {
            return new Transform2((Vector2)Position, Size, (float)DirectionEx.ToAngle(Rotation), MirrorX);
        }

        public Transform2i ShallowClone() => (Transform2i)MemberwiseClone();

        public static Transform2i RoundTransform2(Transform2 transform)
        {
            return new Transform2i(
                (Vector2i)transform.Position.SnapToGrid(Vector2.One), 
                Direction.Right,//transform.Rotation / (Math.PI / 2), 
                (int)Math.Round(transform.Size), 
                transform.MirrorX);
        }
    }
}
