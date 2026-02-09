using System;
using UnityEngine;

namespace Cryo
{
    public static class CryoUI
    {
        private static readonly CryoStyle Style = new CryoStyle();

        // 菜单状态
        private static int _activeDropdownId = 0;
        private static int _activeMenuId = 0;
        private static float _activeMenuX = 0;
        private static float _activeMenuY = 0;
        private static int _currentMenuItemIndex = 0;
        private static Rect _activeMenuDropdownRect;
        private static Rect _activeMenuTitleRect;
        private static bool _menuClickedThisFrame = false;
        private static float _menuBarY;
        private static float _menuBarHeight;

        #region Window

        public static bool BeginWindow(string title, ref bool isOpen, Vector2 defaultPos, Vector2 defaultSize)
        {
            var ctx = CryoContext.Current;
            int id = ctx.GetId(title);
            ctx.PushId(id);

            var state = ctx.GetWindowState(id);
            state.Title = title;
            state.MenuBarHeight = 0;
            state.ScrollAreaStarted = false;  // ★ 重置滚动区域标记

            if (state.Rect.width == 0)
                state.Rect = new Rect(defaultPos, defaultSize);

            // ★ 点击窗口时提升到最前面
            if (state.Rect.Contains(ctx.MousePosition) && ctx.MouseClicked)
            {
                ctx.BringWindowToFront(id);
            }

            HandleWindowDrag(id, state);

            Rect windowRect = state.Rect;
            Rect titleBarRect = new Rect(windowRect.x, windowRect.y, windowRect.width, state.TitleBarHeight);

            ctx.RegisterInteractiveRect(windowRect);

            var drawList = ctx.DrawListBackground;

            // 窗口阴影效果
            Rect shadowRect = new Rect(windowRect.x + 4, windowRect.y + 4, windowRect.width, windowRect.height);
            drawList.AddRectFilled(shadowRect, new Color32(0, 0, 0, 60), new Color32(0, 0, 0, 0));

            // 窗口背景
            drawList.AddRectFilled(windowRect, Style.WindowBackground, Style.WindowBorder, 1f);

            // 添加冰霜边缘高光
            Rect highlightTop = new Rect(windowRect.x + 1, windowRect.y + 1, windowRect.width - 2, 1);
            drawList.AddRect(highlightTop, new Color32(120, 180, 255, 80));

            // 标题栏
            drawList.AddRectFilled(titleBarRect, Style.TitleBarBackground, Style.WindowBorder);

            // 标题文字
            Vector2 titleSize = ctx.TextRenderer.CalcTextSize(title, Style.FontSize);
            Vector2 titlePos = new Vector2(titleBarRect.x + 10, titleBarRect.y + (titleBarRect.height - titleSize.y) * 0.5f);
            ctx.TextRenderer.AddText(title, titlePos, Style.Text, Style.FontSize);

            // 关闭按钮
            float closeButtonSize = 18f;
            Rect closeButtonRect = new Rect(windowRect.xMax - closeButtonSize - 5, windowRect.y + (state.TitleBarHeight - closeButtonSize) * 0.5f, closeButtonSize, closeButtonSize);
            bool closeHovered = closeButtonRect.Contains(ctx.MousePosition);
            drawList.AddRectFilled(closeButtonRect, closeHovered ? Style.CloseButtonHovered : Style.CloseButtonNormal, closeHovered ? Style.CloseButtonHovered : Style.CloseButtonNormal);
            ctx.TextRenderer.AddText("×", new Vector2(closeButtonRect.x + 4, closeButtonRect.y + 1), Style.Text, 14f);

            if (closeHovered && ctx.MouseClicked)
                isOpen = false;

            state.ContentStart = new Vector2(windowRect.x + 10, windowRect.y + state.TitleBarHeight + 5);
            ctx.CursorPosition = state.ContentStart;
            ctx.StartX = state.ContentStart.x;

            ctx.PushWindow(state);
            state.IsOpen = isOpen;

            return isOpen;
        }

