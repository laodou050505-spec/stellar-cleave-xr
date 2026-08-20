using UnityEngine;
using UnityEngine.UI;

namespace CosmicSlice
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CosmicPanelGraphic : MaskableGraphic
    {
        [SerializeField, Min(0f)] private float cornerCut = 18f;

        public float CornerCut
        {
            get => cornerCut;
            set
            {
                cornerCut = Mathf.Max(0f, value);
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var rect = GetPixelAdjustedRect();
            var cut = Mathf.Min(cornerCut, Mathf.Min(rect.width, rect.height) * 0.42f);
            var points = new[]
            {
                new Vector2(rect.xMin + cut, rect.yMax),
                new Vector2(rect.xMax - cut, rect.yMax),
                new Vector2(rect.xMax, rect.yMax - cut),
                new Vector2(rect.xMax, rect.yMin + cut),
                new Vector2(rect.xMax - cut, rect.yMin),
                new Vector2(rect.xMin + cut, rect.yMin),
                new Vector2(rect.xMin, rect.yMin + cut),
                new Vector2(rect.xMin, rect.yMax - cut)
            };

            var center = UIVertex.simpleVert;
            center.color = color;
            center.position = rect.center;
            center.uv0 = new Vector2(0.5f, 0.5f);
            vertexHelper.AddVert(center);

            for (var index = 0; index < points.Length; index++)
            {
                var vertex = UIVertex.simpleVert;
                vertex.color = color;
                vertex.position = points[index];
                vertex.uv0 = new Vector2(
                    Mathf.InverseLerp(rect.xMin, rect.xMax, points[index].x),
                    Mathf.InverseLerp(rect.yMin, rect.yMax, points[index].y));
                vertexHelper.AddVert(vertex);
            }

            for (var index = 0; index < points.Length; index++)
            {
                vertexHelper.AddTriangle(0, index + 1, ((index + 1) % points.Length) + 1);
            }
        }
    }
}
