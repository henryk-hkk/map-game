using HelixToolkit.Wpf.SharpDX;
using System;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace MapGame.Core.Engine
{
    public class MapCameraController(HelixToolkit.Wpf.SharpDX.PerspectiveCamera camera)
    {
        private HelixToolkit.Wpf.SharpDX.PerspectiveCamera _camera = camera;

        public double Speed { get; set; } = 1000.0;
        public double ZoomSpeed { get; set; } = 1.5;

        private double[] acceleration = [0.0, 0.0, 0.0];

        private const double FovMarginFactor = 0.43;

        private const double LookDirectionChangeYTreshhold = 150;

        private const double DefaultZLookDirection = -0.01;
        private const double MaxZLookDirection = -1.0;

        public double MinY { get; set; } = 50;
        public double MaxY { get; set; } = 1920 / FovMarginFactor;

        public void Zoom(double delta)
        {
            Vector3D zoomVector = new(0, -1, -0.5);
            zoomVector.Normalize();

            double moveDistance = delta * ZoomSpeed;

            Point3D currentPos = _camera.Position;
            double normalizationFactor = (currentPos.Y / 1000);

            acceleration[0] += zoomVector.X * moveDistance * normalizationFactor;
            acceleration[1] += zoomVector.Y * moveDistance * normalizationFactor;
            acceleration[2] += zoomVector.Z * moveDistance * normalizationFactor;
        }

        public void Update(double deltaTime)
        {
            WASD(deltaTime);

            Point3D currentPos = _camera.Position;
            Vector3D currentLookDir = _camera.LookDirection;

            Accelerate(ref currentPos);
            NormalizePosition(ref currentPos);
            AdjustLookPosition(ref currentPos, ref currentLookDir);

            _camera.Position = currentPos;
            _camera.LookDirection = currentLookDir;
        }

        private void WASD(double deltaTime)
        {
            double moveDistance = Speed * deltaTime;
            Point3D currentPos = _camera.Position;
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

        private void Accelerate(ref Point3D currentPos)
        {
            currentPos.X += acceleration[0];
            currentPos.Y += acceleration[1];
            currentPos.Z += acceleration[2];

            for (int i = 0; i < acceleration.Length; i++)
            {
                acceleration[i] *= 0.5; // Smoothening
            }
        }

        private void NormalizePosition(ref Point3D currentPos)
        {
            if (currentPos.Y < MinY)
            {
                currentPos.Y = MinY;
                acceleration[1] = 0;
            }
            else if (currentPos.Y > MaxY)
            {
                currentPos.Y = MaxY;
                acceleration[1] = 0;
            }

            double fovMargin = currentPos.Y * FovMarginFactor;

            double minZ = MapContext.MinY + fovMargin;
            double maxZ = MapContext.MaxY - fovMargin;

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

        private void AdjustLookPosition(ref Point3D currentPos, ref Vector3D currentLookDir)
        {
            double targetZ = DefaultZLookDirection;

            if (currentPos.Y <= MinY)
            {
                targetZ = MaxZLookDirection;
            }
            else if (currentPos.Y < LookDirectionChangeYTreshhold)
            {
                double t = (currentPos.Y - MinY) / (LookDirectionChangeYTreshhold - MinY);
                targetZ = MaxZLookDirection + (DefaultZLookDirection - MaxZLookDirection) * t;
            }

            currentLookDir = new Vector3D(0, -1, targetZ);
        }
    }
}