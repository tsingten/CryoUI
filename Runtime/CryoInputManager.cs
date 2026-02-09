using UnityEngine;

namespace Cryo
{
    /// <summary>
    /// CryoUI 输入管理器 - 自动拦截被 UI 占用的输入
    /// </summary>
    [DefaultExecutionOrder(-32000)]
    public class CryoInputManager : MonoBehaviour
    {
        private static CryoInputManager _instance;

        private static bool[] _mouseButtonDown = new bool[3];
        private static bool[] _mouseButton = new bool[3];
        private static bool[] _mouseButtonUp = new bool[3];
        private static bool _blockInput;

        public static bool IsMouseBlocked => _blockInput;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            // 记录原始输入
            for (int i = 0; i < 3; i++)
            {
                _mouseButtonDown[i] = Input.GetMouseButtonDown(i);
                _mouseButton[i] = Input.GetMouseButton(i);
                _mouseButtonUp[i] = Input.GetMouseButtonUp(i);
            }

            _blockInput = CryoContext.Current?.WantCaptureMouse ?? false;
        }

        // 安全的输入方法
        public static bool GetMouseButtonDown(int button)
        {
            if (_blockInput) return false;
            return button >= 0 && button < 3 ? _mouseButtonDown[button] : Input.GetMouseButtonDown(button);
        }

        public static bool GetMouseButton(int button)
        {
            if (_blockInput) return false;
            return button >= 0 && button < 3 ? _mouseButton[button] : Input.GetMouseButton(button);
        }

        public static bool GetMouseButtonUp(int button)
        {
            if (_blockInput) return false;
            return button >= 0 && button < 3 ? _mouseButtonUp[button] : Input.GetMouseButtonUp(button);
        }

        public static Vector3 mousePosition => Input.mousePosition;
    }
}