        public static void EndWindow()
        {
            CheckCloseMenuOnClickOutside();

            var ctx = CryoContext.Current;
            var state = ctx.CurrentWindow;

            if (state != null)
            {
                // ★ 只有当滚动区域已开始时才处理
                if (state.ScrollAreaStarted)
                {
                    // 计算内容高度（加回滚动偏移）
                    float contentBottom = ctx.CursorPosition.y + state.ScrollOffset.y;
                    state.ContentHeight = contentBottom - state.ScrollableTop + 10;

                    // 绘制滚动条
                    if (state.HasVerticalScroll)
                    {
                        DrawScrollbar(state);
                    }

                    // 弹出裁剪区域
                    ctx.DrawListForeground.PopClipRect();
                    ctx.TextRenderer.PopClipRect();
                }
            }

            ctx.PopWindow();
            ctx.PopId();
            ctx.StartX = 10f;
            ctx.CursorPosition = new Vector2(10, (state?.Rect.yMax ?? ctx.CursorPosition.y) + 10);
        }

        private static void DrawScrollbar(WindowState state)
        {
            var ctx = CryoContext.Current;
            float scrollbarWidth = 8f;

            Rect scrollbarTrack = new Rect(
                state.Rect.xMax - scrollbarWidth - 4,
                state.ScrollableTop + 2,
                scrollbarWidth,
                state.ScrollableHeight - 4
            );

            // 计算滑块大小和位置
            float visibleRatio = state.ScrollableHeight / Mathf.Max(state.ContentHeight, 1);
            float thumbHeight = Mathf.Max(scrollbarTrack.height * visibleRatio, 20f);
            float scrollRatio = state.MaxScrollY > 0 ? state.ScrollOffset.y / state.MaxScrollY : 0;
            float thumbY = scrollbarTrack.y + scrollRatio * (scrollbarTrack.height - thumbHeight);

            Rect scrollbarThumb = new Rect(scrollbarTrack.x, thumbY, scrollbarWidth, thumbHeight);

            // ★ 不受裁剪影响，直接绘制到前景层（需要临时移除裁剪）
            ctx.DrawListForeground.PopClipRect();
            
            // 绘制轨道
            ctx.DrawListForeground.AddRectFilled(scrollbarTrack, new Color32(40, 50, 60, 100), new Color32(40, 50, 60, 100));

            // 绘制滑块
            bool thumbHovered = scrollbarThumb.Contains(ctx.MousePosition);
            Color32 thumbColor = thumbHovered ? new Color32(100, 150, 200, 200) : new Color32(80, 120, 160, 150);
            ctx.DrawListForeground.AddRectFilled(scrollbarThumb, thumbColor, thumbColor);

            // ★ 恢复裁剪
            Rect scrollableRect = new Rect(state.Rect.x, state.ScrollableTop, state.Rect.width - 12, state.ScrollableHeight);
            ctx.DrawListForeground.PushClipRect(scrollableRect);

            // 滑块拖拽
            int scrollbarId = ctx.GetId("__scrollbar__");
            if (thumbHovered && ctx.MouseClicked)
                ctx.ActiveId = scrollbarId;

            if (ctx.ActiveId == scrollbarId)
            {
                if (ctx.MouseDown)
                {
                    float newThumbY = ctx.MousePosition.y - thumbHeight * 0.5f;
                    float newScrollRatio = (newThumbY - scrollbarTrack.y) / (scrollbarTrack.height - thumbHeight);
                    state.ScrollOffset.y = Mathf.Clamp(newScrollRatio * state.MaxScrollY, 0, state.MaxScrollY);
                }
                else
                {
                    ctx.ActiveId = 0;
                }
            }
        }
        private static void HandleWindowDrag(int id, WindowState state)
        {
            var ctx = CryoContext.Current;
            Rect titleBarRect = new Rect(state.Rect.x, state.Rect.y, state.Rect.width, state.TitleBarHeight);
            bool hovered = titleBarRect.Contains(ctx.MousePosition);

            if (hovered && ctx.MouseClicked && ctx.DraggingWindowId == 0)
            {
                ctx.DraggingWindowId = id;
                ctx.DragOffset = ctx.MousePosition - new Vector2(state.Rect.x, state.Rect.y);
                ctx.BringWindowToFront(id);  // ★ 拖拽时也提升
            }

            if (ctx.DraggingWindowId == id)
            {
                if (ctx.MouseDown)
                {
                    float newX = Mathf.Clamp(ctx.MousePosition.x - ctx.DragOffset.x, 0, Screen.width - state.Rect.width);
                    float newY = Mathf.Clamp(ctx.MousePosition.y - ctx.DragOffset.y, 0, Screen.height - state.Rect.height);
                    state.Rect = new Rect(newX, newY, state.Rect.width, state.Rect.height);
                }
                else ctx.DraggingWindowId = 0;
            }
        }

