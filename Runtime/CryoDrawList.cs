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

        // 添加裁剪区域栈
        private readonly Stack<Rect> _clipRectStack = new Stack<Rect>();
        private Rect? _currentClipRect;

        public Mesh Mesh { get; private set; }

        public void Clear()
        {
            _vertices.Clear();
            _indices.Clear();
            _colors.Clear();
            _uvs.Clear();
        }

        public void AddRect(Rect rect, Color32 color)
        {
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
            if (IsClipped(rect)) return; // 检查裁剪

            AddRect(rect, borderColor);
            var innerRect = new Rect(rect.x + borderWidth, rect.y + borderWidth, rect.width - borderWidth * 2, rect.height - borderWidth * 2);
            AddRect(innerRect, fillColor);
        }

        // 添加裁剪区域
        public void PushClipRect(Rect rect)
        {
            _clipRectStack.Push(rect);
            _currentClipRect = rect;
        }

        public void PopClipRect()
        {
            if (_clipRectStack.Count > 0)
                _clipRectStack.Pop();
            _currentClipRect = _clipRectStack.Count > 0 ? _clipRectStack.Peek() : (Rect?)null;
        }

        // 在绘制方法中检查裁剪（例如 AddRectFilled 开头添加）
        private bool IsClipped(Rect rect)
        {
            if (!_currentClipRect.HasValue) return false;
            return !_currentClipRect.Value.Overlaps(rect);
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