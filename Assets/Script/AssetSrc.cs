using UnityEngine;

public class AssetSrc
{
    public readonly Atlas tileset32;
    public readonly Atlas tileset64;
    public readonly Atlas sprites32;
    public readonly Atlas sprites64;

    public AssetSrc()
    {
        tileset32 = new Atlas(Resources.Load<Texture2D>("tileset32"), 16, 32);
        tileset64 = new Atlas(Resources.Load<Texture2D>("tileset64"), 32, 64);
        sprites32 = new Atlas(Resources.Load<Texture2D>("sprites32"), 8, 32);
        sprites64 = new Atlas(Resources.Load<Texture2D>("sprites64"), 16, 64);
    }
}