        private static void CheckCloseMenuOnClickOutside()
        {
            if (_activeMenuId == 0 || _menuClickedThisFrame) return;

            var ctx = CryoContext.Current;
            if (!ctx.MouseClicked) return;

            Rect combinedRect = RectUnion(_activeMenuTitleRect, _activeMenuDropdownRect);
            if (!combinedRect.Contains(ctx.MousePosition))
                _activeMenuId = 0;
        }

        #endregion
        #region Menu Bar

        public static void BeginMenuBar()
        {
            var ctx = CryoContext.Current;
            var state = ctx.CurrentWindow;

            _menuBarY = ctx.CursorPosition.y;
            _menuBarHeight = Style.FontSize + 10;
            _menuClickedThisFrame = false;

            if (state != null)
            {
                state.MenuBarHeight = _menuBarHeight + 6;
            }

            float windowWidth = state?.Rect.width - 20 ?? 300f;
            Rect menuBarRect = new Rect(ctx.CursorPosition.x, ctx.CursorPosition.y, windowWidth, _menuBarHeight);

            ctx.DrawListForeground.AddRectFilled(menuBarRect, Style.MenuBarBackground, Style.WindowBorder);
            ctx.CursorPosition = new Vector2(ctx.CursorPosition.x + 6, ctx.CursorPosition.y);
        }

        public static void EndMenuBar()
        {
            var ctx = CryoContext.Current;
            ctx.CursorPosition = new Vector2(ctx.StartX, _menuBarY + _menuBarHeight + 6);

            // ★ 菜单栏结束后，开始可滚动区域
            BeginScrollArea();
        }

        // ★ 开始可滚动区域
        private static void BeginScrollArea()
        {
            var ctx = CryoContext.Current;
            var state = ctx.CurrentWindow;
            if (state == null || state.ScrollAreaStarted) return;

            state.ScrollAreaStarted = true;

            // 可滚动内容区域
            Rect scrollableRect = new Rect(
                state.Rect.x,
                state.ScrollableTop,
                state.Rect.width - 12,
                state.ScrollableHeight
            );

            // 处理滚动输入
            if (scrollableRect.Contains(ctx.MousePosition) && ctx.ScrollDelta != 0)
            {
                state.ScrollOffset.y -= ctx.ScrollDelta * 30f;
                state.ScrollOffset.y = Mathf.Clamp(state.ScrollOffset.y, 0, Mathf.Max(0, state.MaxScrollY));
            }

            // 设置裁剪区域
            ctx.DrawListForeground.PushClipRect(scrollableRect);
            ctx.TextRenderer.PushClipRect(scrollableRect);

            // 应用滚动偏移
            ctx.CursorPosition = new Vector2(ctx.CursorPosition.x, ctx.CursorPosition.y - state.ScrollOffset.y);
        }

        // ★ 无菜单栏时手动开始滚动
        public static void BeginScrollableContent()
        {
            BeginScrollArea();
        }

        public static bool BeginMenu(string label)
        {
            var ctx = CryoContext.Current;
            int id = ctx.GetId(label);

            Vector2 textSize = ctx.TextRenderer.CalcTextSize(label, Style.FontSize);
            Vector2 menuSize = new Vector2(textSize.x + 18, _menuBarHeight);
            Rect rect = new Rect(ctx.CursorPosition.x, _menuBarY, menuSize.x, menuSize.y);

            ctx.RegisterInteractiveRect(rect);

            bool hovered = rect.Contains(ctx.MousePosition);
            bool isOpen = _activeMenuId == id;

            if (hovered && ctx.MouseClicked)
            {
                _menuClickedThisFrame = true;
                _activeMenuId = isOpen ? 0 : id;
                if (!isOpen)
                {
                    _activeMenuX = rect.x;
                    _activeMenuY = rect.yMax;
                    _activeMenuTitleRect = rect;
                    _activeMenuDropdownRect = new Rect(rect.x, rect.yMax, 180f, 0);
                }
                isOpen = _activeMenuId == id;
            }
            else if (hovered && _activeMenuId != 0 && _activeMenuId != id)
            {
                _activeMenuId = id;
                _activeMenuX = rect.x;
                _activeMenuY = rect.yMax;
                _activeMenuTitleRect = rect;
                _activeMenuDropdownRect = new Rect(rect.x, rect.yMax, 180f, 0);
                isOpen = true;
            }

            if (hovered || isOpen)
                ctx.DrawListForeground.AddRectFilled(rect, Style.MenuHovered, Style.MenuHovered);

            ctx.TextRenderer.AddText(label, new Vector2(rect.x + 9, rect.y + (_menuBarHeight - textSize.y) * 0.5f), Style.Text, Style.FontSize);
            ctx.CursorPosition = new Vector2(rect.xMax, _menuBarY);

            if (isOpen)
            {
                ctx.PushId(id);
                _currentMenuItemIndex = 0;
            }

            return isOpen;
        }

