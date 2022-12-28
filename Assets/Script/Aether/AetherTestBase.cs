using BB;
using System;
using System.Collections;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;
using Vec3 = UnityEngine.Vector3;
using Vec3I = UnityEngine.Vector3Int;

namespace BB
{
  public abstract class AetherTestBase {
    protected readonly Game game;

    public AetherTestBase(Game game) {
      this.game = game;
    }

    protected struct SpriteObj {
      public SpriteRenderer sprite;
      public Transform xf;

      public void Render(Vec2 pos) {
        xf.localPosition = pos;
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
      obj.sprite = game.assets.CreateObjectWithRenderer<SpriteRenderer>(
        game.aetherContainer, Vec2.zero, name, RenderLayer.OverMinion.Layer(200));
      obj.sprite.sprite = sprite;
      obj.xf = obj.sprite.gameObject.transform;
      return obj;
    }

  }

}
