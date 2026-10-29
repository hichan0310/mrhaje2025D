using UnityEngine;

namespace PlayerSystem.Weapons
{
    /// <summary>Short-lived world-space stroke; owns its material and supports stopped-time cinematics.</summary>
    public sealed class WeaponVfxStroke : MonoBehaviour
    {
        public LineRenderer Line { get; private set; }
        private Material ownedMaterial;
        private Color tint;
        private float lifetime, elapsed, expansion = 1;
        private bool unscaled, fading;

        public static WeaponVfxStroke Create(string name, Vector3 position, Material source, Color color,
            float width, Vector3[] points, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent) go.transform.SetParent(parent, false);
            go.transform.position = position;
            var stroke = go.AddComponent<WeaponVfxStroke>();
            stroke.Line = go.AddComponent<LineRenderer>();
            stroke.ownedMaterial = new Material(source);
            stroke.ownedMaterial.mainTexture = Texture2D.whiteTexture;
            stroke.Line.sharedMaterial = stroke.ownedMaterial;
            stroke.Line.useWorldSpace = false;
            stroke.Line.positionCount = points.Length;
            stroke.Line.SetPositions(points);
            stroke.Line.startWidth = stroke.Line.endWidth = width;
            stroke.Line.numCapVertices = 2;
            stroke.Line.sortingOrder = 35;
            stroke.SetTint(color);
            return stroke;
        }

        public static WeaponVfxStroke Ring(string name, Vector3 position, float radius, Material material,
            Color color, float width = 0.04f, Transform parent = null)
        {
            var points = new Vector3[65];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / (points.Length - 1);
                points[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            }
            return Create(name, position, material, color, width, points, parent);
        }

        public void SetTint(Color color)
        {
            tint = color;
            Line.startColor = Line.endColor = color;
        }

        public void Fade(float seconds, float expandTo = 1, bool useUnscaledTime = false)
        {
            lifetime = Mathf.Max(0.01f, seconds);
            expansion = expandTo;
            unscaled = useUnscaledTime;
            fading = true;
        }

        private void Update()
        {
            if (!fading) return;
            elapsed += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lifetime);
            transform.localScale = Vector3.one * Mathf.Lerp(1, expansion, 1 - (1 - t) * (1 - t));
            Line.startColor = Line.endColor = new Color(tint.r, tint.g, tint.b, tint.a * (1 - t));
            if (t >= 1) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (ownedMaterial) Destroy(ownedMaterial);
        }
    }

}
