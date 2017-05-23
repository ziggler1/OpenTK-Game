﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Common;
using Game.Models;
using OpenTK.Graphics;
using Game.Rendering;
using OpenTK;

namespace TimeLoopInc
{
    public class Block : IGridEntity
    {
        public int Size { get; }
        public Vector2i StartPosition { get; }
        public int StartTime { get; }
        public int EndTime { get; set; }

        public Block(Vector2i startPosition, int startTime, int size = 1)
        {
            StartPosition = startPosition;
            StartTime = startTime;
            Size = size;
        }

        public IGridEntityInstant CreateInstant() => new BlockInstant(StartPosition);

        public IGridEntity DeepClone() => (Block)MemberwiseClone();

        public List<Model> GetModels()
        {
            var model = ModelFactory.CreatePlane(Vector2.One * Size, new Vector3(-Size / 2));
            model.SetColor(new Color4(0.5f, 1f, 0.8f, 1f));
            return new List<Model>() { model };
        }
    }
}
