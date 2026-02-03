using System;
using System.Collections.Generic;
using System.Text;
using UILayout;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace ChartPlayer
{
    public class QuadBatch : IDisposable
    {
        VertexBuffer quadVertexBuffer;
        IndexBuffer quadIndexBuffer;
        VertexPositionColorTexture[] vertices;

        int numQuads = 0;
        int maxQuads = 0;
        private bool disposedValue;

        public QuadBatch(int maxQuads)
        {
            this.maxQuads = maxQuads;

            quadVertexBuffer = new VertexBuffer(MonoGameLayout.Current.Host.GraphicsDevice, typeof(VertexPositionColorTexture), maxQuads * 4, BufferUsage.WriteOnly);
            quadIndexBuffer = new IndexBuffer(MonoGameLayout.Current.Host.GraphicsDevice, IndexElementSize.SixteenBits, maxQuads * 6, BufferUsage.WriteOnly);

            vertices = new VertexPositionColorTexture[maxQuads * 4];

            ushort[] quadIndices = { 0, 1, 2, 0, 2, 3 };

            ushort[] indices = new ushort[maxQuads * 6];

            int offset = 0;
            int quadOffset = 0;

            for (int quad = 0; quad < maxQuads; quad++)
            {
                foreach (int i in quadIndices)
                {
                    indices[offset++] = (ushort)(i + quadOffset);
                }

                quadOffset += 4;
            }

            quadIndexBuffer.SetData(indices);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (quadVertexBuffer != null)
                        quadVertexBuffer.Dispose();

                    if (quadIndexBuffer != null)
                        quadIndexBuffer.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Begin()
        {
            numQuads = 0;
        }

        public void AddQuad(VertexPositionColorTexture[] verts)
        {
            if (numQuads == maxQuads)
                throw new InvalidOperationException("Maximum number of quads reached");

            Array.Copy(verts, 0, vertices, (numQuads * 4), 4);

            numQuads++;
        }

        public void Draw(BasicEffect effect)
        {
            quadVertexBuffer.SetData(vertices, 0, numQuads * 4);

            MonoGameLayout.Current.Host.GraphicsDevice.SetVertexBuffer(quadVertexBuffer);
            MonoGameLayout.Current.Host.GraphicsDevice.Indices = quadIndexBuffer;

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                MonoGameLayout.Current.Host.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numQuads * 2);
            }

            MonoGameLayout.Current.Host.GraphicsDevice.SetVertexBuffer(null);
        }
    }
}
