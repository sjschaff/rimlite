﻿// A dumping ground for things I don't yet know where to put
using System;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public enum Tool { None, Hammer, Pickaxe, Axe };

    public enum Dir { Down, Left, Up, Right }

    public static class DirExt
    {
        public static Dir NextCW(this Dir dir)
        {
            switch (dir)
            {
                case Dir.Down: return Dir.Left;
                case Dir.Left: return Dir.Up;
                case Dir.Up: return Dir.Right;
                case Dir.Right: return Dir.Down;
                default:
                    throw new ArgumentException();
            }
        }

        public static Dir NextCCW(this Dir dir)
        {
            switch (dir)
            {
                case Dir.Down: return Dir.Right;
                case Dir.Right: return Dir.Up;
                case Dir.Up: return Dir.Left;
                case Dir.Left: return Dir.Down;
                default:
                    throw new ArgumentException();
            }
        }
    }

    public interface ISelectable
    {
        DefNamed def { get; }
        Vec2I pos { get; }
    }
}
