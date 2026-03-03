using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore;
using TMPro;

namespace Cryo
{
    public class CryoTextRenderer
    {
        private TMP_FontAsset _fontAsset;
        private Material _fontMaterial;
        private float _fontSize = 15f;
        private bool _initialized = false;

        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<int> _indices = new List<int>();
        private readonly List<Color32> _colors = new List<Color32>();
        private readonly List<Vector2> _uvs = new List<Vector2>();

        // ★ 裁剪区域支持
        private readonly Stack<Rect> _clipRectStack = new Stack<Rect>();
        private Rect? _currentClipRect;


        public Mesh TextMesh { get; private set; }
        public Material FontMaterial => _fontMaterial;
        public TMP_FontAsset FontAsset => _fontAsset;

        public CryoTextRenderer()
        {
            LoadFont();
        }

        private void LoadFont()
        {
            _fontAsset = Resources.Load<TMP_FontAsset>("Fonts/CryoUI_Font SDF");
            if (_fontAsset == null)
            {
                _fontAsset = TMP_Settings.defaultFontAsset;
            }

            if (_fontAsset != null)
            {
                _fontAsset.ReadFontAssetDefinition();
                _fontMaterial = new Material(_fontAsset.material);
                _fontMaterial.hideFlags = HideFlags.HideAndDontSave;

                // 设置更锐利的SDF渲染参数
                _fontMaterial.SetFloat("_OutlineSoftness", 0f);
                _fontMaterial.SetFloat("_WeightNormal", 0.5f);

                _initialized = true;
            }
            else
            {
                Debug.LogWarning("[CryoTextRenderer] 未找到字体资源！");
            }
        }

        public void SetFontSize(float size) => _fontSize = size;

        // ★ 裁剪区域方法
        public void PushClipRect(Rect rect)
        {
            _clipRectStack.Push(rect);
            _currentClipRect = rect;
        }

        public void PopClipRect()
        {
            if (_clipRectStack.Count > 0)
                _clipRectStack.Pop();
            _currentClipRect = _clipRectStack.Count > 0 ? _clipRectStack.Peek() : null;
        }

        public void Clear()
        {
            _vertices.Clear();
            _indices.Clear();
            _colors.Clear();
            _uvs.Clear();
            _clipRectStack.Clear();
            _currentClipRect = null;
        }

        private bool TryGetCharacter(uint unicode, out TMP_Character character, out TMP_FontAsset sourceFont)
        {
            character = null;
            sourceFont = _fontAsset;

            if (_fontAsset == null) return false;

            if (_fontAsset.HasCharacter((char)unicode, searchFallbacks: true, tryAddCharacter: true))
            {
                if (_fontAsset.characterLookupTable.TryGetValue(unicode, out character))
                    return true;

                if (_fontAsset.fallbackFontAssetTable != null)
                {
                    foreach (var fallback in _fontAsset.fallbackFontAssetTable)
                    {
                        if (fallback != null)
                        {
                            fallback.ReadFontAssetDefinition();
                            if (fallback.characterLookupTable.TryGetValue(unicode, out character))
                            {
                                sourceFont = fallback;
                                return true;
                            }
                        }
                    }
                }
            }

            if (_fontAsset.characterLookupTable.TryGetValue(' ', out character))
                return true;

            return false;
        }