        public static void EndMenu()
        {
            CryoContext.Current.PopId();
        }

        public static bool MenuItem(string label, string shortcut = null)
        {
            var ctx = CryoContext.Current;

            float itemWidth = 180f;
            float itemHeight = Style.FontSize + 10;

            int itemIndex = _currentMenuItemIndex++;
            Rect rect = new Rect(_activeMenuX, _activeMenuY + itemIndex * itemHeight, itemWidth, itemHeight);
            _activeMenuDropdownRect.height = (itemIndex + 1) * itemHeight;

            ctx.RegisterInteractiveRect(rect);

            bool hovered = rect.Contains(ctx.MousePosition);

            // ★ 绘制到覆盖层
            ctx.DrawListOverlay.AddRectFilled(rect, Style.MenuBackground, Style.WindowBorder);
            if (hovered)
                ctx.DrawListOverlay.AddRectFilled(rect, Style.MenuItemHovered, Style.MenuItemHovered);

            ctx.TextRendererOverlay.AddText(label, new Vector2(rect.x + 10, rect.y + (itemHeight - Style.FontSize) * 0.5f), Style.Text, Style.FontSize);

            if (!string.IsNullOrEmpty(shortcut))
            {
                Vector2 shortcutSize = ctx.TextRendererOverlay.CalcTextSize(shortcut, Style.FontSize - 2);
                ctx.TextRendererOverlay.AddText(shortcut, new Vector2(rect.xMax - shortcutSize.x - 10, rect.y + (itemHeight - Style.FontSize + 2) * 0.5f), Style.TextDim, Style.FontSize - 2);
            }

            bool clicked = hovered && ctx.MouseReleased;
            if (clicked)
            {
                _menuClickedThisFrame = true;
                _activeMenuId = 0;
            }

            return clicked;
        }

        #endregion

        #region InputText

