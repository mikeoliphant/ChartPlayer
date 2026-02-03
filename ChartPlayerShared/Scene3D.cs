using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UILayout;

namespace ChartPlayer
{
    public class Scene3D : IDisposable
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
        QuadBatch quadBatch;

        VertexPositionColorTexture[] verts = new VertexPositionColorTexture[4];

        public Scene3D()
        {
            Camera = new Camera3D();
            Camera.Position = new Vector3(0, 0, 5);
            Camera.Forward = new Vector3(0, 0, -1);

            basicEffect = new BasicEffect(MonoGameLayout.Current.Host.GraphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.TextureEnabled = true;

            quadBatch = new QuadBatch(43688);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (basicEffect != null)
                        basicEffect.Dispose();

                    if (quadBatch != null)
                        quadBatch.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public virtual void Stop()
        {
        }

        public virtual void Draw()
        {
            basicEffect.Projection = Camera.GetProjectionMatrix();
            basicEffect.View = Camera.GetViewMatrix();
            basicEffect.World = Matrix.Identity;
            basicEffect.Texture = MonoGameLayout.Current.GraphicsContext.SingleWhitePixelImage.Texture;

            MonoGameLayout.Current.Host.GraphicsDevice.SamplerStates[0] = SamplerState.AnisotropicClamp;
            MonoGameLayout.Current.Host.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            MonoGameLayout.Current.Host.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            MonoGameLayout.Current.Host.GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            quadBatch.Begin();

            DrawQuads();

            quadBatch.Draw(basicEffect);
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

        float[] xPercents = new float[4];
        float[] yPercents = new float[4];
        float[] xTexCoords = new float[4];
        float[] yTexCoords = new float[4];
        float[] blah = new float[4];
        private bool disposedValue;

        public void DrawNinePatch(UIImage image, int xCornerSize, int yCornerSize, Vector3 bottomLeft, Vector3 topLeft,
            Vector3 topRight, Vector3 bottomRight, UIColor color)
        {
            xPercents[0] = 0;
            xPercents[1] = (float)xCornerSize / (float)image.Width;
            xPercents[2] = 1.0f - xPercents[1];
            xPercents[3] = 1.0f;

            yPercents[0] = 0;
            yPercents[1] = (float)xCornerSize / (float)image.Width;
            yPercents[2] = 1.0f - xPercents[1];
            yPercents[3] = 1.0f;

            blah[0] = 0;
            blah[1] = 0.05f;
            blah[2] = 1.0f - blah[1];
            blah[3] = 1.0f;

            for (int x = 0; x < 4; x++)
            {
                xTexCoords[x] = ((float)image.XOffset + (xPercents[x] * (float)image.Width)) / (float)image.ActualWidth;
            }

            for (int y = 0; y < 4; y++)
            {
                yTexCoords[y] = ((float)image.YOffset + (yPercents[y] * (float)image.Height)) / (float)image.ActualHeight;
            }

            // Assumes that image is a parallelogram 
            Vector3 over = topRight - topLeft;
            Vector3 down = bottomLeft - topLeft;

            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    // Bottom Left;
                    verts[0] = new VertexPositionColorTexture
                    {
                        Position = topLeft + (over * blah[x]) + (down * blah[y + 1]),
                        Color = color.NativeColor,
                        TextureCoordinate = new Vector2(xTexCoords[x], yTexCoords[y + 1])
                    };

                    // Top left
                    verts[1] = new VertexPositionColorTexture
                    {
                        Position = topLeft + (over * blah[x]) + (down * blah[y]),
                        Color = color.NativeColor,
                        TextureCoordinate = new Vector2(xTexCoords[x], yTexCoords[y])
                    };

                    // Top Right
                    verts[2] = new VertexPositionColorTexture
                    {
                        Position = topLeft + (over * blah[x + 1]) + (down * blah[y]),
                        Color = color.NativeColor,
                        TextureCoordinate = new Vector2(xTexCoords[x + 1], yTexCoords[y])
                    };

                    // Bottom Right
                    verts[3] = new VertexPositionColorTexture
                    {
                        Position = topLeft + (over * blah[x + 1]) + (down * blah[y + 1]),
                        Color = color.NativeColor,
                        TextureCoordinate = new Vector2(xTexCoords[x + 1], yTexCoords[y + 1])
                    };

                    DrawQuad(image, verts);
                }
            }
        }

        public void DrawQuad(UIImage image, VertexPositionColorTexture[] vertices)
        {
            quadBatch.AddQuad(vertices);
        }
    }
}
