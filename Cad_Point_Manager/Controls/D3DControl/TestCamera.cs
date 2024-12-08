using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class TestCamera
    {
        private Vector3 position;   // Camera position
        private Vector3 target;     // Camera target
        private Vector3 up;         // Up direction

        private float zoomFactor;   // Current zoom factor
        private float rotationAngleX; // Rotation around X-axis (for 3D view)
        private float rotationAngleY; // Rotation around Y-axis (for 3D view)

        private bool is3DView;      // Flag to toggle between 2D and 3D views

        public TestCamera()
        {
            ResetToDefaults();
        }

        public Matrix ViewMatrix => Matrix.LookAtLH(position, target, up);

        public Matrix ProjectionMatrix(float width, float height)
        {
            if (is3DView)
                return Matrix.PerspectiveFovLH(MathUtil.PiOverFour, width / height, 0.1f, 1000f);
            else
                return Matrix.OrthoLH(width, height, 0.1f, 1000f);
        }

        public void ResetToDefaults()
        {
            position = new Vector3(0, 0, 100);  // Default position for 2D overhead
            target = new Vector3(0, 0, 0);      // Look at origin
            up = Vector3.UnitY;                 // Y is up
            zoomFactor = 1.0f;
            rotationAngleX = 0.0f;
            rotationAngleY = 0.0f;
            is3DView = false;
        }

        public void Toggle3DView(bool enable)
        {
            is3DView = enable;

            if (is3DView)
            {
                position = new Vector3(0, 50, 50); // Position camera for a 3D view
                target = new Vector3(0, 0, 0);
                up = Vector3.UnitY;
            }
            else
            {
                ResetToDefaults();
            }
        }

        public void Pan(float deltaX, float deltaY)
        {
            Vector3 panDirection = is3DView ? new Vector3(-deltaX, deltaY, 0) : new Vector3(-deltaX, -deltaY, 0);
            position += panDirection * zoomFactor;
            target += panDirection * zoomFactor;
        }

        public void Zoom(float zoomDelta, Vector2 mousePosition)
        {
            // Update zoom factor
            zoomFactor *= (1.0f + zoomDelta);

            // Adjust position and target for zoom-to-mouse
            Vector3 mouseWorldPosition = ScreenToWorld(mousePosition);
            Vector3 zoomDirection = mouseWorldPosition - position;
            position += zoomDirection * zoomDelta;
        }

        public void Rotate(float deltaX, float deltaY, bool shiftHeld)
        {
            if (!is3DView || !shiftHeld) return;

            rotationAngleX += deltaY * 0.01f; // Sensitivity adjustment
            rotationAngleY += deltaX * 0.01f;

            // Apply rotations around the target
            Matrix rotationMatrix = Matrix.RotationYawPitchRoll(rotationAngleY, rotationAngleX, 0);
            Vector3 direction = Vector3.Normalize(position - target);
            direction = Vector3.TransformNormal(direction, rotationMatrix);

            position = target + direction * (position - target).Length();
        }

        public Vector3 ScreenToWorld(Vector2 screenPosition)
        {
            // Assume an orthographic projection for 2D and near plane for picking in 3D
            if (!is3DView)
            {
                return new Vector3(screenPosition.X, screenPosition.Y, 0) * zoomFactor + target;
            }
            else
            {
                // Placeholder: Implement screen-to-world conversion for 3D
                return Vector3.Zero;
            }
        }
    }
}
