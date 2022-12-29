using BB;
using System;
using System.Collections;
using UnityEngine;

using TM = UnityEngine.Tilemaps;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public class AetherSim : FixedTimeSim
    {
        private class AetherTiles
        {
            public readonly float[,] density;
            public readonly Vec2[,] velocity;

            public AetherTiles(int x, int y)
            {
                density = new float[x, y];
                velocity = new Vec2[x, y];
            }
        }

        private class AetherTilesDB {
            public AetherTiles current;
            public AetherTiles next;

            public AetherTilesDB(int x, int y)
            {
                current = new AetherTiles(x, y);
                next = new AetherTiles(x, y);
            }

            public void Swap()
            {
                var tmp = current;
                current = next;
                next = tmp;

                /*
                for (int x = 0; x < current.density.GetLength(0); ++x)
                    for (int y = 0; y < current.density.GetLength(1); ++y)
                    {
                        next.density[x, y] = current.density[x, y];
                    }
                */
            }

            public float GetDensity(int x, int y)
                => current.density[x, y];
        }


        private struct Aether
        {
            public readonly int width, height;
            public readonly SpriteRenderer[,] sprites;

            public readonly AetherTilesDB[] aethers;

            public Aether(int width, int height, Func<Vec2I, SpriteRenderer> createSpriteFn)
            {
                this.width = width;
                this.height = height;
                sprites = new SpriteRenderer[width, height];
                for (int x = 0; x < width; ++x)
                    for (int y = 0; y < height; ++y)
                        sprites[x, y] = createSpriteFn(new Vec2I(x, y));

                aethers = new AetherTilesDB[3];
                for (int i = 0; i < 3; ++i)
                    aethers[i] = new AetherTilesDB(width, height);
            }

            public void Swap()
            {
                foreach (var a in aethers)
                    a.Swap();
            }
        }


        private const float updateRate = 1 / 60f;

        private readonly Game game;
        private readonly Aether aether;

        public readonly AetherTestBase sph;


        public AetherSim(Game game, int size)
        {
            Debug.Log("Initializing Aether Subsystem");
            sph = new AetherSPH_2();
            return;

            var whiteTex = AssetSrc.CreateFlatTex(Color.white);
            var sprite = Sprite.Create(
                whiteTex, new Rect(Vec2I.zero, Vec2I.one), Vec2I.zero, 1, 0,
                SpriteMeshType.FullRect, Vector4.zero, false);

            SpriteRenderer createSprite(Vec2I pos)
            {
                var name = $"aether_tile_{pos.x}_{pos.y}";
                var spriteRenderer = game.assets.CreateObjectWithRenderer<SpriteRenderer>(
                    game.aetherContainer, pos, name, RenderLayer.OverMinion.Layer(200));
                spriteRenderer.sprite = sprite;
                return spriteRenderer;
            }

            aether = new Aether(size, size, createSprite);
        }


        private void Render()
        {
            for (int x = 0; x < aether.width; ++x)
                for (int y = 0; y < aether.height; ++y)
                {
                    var red = aether.aethers[0].GetDensity(x, y);
                    var green = aether.aethers[1].GetDensity(x, y);
                    var blue = aether.aethers[2].GetDensity(x, y);
                    aether.sprites[x, y].color = new Color(red / 100f, green / 100f, blue / 100f, 1);
                }
        }

       /* void diffuse(int N, int b, float* x, float* x0, float diff, float dt)
        {
            int i, j, k;
            float a = dt * diff * N * N;
            for (k = 0; k < 20; k++)
            {
                for (i = 1; i <= N; i++)
                {
                    for (j = 1; j <= N; j++)
                    {
                        x[IX(i, j)] = (
                            x0[IX(i, j)] +
                            a * (
                                x[IX(i - 1, j)] +
                                x[IX(i + 1, j)] +
                                x[IX(i, j - 1)] +
                                x[IX(i, j + 1)]
                           )
                        ) / (1 + 4 * a);
                    }
                }
                set_bnd(N, b, x);
            }
        }*/

        private void Accumulate(AetherTilesDB tiles, int x, int y, ref float sum, ref int count)
        {
            if (x < 0 || x >= aether.width || y < 0 || y >= aether.height)
                return;

            sum += tiles.current.density[x, y];
            count += 1;
        }

        private void Simulate(AetherTilesDB tiles, float dt)
        {
            //for (int iter = 0; iter < 20; ++iter)
            var diffusion = dt * 1f;
            for (int x = 0; x < aether.width; ++x)
                for (int y = 0; y < aether.height; ++y) {
                    float sumIn = 0;
                    int numTiles = 0;
                    Accumulate(tiles, x - 1, y, ref sumIn, ref numTiles);
                    Accumulate(tiles, x + 1, y, ref sumIn, ref numTiles);
                    Accumulate(tiles, x, y - 1, ref sumIn, ref numTiles);
                    Accumulate(tiles, x, y + 1, ref sumIn, ref numTiles);
                    var cur = tiles.current.density[x, y];

                    tiles.next.density[x, y] = cur + diffusion * (sumIn - numTiles * cur);
                }
        }

        protected override void Tick(float dt)
        {
            //sph.Tick(dt);
            return;
            /*
            Navier-Strokes:
                dV/dt + (V * X)V = -Xp/Y +

            */
            aether.aethers[0].current.density[8, 8] += 500 * dt;

            foreach (var a in aether.aethers)
                Simulate(a, dt);

            aether.Swap();
            Render();
        }
    }
}
