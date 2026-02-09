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
        // 在 CryoUI 类中添加以下 region：

        #region TreeNode

        public static bool TreeNode(string label)
        {
            var ctx = CryoContext.Current;
            int id = ctx.GetId(label);

            bool isOpen = ctx.GetState(id, false);

            Vector2 textSize = ctx.TextRenderer.CalcTextSize(label, Style.FontSize);
            float arrowSize = 16f;
            float windowWidth = ctx.CurrentWindow?.Rect.width - 20 ?? 200f;
            Vector2 nodeSize = new Vector2(windowWidth - (ctx.StartX - ctx.CurrentWindow.Rect.x), Mathf.Max(arrowSize, textSize.y + 6));
            Rect rect = new Rect(ctx.CursorPosition, nodeSize);

            ctx.RegisterInteractiveRect(rect);

            bool hovered = rect.Contains(ctx.MousePosition);
            if (hovered && ctx.MouseClicked)
            {
                isOpen = !isOpen;
                ctx.SetState(id, isOpen);
            }

            if (hovered)
                ctx.DrawListForeground.AddRectFilled(rect, Style.TreeNodeHovered, Style.TreeNodeHovered);

            string arrow = isOpen ? "▼" : "►";
            ctx.TextRenderer.AddText(arrow, new Vector2(ctx.CursorPosition.x + 2, ctx.CursorPosition.y + 3), Style.TextHighlight, 10f);
            ctx.TextRenderer.AddText(label, new Vector2(ctx.CursorPosition.x + arrowSize, ctx.CursorPosition.y + 3), Style.Text, Style.FontSize);

            AdvanceCursor(nodeSize);

            if (isOpen)
            {
                ctx.PushId(id);
                ctx.StartX += 18;
                ctx.CursorPosition = new Vector2(ctx.StartX, ctx.CursorPosition.y);
            }

            return isOpen;
        }

        public static bool TreeLeaf(string label)
        {
            var ctx = CryoContext.Current;

            Vector2 textSize = ctx.TextRenderer.CalcTextSize(label, Style.FontSize);
            float bulletSize = 16f;
            float windowWidth = ctx.CurrentWindow?.Rect.width - 20 ?? 200f;
            Vector2 nodeSize = new Vector2(windowWidth - (ctx.StartX - ctx.CurrentWindow.Rect.x), textSize.y + 6);
            Rect rect = new Rect(ctx.CursorPosition, nodeSize);

            ctx.RegisterInteractiveRect(rect);

            bool hovered = rect.Contains(ctx.MousePosition);
            bool clicked = hovered && ctx.MouseClicked;

            if (hovered)
                ctx.DrawListForeground.AddRectFilled(rect, Style.TreeNodeHovered, Style.TreeNodeHovered);

            ctx.TextRenderer.AddText("◆", new Vector2(ctx.CursorPosition.x + 4, ctx.CursorPosition.y + 4), Style.TextDim, 8f);
            ctx.TextRenderer.AddText(label, new Vector2(ctx.CursorPosition.x + bulletSize, ctx.CursorPosition.y + 3), hovered ? Style.TextHighlight : Style.Text, Style.FontSize);

            AdvanceCursor(nodeSize);
            return clicked;
        }

        public static void TreePop()
        {
            var ctx = CryoContext.Current;
            ctx.StartX -= 18;
            ctx.CursorPosition = new Vector2(ctx.StartX, ctx.CursorPosition.y);
            ctx.PopId();
        }

        #endregion

        #region Dropdown

        public static bool Dropdown(string label, ref int selectedIndex, string[] options, float width = 160f)
        {
            var ctx = CryoContext.Current;
            int id = ctx.GetId(label);

            float height = Style.FontSize + 12;
            Rect rect = new Rect(ctx.CursorPosition, new Vector2(width, height));

            ctx.RegisterInteractiveRect(rect);

            bool hovered = rect.Contains(ctx.MousePosition);
            bool isOpen = _activeDropdownId == id;

            if (hovered && ctx.MouseClicked)
            {
                _activeDropdownId = isOpen ? 0 : id;
                isOpen = _activeDropdownId == id;
            }

            Color32 bgColor = hovered ? Style.DropdownHovered : Style.DropdownNormal;
            ctx.DrawListForeground.AddRectFilled(rect, bgColor, Style.WindowBorder, 1f);

            string currentText = (selectedIndex >= 0 && selectedIndex < options.Length) ? options[selectedIndex] : "选择...";
            ctx.TextRenderer.AddText(currentText, new Vector2(rect.x + 10, rect.y + (height - Style.FontSize) * 0.5f), Style.Text, Style.FontSize);
            ctx.TextRenderer.AddText("▼", new Vector2(rect.xMax - 20, rect.y + (height - 10) * 0.5f), Style.TextHighlight, 10f);

            bool changed = false;

            if (isOpen)
            {
                float optionHeight = Style.FontSize + 10;
                Rect dropdownRect = new Rect(rect.x, rect.yMax + 2, width, options.Length * optionHeight + 6);

                ctx.RegisterInteractiveRect(dropdownRect);

                // ★ 绘制到覆盖层
                ctx.DrawListOverlay.AddRectFilled(dropdownRect, Style.DropdownBackground, Style.WindowBorder, 1f);

                for (int i = 0; i < options.Length; i++)
                {
                    Rect optionRect = new Rect(rect.x + 3, rect.yMax + 5 + i * optionHeight, width - 6, optionHeight);
                    bool optionHovered = optionRect.Contains(ctx.MousePosition);

                    if (optionHovered)
                        ctx.DrawListOverlay.AddRectFilled(optionRect, Style.DropdownOptionHovered, Style.DropdownOptionHovered);

                    if (i == selectedIndex)
                        ctx.TextRendererOverlay.AddText("✓", new Vector2(optionRect.x + 4, optionRect.y + 4), Style.DropdownSelected, 12f);

                    Color32 textColor = (i == selectedIndex) ? Style.DropdownSelected : Style.Text;
                    ctx.TextRendererOverlay.AddText(options[i], new Vector2(optionRect.x + 22, optionRect.y + 5), textColor, Style.FontSize);

                    if (optionHovered && ctx.MouseClicked)
                    {
                        selectedIndex = i;
                        _activeDropdownId = 0;
                        changed = true;
                    }
                }
            }

            if (!string.IsNullOrEmpty(label))
                ctx.TextRenderer.AddText(label, new Vector2(rect.xMax + 10, rect.y + (height - Style.FontSize) * 0.5f), Style.Text, Style.FontSize);

            AdvanceCursor(new Vector2(width, height));
            return changed;
        }

        #endregion

        #region CollapsingHeader

        public static bool CollapsingHeader(string label, ref bool isOpen)
        {
            var ctx = CryoContext.Current;

            Vector2 textSize = ctx.TextRenderer.CalcTextSize(label, Style.FontSize);
            float windowWidth = ctx.CurrentWindow?.Rect.width - 20 ?? 200f;
            Vector2 headerSize = new Vector2(windowWidth, textSize.y + 12);
            Rect rect = new Rect(ctx.CursorPosition, headerSize);

            ctx.RegisterInteractiveRect(rect);

            bool hovered = rect.Contains(ctx.MousePosition);
            if (hovered && ctx.MouseClicked) isOpen = !isOpen;

            Color32 bgColor = hovered ? Style.HeaderHovered : Style.HeaderNormal;
            ctx.DrawListForeground.AddRectFilled(rect, bgColor, Style.WindowBorder, 1f);

            string arrow = isOpen ? "▼" : "►";
            ctx.TextRenderer.AddText(arrow, new Vector2(rect.x + 8, rect.y + 7), Style.TextHighlight, 10f);
            ctx.TextRenderer.AddText(label, new Vector2(rect.x + 26, rect.y + 6), Style.Text, Style.FontSize);

            AdvanceCursor(headerSize);
            return isOpen;
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

            // ★ 记录菜单栏高度到窗口状态
            if (state != null)
            {
                state.MenuBarHeight = _menuBarHeight + 6;  // 包含间距
            }

            float windowWidth = state?.Rect.width - 20 ?? 300f;
            Rect menuBarRect = new Rect(ctx.CursorPosition.x, ctx.CursorPosition.y, windowWidth, _menuBarHeight);

            ctx.DrawListForeground.AddRectFilled(menuBarRect, Style.MenuBarBackground, Style.WindowBorder);
            ctx.CursorPosition = new Vector2(ctx.CursorPosition.x + 6, ctx.CursorPosition.y);
        }

        public static void EndMenuBar()
        {
            var ctx = CryoContext.Current;
            var state = ctx.CurrentWindow;

            ctx.CursorPosition = new Vector2(ctx.StartX, _menuBarY + _menuBarHeight + 6);

            // ★ 菜单栏结束后，开始可滚动区域
            if (state != null)
            {
                BeginScrollArea();
            }
        }

        // ★ 新增：开始可滚动区域
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

        #region Tab Bar

        public static void BeginTabBar(string id)
        {
            var ctx = CryoContext.Current;
            ctx.PushId(ctx.GetId(id));
            ctx.SetState(ctx.GetId("tabbar_y"), ctx.CursorPosition.y);
        }

        public static void EndTabBar()
        {
            var ctx = CryoContext.Current;
            float tabY = ctx.GetState(ctx.GetId("tabbar_y"), ctx.CursorPosition.y);
            ctx.PopId();
            ctx.CursorPosition = new Vector2(ctx.StartX, tabY + Style.FontSize + 16);
        }

        public static bool TabItem(string label, ref int selectedIndex, int thisIndex)
        {
            var ctx = CryoContext.Current;

            Vector2 textSize = ctx.TextRenderer.CalcTextSize(label, Style.FontSize);
            Vector2 tabSize = new Vector2(textSize.x + 26, textSize.y + 12);
            Rect rect = new Rect(ctx.CursorPosition, tabSize);

            ctx.RegisterInteractiveRect(rect);

            bool isSelected = selectedIndex == thisIndex;
            bool hovered = rect.Contains(ctx.MousePosition);

            if (hovered && ctx.MouseClicked)
            {
                selectedIndex = thisIndex;
                isSelected = true;
            }

            Color32 bgColor = isSelected ? Style.TabActive : (hovered ? Style.TabHovered : Style.TabNormal);
            ctx.DrawListForeground.AddRectFilled(rect, bgColor, Style.WindowBorder);

            if (isSelected)
            {
                Rect underline = new Rect(rect.x, rect.yMax - 2, rect.width, 2);
                ctx.DrawListForeground.AddRect(underline, Style.TabUnderline);
            }

            ctx.TextRenderer.AddText(label, new Vector2(rect.x + 13, rect.y + 6), isSelected ? Style.Text : Style.TextDim, Style.FontSize);

            ctx.CursorPosition = new Vector2(rect.xMax + 3, ctx.CursorPosition.y);
            ctx.LastItemEndX = rect.xMax;
            ctx.LastItemY = rect.y;

            return isSelected;
        }

        #endregion

        #region Slider

        public static bool Slider(string label, ref float value, float min, float max, float width = 150f)
        {
            var ctx = CryoContext.Current;
            int id = ctx.GetId(label);

            float height = 20f;
            float trackHeight = 6f;
            float handleSize = 14f;

            Rect totalRect = new Rect(ctx.CursorPosition, new Vector2(width, height));
            Rect trackRect = new Rect(ctx.CursorPosition.x, ctx.CursorPosition.y + (height - trackHeight) * 0.5f, width, trackHeight);

            ctx.RegisterInteractiveRect(totalRect);

            bool hovered = totalRect.Contains(ctx.MousePosition);
            bool dragging = ctx.ActiveId == id;

            if (hovered && ctx.MouseClicked)
                ctx.ActiveId = id;

            if (ctx.ActiveId == id)
            {
                if (ctx.MouseDown)
                {
                    float t = Mathf.Clamp01((ctx.MousePosition.x - trackRect.x) / trackRect.width);
                    value = Mathf.Lerp(min, max, t);
                }
                else
                    ctx.ActiveId = 0;
            }

            float normalizedValue = Mathf.InverseLerp(min, max, value);
            float handleX = trackRect.x + normalizedValue * (trackRect.width - handleSize);

            // 绘制轨道
            ctx.DrawListForeground.AddRectFilled(trackRect, Style.SliderTrack, Style.WindowBorder);

            // 绘制填充
            Rect fillRect = new Rect(trackRect.x, trackRect.y, normalizedValue * trackRect.width, trackRect.height);
            ctx.DrawListForeground.AddRectFilled(fillRect, Style.SliderFill, Style.SliderFill);

            // 绘制手柄
            Rect handleRect = new Rect(handleX, ctx.CursorPosition.y + (height - handleSize) * 0.5f, handleSize, handleSize);
            Color32 handleColor = (hovered || dragging) ? Style.SliderHandleHovered : Style.SliderHandle;
            ctx.DrawListForeground.AddRectFilled(handleRect, handleColor, Style.WindowBorder);

            // 绘制值
            string valueText = value.ToString("F1");
            Vector2 valueSize = ctx.TextRenderer.CalcTextSize(valueText, Style.FontSize - 2);
            ctx.TextRenderer.AddText(valueText, new Vector2(totalRect.xMax + 8, ctx.CursorPosition.y + (height - valueSize.y) * 0.5f), Style.TextHighlight, Style.FontSize - 2);

            // 绘制标签
            if (!string.IsNullOrEmpty(label))
            {
                Vector2 labelSize = ctx.TextRenderer.CalcTextSize(label, Style.FontSize);
                ctx.TextRenderer.AddText(label, new Vector2(totalRect.xMax + valueSize.x + 16, ctx.CursorPosition.y + (height - labelSize.y) * 0.5f), Style.Text, Style.FontSize);
            }

            AdvanceCursor(new Vector2(width, height));
            return dragging;
        }

        public static bool SliderInt(string label, ref int value, int min, int max, float width = 150f)
        {
            float floatValue = value;
            bool changed = Slider(label, ref floatValue, min, max, width);
            value = Mathf.RoundToInt(floatValue);
            return changed;
        }

        #endregion

        // 替换 InputText region

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

        /// <summary>
        /// 带占位符的输入框
        /// </summary>
        public static bool InputText(string label, ref string text, string placeholder, float width = 200f)
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
                focused = false;
            }

            if (focused && ctx.EscapePressed)
            {
                ctx.FocusedInputId = 0;
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
            bool showPlaceholder = string.IsNullOrEmpty(displayText) && !focused;

            Vector2 textPos = new Vector2(rect.x + 8, rect.y + (height - Style.FontSize) * 0.5f);

            if (showPlaceholder)
            {
                ctx.TextRenderer.AddText(placeholder, textPos, Style.TextDim, Style.FontSize);
            }
            else
            {
                ctx.TextRenderer.AddText(displayText, textPos, Style.Text, Style.FontSize);
            }

            if (focused && (int)(Time.time * 2f) % 2 == 0)
            {
                Vector2 cursorTextSize = ctx.TextRenderer.CalcTextSize(displayText, Style.FontSize);
                Rect cursorRect = new Rect(textPos.x + cursorTextSize.x + 1, rect.y + 4, 2, height - 8);
                ctx.DrawListForeground.AddRect(cursorRect, Style.InputCursor);
            }

            if (!string.IsNullOrEmpty(label))
            {
                ctx.TextRenderer.AddText(label, new Vector2(rect.xMax + 10, rect.y + (height - Style.FontSize) * 0.5f), Style.Text, Style.FontSize);
            }

            AdvanceCursor(new Vector2(width, height));
            return changed;
        }

        /// <summary>
        /// 数字输入框
        /// </summary>
        public static bool InputInt(string label, ref int value, float width = 100f)
        {
            string text = value.ToString();
            bool changed = InputText(label, ref text, width);

            if (changed)
            {
                if (int.TryParse(text, out int newValue))
                {
                    value = newValue;
                }
            }

            return changed;
        }

        /// <summary>
        /// 浮点数输入框
        /// </summary>
        public static bool InputFloat(string label, ref float value, float width = 100f)
        {
            string text = value.ToString("F2");
            bool changed = InputText(label, ref text, width);

            if (changed)
            {
                if (float.TryParse(text, out float newValue))
                {
                    value = newValue;
                }
            }

            return changed;
        }

        #endregion

        #region Basic Controls

        public static bool Button(string label, Vector2? size = null)
        {
            var ctx = CryoContext.Current;
            int id = ctx.GetId(label);

            Vector2 textSize = ctx.TextRenderer.CalcTextSize(label, Style.FontSize);
            Vector2 padding = new Vector2(18, 10);
            Vector2 buttonSize = size ?? new Vector2(Mathf.Max(textSize.x + padding.x * 2, 70), Mathf.Max(textSize.y + padding.y * 2, 32));

            Rect rect = new Rect(ctx.CursorPosition, buttonSize);
            ctx.RegisterInteractiveRect(rect);

            bool hovered = rect.Contains(ctx.MousePosition);
            bool pressed = false;

            if (hovered)
            {
                ctx.HotId = id;
                if (ctx.MouseClicked) ctx.ActiveId = id;
            }

            if (ctx.ActiveId == id && ctx.MouseReleased)
            {
                pressed = hovered;
                ctx.ActiveId = 0;
            }

            Color32 bgColor = ctx.ActiveId == id ? Style.ButtonActive : (ctx.HotId == id ? Style.ButtonHovered : Style.ButtonNormal);
            ctx.DrawListForeground.AddRectFilled(rect, bgColor, Style.ButtonBorder, 1f);

            // 高光边缘
            if (hovered)
            {
                Rect highlight = new Rect(rect.x + 1, rect.y + 1, rect.width - 2, 1);
                ctx.DrawListForeground.AddRect(highlight, new Color32(150, 200, 255, 60));
            }

            Vector2 textPos = new Vector2(rect.x + (rect.width - textSize.x) * 0.5f, rect.y + (rect.height - textSize.y) * 0.5f);
            ctx.TextRenderer.AddText(label, textPos, Style.Text, Style.FontSize);

            AdvanceCursor(buttonSize);
            return pressed;
        }

        public static void Label(string text)
        {
            var ctx = CryoContext.Current;
            Vector2 textSize = ctx.TextRenderer.CalcTextSize(text, Style.FontSize);
            ctx.TextRenderer.AddText(text, ctx.CursorPosition, Style.Text, Style.FontSize);
            AdvanceCursor(textSize);
        }

        public static void Text(string text, Color32? color = null)
        {
            var ctx = CryoContext.Current;
            Color32 textColor = color ?? Style.Text;
            Vector2 textSize = ctx.TextRenderer.CalcTextSize(text, Style.FontSize);
            ctx.TextRenderer.AddText(text, ctx.CursorPosition, textColor, Style.FontSize);
            AdvanceCursor(textSize);
        }

        public static bool Checkbox(string label, ref bool value)
        {
            var ctx = CryoContext.Current;

            float boxSize = 18f;
            Vector2 textSize = ctx.TextRenderer.CalcTextSize(label, Style.FontSize);
            float spacing = 10f;

            Rect boxRect = new Rect(ctx.CursorPosition.x, ctx.CursorPosition.y + 1, boxSize, boxSize);
            Vector2 totalSize = new Vector2(boxSize + spacing + textSize.x, Mathf.Max(boxSize + 2, textSize.y + 4));
            Rect totalRect = new Rect(ctx.CursorPosition, totalSize);

            ctx.RegisterInteractiveRect(totalRect);

            bool hovered = totalRect.Contains(ctx.MousePosition);
            if (hovered && ctx.MouseClicked)
                value = !value;

            Color32 boxBg = value ? Style.CheckboxChecked : Style.CheckboxUnchecked;
            Color32 boxBorder = hovered ? Style.CheckboxHovered : Style.CheckboxBorder;
            ctx.DrawListForeground.AddRectFilled(boxRect, boxBg, boxBorder, 1f);

            if (value)
                ctx.TextRenderer.AddText("✓", new Vector2(boxRect.x + 3, boxRect.y + 1), Style.Text, 14f);

            Vector2 textPos = new Vector2(boxRect.xMax + spacing, ctx.CursorPosition.y + (totalSize.y - textSize.y) * 0.5f);
            ctx.TextRenderer.AddText(label, textPos, hovered ? Style.Text : Style.TextDim, Style.FontSize);

            AdvanceCursor(totalSize);
            return value;
        }

        public static bool Toggle(string label, ref bool value) => Checkbox(label, ref value);

        public static void Separator()
        {
            var ctx = CryoContext.Current;
            float width = ctx.CurrentWindow?.Rect.width - 20 ?? 200f;
            Rect rect = new Rect(ctx.CursorPosition.x, ctx.CursorPosition.y + 5, width, 1);
            ctx.DrawListForeground.AddRect(rect, Style.Separator);
            AdvanceCursor(new Vector2(width, 12));
        }

        public static void SameLine(float spacing = 6f)
        {
            var ctx = CryoContext.Current;
            ctx.CursorPosition = new Vector2(ctx.LastItemEndX + spacing, ctx.LastItemY);
        }

        public static void Spacing(float height = 12f)
        {
            var ctx = CryoContext.Current;
            ctx.CursorPosition = new Vector2(ctx.StartX, ctx.CursorPosition.y + height);
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