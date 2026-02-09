using System.Collections.Generic;
using UnityEngine;

namespace Cryo
{
    public class CryoContext
    {
        public static CryoContext Current { get; private set; } = new CryoContext();

        private readonly Stack<int> _idStack = new Stack<int>();
        private int _currentItemIndex;
        private readonly Dictionary<int, object> _stateStorage = new Dictionary<int, object>();

        public int HotId { get; set; }
        public int ActiveId { get; set; }

        // 鼠标输入
        public Vector2 MousePosition { get; set; }
        public bool MouseDown { get; set; }
        public bool MouseClicked { get; set; }
        public bool MouseReleased { get; set; }
        public Vector2 MouseDelta { get; set; }
        public float ScrollDelta { get; set; }

        // ★ 键盘输入
        public string InputText { get; set; } = "";
        public bool HasKeyboardInput { get; set; }
        public bool BackspacePressed { get; set; }
        public bool EnterPressed { get; set; }
        public bool EscapePressed { get; set; }

        // 绘制列表 - 分层
        public CryoDrawList DrawListBackground { get; } = new CryoDrawList();
        public CryoDrawList DrawListForeground { get; } = new CryoDrawList();
        public CryoDrawList DrawListOverlay { get; } = new CryoDrawList();
        public CryoTextRenderer TextRenderer { get; } = new CryoTextRenderer();
        public CryoTextRenderer TextRendererOverlay { get; } = new CryoTextRenderer();

        // 布局
        public Vector2 CursorPosition { get; set; }
        public float CurrentLineHeight { get; set; }
        public float StartX { get; set; } = 10f;
        public float ItemSpacing { get; set; } = 5f;
        public float LastItemY { get; set; }
        public float LastItemEndX { get; set; }

        // 窗口系统
        private readonly Stack<WindowState> _windowStack = new Stack<WindowState>();
        private readonly Dictionary<int, WindowState> _windowStates = new Dictionary<int, WindowState>();
        private readonly List<int> _windowOrder = new List<int>();  // ★ 窗口绘制顺序
        public WindowState CurrentWindow => _windowStack.Count > 0 ? _windowStack.Peek() : null;

        // 输入遮挡
        private readonly List<Rect> _interactiveRects = new List<Rect>();
        public bool WantCaptureMouse { get; private set; }
        public bool WantCaptureKeyboard { get; set; }

        // 拖拽
        public int DraggingWindowId { get; set; }
        public Vector2 DragOffset { get; set; }

        // ★ 焦点输入框
        public int FocusedInputId { get; set; }

        public void BeginFrame()
        {
            _currentItemIndex = 0;
            _idStack.Clear();
            _idStack.Push(0);
            DrawListBackground.Clear();
            DrawListForeground.Clear();
            DrawListOverlay.Clear();
            TextRenderer.Clear();
            TextRendererOverlay.Clear();
            _interactiveRects.Clear();
            CursorPosition = new Vector2(StartX, 10);
            CurrentLineHeight = 0;
            LastItemY = 10;
            LastItemEndX = StartX;
            WantCaptureMouse = false;
        }

        public void EndFrame()
        {
            DrawListBackground.BuildMesh();
            DrawListForeground.BuildMesh();
            DrawListOverlay.BuildMesh();
            TextRenderer.BuildMesh();
            TextRendererOverlay.BuildMesh();

            WantCaptureMouse = false;
            foreach (var rect in _interactiveRects)
            {
                if (rect.Contains(MousePosition))
                {
                    WantCaptureMouse = true;
                    break;
                }
            }

            if (DraggingWindowId != 0) WantCaptureMouse = true;
            if (FocusedInputId != 0) WantCaptureKeyboard = true;
        }

        public void RegisterInteractiveRect(Rect rect) => _interactiveRects.Add(rect);

        public int GetId(string label)
        {
            int parentId = _idStack.Peek();
            int hash = HashCombine(parentId, label.GetHashCode());
            hash = HashCombine(hash, _currentItemIndex++);
            return hash;
        }

        public void PushId(int id) => _idStack.Push(id);
        public void PopId() => _idStack.Pop();

        public T GetState<T>(int id, T defaultValue = default) =>
            _stateStorage.TryGetValue(id, out var value) ? (T)value : defaultValue;

        public void SetState<T>(int id, T value) => _stateStorage[id] = value;

        public WindowState GetWindowState(int id)
        {
            if (!_windowStates.TryGetValue(id, out var state))
            {
                state = new WindowState { Id = id };
                _windowStates[id] = state;
            }
            return state;
        }

        // ★ 窗口 Z-order 管理
        public void BringWindowToFront(int id)
        {
            _windowOrder.Remove(id);
            _windowOrder.Add(id);
        }

        public int GetWindowZOrder(int id)
        {
            int index = _windowOrder.IndexOf(id);
            return index >= 0 ? index : 0;
        }

        public bool IsWindowTopMost(int id)
        {
            return _windowOrder.Count > 0 && _windowOrder[_windowOrder.Count - 1] == id;
        }

        public void PushWindow(WindowState window) => _windowStack.Push(window);
        public void PopWindow() => _windowStack.Pop();

        private static int HashCombine(int seed, int value)
        {
            unchecked { return seed ^ (value + (int)0x9e3779b9 + (seed << 6) + (seed >> 2)); }
        }
    }

    public class WindowState
    {
        public int Id;
        public string Title;
        public Rect Rect;
        public bool IsOpen = true;
        public Vector2 ContentStart;
        public float TitleBarHeight = 26f;

        // 滚动支持
        public Vector2 ScrollOffset;
        public float ContentHeight;        // ★ 不要每帧重置，保持上一帧的值
        public float MenuBarHeight;
        public bool ScrollAreaStarted;
        public float ContentStartY;        // ★ 记录内容开始的Y位置

        public float ScrollableTop => Rect.y + TitleBarHeight + MenuBarHeight;
        public float ScrollableHeight => Mathf.Max(Rect.height - TitleBarHeight - MenuBarHeight - 10, 1);
        public float MaxScrollY => Mathf.Max(0, ContentHeight - ScrollableHeight);
        public bool HasVerticalScroll => ContentHeight > ScrollableHeight;
    }
}