﻿using Game;
using Game.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeLoopInc
{
    public class Scene
    {
        public HashSet<Vector2i> Walls = new HashSet<Vector2i>();
        public List<Player> Players = new List<Player>();
        public Player CurrentPlayer;
        public List<TimePortal> Portals = new List<TimePortal>();
        public List<Block> Blocks = new List<Block>();
        public SceneState State = new SceneState();
        public int StartTime => Blocks.OfType<IGridEntity>().Concat(Players).Min(item => item.StartTime);

        public Scene()
        {
            Walls = new HashSet<Vector2i>()
            {
                new Vector2i(1, 1),
                new Vector2i(1, 2),
                new Vector2i(1, 4),
            };
            CurrentPlayer = new Player(new Vector2i(), 0);
            Players.Add(CurrentPlayer);

            var portal0 = new TimePortal(new Vector2i(4, 0), Direction.Right);
            var portal1 = new TimePortal(new Vector2i(-2, -2), Direction.Left);
            portal0.SetLinked(portal1);
            portal0.SetTimeOffset(10);

            Portals.Add(portal0);
            Portals.Add(portal1);
            Blocks.Add(new Block(new Vector2i(2, 0), 0));

            SetTime(StartTime);
        }

        public void Step(Input input)
        {
            CurrentPlayer.Input.Add(input);
            SetTime(State.Time);
        }

        void SetTime(int time)
        {
            State.Time = StartTime;
            State = new SceneState();
            for (int i = StartTime; i <= time; i++)
            {
                _step();
            }
        }

        void _step()
        {
            foreach (var entity in State.Entities.Keys.ToList())
            {
                if (entity.EndTime == State.Time)
                {
                    State.Entities.Remove(entity);
                }
            }

            foreach (var entity in State.Entities.Keys)
            {
                if (entity is Player player)
                {
                    State.Entities[entity].SetPosition(Move(State.Entities[entity].Position, player.GetInput(State.Time).Heading));
                }
            }

            foreach (var block in State.Entities.Values.OfType<BlockInstant>())
            {
                block.IsPushed = false;
                block.AddPosition(new Vector2i());
            }

            var directions = new[] { Direction.Left, Direction.Right, Direction.Up, Direction.Down };
            for (int i = 0; i < directions.Length; i++)
            {
                foreach (var block in State.Entities.Keys.OfType<Block>())
                {
                    var adjacent = State.Entities[block].Position - DirectionToVector(directions[i]);
                    var pushes = State.Entities.Values
                        .OfType<PlayerInstant>()
                        .Where(item => item.PreviousPosition == adjacent && item.Position == State.Entities[block].Position);

                    var blockInstant = (BlockInstant)State.Entities[block];
                    if (pushes.Count() >= block.Size && !blockInstant.IsPushed)
                    {
                        blockInstant.IsPushed = true;
                        blockInstant.SetPosition(Move(blockInstant.Position, directions[i]));
                    }
                }
            }

            if (State.Entities.ContainsKey(CurrentPlayer))
            {
                var portal = Portals.FirstOrDefault(item => item.Position == State.Entities[CurrentPlayer].Position);
                if (portal != null)
                {
                    var newTime = State.Time + portal.TimeOffset;
                    CurrentPlayer.EndTime = State.Time;
                    CurrentPlayer = new Player(portal.Position, newTime);
                    Players.Add(CurrentPlayer);
                    SetTime(newTime);
                    return;
                }
            }

            foreach (var player in Players)
            {
                if (player.StartTime == State.Time)
                {
                    State.Entities.Add(player, new PlayerInstant(player.StartPosition));
                }
            }
            foreach (var block in Blocks)
            {
                if (block.StartTime == State.Time)
                {
                    State.Entities.Add(block, new BlockInstant(block.StartPosition));
                }
            }

            State.Time++;
        }

        Vector2i Move(Vector2i position, Direction? heading)
        {
            var posNext = position + DirectionToVector(heading);
            if (!Walls.Contains(posNext))
            {
                return posNext;
            }
            return position;
        }

        public static Vector2i DirectionToVector(Direction? heading)
        {
            switch (heading)
            {
                case Direction.Right: return new Vector2i(1, 0);
                case Direction.Up: return new Vector2i(0, 1);
                case Direction.Left: return new Vector2i(-1, 0);
                case Direction.Down: return new Vector2i(0, -1);
                case null: return new Vector2i();
                default: throw new Exception();
            }
        }
    }
}