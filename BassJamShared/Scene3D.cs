using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UILayout;

namespace BassJam
{
    public class Scene3D
    {
        public Camera3D Camera { get; set; }

        public bool FogEnabled
        {
            get { return basicEffect.FogEnabled; }
            set { basicEffect.FogEnabled = value; }
        }

        public float FogStart
        {
            get { return basicEffect.FogStart; }
            set { basicEffect.FogStart = value; }
        }

        public float FogEnd
        {
            get { return basicEffect.FogEnd; }
            set { basicEffect.FogEnd = value; }
        }

        public UIColor FogColor
        {
            get { return new UIColor(new System.Numerics.Vector3(basicEffect.FogColor.X, basicEffect.FogColor.Y, basicEffect.FogColor.Z)); }
            set { basicEffect.FogColor = value.NativeColor.ToVector3(); }
        }

        BasicEffect basicEffect;
        VertexBuffer quadVertexBuffer;
        IndexBuffer quadIndexBuffer;

        VertexPositionColorTexture[] verts = new VertexPositionColorTexture[4];

        public Scene3D()
        {
            Camera = new Camera3D();
            Camera.Position = new Vector3(0, 0, 5);
            Camera.Up = Vector3.Up;
            Camera.Forward = new Vector3(0, 0, -1);

            basicEffect = new BasicEffect(MonoGameLayout.Current.Host.GraphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.TextureEnabled = true;

            quadVertexBuffer = new VertexBuffer(MonoGameLayout.Current.Host.GraphicsDevice, typeof(VertexPositionColorTexture), 4, BufferUsage.WriteOnly);
            quadIndexBuffer = new IndexBuffer(MonoGameLayout.Current.Host.GraphicsDevice, IndexElementSize.SixteenBits, 6, BufferUsage.WriteOnly);

            ushort[] indices = new ushort[] { 0, 1, 2, 0, 2, 3 };
            quadIndexBuffer.SetData(indices);
        }

        public virtual void Draw()
        {
            basicEffect.Projection = Camera.GetProjectionMatrix();
            basicEffect.View = Camera.GetViewMatrix();
            basicEffect.World = Matrix.Identity;

            MonoGameLayout.Current.Host.GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
            MonoGameLayout.Current.Host.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            MonoGameLayout.Current.Host.GraphicsDevice.DepthStencilState = DepthStencilState.None;

            DrawQuads();
        }

        public virtual void DrawQuads()
        {
        }

        public void DrawQuad(UIImage image, Vector3 bottomLeft, UIColor blColor, Vector3 topLeft, UIColor tlColor,
            Vector3 topRight, UIColor trColor, Vector3 bottomRight, UIColor brColor)
        {
            verts[0] = new VertexPositionColorTexture
            {
                Position = bottomLeft,
                Color = blColor.NativeColor,
                TextureCoordinate = new Vector2((float)image.XOffset / (float)image.ActualWidth, (float)(image.YOffset + image.Height) / (float)image.ActualHeight)
            };
            verts[1] = new VertexPositionColorTexture
            {
                Position = topLeft,
                Color = tlColor.NativeColor,
                TextureCoordinate = new Vector2((float)image.XOffset / (float)image.ActualWidth, (float)image.YOffset / (float)image.ActualHeight)
            };
            verts[2] = new VertexPositionColorTexture
            {
                Position = topRight,
                Color = trColor.NativeColor,
                TextureCoordinate = new Vector2((float)(image.XOffset + image.Width) / (float)image.ActualWidth, (float)image.YOffset / (float)image.ActualHeight)
            };
            verts[3] = new VertexPositionColorTexture
            {
                Position = bottomRight,
                Color = brColor.NativeColor,
                TextureCoordinate = new Vector2((image.XOffset + image.Width) / (float)image.ActualWidth, (float)(image.YOffset + image.Height) / (float)image.ActualHeight)
            };

            DrawQuad(image, verts);
        }

        public void DrawQuad(UIImage image, Rectangle srcRectangle, Vector3 bottomLeft, UIColor blColor, Vector3 topLeft, UIColor tlColor,
            Vector3 topRight, UIColor trColor, Vector3 bottomRight, UIColor brColor)
        {
            verts[0] = new VertexPositionColorTexture
            {
                Position = bottomLeft,
                Color = blColor.NativeColor,
                TextureCoordinate = new Vector2((float)(image.XOffset + srcRectangle.Left) / (float)image.ActualWidth, (float)(image.YOffset + srcRectangle.Bottom) / (float)image.ActualHeight)
            };
            verts[1] = new VertexPositionColorTexture
            {
                Position = topLeft,
                Color = tlColor.NativeColor,
                TextureCoordinate = new Vector2((float)(image.XOffset + srcRectangle.Left) / (float)image.ActualWidth, (float)(image.YOffset + srcRectangle.Top) / (float)image.ActualHeight)
            };
            verts[2] = new VertexPositionColorTexture
            {
                Position = topRight,
                Color = trColor.NativeColor,
                TextureCoordinate = new Vector2((float)(image.XOffset + srcRectangle.Right) / (float)image.ActualWidth, (float)(image.YOffset + srcRectangle.Top) / (float)image.ActualHeight)
            };
            verts[3] = new VertexPositionColorTexture
            {
                Position = bottomRight,
                Color = brColor.NativeColor,
                TextureCoordinate = new Vector2((image.XOffset + srcRectangle.Right) / (float)image.ActualWidth, (float)(image.YOffset + srcRectangle.Bottom) / (float)image.ActualHeight)
            };

            DrawQuad(image, verts);
        }

        public void DrawQuad(UIImage image, VertexPositionColorTexture[] vertices)
        {
            quadVertexBuffer.SetData(vertices);

            MonoGameLayout.Current.Host.GraphicsDevice.SetVertexBuffer(quadVertexBuffer);
            MonoGameLayout.Current.Host.GraphicsDevice.Indices = quadIndexBuffer;

            basicEffect.Texture = image.Texture;

            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();

                MonoGameLayout.Current.Host.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 2);
            }

            MonoGameLayout.Current.Host.GraphicsDevice.SetVertexBuffer(null);
        }
    }
}
