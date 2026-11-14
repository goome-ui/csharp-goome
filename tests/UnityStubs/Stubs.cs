#pragma warning disable CS0067, CS1591
using System;

namespace UnityEngine
{
    public class Object
    {
        public string name = "";
        public static void Destroy(Object o) { }
    }

    public class Component : Object
    {
        public Transform transform = null!;
        public GameObject gameObject = null!;
        public T GetComponent<T>() => default!;
        public T GetComponentInParent<T>() => default!;
    }

    public class Behaviour : Component
    {
        public bool enabled;
    }

    public class MonoBehaviour : Behaviour { }
    public class GameObject : Object { }

    public class Transform : Component
    {
        public void Rotate(float x, float y, float z) { }
    }

    public class RectTransform : Transform
    {
        public Rect rect;
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) => (this.x, this.y) = (x, y);
        public static Vector2 zero => default;
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0);
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);
    }

    public struct Vector2Int
    {
        public int x, y;
        public Vector2Int(int x, int y) => (this.x, this.y) = (x, y);
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) => (this.x, this.y, this.z) = (x, y, z);
    }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) => (this.x, this.y, width, height) = (x, y, w, h);
        public float xMin => x;
        public float yMax => y + height;
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) => (this.r, this.g, this.b, a) = (r, g, b, 1);
    }

    public enum TextureFormat { RGBA32 = 4, BGRA32 = 14 }
    public enum TextureWrapMode { Repeat, Clamp }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum CursorMode { Auto, ForceSoftware }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }

    public class Texture : Object
    {
        public int width, height;
        public TextureWrapMode wrapMode;
        public FilterMode filterMode;
    }

    public class Texture2D : Texture
    {
        public Texture2D(int w, int h, TextureFormat f, bool mips) { }
        public void LoadRawTextureData(IntPtr data, int size) { }
        public void SetPixelData<T>(T[] data, int mipLevel, int sourceDataStartIndex = 0) { }
        public void Apply(bool updateMipmaps) { }
    }

    public class Shader : Object
    {
        public static Shader Find(string name) => null!;
    }

    public class Material : Object
    {
        public Material(Shader s) { }
        public Texture mainTexture = null!;
        public Vector2 mainTextureScale, mainTextureOffset;
        public void SetFloat(string name, float v) { }
    }

    public class Renderer : Component
    {
        public Material material = null!;
    }

    public class Canvas : Behaviour
    {
        public float scaleFactor;
        public RenderMode renderMode;
        public Camera worldCamera = null!;
    }

    public class Camera : Behaviour
    {
        public static Camera main = null!;
        public Ray ScreenPointToRay(Vector3 p) => default;
    }

    public struct Ray { }

    public struct RaycastHit
    {
        public Transform transform;
        public Vector2 textureCoord;
    }

    public static class Physics
    {
        public static bool Raycast(Ray ray, out RaycastHit hit)
        {
            hit = default;
            return false;
        }
    }

    public static class RectTransformUtility
    {
        public static bool ScreenPointToLocalPointInRectangle(RectTransform rect, Vector2 screenPoint, Camera cam, out Vector2 localPoint)
        {
            localPoint = default;
            return false;
        }
    }

    public interface ICanvasRaycastFilter
    {
        bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera);
    }

    public static class Mathf
    {
        public static int RoundToInt(float f) => (int)Math.Round(f);
        public static float Max(float a, float b) => Math.Max(a, b);
    }

    public static class Debug
    {
        public static void LogError(object m) { }
        public static void LogWarning(object m) { }
        public static void LogException(Exception e) { }
    }

    public static class Time
    {
        public static float deltaTime, unscaledDeltaTime, unscaledTime, smoothDeltaTime;
        public static int frameCount;
    }

    public static class GUIUtility
    {
        public static string systemCopyBuffer = "";
    }

    public static class Cursor
    {
        public static bool visible;
        public static void SetCursor(Texture2D texture, Vector2 hotspot, CursorMode mode) { }
    }

    public static class Application
    {
        public static bool isEditor;
        public static void Quit() { }
        public static void Quit(int code) { }
    }

    public enum KeyCode
    {
        None, Backspace, Tab, Return, KeypadEnter, Escape, Delete, LeftArrow, RightArrow, UpArrow, DownArrow, Home, End, PageUp, PageDown, Insert,
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Alpha0, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9,
        LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt, LeftCommand, RightCommand,
    }

    public static class Input
    {
        public static Vector3 mousePosition;
        public static bool mousePresent;
        public static Vector2 mouseScrollDelta;
        public static string inputString = "";
        public static bool GetMouseButton(int b) => false;
        public static bool GetMouseButtonDown(int b) => false;
        public static bool GetKey(KeyCode k) => false;
    }

    public class Light : Behaviour
    {
        public Color color;
    }

    public class Event { }
    public enum EventType { MouseDown }
    public class Font : Object { }

    [AttributeUsage(AttributeTargets.Field)]
    public class TooltipAttribute : Attribute
    {
        public TooltipAttribute(string t) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class DisallowMultipleComponent : Attribute { }
}

namespace UnityEngine.UI
{
    public class Graphic : Behaviour
    {
        public RectTransform rectTransform = null!;
        public Material material = null!;
        public Color color;
    }

    public class RawImage : Graphic
    {
        public Texture texture = null!;
        public Rect uvRect;
    }

    public class Image : Graphic { }
}

namespace UnityEngine.InputSystem
{
    public class InputControl<T>
    {
        public T ReadValue() => default!;
    }

    public class ButtonControl : InputControl<float>
    {
        public bool isPressed, wasPressedThisFrame;
    }

    public class KeyControl : ButtonControl { }
    public class Vector2Control : InputControl<Vector2> { }

    public class Mouse
    {
        public static Mouse current = null!;
        public Vector2Control position = null!, scroll = null!;
        public ButtonControl leftButton = null!, rightButton = null!, middleButton = null!;
    }

    public enum Key
    {
        None, Backspace, Tab, Enter, NumpadEnter, Escape, Delete, LeftArrow, RightArrow, UpArrow, DownArrow, Home, End, PageUp, PageDown, Insert,
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Digit0, Digit1, Digit2, Digit3, Digit4, Digit5, Digit6, Digit7, Digit8, Digit9,
    }

    public class Keyboard
    {
        public static Keyboard current = null!;
        public KeyControl shiftKey = null!, ctrlKey = null!, altKey = null!, leftMetaKey = null!, rightMetaKey = null!;
        public KeyControl this[Key k] => null!;
        public event Action<char> onTextInput;
    }
}
