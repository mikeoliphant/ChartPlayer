﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace ChartPlayer
{
    public class Camera3D
    {
        public int ViewportWidth { get; set; }
        public int ViewportHeight { get; set; }

        public bool IsOrthographic { get; set; }
        public float OrthographicScale { get; set; }

        public float FieldOfView { get; set; } = (float)Math.PI / 4.0f;
        public float NearPlane { get; set; } = 1;
        public float FarPlane { get; set; } = 10000;

        public Vector3 Position { get; set; }
        public Vector3 Up { get; set; } = Vector3.Up;
        public Vector3 Right { get; set; }
        public Vector3 Forward { get; set; }

        public bool MirrorLeftRight { get; set; } = false;

        public Camera3D()
        {
            OrthographicScale = 1;
        }

        public void SetLookAt(Vector3 lookAt)
        {
            Forward = Vector3.Normalize(lookAt - Position);
            Right = Vector3.Normalize(Vector3.Cross(Forward, Vector3.Up));
            Up = Vector3.Normalize(Vector3.Cross(Right, Forward));
        }

        public virtual Matrix GetProjectionMatrix()
        {
            if (IsOrthographic)
            {
                return Matrix.CreateOrthographic((float)ViewportWidth / OrthographicScale, (float)ViewportHeight / OrthographicScale, NearPlane, FarPlane);
            }
            else
            {
                return Matrix.CreatePerspectiveFieldOfView(FieldOfView,
                    (float)ViewportWidth /
                    (float)ViewportHeight,
                    NearPlane, FarPlane);
            }
        }

        public virtual Matrix GetViewMatrix()
        {
            if (MirrorLeftRight)
                return Matrix.CreateLookAt(Position, Position + Forward, Up) * Matrix.CreateScale(-1, 1, 1);
            else
                return Matrix.CreateLookAt(Position, Position + Forward, Up);

        }

        public float GetDistanceForWidth(float width)
        {
            return width / (2.0f * (float)Math.Tan(FieldOfView / 2.0f));
        }
    }
}
