using UnityEngine;

using Vec2 = UnityEngine.Vector2;

namespace BB
{
    public class Projectile : Effect
    {
        private readonly Vec2 target;
        private readonly float speed;

        private readonly SpriteRenderer sprite;
        private readonly SpriteRenderer spriteTrailA;
        private readonly SpriteRenderer spriteTrailB;

        private readonly Vec2?[] prevPos = new Vec2?[2];
        private bool finished = false;

        public Projectile(Game game, Vec2 start, Vec2 target, float speed,
            SpriteDef def, Vec2 spriteForward)
            : base(game)
        {
            this.target = target;
            this.speed = speed;

            Vec2 dir = target - start;
            float angle = Vec2.SignedAngle(spriteForward, dir);
            var rot = Quaternion.Euler(0, 0, angle);
            sprite = MakeProjSprite(game, start, rot, def, Color.white, 3);
            spriteTrailA = MakeProjSprite(game, start, rot, def, Color.white.Alpha(.25f), 2);
            spriteTrailB = MakeProjSprite(game, start, rot, def, Color.white.Alpha(.0625f), 1);
        }

        private SpriteRenderer MakeProjSprite(
            Game game, Vec2 start, Quaternion rot, SpriteDef def, Color color, int layer)
        {
            var sprite = game.assets.CreateSpriteObject(
                game.effectsContainer, start, "Projectile",
                def, color, RenderLayer.OverMap.Layer(layer));
            sprite.transform.localRotation = rot;
            return sprite;
        }

        public override void Update(float dt)
        {
            prevPos[1] = prevPos[0];

            if (!finished)
            {
                Vec2 pos = sprite.transform.position.xy();
                prevPos[0] = pos;

                Vec2 dirTarget = target - pos;
                float dist = dt * speed;
                if (dist * dist > dirTarget.sqrMagnitude)
                {
                    dist = dirTarget.magnitude;
                    finished = true;
                }

                Vec2 travel = dirTarget.normalized * dist;
                if (game.GetFirstRaycastTarget(Ray.FromDir(pos, travel), false) != null)
                {
                    // TODO: hit that target
                    finished = true;
                    ConfigSprite(sprite, null);
                }
                else
                    ConfigSprite(sprite, pos + travel);
            }
            else
            {
                if (prevPos[1] == null)
                    game.RemoveEffect(this);

                prevPos[0] = null;
            }

            ConfigSprite(spriteTrailA, prevPos[0]);
            ConfigSprite(spriteTrailB, prevPos[1]);
        }

        private void ConfigSprite(SpriteRenderer sprite, Vec2? pos)
        {
            if (pos == null)
                sprite.enabled = false;
            else
            {
                sprite.enabled = true;
                sprite.transform.localPosition = (Vec2)pos;
            }
        }

        public override void Destroy()
        {
            sprite.Destroy();
            spriteTrailA.Destroy();
            spriteTrailB.Destroy();
        }
    }
}
