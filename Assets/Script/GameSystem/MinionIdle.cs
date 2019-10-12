using System.Collections.Generic;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;
using Vec3 = UnityEngine.Vector3;

namespace BB
{
    public class MinionIdle
    {
        public readonly Game game;
        private readonly System.Random rand;

        public MinionIdle(Game game) {
            this.game = game;
            this.rand = new System.Random();
        }

        public void AssignIdleTask(Minion minion)
        {
            const int maxTries = 10;
            const float maxWander = 16;
            for (int i = 0; i < maxTries; ++i)
            {
                float rad = (float)rand.NextDouble() * maxWander;
                float deg = (float)rand.NextDouble() * 360;
                var roto = Quaternion.Euler(0, 0, deg);
                Vec2 vec = roto * new Vec3(rad, 0, 0);
                Vec2 pt = minion.pos + Vec2.one * .5f + vec;
                Vec2I tile = pt.Floor();
                if (game.ValidTile(tile) && tile != minion.pos && game.Tile(tile).passable)
                {
                    JobTransient.AssignIdleWork(minion, "IdleWanter", WanderTasks(tile));
                    break;
                }
            }
        }

        private IEnumerable<Task> WanderTasks(Vec2I pos)
        {
            yield return new TaskGoTo(game, "Wandering.", PathCfg.Point(pos));
            yield return new TaskWaitDuration(game, "Wandering.", 1f);
        }
    }
}
