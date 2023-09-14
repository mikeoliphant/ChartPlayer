using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelEngine;

namespace BassJam
{
    public class Scene3D
    {
        public Camera3D Camera { get; set; }

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

            basicEffect = new BasicEffect(PixGame.Instance.GameHost.GraphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.TextureEnabled = true;

            quadVertexBuffer = new VertexBuffer(PixGame.Instance.GameHost.GraphicsDevice, typeof(VertexPositionColorTexture), 4, BufferUsage.WriteOnly);
            quadIndexBuffer = new IndexBuffer(PixGame.Instance.GameHost.GraphicsDevice, IndexElementSize.SixteenBits, 6, BufferUsage.WriteOnly);

            ushort[] indices = new ushort[] { 0, 1, 2, 0, 2, 3 };
            quadIndexBuffer.SetData(indices);
        }

        public virtual void Draw()
        {
            basicEffect.Projection = Camera.GetProjectionMatrix();
            basicEffect.View = Camera.GetViewMatrix();
            basicEffect.World = Matrix.Identity;

            //PixGame.Instance.GameHost.GraphicsDevice.RasterizerState = new RasterizerState { MultiSampleAntiAlias = true };
            PixGame.Instance.GameHost.GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
            PixGame.Instance.GameHost.GraphicsDevice.BlendState = BlendState.AlphaBlend;

            DrawQuads();
        }

        public virtual void DrawQuads()
        {
            DrawQuad(PixGame.Instance.GetImage("GuitarOrange"), new Vector3(-1, -1, 0), PixColor.White, new Vector3(-1, 1, 0), PixColor.Yellow, new Vector3(1, 1, 0), PixColor.Blue, new Vector3(1, -1, 0), PixColor.Red); 
        }

        public void DrawQuad(PixImage image, Vector3 bottomLeft, PixColor blColor, Vector3 topLeft, PixColor tlColor,
            Vector3 topRight, PixColor trColor, Vector3 bottomRight, PixColor brColor)
        {
            verts[0] = new VertexPositionColorTexture
            {
                Position = bottomLeft,
                Color = blColor.ToNativeColor(),
                TextureCoordinate = new Vector2((float)image.XOffset / (float)image.ActualWidth, (float)(image.YOffset + image.Height) / (float)image.ActualHeight)
            };
            verts[1] = new VertexPositionColorTexture
            {
                Position = topLeft,
                Color = tlColor.ToNativeColor(),
                TextureCoordinate = new Vector2((float)image.XOffset / (float)image.ActualWidth, (float)image.YOffset / (float)image.ActualHeight)
            };
            verts[2] = new VertexPositionColorTexture
            {
                Position = topRight,
                Color = trColor.ToNativeColor(),
                TextureCoordinate = new Vector2((float)(image.XOffset + image.Width) / (float)image.ActualWidth, (float)image.YOffset / (float)image.ActualHeight)
            };
            verts[3] = new VertexPositionColorTexture
            {
                Position = bottomRight,
                Color = brColor.ToNativeColor(),
                TextureCoordinate = new Vector2((image.XOffset + image.Width) / (float)image.ActualWidth, (float)(image.YOffset + image.Height) / (float)image.ActualHeight)
            };

            DrawQuad(image, verts);
        }

        public void DrawQuad(PixImage image, Rectangle srcRectangle, Vector3 bottomLeft, PixColor blColor, Vector3 topLeft, PixColor tlColor,
            Vector3 topRight, PixColor trColor, Vector3 bottomRight, PixColor brColor)
        {
            verts[0] = new VertexPositionColorTexture
            {
                Position = bottomLeft,
                Color = blColor.ToNativeColor(),
                TextureCoordinate = new Vector2((float)(image.XOffset + srcRectangle.Left) / (float)image.ActualWidth, (float)(image.YOffset + srcRectangle.Bottom) / (float)image.ActualHeight)
            };
            verts[1] = new VertexPositionColorTexture
            {
                Position = topLeft,
                Color = tlColor.ToNativeColor(),
                TextureCoordinate = new Vector2((float)(image.XOffset + srcRectangle.Left) / (float)image.ActualWidth, (float)(image.YOffset + srcRectangle.Top) / (float)image.ActualHeight)
            };
            verts[2] = new VertexPositionColorTexture
            {
                Position = topRight,
                Color = trColor.ToNativeColor(),
                TextureCoordinate = new Vector2((float)(image.XOffset + srcRectangle.Right) / (float)image.ActualWidth, (float)(image.YOffset + srcRectangle.Top) / (float)image.ActualHeight)
            };
            verts[3] = new VertexPositionColorTexture
            {
                Position = bottomRight,
                Color = brColor.ToNativeColor(),
                TextureCoordinate = new Vector2((image.XOffset + srcRectangle.Right) / (float)image.ActualWidth, (float)(image.YOffset + srcRectangle.Bottom) / (float)image.ActualHeight)
            };

            DrawQuad(image, verts);
        }

        public void DrawQuad(PixImage image, VertexPositionColorTexture[] vertices)
        {
            quadVertexBuffer.SetData(vertices);

            PixGame.Instance.GameHost.GraphicsDevice.SetVertexBuffer(quadVertexBuffer);
            PixGame.Instance.GameHost.GraphicsDevice.Indices = quadIndexBuffer;

            basicEffect.Texture = image.Texture;

            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();

                PixGame.Instance.GameHost.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 2);
            }

            PixGame.Instance.GameHost.GraphicsDevice.SetVertexBuffer(null);
        }
    }
}