        public Vector2 AddText(string text, Vector2 position, Color32 color, float fontSize = 0)
        {
            if (_fontAsset == null || string.IsNullOrEmpty(text))
                return Vector2.zero;
            if (!_initialized) LoadFont();

            float size = fontSize > 0 ? fontSize : _fontSize;
            float scale = size / _fontAsset.faceInfo.pointSize;
            float lineHeight = _fontAsset.faceInfo.lineHeight * scale;
            float ascender = _fontAsset.faceInfo.ascentLine * scale;

            float cursorX = position.x;
            float cursorY = position.y;
            float maxWidth = 0;

            foreach (char c in text)
            {
                if (c == '\n')
                {
                    maxWidth = Mathf.Max(maxWidth, cursorX - position.x);
                    cursorX = position.x;
                    cursorY += lineHeight;
                    continue;
                }

                if (!TryGetCharacter(c, out TMP_Character character, out TMP_FontAsset sourceFont))
                {
                    cursorX += size * 0.5f;
                    continue;
                }

                Glyph glyph = character.glyph;
                if (glyph == null)
                {
                    cursorX += size * 0.5f;
                    continue;
                }

                float fontScale = size / sourceFont.faceInfo.pointSize;
                float sourceAscender = sourceFont.faceInfo.ascentLine * fontScale;

                GlyphMetrics metrics = glyph.metrics;
                GlyphRect glyphRect = glyph.glyphRect;

                float baselineY = cursorY + sourceAscender;

                float charLeft = Mathf.Floor(cursorX + metrics.horizontalBearingX * fontScale + 0.5f);
                float charTop = Mathf.Floor(baselineY - metrics.horizontalBearingY * fontScale + 0.5f);
                float charRight = charLeft + Mathf.Ceil(metrics.width * fontScale);
                float charBottom = charTop + Mathf.Ceil(metrics.height * fontScale);

                // ★ 裁剪检查：完全不可见则跳过
                if (_currentClipRect.HasValue)
                {
                    Rect clip = _currentClipRect.Value;
                    if (charRight <= clip.xMin || charLeft >= clip.xMax ||
                        charBottom <= clip.yMin || charTop >= clip.yMax)
                    {
                        cursorX += metrics.horizontalAdvance * fontScale;
                        continue;
                    }
                }

                float unityTop = Screen.height - charTop;
                float unityBottom = Screen.height - charBottom;

                float atlasWidth = sourceFont.atlasWidth;
                float atlasHeight = sourceFont.atlasHeight;

                float halfPixelU = 0.5f / atlasWidth;
                float halfPixelV = 0.5f / atlasHeight;

                float u0 = glyphRect.x / atlasWidth + halfPixelU;
                float u1 = (glyphRect.x + glyphRect.width) / atlasWidth - halfPixelU;
                float v0 = glyphRect.y / atlasHeight + halfPixelV;
                float v1 = (glyphRect.y + glyphRect.height) / atlasHeight - halfPixelV;

                // ★ 边缘裁剪：将顶点和 UV 裁剪到裁剪区域边界
                if (_currentClipRect.HasValue)
                {
                    Rect clip = _currentClipRect.Value;
                    float origWidth = charRight - charLeft;
                    float origHeight = charBottom - charTop;

                    if (origWidth > 0 && origHeight > 0)
                    {
                        float clippedLeft = Mathf.Max(charLeft, clip.xMin);
                        float clippedRight = Mathf.Min(charRight, clip.xMax);
                        float clippedTop = Mathf.Max(charTop, clip.yMin);
                        float clippedBottom = Mathf.Min(charBottom, clip.yMax);

                        // 按裁剪比例插值 UV
                        float origU0 = u0, origU1 = u1, origV0 = v0, origV1 = v1;

                        u0 = Mathf.Lerp(origU0, origU1, (clippedLeft - charLeft) / origWidth);
                        u1 = Mathf.Lerp(origU0, origU1, (clippedRight - charLeft) / origWidth);
                        // GUI空间 charTop→v1(图集顶部), charBottom→v0(图集底部)
                        v1 = Mathf.Lerp(origV1, origV0, (clippedTop - charTop) / origHeight);
                        v0 = Mathf.Lerp(origV1, origV0, (clippedBottom - charTop) / origHeight);

                        charLeft = clippedLeft;
                        charRight = clippedRight;
                        unityTop = Screen.height - clippedTop;
                        unityBottom = Screen.height - clippedBottom;
                    }
                }

                int vertexOffset = _vertices.Count;

                _vertices.Add(new Vector3(charLeft, unityTop, 0));
                _vertices.Add(new Vector3(charRight, unityTop, 0));
                _vertices.Add(new Vector3(charRight, unityBottom, 0));
                _vertices.Add(new Vector3(charLeft, unityBottom, 0));

                _colors.Add(color);
                _colors.Add(color);
                _colors.Add(color);
                _colors.Add(color);

                _uvs.Add(new Vector2(u0, v1));
                _uvs.Add(new Vector2(u1, v1));
                _uvs.Add(new Vector2(u1, v0));
                _uvs.Add(new Vector2(u0, v0));

                _indices.Add(vertexOffset);
                _indices.Add(vertexOffset + 1);
                _indices.Add(vertexOffset + 2);
                _indices.Add(vertexOffset);
                _indices.Add(vertexOffset + 2);
                _indices.Add(vertexOffset + 3);

                cursorX += metrics.horizontalAdvance * fontScale;
            }

            maxWidth = Mathf.Max(maxWidth, cursorX - position.x);
            return new Vector2(maxWidth, lineHeight);
        }

