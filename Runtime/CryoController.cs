using UnityEngine;

namespace Cryo
{
    [DefaultExecutionOrder(-31000)]
    public class CryoController : MonoBehaviour
    {
        private Vector2 _lastMousePosition;
        private static CryoInputManager _inputManager;

        // ★ 按键重复支持
        private readonly float[] _keyRepeatTimers = new float[6];
        private const float KeyRepeatDelay = 0.4f;
        private const float KeyRepeatRate = 0.035f;
        private float _lastClickTime;

        public static bool IsMouseOverUI => CryoContext.Current?.WantCaptureMouse ?? false;

        protected virtual void Awake()
        {
            if (_inputManager == null && FindFirstObjectByType<CryoInputManager>() == null)
            {
                var go = new GameObject("[CryoUI Input Manager]");
                _inputManager = go.AddComponent<CryoInputManager>();
            }
        }

        private bool HandleKeyRepeat(int idx, KeyCode key)
        {
            if (Input.GetKeyDown(key))
            {
                _keyRepeatTimers[idx] = Time.time + KeyRepeatDelay;
                return true;
            }
            if (Input.GetKey(key) && Time.time > _keyRepeatTimers[idx])
            {
                _keyRepeatTimers[idx] = Time.time + KeyRepeatRate;
                return true;
            }
            return false;
        }

        protected virtual void Update()
        {
            var ctx = CryoContext.Current;

            Vector2 currentMousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            ctx.MouseDelta = currentMousePos - _lastMousePosition;
            _lastMousePosition = currentMousePos;

            ctx.MousePosition = currentMousePos;
            ctx.MouseDown = Input.GetMouseButton(0);
            ctx.MouseClicked = Input.GetMouseButtonDown(0);
            ctx.MouseReleased = Input.GetMouseButtonUp(0);

            // ★ 双击检测
            ctx.DoubleClicked = false;
            if (ctx.MouseClicked)
            {
                if (Time.time - _lastClickTime < 0.3f)
                    ctx.DoubleClicked = true;
                _lastClickTime = Time.time;
            }

            // ★ 键盘输入
            ctx.InputText = Input.inputString;
            ctx.HasKeyboardInput = !string.IsNullOrEmpty(ctx.InputText);

            // ★ 带重复的按键
            ctx.BackspacePressed = HandleKeyRepeat(0, KeyCode.Backspace);
            ctx.DeletePressed = HandleKeyRepeat(1, KeyCode.Delete);
            ctx.LeftArrowPressed = HandleKeyRepeat(2, KeyCode.LeftArrow);
            ctx.RightArrowPressed = HandleKeyRepeat(3, KeyCode.RightArrow);
            ctx.HomePressed = Input.GetKeyDown(KeyCode.Home);
            ctx.EndPressed = Input.GetKeyDown(KeyCode.End);
            ctx.EnterPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
            ctx.EscapePressed = Input.GetKeyDown(KeyCode.Escape);

            // ★ 修饰键
            ctx.ShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            ctx.CtrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
                        || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);

            // ★ 快捷键
            ctx.SelectAllRequested = ctx.CtrlHeld && Input.GetKeyDown(KeyCode.A);
            ctx.CopyRequested = ctx.CtrlHeld && Input.GetKeyDown(KeyCode.C);
            ctx.PasteRequested = ctx.CtrlHeld && Input.GetKeyDown(KeyCode.V);
            ctx.CutRequested = ctx.CtrlHeld && Input.GetKeyDown(KeyCode.X);

            ctx.BeginFrame();
            OnCryoUI();
            ctx.EndFrame();
            ctx.HotId = 0;
        }

        protected virtual void OnCryoUI() { }
    }
}