using UnityEngine;
using UnityEngine.UI;

public class ItemVis : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Text text;

    public void Init(Sprite sprite, string text)
    {
        spriteRenderer.sprite = sprite;
        this.text.text = text;
    }
}