        public Vector2 CalcTextSize(string text, float fontSize = 0)
        {
            if (_fontAsset == null || string.IsNullOrEmpty(text))
                return Vector2.zero;

            float size = fontSize > 0 ? fontSize : _fontSize;
            float scale = size / _fontAsset.faceInfo.pointSize;
            float lineHeight = _fontAsset.faceInfo.lineHeight * scale;

            float cursorX = 0;
            float maxWidth = 0;
            int lineCount = 1;

            foreach (char c in text)
            {
                if (c == '\n')
                {
                    maxWidth = Mathf.Max(maxWidth, cursorX);
                    cursorX = 0;
                    lineCount++;
                    continue;
                }

                if (TryGetCharacter(c, out TMP_Character character, out TMP_FontAsset sourceFont))
                {
                    float fontScale = size / sourceFont.faceInfo.pointSize;
                    cursorX += character.glyph.metrics.horizontalAdvance * fontScale;
                }
                else
                {
                    cursorX += size * 0.5f;
                }
            }

            maxWidth = Mathf.Max(maxWidth, cursorX);
            return new Vector2(maxWidth, lineHeight * lineCount);
        }

        public void BuildMesh()
        {
            if (TextMesh == null)
            {
                TextMesh = new Mesh { name = "CryoUI Text Mesh" };
                TextMesh.MarkDynamic();
            }

            TextMesh.Clear();
            if (_vertices.Count == 0) return;

            TextMesh.SetVertices(_vertices);
            TextMesh.SetColors(_colors);
            TextMesh.SetUVs(0, _uvs);
            TextMesh.SetTriangles(_indices, 0);
        }

        // 在类中添加方法
        public void SetSharpness(float sharpness)
        {
            if (_fontMaterial != null)
            {
                // 调整SDF边缘锐度 (值越大越锐利，通常0.4-0.6)
                _fontMaterial.SetFloat("_OutlineSoftness", Mathf.Clamp01(1f - sharpness));

                // 如果材质支持，调整面部膨胀
                if (_fontMaterial.HasProperty("_FaceDilate"))
                {
                    _fontMaterial.SetFloat("_FaceDilate", 0.1f);
                }
            }
        }

        /// <summary>
        /// 计算文本从 startIndex 开始 length 个字符的渲染宽度
        /// </summary>
        public float CalcTextWidth(string text, int startIndex, int length, float fontSize = 0)
        {
            if (_fontAsset == null || string.IsNullOrEmpty(text)) return 0;
            if (!_initialized) LoadFont();

            float size = fontSize > 0 ? fontSize : _fontSize;
            float x = 0;
            int end = Mathf.Min(startIndex + length, text.Length);

            for (int i = startIndex; i < end; i++)
            {
                char c = text[i];
                if (TryGetCharacter(c, out var character, out var sourceFont))
                {
                    float fontScale = size / sourceFont.faceInfo.pointSize;
                    x += character.glyph.metrics.horizontalAdvance * fontScale;
                }
                else
                {
                    x += size * 0.5f;
                }
            }
            return x;
        }

        /// <summary>
        /// 根据局部 X 坐标，找到最接近的字符索引（用于鼠标点击定位光标）
        /// </summary>
        public int GetCharIndexAtX(string text, float localX, float fontSize = 0)
        {
            if (_fontAsset == null || string.IsNullOrEmpty(text)) return 0;
            if (!_initialized) LoadFont();

            float size = fontSize > 0 ? fontSize : _fontSize;
            float x = 0;

            for (int i = 0; i < text.Length; i++)
            {
                float advance;
                if (TryGetCharacter(text[i], out var character, out var sourceFont))
                {
                    float fontScale = size / sourceFont.faceInfo.pointSize;
                    advance = character.glyph.metrics.horizontalAdvance * fontScale;
                }
                else
                {
                    advance = size * 0.5f;
                }

                // 如果 x 在字符前半部分，返回当前索引；后半部分返回下一个索引
                if (x + advance * 0.5f > localX)
                    return i;
                x += advance;
            }
            return text.Length;
        }
    }
}