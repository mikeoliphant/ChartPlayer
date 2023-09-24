using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace BassJam
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
        public Vector3 Up { get; set; }
        public Vector3 Forward { get; set; }

        public Camera3D()
        {
            ViewportWidth = BassJamGame.Instance.ScreenWidth;
            ViewportHeight = BassJamGame.Instance.ScreenHeight;

            OrthographicScale = 1;
        }

        public void SetLookAt(Vector3 lookAt)
        {
            Forward = Vector3.Normalize(lookAt - Position);
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

        public Matrix GetViewMatrix()
        {
            return Matrix.CreateLookAt(Position, Position + Forward, Up);
        }

        public float GetDistanceForWidth(float width)
        {
            return width / (2.0f * (float)Math.Tan(FieldOfView / 2.0f));
        }
    }
}
