using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace MapGame.Core.Engine
{
    public class Camera
    {
        public Point3D Position { get; private set; }
        public Vector3D LookDirection { get; private set; }
        public Vector3D UpDirection { get; private set; }

        public double Speed { get; set; } = 1000.0;
        public double ZoomSpeed { get; set; } = 2.0;

        private double[] acceleration = { 0.0, 0.0, 0.0 };

        public Camera(Point3D startPosition, Vector3D startLook, Vector3D startUp)
        {
            Position = startPosition;
            LookDirection = startLook;
            UpDirection = startUp;
        }

        public void Zoom(double delta)
        {
            Vector3D zoomVector = LookDirection;
            zoomVector.Normalize();

            double moveDistance = delta * ZoomSpeed;
            
            Point3D currentPos = Position;

            double normalizationFactor = (currentPos.Y / 1000);

            acceleration[0] += zoomVector.X * moveDistance * normalizationFactor;
            acceleration[1] += zoomVector.Y * moveDistance * normalizationFactor;
            acceleration[2] += zoomVector.Z * moveDistance * normalizationFactor;
        }

        public void WASD(double deltaTime)
        {
            double moveDistance = Speed * deltaTime;
            Point3D currentPos = Position;
            double normalizationFactor = (currentPos.Y / 2000);

            if (Keyboard.IsKeyDown(Key.W) || Keyboard.IsKeyDown(Key.Up))
            {
                acceleration[2] -= moveDistance * normalizationFactor;
            }
            if (Keyboard.IsKeyDown(Key.S) || Keyboard.IsKeyDown(Key.Down))
            {
                acceleration[2] += moveDistance * normalizationFactor;
            }
            if (Keyboard.IsKeyDown(Key.A) || Keyboard.IsKeyDown(Key.Left))
            {
                acceleration[0] -= moveDistance * normalizationFactor;
            }
            if (Keyboard.IsKeyDown(Key.D) || Keyboard.IsKeyDown(Key.Right))
            {
                acceleration[0] += moveDistance * normalizationFactor;
            }
        }

        public void Update()
        {
            Point3D currentPos = Position;
            Accelerate(ref currentPos);
            NormalizePosition(ref currentPos);

            Position = currentPos;
        }

        private void Accelerate(ref Point3D currentPos)
        {
            currentPos.X += acceleration[0];
            currentPos.Y += acceleration[1];
            currentPos.Z += acceleration[2];

            for (int i = 0; i < acceleration.Count(); i++)
            {
                acceleration[i] *= 0.5;
            }
        }

        private void NormalizePosition(ref Point3D currentPos)
        {
            if (currentPos.Y < 50) currentPos.Y = 50;
            else if (currentPos.Y > 5818) currentPos.Y = 5818;

            double fovMargin = currentPos.Y * 0.33;

            double minZ = 0 + fovMargin;
            double maxZ = 3840 - fovMargin;

            if (currentPos.Z < minZ)
            {
                currentPos.Z = minZ;
                acceleration[2] = 0;
            }
            else if (currentPos.Z > maxZ)
            {
                currentPos.Z = maxZ;
                acceleration[2] = 0;
            }
        }
    }
}
