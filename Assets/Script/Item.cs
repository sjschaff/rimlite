using System;
using UnityEngine;
using UnityEngine.UI;

using Vec2 = UnityEngine.Vector2;

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

        public ItemInfo WithAmount(int amt) => new ItemInfo(def, amt);
    }

    public class Item
    {
        public readonly Game game;
        private readonly GameObject gameObject;
        private readonly SpriteRenderer spriteRenderer;
        private readonly Text text;

        public ItemInfo info { get; private set; }
        public ItemDef def => info.def;

        public Item(Game game, ItemInfo info)
        {
            this.game = game;
            this.info = info;

            // TODO: move to item vis
            gameObject = new GameObject("Item:" + info.def.defName);

            spriteRenderer = game.assets.CreateSpriteObject(
                gameObject.transform, new Vec2(.5f, .5f), "icon",
                info.def.sprite, Color.white, RenderLayer.Default.Layer(100));
            spriteRenderer.transform.localScale = new Vector3(1.5f, 1.5f, 1);

            // TODO: figure out htf text works
            var canvasObj = new GameObject("canvas", typeof(RectTransform));
            var canvasTrans = (RectTransform)canvasObj.transform;
            canvasTrans.SetParent(gameObject.transform, false);

            canvasTrans.offsetMin = new Vec2(0, -.3f);
            canvasTrans.offsetMax = new Vec2(1, .7f);
            canvasTrans.sizeDelta = Vec2.one;
            canvasTrans.anchorMin = Vec2.zero;
            canvasTrans.anchorMax = Vec2.zero;
            canvasTrans.localPosition = new Vec2(.5f, .17f); // has to be last for some reason

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

        public void ChangeAmt(int amt)
        {
            info = info.WithAmount(amt);
            UpdateText();
        }


        // TODO: investigate these
        private void ReParentInternal(Transform parent, Vec2 pos)
        {
            gameObject.transform.parent = parent;
            gameObject.transform.localPosition = pos;
        }

        public void ReParent(Transform parent)
        {
            ReParentInternal(parent, Vec2.zero);
        }

        public void ReParent(Tile tile)
        {
            ReParentInternal(game.itemContainer, tile.pos);
        }

        public void Destroy() => gameObject.Destroy();


        // TODO: investigate merging with reparent stuff
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
    }
}