using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Per-instance emission tint via <see cref="MaterialPropertyBlock"/> — no material
    /// cloning, no GC, no leaked instance materials, and GPU-instancing friendly.
    /// Drop-in replacement for the <c>renderer.material.SetColor("_EmissionColor", c)</c>
    /// pattern. Requires the renderer's shared material to have emission enabled.
    /// </summary>
    public static class RendererEmission
    {
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static MaterialPropertyBlock _mpb;

        public static void Set(Renderer r, Color emission)
        {
            if (r == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(EmissionId, emission);
            r.SetPropertyBlock(_mpb);
        }
    }
}
