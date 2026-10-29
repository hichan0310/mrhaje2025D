using UnityEngine;
using UnityEngine.UI;

namespace Frontend
{
    /// <summary>A lightweight segmented gauge drawn as UI geometry, without a square fill texture.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WeaponHudArc : MaskableGraphic
    {
        private float fill, threshold;
        public void SetState(float amount, float useThreshold)
        {
            fill = Mathf.Clamp01(amount);
            threshold = Mathf.Clamp01(useThreshold);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.46f;
            const int segments = 48;
            for (int i = 0; i < segments; i++)
            {
                float start = (90 - i * 360f / segments) * Mathf.Deg2Rad;
                float end = (90 - (i + 0.8f) * 360f / segments) * Mathf.Deg2Rad;
                Color tint = i < fill * segments ? color : new Color(0.4f, 0.5f, 0.6f, 0.28f);
                AddQuad(mesh, start, end, radius - 3, radius, tint);
            }
            if (threshold > 0 && threshold < 1)
            {
                float angle = (90 - threshold * 360) * Mathf.Deg2Rad;
                AddQuad(mesh, angle - 0.018f, angle + 0.018f, radius - 5, radius + 3, Color.white);
            }
        }

        private void AddQuad(VertexHelper mesh, float a, float b, float inner, float outer, Color tint)
        {
            int start = mesh.currentVertCount;
            Vector2 center = rectTransform.rect.center;
            mesh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * inner, tint, Vector2.zero);
            mesh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * outer, tint, Vector2.zero);
            mesh.AddVert(center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * outer, tint, Vector2.zero);
            mesh.AddVert(center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * inner, tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
