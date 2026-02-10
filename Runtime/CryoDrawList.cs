using System.Collections.Generic;
using UnityEngine;

namespace Cryo
{
    public class CryoDrawList
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<int> _indices = new List<int>();
        private readonly List<Color32> _colors = new List<Color32>();
        private readonly List<Vector2> _uvs = new List<Vector2>();

        // ★ 裁剪栈
        private readonly Stack<Rect> _clipRectStack = new Stack<Rect>();
        private Rect _currentClipRect = new Rect(0, 0, float.MaxValue, float.MaxValue);

        public Mesh Mesh { get; private set; }

        public void Clear()
        {
            _vertices.Clear();
            _indices.Clear();
            _colors.Clear();
            _uvs.Clear();
            _clipRectStack.Clear();
            _currentClipRect = new Rect(0, 0, Screen.width, Screen.height);
        }

        public void PushClipRect(Rect rect)
        {
            _clipRectStack.Push(_currentClipRect);
            // 与当前裁剪区域求交集
            _currentClipRect = RectIntersect(_currentClipRect, ConvertToScreenRect(rect));
        }

        public void PopClipRect()
        {
            if (_clipRectStack.Count > 0)
            {
                _currentClipRect = _clipRectStack.Pop();
            }
            else
            {
                _currentClipRect = new Rect(0, 0, Screen.width, Screen.height);
            }
        }

        private Rect ConvertToScreenRect(Rect guiRect)
        {
            // GUI 坐标系：左上角为原点，Y 向下
            // 屏幕坐标系：左下角为原点，Y 向上
            float screenTop = Screen.height - guiRect.y;
            float screenBottom = Screen.height - (guiRect.y + guiRect.height);
            return new Rect(guiRect.x, screenBottom, guiRect.width, screenTop - screenBottom);
        }

        private Rect RectIntersect(Rect a, Rect b)
        {
            float xMin = Mathf.Max(a.xMin, b.xMin);
            float yMin = Mathf.Max(a.yMin, b.yMin);
            float xMax = Mathf.Min(a.xMax, b.xMax);
            float yMax = Mathf.Min(a.yMax, b.yMax);
            if (xMax < xMin || yMax < yMin)
                return new Rect(0, 0, 0, 0);
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        public void AddRect(Rect rect, Color32 color)
        {
            // ★ 裁剪检查
            Rect screenRect = ConvertToScreenRect(rect);
            if (!_currentClipRect.Overlaps(screenRect))
                return;

            int vertexOffset = _vertices.Count;

            float y1 = Screen.height - rect.y;
            float y2 = Screen.height - (rect.y + rect.height);

            _vertices.Add(new Vector3(rect.x, y1, 0));
            _vertices.Add(new Vector3(rect.x + rect.width, y1, 0));
            _vertices.Add(new Vector3(rect.x + rect.width, y2, 0));
            _vertices.Add(new Vector3(rect.x, y2, 0));

            _colors.Add(color);
            _colors.Add(color);
            _colors.Add(color);
            _colors.Add(color);

            _uvs.Add(new Vector2(0, 0));
            _uvs.Add(new Vector2(1, 0));
            _uvs.Add(new Vector2(1, 1));
            _uvs.Add(new Vector2(0, 1));

            _indices.Add(vertexOffset);
            _indices.Add(vertexOffset + 1);
            _indices.Add(vertexOffset + 2);
            _indices.Add(vertexOffset);
            _indices.Add(vertexOffset + 2);
            _indices.Add(vertexOffset + 3);
        }

        public void AddRectFilled(Rect rect, Color32 fillColor, Color32 borderColor, float borderWidth = 1f)
        {
            AddRect(rect, borderColor);
            var innerRect = new Rect(rect.x + borderWidth, rect.y + borderWidth, rect.width - borderWidth * 2, rect.height - borderWidth * 2);
            AddRect(innerRect, fillColor);
        }

        public void BuildMesh()
        {
            if (Mesh == null)
            {
                Mesh = new Mesh { name = "CryoUI Mesh" };
                Mesh.MarkDynamic();
            }

            Mesh.Clear();
            if (_vertices.Count == 0) return;

            Mesh.SetVertices(_vertices);
            Mesh.SetColors(_colors);
            Mesh.SetUVs(0, _uvs);
            Mesh.SetTriangles(_indices, 0);
        }
    }
}