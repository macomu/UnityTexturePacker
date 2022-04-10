using UnityEngine;
using UnityEditor;

namespace macomu
{

/// <summary>
/// The interface which to packing textures
/// </summary>
public interface ITexturePacker
{
    /// <summary>
    /// Packing textures.
    /// </summary>
    /// <param name="textures">The target textures</param>
    /// <param name="padding">Spaces between other textures</param>
    /// <param name="maxSize">Atlas size limitation</param>
    /// <returns>Return texture place rects if success, otherwise null.</returns>
    (Rect[], Texture2D) Pack(Texture2D[] textures, int padding, int maxSize);
}

}
