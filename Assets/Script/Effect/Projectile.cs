using UnityEngine;

using Vec2 = UnityEngine.Vector2;

namespace BB
{
    public class Projectile : Effect
    {
        private readonly Vec2 target;
        private readonly float speed;
        private readonly Transform transform;

        public Projectile(Game game, Vec2 start, Vec2 target, float speed,
            SpriteDef sprite, Vec2 spriteForward)
            : base(game)
        {
            this.target = target;
            this.speed = speed;
            this.transform = game.assets.CreateSpriteObject(
                game.effectsContainer, start, "Projectile",
                sprite, Color.white, RenderLayer.OverMap.Layer(1)).transform;

            Vec2 dir = target - start;
            float angle = Vec2.SignedAngle(spriteForward, dir);
            transform.localRotation = Quaternion.Euler(0, 0, angle);
        }

        public override void Update(float dt)
        {
            Vec2 pos = transform.position.xy();
            Vec2 dirTarget = target - pos;
            bool finished = false;
            float dist = dt * speed;
            if (dist * dist > dirTarget.sqrMagnitude)
            {
                dist = dirTarget.magnitude;
                finished = true;
            }

            Vec2 travel = dirTarget.normalized * dist;
            if (game.GetFirstRaycastTarget(new Ray(pos, travel), false) != null)
            {
                // TODO: hit that target
                finished = true;
            }

            if (finished)
                game.RemoveEffect(this);
            else
                transform.localPosition = pos + travel;
        }

        public override void Destroy() => transform.Destroy();
    }
}