        public static bool InputText(string label, ref string text, float width = 200f)
        {
            var ctx = CryoContext.Current;
            int id = ctx.GetId(label);

            float height = Style.FontSize + 12;
            Rect rect = new Rect(ctx.CursorPosition, new Vector2(width, height));

            ctx.RegisterInteractiveRect(rect);

            bool hovered = rect.Contains(ctx.MousePosition);
            bool focused = ctx.FocusedInputId == id;

            if (hovered && ctx.MouseClicked)
            {
                ctx.FocusedInputId = id;
                ctx.WantCaptureKeyboard = true;
                focused = true;
            }

            if (focused && ctx.MouseClicked && !hovered)
            {
                ctx.FocusedInputId = 0;
                ctx.WantCaptureKeyboard = false;
                focused = false;
            }

            if (focused && ctx.EscapePressed)
            {
                ctx.FocusedInputId = 0;
                ctx.WantCaptureKeyboard = false;
                focused = false;
            }

            bool changed = false;
            if (focused)
            {
                if (ctx.BackspacePressed && text.Length > 0)
                {
                    text = text.Substring(0, text.Length - 1);
                    changed = true;
                }

                if (ctx.HasKeyboardInput)
                {
                    foreach (char c in ctx.InputText)
                    {
                        if (c == '\b' || c == '\n' || c == '\r') continue;
                        if (c >= 32)
                        {
                            text += c;
                            changed = true;
                        }
                    }
                }
            }

            Color32 borderColor = focused ? Style.InputBorderFocused : (hovered ? Style.CheckboxHovered : Style.InputBorder);
            ctx.DrawListForeground.AddRectFilled(rect, Style.InputBackground, borderColor, 1f);

            string displayText = text ?? "";
            Vector2 textSize = ctx.TextRenderer.CalcTextSize(displayText, Style.FontSize);

            float maxTextWidth = width - 16;
            string visibleText = displayText;
            if (textSize.x > maxTextWidth)
            {
                while (ctx.TextRenderer.CalcTextSize(visibleText, Style.FontSize).x > maxTextWidth && visibleText.Length > 0)
                {
                    visibleText = visibleText.Substring(1);
                }
            }

            Vector2 textPos = new Vector2(rect.x + 8, rect.y + (height - Style.FontSize) * 0.5f);
            ctx.TextRenderer.AddText(visibleText, textPos, Style.Text, Style.FontSize);

            if (focused)
            {
                float blinkTime = Time.time * 2f;
                if ((int)blinkTime % 2 == 0)
                {
                    Vector2 cursorTextSize = ctx.TextRenderer.CalcTextSize(visibleText, Style.FontSize);
                    float cursorX = textPos.x + cursorTextSize.x + 1;
                    Rect cursorRect = new Rect(cursorX, rect.y + 4, 2, height - 8);
                    ctx.DrawListForeground.AddRect(cursorRect, Style.InputCursor);
                }
            }

            // ★ 计算总宽度（包含标签）
            float totalWidth = width;
            if (!string.IsNullOrEmpty(label))
            {
                Vector2 labelSize = ctx.TextRenderer.CalcTextSize(label, Style.FontSize);
                ctx.TextRenderer.AddText(label, new Vector2(rect.xMax + 10, rect.y + (height - Style.FontSize) * 0.5f), Style.Text, Style.FontSize);
                totalWidth = width + 10 + labelSize.x;  // ★ 包含标签宽度
            }

            // ★ 使用总宽度更新 LastItemEndX
            ctx.LastItemY = ctx.CursorPosition.y;
            ctx.LastItemEndX = ctx.CursorPosition.x + totalWidth;
            ctx.CurrentLineHeight = Mathf.Max(ctx.CurrentLineHeight, height);
            ctx.CursorPosition = new Vector2(ctx.StartX, ctx.CursorPosition.y + height + ctx.ItemSpacing);

            return changed;
        }

        #endregion

        #region Helpers

        private static void AdvanceCursor(Vector2 size)
        {
            var ctx = CryoContext.Current;
            ctx.LastItemY = ctx.CursorPosition.y;
            ctx.LastItemEndX = ctx.CursorPosition.x + size.x;
            ctx.CurrentLineHeight = Mathf.Max(ctx.CurrentLineHeight, size.y);
            ctx.CursorPosition = new Vector2(ctx.StartX, ctx.CursorPosition.y + size.y + ctx.ItemSpacing);
        }

        // ★ 新增：带标签的 AdvanceCursor
        private static void AdvanceCursorWithLabel(Vector2 size, string label, float spacing = 10f)
        {
            var ctx = CryoContext.Current;
            float totalWidth = size.x;
            if (!string.IsNullOrEmpty(label))
            {
                Vector2 labelSize = ctx.TextRenderer.CalcTextSize(label, Style.FontSize);
                totalWidth += spacing + labelSize.x;
            }
            ctx.LastItemY = ctx.CursorPosition.y;
            ctx.LastItemEndX = ctx.CursorPosition.x + totalWidth;
            ctx.CurrentLineHeight = Mathf.Max(ctx.CurrentLineHeight, size.y);
            ctx.CursorPosition = new Vector2(ctx.StartX, ctx.CursorPosition.y + size.y + ctx.ItemSpacing);
        }

        private static Rect RectUnion(Rect a, Rect b)
        {
            if (a.width == 0 && a.height == 0) return b;
            if (b.width == 0 && b.height == 0) return a;
            float xMin = Mathf.Min(a.xMin, b.xMin);
            float yMin = Mathf.Min(a.yMin, b.yMin);
            float xMax = Mathf.Max(a.xMax, b.xMax);
            float yMax = Mathf.Max(a.yMax, b.yMax);
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        public static bool WantCaptureMouse() => CryoContext.Current.WantCaptureMouse;

        #endregion
    }
}