using BB;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;
using Vec3 = UnityEngine.Vector3;
using Vec3I = UnityEngine.Vector3Int;

namespace BB
{
  public abstract class FixedTimeSim
  {
      private float dTRemainder = 0;
      private const float updateRate = 1 / 120f;

      public void Update(float dt)
      {
          dt += dTRemainder;
          if (dt >= updateRate) {
              Tick(updateRate);
              dt -= updateRate;
          }

          dTRemainder = dt;
      }

      protected abstract void Tick(float dt);
  }

  public abstract class AetherTestBase : FixedTimeSim {

    protected Transform root_xf;

    public AetherTestBase() {
      root_xf = new GameObject("aether_test").transform;
    }

    protected struct SpriteObj {
      public SpriteRenderer sprite;
      public Transform xf;

      public List<GameObject> objs;

      public void Render(Vec2 pos) {
        xf.localPosition = pos;
      }

      public void Destroy() {
        if (xf != null)
          xf.gameObject.Destroy();
        if (objs != null)
          foreach (var obj in objs)
            obj.Destroy();
      }
    }


    protected Sprite CreateParticleSprite(Texture2D tex, float size) {
      var ppu = tex.width / size;
      var rect = new Rect(Vec2I.zero, new Vec2I(tex.width, tex.height));
      return Sprite.Create(
        tex, rect, .5f * Vec2.one, ppu, 0,
        SpriteMeshType.FullRect, Vector4.zero, false);
    }

    protected SpriteObj CreateSpriteObj(Sprite sprite) {
      var obj = new SpriteObj();
      var id = Guid.NewGuid();
      var name = $"aether_sprite_{id}";
      obj.sprite = AssetSrc.singleton.CreateObjectWithRenderer<SpriteRenderer>(
        root_xf, Vec2.zero, name, RenderLayer.OverMinion.Layer(200));
      obj.sprite.sprite = sprite;
      obj.xf = obj.sprite.gameObject.transform;
      obj.objs = new List<GameObject>();
      return obj;
    }

  }

}
