using SNEngine.Debugging;
using SNEngine.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SNEngine.InputSystem
{
    public class InputSystem : MonoBehaviour, IInputSystem
    {
#if UNITY_STANDALONE || UNITY_WEBGL
        private event UnityAction<KeyCode> OnKeyUp;

        private event UnityAction<KeyCode> OnKeyDown;

        private event UnityAction<KeyCode> OnKey;

        private Array _keyCodes;
#endif

#if UNITY_ANDROID || UNITY_IOS
        private event UnityAction<Touch> OnTouchBegan;

        private event UnityAction<Touch> OnTouchCanceled;

        private event UnityAction<Touch> OnTouchEnded;

        private event UnityAction<Touch> OnTouchMoved;

        private event UnityAction<Touch> OnTouchStationary;
#endif

#if UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
        private event UnityAction<KeyCode> OnButtonDown;

        private event UnityAction<KeyCode> OnButtonUp;

        private event UnityAction<KeyCode> OnButton;

        private event UnityAction<string, float> OnAxisChanged;

        private Dictionary<string, float> _axisValues = new Dictionary<string, float>();
        private List<KeyCode> _gamepadButtonCodes;
#endif

        private void Awake()
        {
#if UNITY_STANDALONE || UNITY_WEBGL
            _keyCodes = Enum.GetValues(typeof(KeyCode));

            Log("Enabled Standalone Input");
#endif

#if UNITY_ANDROID || UNITY_IOS
            Log("Enabled Mobile Input");
#endif

#if UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
            _gamepadButtonCodes = new List<KeyCode>();
            for (int i = 0; i <= 19; i++)
            {
                if (Enum.IsDefined(typeof(KeyCode), "JoystickButton" + i))
                {
                    _gamepadButtonCodes.Add((KeyCode)Enum.Parse(typeof(KeyCode), "JoystickButton" + i));
                }
            }

            Log("Enabled Gamepad Input");
#endif
        }


        private void Update()
        {
            #region Standalone Input

#if UNITY_STANDALONE || UNITY_WEBGL

            ListeringKeyCodes();
#endif

            #endregion

            #region Mobile Input

#if UNITY_ANDROID || UNITY_IOS
            ListeringTouch();

#endif

            #endregion

            #region Gamepad Input

#if UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
            ListeringButtons();
            ListeringAxis();
#endif
            #endregion


        }

#if UNITY_STANDALONE || UNITY_WEBGL
        private void ListeringKeyCodes()
        {
            if (Input.anyKeyDown && OnKeyDown.IsHaveSubcribe())
            {
                foreach (KeyCode keyCode in _keyCodes)
                {
                    if (Input.GetKeyDown(keyCode))
                    {
                        OnKeyDown?.Invoke(keyCode);
                    }
                }
            }

            if (Input.anyKey && OnKeyUp.IsHaveSubcribe() || OnKey.IsHaveSubcribe())
            {
                foreach (KeyCode keyCode in _keyCodes)
                {
                    if (Input.GetKey(keyCode))
                    {
                        OnKey?.Invoke(keyCode);
                    }

                    if (Input.GetKeyUp(keyCode))
                    {
                        OnKeyUp?.Invoke(keyCode);
                    }
                }
            }
        }
#endif

#if UNITY_ANDROID || UNITY_IOS
        private void ListeringTouch()
        {
            if (Input.touchCount > 0)
            {
                var touches = Input.touches;
                foreach (var touch in touches)
                {
                    switch (touch.phase)
                    {
                        case TouchPhase.Began:
                            OnTouchBegan?.Invoke(touch);
                            break;
                        case TouchPhase.Moved:
                            OnTouchMoved?.Invoke(touch);
                            break;
                        case TouchPhase.Stationary:
                            OnTouchStationary?.Invoke(touch);
                            break;
                        case TouchPhase.Ended:
                            OnTouchEnded?.Invoke(touch);
                            break;
                        case TouchPhase.Canceled:
                            OnTouchCanceled?.Invoke(touch);
                            break;
                    }
                }
            }
        }

#endif

#if UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
        private void ListeringButtons()
        {
            foreach (var keyCode in _gamepadButtonCodes)
            {
                if (Input.GetKeyDown(keyCode))
                {
                    OnButtonDown?.Invoke(keyCode);
                }

                if (Input.GetKey(keyCode))
                {
                    OnButton?.Invoke(keyCode);
                }

                if (Input.GetKeyUp(keyCode))
                {
                    OnButtonUp?.Invoke(keyCode);
                }
            }
        }

        private void ListeringAxis()
        {
            if (OnAxisChanged == null) return;

            var axesToUpdate = new List<string>(_axisValues.Keys);

            foreach (var axisName in axesToUpdate)
            {
                float currentValue = Input.GetAxis(axisName);
                if (Mathf.Abs(currentValue - _axisValues[axisName]) > float.Epsilon)
                {
                    _axisValues[axisName] = currentValue;
                    OnAxisChanged?.Invoke(axisName, currentValue);
                }
            }
        }
#endif

        public void AddListener(UnityAction<KeyCode> action, StandaloneInputEventType eventType)
        {
            AddListener(eventType, action);
        }

        public void RemoveListener(UnityAction<KeyCode> action, StandaloneInputEventType eventType)
        {
            RemoveListener(eventType, action);

        }
        private void AddListener(StandaloneInputEventType eventType, UnityAction<KeyCode> observer)
        {
#if UNITY_STANDALONE || UNITY_WEBGL
            try
            {
                switch (eventType)
                {
                    case StandaloneInputEventType.KeyDown:
                        OnKeyDown += observer;

                        Log(observer, eventType, nameof(AddListener));
                        break;
                    case StandaloneInputEventType.KeyUp:
                        OnKeyUp += observer;

                        Log(observer, eventType, nameof(AddListener));
                        break;
                    case StandaloneInputEventType.KeyPressing:
                        OnKey += observer;

                        Log(observer, eventType, nameof(AddListener));
                        break;
                    default:
                        break;
                }
            }
            catch
            {

            }
#endif

        }

        private void RemoveListener(StandaloneInputEventType eventType, UnityAction<KeyCode> observer)
        {
#if UNITY_STANDALONE || UNITY_WEBGL
            try
            {
                switch (eventType)
                {
                    case StandaloneInputEventType.KeyDown:
                        OnKeyDown -= observer;

                        Log(observer, eventType, nameof(RemoveListener));
                        break;
                    case StandaloneInputEventType.KeyUp:
                        OnKeyUp -= observer;

                        Log(observer, eventType, nameof(RemoveListener));
                        break;
                    case StandaloneInputEventType.KeyPressing:
                        OnKey -= observer;

                        Log(observer, eventType, nameof(RemoveListener));

                        break;
                    default:
                        break;
                }
            }
            catch
            {

            }
#endif
        }
        public void AddListener(UnityAction<Touch> action, MobileInputEventType eventType)
        {
#if UNITY_ANDROID || UNITY_IOS
            try
            {
                switch (eventType)
                {
                    case MobileInputEventType.TouchBegan:
                        OnTouchBegan += action;

                        Log(action, eventType, nameof(AddListener));
                        break;
                    case MobileInputEventType.TouchCanceled:
                        OnTouchCanceled += action;

                        Log(action, eventType, nameof(AddListener));
                        break;
                    case MobileInputEventType.TouchEnded:
                        OnTouchEnded += action;

                        Log(action, eventType, nameof(AddListener));
                        break;
                    case MobileInputEventType.TouchMoved:
                        OnTouchMoved += action;

                        Log(action, eventType, nameof(AddListener));
                        break;
                    case MobileInputEventType.TouchStationary:
                        OnTouchStationary += action;

                        Log(action, eventType, nameof(AddListener));
                        break;
                    default:
                        break;
                }
            }
            catch
            {

            }

#endif
        }

        public void RemoveListener(UnityAction<Touch> action, MobileInputEventType eventType)
        {
#if UNITY_ANDROID || UNITY_IOS
            try
            {
                switch (eventType)
                {
                    case MobileInputEventType.TouchBegan:
                        OnTouchBegan -= action;

                        Log(action, eventType, nameof(RemoveListener));
                        break;
                    case MobileInputEventType.TouchCanceled:
                        OnTouchCanceled -= action;

                        Log(action, eventType, nameof(RemoveListener));
                        break;
                    case MobileInputEventType.TouchEnded:
                        OnTouchEnded -= action;

                        Log(action, eventType, nameof(RemoveListener));
                        break;
                    case MobileInputEventType.TouchMoved:
                        OnTouchMoved -= action;

                        Log(action, eventType, nameof(RemoveListener));
                        break;
                    case MobileInputEventType.TouchStationary:
                        OnTouchStationary -= action;

                        Log(action, eventType, nameof(RemoveListener));
                        break;
                    default:
                        break;
                }
            }
            catch
            {

            }

#endif
        }

        public void AddListener(UnityAction<KeyCode> action, GamepadButtonEventType eventType)
        {
#if UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
            try
            {
                switch (eventType)
                {
                    case GamepadButtonEventType.ButtonDown:
                        OnButtonDown += action;
                        Log(action, eventType, nameof(AddListener));
                        break;
                    case GamepadButtonEventType.ButtonUp:
                        OnButtonUp += action;
                        Log(action, eventType, nameof(AddListener));
                        break;
                    case GamepadButtonEventType.ButtonPressing:
                        OnButton += action;
                        Log(action, eventType, nameof(AddListener));
                        break;
                    default:
                        break;
                }
            }
            catch
            {

            }
#endif
        }

        public void RemoveListener(UnityAction<KeyCode> action, GamepadButtonEventType eventType)
        {
#if UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
            try
            {
                switch (eventType)
                {
                    case GamepadButtonEventType.ButtonDown:
                        OnButtonDown -= action;
                        Log(action, eventType, nameof(RemoveListener));
                        break;
                    case GamepadButtonEventType.ButtonUp:
                        OnButtonUp -= action;
                        Log(action, eventType, nameof(RemoveListener));
                        break;
                    case GamepadButtonEventType.ButtonPressing:
                        OnButton -= action;
                        Log(action, eventType, nameof(RemoveListener));
                        break;
                    default:
                        break;
                }
            }
            catch
            {

            }
#endif
        }

        public void AddAxisListener(UnityAction<string, float> action, string axisName)
        {
#if UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
            OnAxisChanged += action;
            if (!_axisValues.ContainsKey(axisName))
            {
                _axisValues.Add(axisName, 0f);
            }
            Log(action.Target.GetType().Name, axisName, nameof(AddAxisListener));
#endif
        }

        public void RemoveAxisListener(UnityAction<string, float> action)
        {
#if UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
            OnAxisChanged -= action;
            Log(action.Target.GetType().Name, "Axis Listener", nameof(RemoveAxisListener));
#endif
        }

#if UNITY_STANDALONE || UNITY_WEBGL
        private void Log(UnityAction<KeyCode> action, StandaloneInputEventType eventType, string message)
        {
            Log($"{message}:Target Event: <b>On{eventType}</b> Observer: <b>{action.Target.GetType().Name}</b>");
        }
#endif

#if UNITY_ANDROID || UNITY_IOS
        private void Log(UnityAction<Touch> action, MobileInputEventType eventType, string message)
        {
            Log($"{message}:Target Event: <b>On{eventType}</b> Observer: <b>{action.Target.GetType().Name}</b>");
        }
#endif

#if UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
        private void Log(UnityAction<KeyCode> action, GamepadButtonEventType eventType, string message)
        {
            Log($"{message}:Target Event: <b>On{eventType}</b> Observer: <b>{action.Target.GetType().Name}</b>");
        }

        private void Log(string observerName, string eventType, string message)
        {
            Log($"{message}:Target Event: <b>{eventType}</b> Observer: <b>{observerName}</b>");
        }
#endif

        private void Log(string message)
        {
            NovelGameDebug.Log($"<color=#baa229>{nameof(InputSystem)}:</color> <b>{message}</b>.");
        }
    }
}