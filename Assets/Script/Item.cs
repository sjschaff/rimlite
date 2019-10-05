using System;
using UnityEngine;
using UnityEngine.UI;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public struct ItemInfo
    {
        public readonly ItemDef def;
        public readonly int amt;

        public ItemInfo(ItemDef def, int amt)
        {
            BB.Assert(amt > 0);
            this.def = def;
            this.amt = amt;
        }

        public ItemInfo WithNewAmount(int amt) => new ItemInfo(def, amt);
    }

    public class Item : ISelectable
    {
        public readonly Game game;
        private readonly GameObject gameObject;
        private readonly SpriteRenderer spriteRenderer;
        private readonly Text text;

        public Vec2I pos { get; private set; }
        public ItemInfo info { get; private set; }
        private int amtClaimed;
        public int amtAvailable => info.amt - amtClaimed;
        public int amt => info.amt;
        public ItemDef def => info.def;

        public string name => info.def.name;

        public Item(Game game, Vec2I pos, ItemInfo info)
        {
            BB.Assert(info.amt > 0);
            this.game = game;
            this.pos = pos;
            this.info = info;
            this.amtClaimed = 0;

            gameObject = new GameObject("Item:" + info.def.defName);
            ReParent(game.transform, pos);

            spriteRenderer = game.assets.CreateSpriteObject(
                gameObject.transform, new Vec2(.5f, .5f), "icon",
                info.def.sprite, Color.white, RenderLayer.Default.Layer(100));

            // TODO: figure out htf text works
            var canvasObj = new GameObject("canvas", typeof(RectTransform));
            var canvasTrans = (RectTransform)canvasObj.transform;
            canvasTrans.SetParent(gameObject.transform, false);
            canvasTrans.localPosition = new Vec2(.5f, .228f);

            canvasTrans.offsetMin = new Vec2(0, -.3f);
            canvasTrans.offsetMax = new Vec2(1, .7f);
            canvasTrans.sizeDelta = Vec2.one;
            canvasTrans.anchorMin = Vec2.zero;
            canvasTrans.anchorMax = Vec2.zero;
            canvasTrans.localPosition = new Vec2(.5f, .228f); // has to be last for some reason

            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.SetLayer(RenderLayer.Default.Layer(101));

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;
            scaler.referencePixelsPerUnit = 1;

            var textObj = new GameObject("text", typeof(RectTransform));
            var textTrans = (RectTransform)textObj.transform;
            textTrans.SetParent(canvasObj.transform, false);
            textTrans.localScale = new Vector3(0.004753981f, 0.004753981f, 1);
            textTrans.offsetMin = new Vec2(-.5f, -.5f);
            textTrans.offsetMax = new Vec2(.5f, .5f);
            textTrans.sizeDelta = Vec2.one;

            textObj.AddComponent<CanvasRenderer>();
            text = textObj.AddComponent<Text>();
            text.font = game.assets.fonts.Get("Arial.ttf");
            text.fontSize = 30;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.UpperCenter;
            text.raycastTarget = false;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            UpdateText();
        }

        public void ReParent(Transform parent, Vec2 pos)
        {
            gameObject.transform.parent = parent;
            gameObject.transform.localPosition = pos;
        }

        public void Destroy() => gameObject.Destroy();

        public void Claim(int amt)
        {
            BB.Assert(amtClaimed + amt <= info.amt);
            amtClaimed += amt;
        }

        public void Unclaim(int amt)
        {
            BB.Assert(amtClaimed >= amt);
            amtClaimed -= amt;
        }

        // TODO: Add
        public void Remove(int amt)
        {
            BB.Assert(amt < info.amt);
            BB.Assert(amt <= amtAvailable);
            info = info.WithNewAmount(info.amt - amt);
            UpdateText();
        }

        public Item Split(int amt)
        {
            Remove(amt);
            return new Item(game, pos, new ItemInfo(def, amt));
        }

        public enum Config { Ground, PlayerAbove, PlayerBelow }

        public void Configure(Config config)
        {
            if (config == Config.Ground)
            {
                text.enabled = true;
                spriteRenderer.SetLayer(RenderLayer.Default.Layer(1000));
            }
            else
            {
                text.enabled = false;
                if (config == Config.PlayerAbove)
                    spriteRenderer.SetLayer(RenderLayer.OverMinion);
                else if (config == Config.PlayerBelow)
                    spriteRenderer.SetLayer(RenderLayer.Default.Layer(1000));
                else
                    throw new NotSupportedException("Unknown Item Config: " + config);
            }
        }

        private void UpdateText() => text.text = info.amt.ToString();

        public void Place(Vec2I pos) => this.pos = pos;
    }
}