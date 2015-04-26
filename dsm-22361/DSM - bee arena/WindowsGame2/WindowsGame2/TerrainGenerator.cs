using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Series3D2;

namespace WindowsGame2
{
    public struct VertexMultitextured : IVertexType
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector4 textureCoordinate;
        public Vector4 texWeights;

        public static int SizeInBytes = sizeof(float) * (4 + 4 + 4 + 4);

        public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration
        (
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(sizeof(float) * (3 + 3), VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(sizeof(float) * (3 + 3 + 4), VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1)
        );

        public VertexMultitextured(Vector3 position, Vector3 normal, Vector4 textureCoordinate, Vector4 texWeights)
        {
            this.position = position;
            this.normal = normal;
            this.textureCoordinate = textureCoordinate;
            this.texWeights = texWeights;
        }

        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }

        public Vector3 Normal
        {
            get { return normal; }
            set { normal = value; }
        }

        public Vector4 TextureCoordinate
        {
            get { return textureCoordinate; }
            set { textureCoordinate = value; }
        }

        public Vector4 TexWeights
        {
            get { return texWeights; }
            set { texWeights = value; }
        }

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }

    }

    public class TerrainGenerator
    {
        GraphicsDeviceManager graphics;
        GraphicsDevice device;

        int terrainWidth;
        int terrainLength;
        public float[,] heightData;

        VertexBuffer terrainVertexBuffer;
        IndexBuffer terrainIndexBuffer;
        VertexDeclaration terrainVertexDeclaration;

        VertexBuffer waterVertexBuffer;
        VertexDeclaration waterVertexDeclaration;

        VertexBuffer treeVertexBuffer;
        VertexDeclaration treeVertexDeclaration;

        Effect effect;
        Effect bbEffect;
        public Matrix viewMatrix;
        public Matrix projectionMatrix;

        public Vector3 cameraPosition = new Vector3(130, 30, -50);
        float leftrightRot = MathHelper.PiOver2;
        float updownRot = -MathHelper.Pi / 10.0f;
        const float rotationSpeed = 0.5f;
        const float moveSpeed = 25.0f;

        Texture2D grassTexture;
        Texture2D sandTexture;
        Texture2D rockTexture;
        Texture2D snowTexture;
        Texture2D cloudMap;
        Texture2D treeTexture;

        Model skyDome;

        RenderTarget2D cloudsRenderTarget;
        Texture2D cloudStaticMap;
        VertexPositionTexture[] fullScreenVertices;
        VertexDeclaration fullScreenVertexDeclaration;

        float waterHeight = 0.01f;
        public float maxHeight = 160;
        RenderTarget2D refractionRenderTarget;
        Texture2D refractionMap;
        RenderTarget2D reflectionRenderTarget;
        Texture2D reflectionMap;
        Matrix reflectionViewMatrix;
        Texture2D waterBumpMap;
        Vector3 windDirection = new Vector3(1, 0, 0);

        Game1 game;
        public void LoadContent(Game1 game)
        {
            this.game = game;
            device = game.GraphicsDevice;

            effect = game.Content.Load<Effect>("terrain/Series4Effects");
            bbEffect = game.Content.Load<Effect>("terrain/bbEffect");
            UpdateViewMatrix();
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 0.3f, 1000.0f);

            skyDome = game.Content.Load<Model>("terrain/dome");
            skyDome.Meshes[0].MeshParts[0].Effect = effect.Clone();

            LoadVertices();
            LoadTextures();

            PresentationParameters pp = device.PresentationParameters;
            refractionRenderTarget = new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, false, device.DisplayMode.Format, DepthFormat.Depth24Stencil8, 1, RenderTargetUsage.DiscardContents);
            reflectionRenderTarget = new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, false, device.DisplayMode.Format, DepthFormat.Depth24Stencil8, 1, RenderTargetUsage.DiscardContents);
            cloudsRenderTarget = new RenderTarget2D(device, pp.BackBufferWidth, pp.BackBufferHeight, true, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 1, RenderTargetUsage.DiscardContents);
        }

        private void LoadVertices()
        {

            Texture2D heightMap = game.Content.Load<Texture2D>("terrain/heightmap2"); 
            //Texture2D heightMap = game.Content.Load<Texture2D>("terrain/plansza2"); 
            LoadHeightData(heightMap);

            VertexMultitextured[] terrainVertices = SetUpTerrainVertices();
            int[] terrainIndices = SetUpTerrainIndices();
            terrainVertices = CalculateNormals(terrainVertices, terrainIndices);
            CopyToTerrainBuffers(terrainVertices, terrainIndices);
            terrainVertexDeclaration = VertexMultitextured.VertexDeclaration;
            SetUpWaterVertices();
            waterVertexDeclaration = VertexPositionTexture.VertexDeclaration;

            Texture2D treeMap = game.Content.Load<Texture2D>("terrain/treeMap");
            List<Vector3> treeList = GenerateTreePositions(treeMap, terrainVertices);
            CreateBillboardVerticesFromList(treeList);

            fullScreenVertices = SetUpFullscreenVertices();
            fullScreenVertexDeclaration = VertexPositionTexture.VertexDeclaration;
        }

        private void LoadTextures()
        {
            sandTexture = game.Content.Load<Texture2D>("terrain/sand");
            grassTexture = game.Content.Load<Texture2D>("terrain/grass");
            rockTexture = game.Content.Load<Texture2D>("terrain/rock");
            snowTexture = game.Content.Load<Texture2D>("terrain/snow");

            cloudMap = game.Content.Load<Texture2D>("terrain/cloudmap");
            waterBumpMap = game.Content.Load<Texture2D>("terrain/waterbump");

            treeTexture = game.Content.Load<Texture2D>("terrain/tree");

            cloudStaticMap = CreateStaticMap(32);
        }

        private void LoadHeightData(Texture2D heightMap)
        {
            float minimumHeight = float.MaxValue;
            float maximumHeight = float.MinValue;

            terrainWidth = heightMap.Width;
            terrainLength = heightMap.Height;

            Color[] heightMapColors = new Color[terrainWidth * terrainLength];
            heightMap.GetData(heightMapColors);

            heightData = new float[terrainWidth, terrainLength];
            for (int x = 0; x < terrainWidth; x++)
                for (int y = 0; y < terrainLength; y++)
                {
                    heightData[x, y] = heightMapColors[x + y * terrainWidth].R;
                    if (heightData[x, y] < minimumHeight) minimumHeight = heightData[x, y];
                    if (heightData[x, y] > maximumHeight) maximumHeight = heightData[x, y];
                }

            for (int x = 0; x < terrainWidth; x++)
                for (int y = 0; y < terrainLength; y++)
                    heightData[x, y] = (heightData[x, y] - minimumHeight) / (maximumHeight - minimumHeight) * 30.0f;
        }

        private Texture2D CreateStaticMap(int resolution)
        {
            Random rand = new Random();
            Color[] noisyColors = new Color[resolution * resolution];

            for (int x = 0; x < resolution; x++)
                for (int y = 0; y < resolution; y++)
                    noisyColors[x + y * resolution] = new Color(new Vector3((float)rand.Next(1000) / 1000.0f, 0, 0));

            Texture2D noiseImage = new Texture2D(device, resolution, resolution, true, SurfaceFormat.Color);
            noiseImage.SetData(noisyColors);

            return noiseImage;
        }

        public bool CheckCollision(BoundingSphere sphere)
        {
            //Vector3(x, heightData[x, y], -y);
            if (sphere.Center.X < 0 || sphere.Center.X >= heightData.GetLength(0))
                return true;
            if (sphere.Center.Z > 0 || sphere.Center.Z <= -heightData.GetLength(1))
                return true;
            if (sphere.Center.Y < waterHeight)
                return true;
            if (sphere.Center.Y > maxHeight)
                return true;
            int xIndex = (int)sphere.Center.X;
            int yIndex = (int)(-1 * sphere.Center.Z);

            if (xIndex < 0) xIndex = 0;
            if (xIndex >= heightData.GetLength(0)) xIndex = heightData.GetLength(0) - 1;

            if (yIndex < 0) yIndex = 0;
            if (xIndex >= heightData.GetLength(1)) yIndex = heightData.GetLength(1) - 1;


            int xIndex2 = xIndex + 1;
            int yIndex2 = yIndex + 1;
            if (xIndex2 >= heightData.GetLength(0))
                xIndex2 = heightData.GetLength(0) -1;
            if (yIndex2 >= heightData.GetLength(1))
                yIndex2 = heightData.GetLength(1) -1;
            float Q11 = heightData[xIndex, yIndex];
            float Q12 = heightData[xIndex, yIndex2];
            float Q21 = heightData[xIndex2, yIndex];
            float Q22 = heightData[xIndex2, yIndex2];
            float x = sphere.Center.X;
            float y = -sphere.Center.Z;
            float value = 0;
            if (xIndex2 == xIndex)
                if (yIndex2 == yIndex)
                    value = Q11;
                else
                {
                    value = Q11 + (x - xIndex) * (Q21 - Q11) / (xIndex2 - xIndex);
                }
            else
            {
                if (yIndex == yIndex2)
                {
                    value = Q11 + (y - yIndex) * (Q12 - Q11) / (yIndex2 - yIndex);
                }
                else value = 1.0f / ((xIndex2 - xIndex) * (yIndex2 - yIndex)) *
                    ((Q11 * (xIndex2 - x) * (yIndex2 - y)) +
                    (Q21 * (x - xIndex) * (yIndex2 - y)) +
                    (Q12 * (xIndex2 - x) * (y - yIndex)) +
                    (Q22 * (x - xIndex) * (y - yIndex)));
            }
            if (value >= sphere.Center.Y - sphere.Radius)
                return true;
            //sphere.Center.
            return false;
        }

        private VertexMultitextured[] SetUpTerrainVertices()
        {
            VertexMultitextured[] terrainVertices = new VertexMultitextured[terrainWidth * terrainLength];

            for (int x = 0; x < terrainWidth; x++)
            {
                for (int y = 0; y < terrainLength; y++)
                {
                    terrainVertices[x + y * terrainWidth].Position = new Vector3(x, heightData[x, y], -y);
                    terrainVertices[x + y * terrainWidth].textureCoordinate.X = (float)x / 30.0f;
                    terrainVertices[x + y * terrainWidth].textureCoordinate.Y = (float)y / 30.0f;

                    terrainVertices[x + y * terrainWidth].texWeights.X = MathHelper.Clamp(1.0f - Math.Abs(heightData[x, y] - 0) / 8.0f, 0, 1);
                    terrainVertices[x + y * terrainWidth].texWeights.Y = MathHelper.Clamp(1.0f - Math.Abs(heightData[x, y] - 12) / 6.0f, 0, 1);
                    terrainVertices[x + y * terrainWidth].texWeights.Z = MathHelper.Clamp(1.0f - Math.Abs(heightData[x, y] - 20) / 6.0f, 0, 1);
                    terrainVertices[x + y * terrainWidth].texWeights.W = MathHelper.Clamp(1.0f - Math.Abs(heightData[x, y] - 30) / 6.0f, 0, 1);

                    float total = terrainVertices[x + y * terrainWidth].TexWeights.X;
                    total += terrainVertices[x + y * terrainWidth].TexWeights.Y;
                    total += terrainVertices[x + y * terrainWidth].TexWeights.Z;
                    total += terrainVertices[x + y * terrainWidth].TexWeights.W;

                    terrainVertices[x + y * terrainWidth].texWeights.X /= total;
                    terrainVertices[x + y * terrainWidth].texWeights.Y /= total;
                    terrainVertices[x + y * terrainWidth].texWeights.Z /= total;
                    terrainVertices[x + y * terrainWidth].texWeights.W /= total;
                }
            }

            return terrainVertices;
        }

        private void SetUpWaterVertices()
        {
            VertexPositionTexture[] waterVertices = new VertexPositionTexture[6];
            /*
            waterVertices[0] = new VertexPositionTexture(new Vector3(0, waterHeight, 0), new Vector2(0, 1));
            waterVertices[2] = new VertexPositionTexture(new Vector3(terrainWidth, waterHeight, -terrainLength), new Vector2(1, 0));
            waterVertices[1] = new VertexPositionTexture(new Vector3(0, waterHeight, -terrainLength), new Vector2(0, 0));

            waterVertices[3] = new VertexPositionTexture(new Vector3(0, waterHeight, 0), new Vector2(0, 1));
            waterVertices[5] = new VertexPositionTexture(new Vector3(terrainWidth, waterHeight, 0), new Vector2(1, 1));
            waterVertices[4] = new VertexPositionTexture(new Vector3(terrainWidth, waterHeight, -terrainLength), new Vector2(1, 0));
            */
            
            waterVertices[0] = new VertexPositionTexture(new Vector3(-5 * terrainLength, waterHeight, 5 * terrainLength), new Vector2(0, 1));
            waterVertices[2] = new VertexPositionTexture(new Vector3(5 * terrainWidth, waterHeight, -5 * terrainLength), new Vector2(1, 0));
            waterVertices[1] = new VertexPositionTexture(new Vector3(-5 * terrainLength, waterHeight, -5 * terrainLength), new Vector2(0, 0));

            waterVertices[3] = new VertexPositionTexture(new Vector3(-5 * terrainLength, waterHeight, 5 * terrainLength), new Vector2(0, 1));
            waterVertices[5] = new VertexPositionTexture(new Vector3(5 * terrainWidth, waterHeight, 5 * terrainLength), new Vector2(1, 1));
            waterVertices[4] = new VertexPositionTexture(new Vector3(5 * terrainWidth, waterHeight, -5 * terrainLength), new Vector2(1, 0));
            
            /*
            waterVertices[0] = new VertexPositionTexture(new Vector3(-10 * terrainLength, waterHeight, 10 * terrainLength), new Vector2(0, 1));
            waterVertices[2] = new VertexPositionTexture(new Vector3(10 * terrainWidth, waterHeight, -10 * terrainLength), new Vector2(1, 0));
            waterVertices[1] = new VertexPositionTexture(new Vector3(-10 * terrainLength, waterHeight, -10 * terrainLength), new Vector2(0, 0));

            waterVertices[3] = new VertexPositionTexture(new Vector3(-10 * terrainLength, waterHeight, 10 * terrainLength), new Vector2(0, 1));
            waterVertices[5] = new VertexPositionTexture(new Vector3(10 * terrainWidth, waterHeight, 10 * terrainLength), new Vector2(1, 1));
            waterVertices[4] = new VertexPositionTexture(new Vector3(10 * terrainWidth, waterHeight, -10 * terrainLength), new Vector2(1, 0));
            */

            waterVertexBuffer = new VertexBuffer(device, typeof(VertexPositionTexture), waterVertices.Length, BufferUsage.WriteOnly);
            waterVertexBuffer.SetData(waterVertices);
        }

        private int[] SetUpTerrainIndices()
        {
            int[] indices = new int[(terrainWidth - 1) * (terrainLength - 1) * 6];
            int counter = 0;
            for (int y = 0; y < terrainLength - 1; y++)
            {
                for (int x = 0; x < terrainWidth - 1; x++)
                {
                    int lowerLeft = x + y * terrainWidth;
                    int lowerRight = (x + 1) + y * terrainWidth;
                    int topLeft = x + (y + 1) * terrainWidth;
                    int topRight = (x + 1) + (y + 1) * terrainWidth;

                    indices[counter++] = topLeft;
                    indices[counter++] = lowerRight;
                    indices[counter++] = lowerLeft;

                    indices[counter++] = topLeft;
                    indices[counter++] = topRight;
                    indices[counter++] = lowerRight;
                }
            }

            return indices;
        }

        private VertexPositionTexture[] SetUpFullscreenVertices()
        {
            VertexPositionTexture[] vertices = new VertexPositionTexture[4];

            vertices[0] = new VertexPositionTexture(new Vector3(-1, 1, 0f), new Vector2(0, 1));
            vertices[1] = new VertexPositionTexture(new Vector3(1, 1, 0f), new Vector2(1, 1));
            vertices[2] = new VertexPositionTexture(new Vector3(-1, -1, 0f), new Vector2(0, 0));
            vertices[3] = new VertexPositionTexture(new Vector3(1, -1, 0f), new Vector2(1, 0));

            return vertices;
        }

        private List<Vector3> GenerateTreePositions(Texture2D treeMap, VertexMultitextured[] terrainVertices)
        {
            Color[] treeMapColors = new Color[treeMap.Width * treeMap.Height];
            treeMap.GetData(treeMapColors);

            int[,] noiseData = new int[treeMap.Width, treeMap.Height];
            for (int x = 0; x < treeMap.Width; x++)
                for (int y = 0; y < treeMap.Height; y++)
                    noiseData[x, y] = treeMapColors[y + x * treeMap.Height].R;

            List<Vector3> treeList = new List<Vector3>();
            Random random = new Random();

            for (int x = 0; x < terrainWidth; x++)
            {
                for (int y = 0; y < terrainLength; y++)
                {
                    float terrainHeight = heightData[x, y];
                    if ((terrainHeight > 12) && (terrainHeight < 16))
                    {
                        float flatness = Vector3.Dot(terrainVertices[x + y * terrainWidth].Normal, new Vector3(0, 1, 0));
                        float minFlatness = (float)Math.Cos(MathHelper.ToRadians(15));
                        if (flatness > minFlatness)
                        {
                            float relx = (float)x / (float)terrainWidth;
                            float rely = (float)y / (float)terrainLength;

                            float noiseValueAtCurrentPosition = noiseData[(int)(relx * treeMap.Width), (int)(rely * treeMap.Height)];
                            float treeDensity;
                            if (noiseValueAtCurrentPosition > 200)
                                treeDensity = 10;
                            else if (noiseValueAtCurrentPosition > 150)
                                treeDensity = 4;
                            else if (noiseValueAtCurrentPosition > 100)
                                treeDensity = 3;
                            else
                                treeDensity = 0;

                            for (int currDetail = 0; currDetail < treeDensity; currDetail++)
                            {
                                float rand1 = (float)random.Next(1000) / 1000.0f;
                                float rand2 = (float)random.Next(1000) / 1000.0f;

                                Vector3 treePos = new Vector3((float)x - rand1, 0, -(float)y - rand2);
                                treePos.Y = heightData[x, y];
                                treeList.Add(treePos);
                            }
                        }
                    }
                }
            }

            return treeList;
        }

        private void CreateBillboardVerticesFromList(List<Vector3> treeList)
        {
            VertexPositionTexture[] billboardVertices = new VertexPositionTexture[treeList.Count * 6];
            int i = 0;
            foreach (Vector3 currentV3 in treeList)
            {
                billboardVertices[i++] = new VertexPositionTexture(currentV3, new Vector2(0, 0));
                billboardVertices[i++] = new VertexPositionTexture(currentV3, new Vector2(1, 0));
                billboardVertices[i++] = new VertexPositionTexture(currentV3, new Vector2(1, 1));

                billboardVertices[i++] = new VertexPositionTexture(currentV3, new Vector2(0, 0));
                billboardVertices[i++] = new VertexPositionTexture(currentV3, new Vector2(1, 1));
                billboardVertices[i++] = new VertexPositionTexture(currentV3, new Vector2(0, 1));
            }

            treeVertexBuffer = new VertexBuffer(device, typeof(VertexPositionTexture), billboardVertices.Length * (3 + 2), BufferUsage.WriteOnly);
            treeVertexBuffer.SetData(billboardVertices);
            treeVertexDeclaration = VertexPositionTexture.VertexDeclaration;
        }

        private VertexMultitextured[] CalculateNormals(VertexMultitextured[] vertices, int[] indices)
        {
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Normal = new Vector3(0, 0, 0);

            for (int i = 0; i < indices.Length / 3; i++)
            {
                int index1 = indices[i * 3];
                int index2 = indices[i * 3 + 1];
                int index3 = indices[i * 3 + 2];

                Vector3 side1 = vertices[index1].Position - vertices[index3].Position;
                Vector3 side2 = vertices[index1].Position - vertices[index2].Position;
                Vector3 normal = Vector3.Cross(side1, side2);

                vertices[index1].Normal += normal;
                vertices[index2].Normal += normal;
                vertices[index3].Normal += normal;
            }

            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Normal.Normalize();

            return vertices;
        }

        private void CopyToTerrainBuffers(VertexMultitextured[] vertices, int[] indices)
        {
            terrainVertexBuffer = new VertexBuffer(device, typeof(VertexMultitextured), vertices.Length * VertexMultitextured.SizeInBytes, BufferUsage.WriteOnly);
            terrainVertexBuffer.SetData(vertices);

            terrainIndexBuffer = new IndexBuffer(device, typeof(int), indices.Length, BufferUsage.WriteOnly);
            terrainIndexBuffer.SetData(indices);
        }

        protected void UnloadContent()
        {
        }



        private void AddToCameraPosition(Vector3 vectorToAdd)
        {
            Matrix cameraRotation = Matrix.CreateRotationX(updownRot) * Matrix.CreateRotationY(leftrightRot);
            Vector3 rotatedVector = Vector3.Transform(vectorToAdd, cameraRotation);
            cameraPosition += moveSpeed * rotatedVector;
            UpdateViewMatrix();
        }

        private void UpdateViewMatrix()
        {
            Matrix cameraRotation = Matrix.CreateRotationX(updownRot) * Matrix.CreateRotationY(leftrightRot);

            Vector3 cameraOriginalTarget = new Vector3(0, 0, -1);
            Vector3 cameraOriginalUpVector = new Vector3(0, 1, 0);
            Vector3 cameraRotatedTarget = Vector3.Transform(cameraOriginalTarget, cameraRotation);
            Vector3 cameraFinalTarget = cameraPosition + cameraRotatedTarget;
            Vector3 cameraRotatedUpVector = Vector3.Transform(cameraOriginalUpVector, cameraRotation);

            viewMatrix = Matrix.CreateLookAt(cameraPosition, cameraFinalTarget, cameraRotatedUpVector);

            Vector3 reflCameraPosition = cameraPosition;
            reflCameraPosition.Y = -cameraPosition.Y + waterHeight * 2;
            Vector3 reflTargetPos = cameraFinalTarget;
            reflTargetPos.Y = -cameraFinalTarget.Y + waterHeight * 2;

            Vector3 cameraRight = Vector3.Transform(new Vector3(1, 0, 0), cameraRotation);
            Vector3 invUpVector = Vector3.Cross(cameraRight, reflTargetPos - reflCameraPosition);

            reflectionViewMatrix = Matrix.CreateLookAt(reflCameraPosition, reflTargetPos, invUpVector);
        }
        public void Update()
        {
            //////////////////////
        }

        public void Draw(GameTime gameTime,  Vector3 campos, Vector3 xwingPosition, Quaternion cameraRotation)
        {
            float time = (float)gameTime.TotalGameTime.TotalMilliseconds / 100.0f;
            BlendState bs = device.BlendState;
            device.BlendState = BlendState.Opaque;
            DrawRefractionMap();
            DrawReflectionMap();
            GeneratePerlinNoise(time);

            //device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);
            device.RasterizerState = RasterizerState.CullNone;
            DrawSkyDome(viewMatrix);
            DrawTerrain(viewMatrix);
            //DrawWater(time);
            DrawWater(time, campos, xwingPosition, cameraRotation);
            DrawBillboards(viewMatrix);
            device.BlendState = bs;
        }

        public void DrawTerrain(Matrix currentViewMatrix)
        {
            effect.CurrentTechnique = effect.Techniques["Multitextured"];
            effect.Parameters["xTexture0"].SetValue(sandTexture);
            effect.Parameters["xTexture1"].SetValue(grassTexture);
            effect.Parameters["xTexture2"].SetValue(rockTexture);
            effect.Parameters["xTexture3"].SetValue(snowTexture);

            Matrix worldMatrix = Matrix.Identity;
            effect.Parameters["xWorld"].SetValue(worldMatrix);
            effect.Parameters["xView"].SetValue(currentViewMatrix);
            effect.Parameters["xProjection"].SetValue(projectionMatrix);

            effect.Parameters["xEnableLighting"].SetValue(true);
            effect.Parameters["xAmbient"].SetValue(0.4f);
            effect.Parameters["xLightDirection"].SetValue(new Vector3(-0.5f, -1, -0.5f));

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                device.SetVertexBuffer(terrainVertexBuffer);
                device.Indices = terrainIndexBuffer;

                int noVertices = terrainVertexBuffer.VertexCount;
                int noTriangles = terrainIndexBuffer.IndexCount / 3;
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, noVertices, 0, noTriangles);
            }
        }

        private void DrawSkyDome(Matrix currentViewMatrix)
        {
            Matrix[] modelTransforms = new Matrix[skyDome.Bones.Count];
            skyDome.CopyAbsoluteBoneTransformsTo(modelTransforms);

            Matrix wMatrix = Matrix.CreateTranslation(0, -0.3f, 0) * Matrix.CreateScale(200) * Matrix.CreateTranslation(cameraPosition);

            foreach (ModelMesh mesh in skyDome.Meshes)
            {
                foreach (Effect currentEffect in mesh.Effects)
                {
                    Matrix worldMatrix = modelTransforms[mesh.ParentBone.Index] * wMatrix;
                    currentEffect.CurrentTechnique = currentEffect.Techniques["SkyDome"];
                    currentEffect.Parameters["xWorld"].SetValue(worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(currentViewMatrix);
                    currentEffect.Parameters["xProjection"].SetValue(projectionMatrix);
                    currentEffect.Parameters["xTexture0"].SetValue(cloudMap);
                    currentEffect.Parameters["xEnableLighting"].SetValue(false);
                }
                device.RasterizerState = RasterizerState.CullNone;
                mesh.Draw();
            }
        }

        private void DrawRefractionMap()
        {
            Vector4 refractionPlane = CreatePlane(waterHeight + 3.0f, new Vector3(0, -1, 0), -viewMatrix, false);

            device.SetRenderTarget(refractionRenderTarget);
            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);
            effect.Parameters["xClip"].SetValue(true);
            effect.Parameters["xClipPlane"].SetValue(refractionPlane);
            DrawTerrain(viewMatrix);
            device.SetRenderTarget(null);
            effect.Parameters["xClip"].SetValue(false);
            refractionMap = refractionRenderTarget;

            //FileStream stream = new FileStream("refractMap.jpg", FileMode.OpenOrCreate);
            //refractionMap.SaveAsPng(stream, refractionMap.Width, refractionMap.Height);
            //stream.Close();
        }

        private void DrawReflectionMap()
        {
            Vector4 reflectionPlane = CreatePlane(waterHeight - 0.5f, new Vector3(0, -1, 0), reflectionViewMatrix, true);
            device.SetRenderTarget(reflectionRenderTarget);
            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);
            effect.Parameters["xClip"].SetValue(true);
            effect.Parameters["xClipPlane"].SetValue(reflectionPlane);
            DrawTerrain(reflectionViewMatrix);
            DrawSkyDome(reflectionViewMatrix);
            device.SetRenderTarget(null);
            effect.Parameters["xClip"].SetValue(false);
            reflectionMap = reflectionRenderTarget;

            //FileStream stream = new FileStream("reflecttMap.jpg", FileMode.OpenOrCreate);
            //reflectionMap.SaveAsPng(stream, reflectionMap.Width, reflectionMap.Height);
            //stream.Close();
        }

        private void DrawWater(float time, Vector3 campos, Vector3 xwingPosition, Quaternion cameraRotation)
        {
            Vector3 reflCameraPosition = campos;
            reflCameraPosition.Y = -cameraPosition.Y + waterHeight * 2;
            Vector3 reflTargetPos = xwingPosition;
            reflTargetPos.Y = -xwingPosition.Y + waterHeight * 2;

            Vector3 cameraRight = Vector3.Transform(new Vector3(1, 0, 0), cameraRotation);
            Vector3 invUpVector = Vector3.Cross(cameraRight, reflTargetPos - reflCameraPosition);

            reflectionViewMatrix = Matrix.CreateLookAt(reflCameraPosition, reflTargetPos, invUpVector);

            //reflectionViewMatrix = Matrix.CreateLookAt(reflCameraPosition, reflTargetPos, invUpVector);


            effect.CurrentTechnique = effect.Techniques["Water"];
            Matrix worldMatrix = Matrix.Identity;
            effect.Parameters["xWorld"].SetValue(worldMatrix);
            effect.Parameters["xView"].SetValue(viewMatrix);
            effect.Parameters["xReflectionView"].SetValue(reflectionViewMatrix);
            effect.Parameters["xProjection"].SetValue(projectionMatrix);
            effect.Parameters["xReflectionMap"].SetValue(reflectionMap);
            effect.Parameters["xRefractionMap"].SetValue(refractionMap);
            effect.Parameters["xWaterBumpMap"].SetValue(waterBumpMap);
            effect.Parameters["xWaveLength"].SetValue(0.05f);
            effect.Parameters["xWaveHeight"].SetValue(0.04f);
            effect.Parameters["xCamPos"].SetValue(cameraPosition);
            effect.Parameters["xTime"].SetValue(time);
            effect.Parameters["xWindForce"].SetValue(0.0002f);
            effect.Parameters["xWindDirection"].SetValue(windDirection);
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                device.SetVertexBuffer(waterVertexBuffer, 0);
                int noVertices = waterVertexBuffer.VertexCount;
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, noVertices / 3);
            }
        }

        private void DrawBillboards(Matrix currentViewMatrix)
        {
            DepthStencilState dssBillboard = new DepthStencilState();

            dssBillboard.DepthBufferWriteEnable = false;

            bbEffect.CurrentTechnique = bbEffect.Techniques["CylBillboard"];
            bbEffect.Parameters["xWorld"].SetValue(Matrix.Identity);
            bbEffect.Parameters["xView"].SetValue(currentViewMatrix);
            bbEffect.Parameters["xProjection"].SetValue(projectionMatrix);
            bbEffect.Parameters["xCamPos"].SetValue(cameraPosition);
            bbEffect.Parameters["xAllowedRotDir"].SetValue(new Vector3(0, 1, 0));
            bbEffect.Parameters["xBillboardTexture"].SetValue(treeTexture);

            device.SetVertexBuffer(treeVertexBuffer, 0);
            int noVertices = treeVertexBuffer.VertexCount;
            int noTriangles = noVertices / 3;
            {
                device.DepthStencilState = dssBillboard;
                device.BlendState = BlendState.AlphaBlend;
                device.RasterizerState = RasterizerState.CullNone;
                device.SamplerStates[0] = SamplerState.LinearClamp;
                bbEffect.CurrentTechnique.Passes[0].Apply();
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, noTriangles);
            }

            {
                device.BlendState = BlendState.NonPremultiplied;
                device.DepthStencilState = new DepthStencilState();
                bbEffect.CurrentTechnique.Passes[0].Apply();
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, noTriangles);
            }

            device.DepthStencilState = new DepthStencilState();
            device.BlendState = BlendState.Opaque;
        }

        private void GeneratePerlinNoise(float time)
        {
            device.SetRenderTarget(cloudsRenderTarget);
            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);

            effect.CurrentTechnique = effect.Techniques["PerlinNoise"];
            effect.Parameters["xTexture"].SetValue(cloudStaticMap);
            effect.Parameters["xOvercast"].SetValue(1.1f);
            effect.Parameters["xTime"].SetValue(time / 1000.0f);
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.TriangleStrip, fullScreenVertices, 0, 2);
            }

            device.SetRenderTarget(null);
            cloudMap = cloudsRenderTarget;

            //FileStream stream = new FileStream("cloudMap.jpg", FileMode.OpenOrCreate);
            //cloudMap.SaveAsPng(stream, cloudMap.Width, cloudMap.Height);
            //stream.Close();
        }

        private Vector4 CreatePlane(float height, Vector3 planeNormalDirection, Matrix currentViewMatrix, bool clipSide)
        {
            planeNormalDirection.Normalize();
            Vector4 planeCoeffs = new Vector4(planeNormalDirection, height);
            if (clipSide)
                planeCoeffs *= -1;

            Matrix worldViewProjection = currentViewMatrix * projectionMatrix;
            Matrix inverseWorldViewProjection = Matrix.Invert(worldViewProjection);
            inverseWorldViewProjection = Matrix.Transpose(inverseWorldViewProjection);

            Vector4 finalPlane = planeCoeffs;

            return finalPlane;
        }
    }
}
