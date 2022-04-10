using System.Linq;
using UnityEngine;

namespace macomu
{
public class UnityTexturePacker : ITexturePacker
{
    public (Rect[], Texture2D) Pack(Texture2D[] textures, int padding, int maxSize)
    {
        var packedTexture = new Texture2D(1, 1);
        var rects = packedTexture.PackTextures(textures, padding, maxSize, false);
        if (rects == null)
        {
            return (null, null);
        }
        // Texture2D.PackTexture can resize texture size when overflow.
        // So checking if rects are shrinked.
        for (int i = 0; i < rects.Length; i++)
        {
            var rect = rects[i];
            var texture = textures[i];
            if (((int)(rect.size.x * packedTexture.width)) != texture.width || ((int)(rect.size.y * packedTexture.height)) != texture.height)
            {
                rects = null;
                break;
            }
        }
        if (rects == null)
        {
            Texture2D.DestroyImmediate(packedTexture);
            return (null, null);
        }
        return (rects, packedTexture);
    }
}

}