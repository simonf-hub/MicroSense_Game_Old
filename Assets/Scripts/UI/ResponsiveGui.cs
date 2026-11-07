using UnityEngine;

namespace MicroSense
{
    internal static class ResponsiveGui
    {
        private const float ReferenceWidth = 1280f;
        private const float ReferenceHeight = 720f;

        public static Matrix4x4 ApplyScale(out Vector2 canvasSize)
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            float scale = Mathf.Max(
                0.01f,
                Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight));

            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f)) * previousMatrix;
            canvasSize = new Vector2(Screen.width / scale, Screen.height / scale);
            return previousMatrix;
        }
    }
}
