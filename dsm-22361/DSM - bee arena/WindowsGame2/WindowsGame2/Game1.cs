using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Collections;
using WindowsGame2;
using Particle3DSample;


namespace Series3D2
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        
        enum CollisionType { None, Building, Boundary, Target }
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        GraphicsDevice device;
        //device.GraphicsAdapter.IsFullScreen = true;
        Effect effect;
        SpriteFont font;
        private int frags = 0;

        int[,] floorPlan;
        int[] buildingHeights = new int[] { 0, 2, 2, 6, 5, 4 };
        Vector3 lightDirection = new Vector3(3, -2, 5);
        Vector3 xwingPosition = new Vector3(50, 30, -20);
        Vector4 xwingColor = new Vector4(1, 1, 1, 1);
        Quaternion xwingRotation = Quaternion.Identity;
        float gameSpeed = 2;//0.4f;
        float maxSpeed = 4;//1.2f;
        float minSpeed = 0.1f;
        int maxModels = 4;
        BoundingBox[] buildingBoundingBoxes;
        BoundingBox completeCityBox;
        const int maxTargets = 0;
        
        ArrayList Enemies = new ArrayList();
        List<BoundingSphere> targetList = new List<BoundingSphere>(); 
        Texture2D bulletTexture;

        float fps = 0;
        float elapseTime = 0;

        ArrayList bulletList = new ArrayList();
        double lastBulletTime = 0;
        Vector3 cameraPosition;
        Vector3 cameraUpDirection;

        Quaternion cameraRotation = Quaternion.Identity;

        Matrix viewMatrix;
        Matrix projectionMatrix;

        int port = 9000;
        string ip = "127.0.0.1";
        int uid = 0;
        float fieldOfView = (float)(Math.PI / 3.0);

        TerrainGenerator tg;
        float shipRadius = 0.30f;
        float bulletRadius = 0.2f;
        float bulletSpriteSize = 0.7f;
        int droppingTime = 600;
        
        SoundEffect gunshot = null;
        SoundEffect explosion = null;
        SoundEffect bullet = null;
        SoundEffect alert = null;
        SoundEffectInstance alertInstance = null;
        SoundEffect engine = null;
        SoundEffectInstance engineInstance = null;


        bool drawBoundingSpheres = true;
        bool randomPosition = false;
        ModelDescription[] Models = null;
        Random random = null;
        bool soundOn = true;

        private void playAlert()
        {
            if (!soundOn) return;
            if (alertInstance.State == SoundState.Playing) return;
            alertInstance.Play();
        }

        private void playEngine()
        {
            if (!soundOn)
            {
                if (engineInstance.State == SoundState.Playing) engineInstance.Stop();
                return;
            }
            float volume = gameSpeed / maxSpeed;
            if (volume > 1) volume = 1;
            engineInstance.Volume = volume;
            if (engineInstance.State == SoundState.Playing) return;
            engineInstance.Play();
        }

        private void playExplosion(Vector3 positionOfExplosion)
        {
            if (!soundOn) return;
            float volume = 1;
            float pan = 0;
            volume = 1 - (float)(Vector3.Distance(xwingPosition, positionOfExplosion) / 30.0);
            if (volume < 0) volume = 0;
            
            explosion.Play(volume, 0, pan);
        }

        private bool playBullet(Vector3 positionOfBullet)
        {
            if (!soundOn) return true;
            if (Vector3.Distance(xwingPosition, positionOfBullet) < 5)
            {
                bullet.Play(1.0f, 0, 0);
                return true;
            }
            return false;
        }

        private void playGunshot()
        {
            if (!soundOn) return;
            gunshot.Play(1.0f, 0, 0);
        }
        // The explosions effect works by firing projectiles up into the
        // air, so we need to keep track of all the active projectiles.
        List<Projectile> projectiles = new List<Projectile>();

        TimeSpan timeToNextProjectile = TimeSpan.Zero;
        ParticleSystem explosionParticles;
        ParticleSystem explosionSmokeParticles;
        ParticleSystem projectileTrailParticles;
        
        /// <summary>
        /// Helper for updating the explosions effect.
        /// </summary>
        void UpdateExplosions(GameTime gameTime)
        {
            timeToNextProjectile -= gameTime.ElapsedGameTime;

            if (timeToNextProjectile <= TimeSpan.Zero)
            {
                // Create a new projectile once per second. The real work of moving
                // and creating particles is handled inside the Projectile class.
                projectiles.Add(new Projectile(explosionParticles,
                                               explosionSmokeParticles,
                                               projectileTrailParticles, new Vector3(10,10,-100)));

                timeToNextProjectile += TimeSpan.FromSeconds(1);
            }
        }

        /// <summary>
        /// Helper for updating the list of active projectiles.
        /// </summary>
        void UpdateProjectiles(GameTime gameTime)
        {
            int i = 0;

            while (i < projectiles.Count)
            {
                if (!projectiles[i].Update(gameTime))
                {
                    // Remove projectiles at the end of their life.
                    projectiles.RemoveAt(i);
                }
                else
                {
                    // Advance to the next projectile.
                    i++;
                }
            }
        }

        void NewRandormPosition()
        {
            actualBullets = maxBullets;
            //explosion.Play();
            if (randomPosition)
            {
                bool found = false;
                float yaw = (float)(2 * Math.PI) * (float)random.Next(360) / (float)360.0;
                float pitch = 0;// (float)(2 * Math.PI) * (float)random.Next(360) / (float)360.0;
                float roll = 0;//(float)(2 * Math.PI) * (float)random.Next(360) / (float)360.0;
                xwingRotation = Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);
                
                while (!found)
                {
                    found = true;
                    float pos1 = random.Next(1, tg.heightData.GetLength(0) - 2);
                    float pos2 = random.Next(1, tg.heightData.GetLength(1) - 2);
                    
                    xwingPosition = new Vector3(pos1, 35, -1 * pos2);
                    BoundingSphere xWingSphere = new BoundingSphere(xwingPosition, shipRadius);
                    if (!tg.CheckCollision(xWingSphere))
                    {
                        XwingPosition xwp;
                        lock (Enemies.SyncRoot)
                        {
                            for (int a = 0; a < Enemies.Count; a++)
                            {
                                xwp = (XwingPosition)Enemies[a];
                                if (xWingSphere.Intersects(new BoundingSphere(xwp.position, shipRadius)))
                                {
                                    //xwingPosition = NewRandormPosition();
                                    //xwingRotation = Quaternion.Identity;
                                    found = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        found = false;
                    }
                }
            }
            else
            {
                xwingPosition = new Vector3(tg.heightData.GetLength(0) / 2, 35, -tg.heightData.GetLength(1) / 2);
                xwingRotation = Quaternion.Identity;
            }
        }

        public Game1(String ip, int port, Vector4 color,int uid, float fieldOfView,byte shipModel, bool randomPosition)
        {
            this.ip = ip;
            this.port = port;
            this.uid = uid;
            this.fieldOfView = fieldOfView;
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            xwingColor = color;
            this.randomPosition = randomPosition;
            this.shipModel = shipModel;
        }

        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = 1920;
            graphics.PreferredBackBufferHeight = 1080;
            graphics.IsFullScreen = true;
            graphics.ApplyChanges();
            Window.Title = "Deadly Sky Masters";
            random = new Random();
            lightDirection.Normalize();

            t1 = new TCPClient(ip, port);
            t1.StartConnection();
            t1.NewData += new TCPClient.NewDataRecived(NewDataRecived);

            tg = new TerrainGenerator();

            //PAR
            
            explosionParticles = new ExplosionParticleSystem(this, Content);
            explosionSmokeParticles = new ExplosionSmokeParticleSystem(this, Content);
            projectileTrailParticles = new ProjectileTrailParticleSystem(this, Content);
            
            Components.Add(explosionParticles);
            Components.Add(explosionSmokeParticles);
            Components.Add(projectileTrailParticles);
            
            explosionParticles.DrawOrder = 300;
            explosionSmokeParticles.DrawOrder = 100;
            projectileTrailParticles.DrawOrder = 200;
            //renderTarget = new RenderTarget2D(GraphicsDevice, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            
            base.Initialize();
            renderTarget = new RenderTarget2D(device, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight, false, device.DisplayMode.Format, DepthFormat.Depth24Stencil8, 1, RenderTargetUsage.DiscardContents);

        }
        RenderTarget2D renderTarget = null;
        TCPClient t1 = null;
        public void NewDataRecived(object sender, byte[] data)
        {
            try
            {
                //XwingPosition xp = (XwingPosition)ByteArrayToObject(data);
                XwingPosition xp = XwingPosition.FromByteArray(data);
                XwingPosition xHelp = null;
                if (xp.shipModel >= maxModels)
                    return;
                bool found = false;
                if (xp.killedByUid == uid)
                    frags++;
                
                lock (Enemies.SyncRoot)
                {
                    
                    if (xp.newBullet != null)
                        if (xp.newBullet.ownerUid >= 0)
                            lock (bulletList.SyncRoot)
                            {
                                bulletList.Add(xp.newBullet);
                            }
                    for (int a = 0; a < Enemies.Count; a++)
                    {
                        xHelp = (XwingPosition)Enemies[a];
                        if (xHelp.uid == xp.killedByUid)
                        {
                            if (xp.uid == xp.killedByUid)
                                xHelp.frags--;
                            else
                                xHelp.frags++;
                        }
                        if (xHelp.uid == xp.uid)
                        {
                            if (xp.killedByUid > 0)
                            {
                                projectiles.Add(new Projectile(explosionParticles,
                                               explosionSmokeParticles,
                                               projectileTrailParticles, xHelp.position));
                                playExplosion(xHelp.position);
                            }
                            xHelp.position = xp.position;
                            xHelp.rotation = xp.rotation;
                            xHelp.lastUpdate = 0;
                            found = true;
                        }
                        
                    }
                    if (!found)
                    {
                        //PlayersCount++;
                        Enemies.Add(xp);
                    }
                }
            }
            catch { }
        }

        public void SendDataToServer()
        {
            XwingPosition xp = new XwingPosition();
            xp.position = xwingPosition;
            xp.rotation = xwingRotation;
            xp.uid = uid;
            xp.color = xwingColor;
            xp.newBullet = this.newBullet;
            xp.killedByUid = killedByUid;
            xp.shipModel = shipModel;
            byte[] data = XwingPosition.ToByteArray(xp);
            t1.SendData(data);
        }

        private byte[] ToByteArray(object source)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, source);
                return stream.ToArray();
            }
        }

        public object ByteArrayToObject(byte[] _ByteArray)
        {
            try
            {
                // convert byte array to memory stream
                MemoryStream _MemoryStream = new MemoryStream(_ByteArray);

                // create new BinaryFormatter
   	            BinaryFormatter _BinaryFormatter = new BinaryFormatter();
 
	            // set memory stream position to starting point
	            _MemoryStream.Position = 0;
	 
	            // Deserializes a stream into an object graph and return as a object.
	            return _BinaryFormatter.Deserialize(_MemoryStream);
	        }
	        catch (Exception _Exception)
	        {
	            // Error
	            Console.WriteLine("Exception caught in process: {0}", _Exception.ToString());
	        }
	 
	    // Error occured, return null
	    return null;
	}
        Texture2D background = null;
        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            device = graphics.GraphicsDevice;

            effect = Content.Load<Effect>("effects");
            LoadModels();
            bulletTexture = Content.Load<Texture2D>("particle/bullet");
            
            background = Content.Load<Texture2D>("background");
            font = Content.Load<SpriteFont>("Font");
            tg.LoadContent(this);

            gunshot = Content.Load<SoundEffect>("sounds/GUNSHOT2");
            explosion = Content.Load<SoundEffect>("sounds/explosion");
            bullet = Content.Load<SoundEffect>("sounds/ric");
            alert = Content.Load<SoundEffect>("sounds/alert");
            alertInstance = alert.CreateInstance();
            engine = Content.Load<SoundEffect>("sounds/engine");
            engineInstance = engine.CreateInstance();
            NewRandormPosition();
        }
        public byte shipModel = 0;
        private void LoadModels()
        {
            Models = new ModelDescription[4];
            LoadModel("Ships/xwing", 0);
            Models[0].RotationY = (float)Math.PI;
            Models[0].Scale = 0.003f;
            Models[0].CameraPosition = new Vector3(0, 0.1f, 1.6f);
            
            LoadModel("Ships/Tie-Fighter", 1);
            Models[1].RotationX = (float)Math.PI / 2.0f;
            Models[1].Scale = 0.4f;
            Models[1].CameraPosition = new Vector3(0, 0.15f, 1.6f);

            LoadModel("Ships/Tie-Invader", 2);
            Models[2].RotationX = (float)Math.PI / 2.0f;
            Models[2].Scale = 0.4f;
            Models[2].CameraPosition = new Vector3(0, 0.1f, 1.6f);

            LoadModel("Ships/Voinian Spaceship", 3);
            Models[3].RotationX = 0;
            Models[3].RotationY = (float)Math.PI;
            Models[3].Scale = 0.07f;
            Models[3].CameraPosition = new Vector3(0, 0.2f, 1.6f);

            maxModels = Models.Length;
            /*
            LoadModel("Ships/Tie-Interceptor", 4);
            Models[4].RotationX = 0;
            Models[4].RotationY = 0;
            Models[4].Scale = 0.05f;
            Models[4].CameraPosition = new Vector3(0, 0.15f, 1.6f);*/
        }
        private void LoadModel(string assetName, int index)
        {
            Models[index] = new ModelDescription();
            Model newModel = Content.Load<Model>(assetName);
            //helpModel = Content.Load<Model>(assetName);
            for (int a = 0; a < newModel.Meshes.Count; a++)
            {
                for (int b = 0; b < newModel.Meshes[a].Effects.Count; b++)
                {
                    Models[index].ModelColors.Add(((BasicEffect)(newModel.Meshes[a].Effects[b])).DiffuseColor);
                }
            }

            foreach (ModelMesh mesh in newModel.Meshes)
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
                    meshPart.Effect = effect.Clone();
                }
            Models[index].ShipModel = newModel;
        }


        private Model LoadModel(string assetName, out Texture2D[] textures)
        {

            Model newModel = Content.Load<Model>(assetName);
            textures = new Texture2D[newModel.Meshes.Count];
            int i = 0;
            foreach (ModelMesh mesh in newModel.Meshes)
                foreach (BasicEffect currentEffect in mesh.Effects)
                    textures[i++] = currentEffect.Texture;

            foreach (ModelMesh mesh in newModel.Meshes)
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                    meshPart.Effect = effect.Clone();

            return newModel;
        }

        protected override void UnloadContent()
        {
            t1.StopConnection();
        }

        double lastBulletGameTime = 0;
        protected override void Update(GameTime gameTime)
        {
            double currentTime = gameTime.TotalGameTime.TotalMilliseconds;
            if (currentTime - lastBulletGameTime > bulletGain)
            {
                lastBulletGameTime = currentTime;
                if (actualBullets < maxBullets)
                    actualBullets++;
            }


            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();
            tg.Update();
            ProcessKeyboard(gameTime);
            float moveSpeed = gameTime.ElapsedGameTime.Milliseconds / 500.0f * gameSpeed;
            MoveShipForward(ref xwingPosition, xwingRotation, moveSpeed);

            BoundingSphere xWingSphere = new BoundingSphere(xwingPosition, shipRadius);
            if (tg.CheckCollision(xWingSphere))
            {
                projectiles.Add(new Projectile(explosionParticles,
                                               explosionSmokeParticles,
                                               projectileTrailParticles, xwingPosition));
                playExplosion(xwingPosition);
                frags--;
                NewRandormPosition();
                //xwingPosition = NewRandormPosition();
                //xwingRotation = Quaternion.Identity;
                killedByUid = uid;
            }

            UpdateCamera();
            UpdateBulletPositions(0.4f);


            XwingPosition xwp;
            lock (Enemies.SyncRoot)
            {
                for (int a = 0; a < Enemies.Count; a++)
                {
                    xwp = (XwingPosition)Enemies[a];
                    if (xwp.uid == killedByUid)
                    {
                        xwp.frags++;
                    }
                    if (xWingSphere.Intersects(new BoundingSphere(xwp.position, shipRadius)))
                    {
                        //xwingPosition = NewRandormPosition();
                        //xwingRotation = Quaternion.Identity;
                        projectiles.Add(new Projectile(explosionParticles,
                                               explosionSmokeParticles,
                                               projectileTrailParticles, xwingPosition));
                        playExplosion(xwingPosition);
                        NewRandormPosition();
                    }
                }
            }
            
            SendDataToServer();
            newBullet = null;
            killedByUid = -1;
            elapseTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            frameCounter++;

            if (elapseTime > 1)
            {
                fps = frameCounter;
                frameCounter = 0;
                elapseTime = 0;
            }

            //PAR
            UpdateProjectiles(gameTime);
            playEngine();
            base.Update(gameTime);
        }
        int frameCounter = 0;

        private void UpdateCamera()
        {

            cameraRotation = Quaternion.Lerp(cameraRotation, xwingRotation, 0.1f);

            Vector3 campos = Models[shipModel].CameraPosition;//new Vector3(0, 0.1f, 1.6f);

            campos = Vector3.Transform(campos, Matrix.CreateFromQuaternion(cameraRotation));
            campos += xwingPosition;

            Vector3 camup = new Vector3(0, 1, 0);
            camup = Vector3.Transform(camup, Matrix.CreateFromQuaternion(cameraRotation));

            viewMatrix = Matrix.CreateLookAt(campos, xwingPosition, camup);
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(fieldOfView, device.Viewport.AspectRatio, 0.2f, 500.0f);

            cameraPosition = campos;
            cameraUpDirection = camup;
        }
        int maxBullets = 100;
        int actualBullets = 100;
        double bulletGain = 1000;

        private void ProcessKeyboard(GameTime gameTime)
        {
            float leftRightRot = 0;

            float turningSpeed = (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f;
            //turningSpeed *= 3f * gameSpeed;
            //turningSpeed *= 1f * gameSpeed;
            turningSpeed *= gameSpeed;
            KeyboardState keys = Keyboard.GetState();
            if (keys.IsKeyDown(Keys.Right))
                leftRightRot += turningSpeed;
            if (keys.IsKeyDown(Keys.Left))
                leftRightRot -= turningSpeed;

            float upDownRot = 0;
            if (keys.IsKeyDown(Keys.Down))
                upDownRot += turningSpeed;
            if (keys.IsKeyDown(Keys.Up))
                upDownRot -= turningSpeed;

            Quaternion additionalRot = Quaternion.CreateFromAxisAngle(new Vector3(0, 0, -1), leftRightRot) * Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), upDownRot);
            xwingRotation *= additionalRot;

            if (keys.IsKeyDown(Keys.F))
            {
                graphics.IsFullScreen = !graphics.IsFullScreen;
                graphics.ApplyChanges();
            }
            if (keys.IsKeyDown(Keys.Add))
                if (gameSpeed < maxSpeed)
                    gameSpeed += 0.1f;
            if (keys.IsKeyDown(Keys.Subtract))
                if (gameSpeed > minSpeed)
                    gameSpeed -= 0.1f;
            if (gameSpeed < minSpeed)
                gameSpeed = minSpeed;
            if (keys.IsKeyDown(Keys.Space) && actualBullets > 0)
            {
                double currentTime = gameTime.TotalGameTime.TotalMilliseconds;
                if (currentTime - lastBulletTime > 100)
                {
                    actualBullets--;
                    Bullet newBullet = new Bullet();
                    newBullet.position = xwingPosition;
                    newBullet.rotation = xwingRotation;
                    newBullet.ownerUid = uid;
                    lock (bulletList.SyncRoot)
                    {
                        bulletList.Add(newBullet);
                    }
                    playGunshot();
                    
                    lastBulletTime = currentTime;
                    this.newBullet = newBullet;
                }
            }
            if (keys.IsKeyDown(Keys.LeftAlt) && !keys.IsKeyDown(Keys.Space) && actualBullets > 0)
            {
                actualBullets--;
                Bullet newBullet = new Bullet();
                newBullet.position = xwingPosition;
                newBullet.rotation = xwingRotation;
                newBullet.ownerUid = uid;
                lock (bulletList.SyncRoot)
                {
                    bulletList.Add(newBullet);
                }
                playGunshot();
            }
            if (keys.IsKeyDown(Keys.B))
            {
                drawBoundingSpheres = !drawBoundingSpheres;
            }
            if (keys.IsKeyDown(Keys.S))
            {
                soundOn = !soundOn;
            }
        }

        private Bullet newBullet = null;
        private int killedByUid = -1;

        private void MoveShipForward(ref Vector3 position, Quaternion rotationQuat, float speed)
        {
            Vector3 addVector = Vector3.Transform(new Vector3(0, 0, -3), rotationQuat);
            Vector3 prevPosition = position;
            position += addVector * speed;
            bool border = false;
            if (position.Y > tg.maxHeight)
            {
                position.Y = tg.maxHeight;
                border = true;
            }
            if (position.X < 0 || position.X >= tg.heightData.GetLength(0))
            {
                position.X = prevPosition.X;
                border = true;
            }
            if (position.Z > 0 || position.Z <= -tg.heightData.GetLength(1))
            {
                position.Z = prevPosition.Z;
                border = true;
            }
            if (border) playAlert();
        }

        private void MoveBulletForward(ref Vector3 position, Quaternion rotationQuat, float speed)
        {
            Vector3 addVector = Vector3.Transform(new Vector3(0, 0, -1), rotationQuat);
            position += addVector * speed;
        }

        private bool CheckCollisionShot(BoundingSphere sphere)
        {
            BoundingSphere xwingSpere = new BoundingSphere(xwingPosition, shipRadius);
            if (xwingSpere.Contains(sphere) == ContainmentType.Contains || xwingSpere.Contains(sphere) == ContainmentType.Intersects
                || xwingSpere.Intersects(sphere))
                return true;

            return false;
        }
        private void UpdateBulletPositions(float moveSpeed)
        {
            lock (bulletList.SyncRoot)
            {
                for (int i = 0; i < bulletList.Count; i++)
                {
                    Bullet currentBullet = (Bullet)bulletList[i];
                    if (currentBullet.played == false && currentBullet.ownerUid != uid)
                        currentBullet.played = playBullet(currentBullet.position);
                    MoveBulletForward(ref currentBullet.position, currentBullet.rotation, moveSpeed);
                    bulletList[i] = currentBullet;

                    BoundingSphere bulletSphere = new BoundingSphere(currentBullet.position, bulletRadius);
                    if (tg.CheckCollision(bulletSphere))
                    {
                        bulletList.RemoveAt(i);
                        i--;
                    }
                    if (currentBullet.ownerUid != uid)
                        if (CheckCollisionShot(bulletSphere))
                        {
                            projectiles.Add(new Projectile(explosionParticles,
                                               explosionSmokeParticles,
                                               projectileTrailParticles, xwingPosition));
                            playExplosion(xwingPosition);
                            NewRandormPosition();
                            killedByUid = currentBullet.ownerUid;
                        }
                }
            }
        }


        protected override void Draw(GameTime gameTime)
        {
            //tg.Draw(gameTime, cameraPosition, xwingPosition, cameraRotation);
            GraphicsDevice.SetRenderTarget(renderTarget);

            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Transparent, 1.0f, 0);
            Vector3 np = new Vector3(xwingPosition.X + 1, xwingPosition.Y, xwingPosition.Z);
            //DrawHelpShip(xwingRotation, xwingColor, shipModel);
            //GraphicsDevice.Clear(Color.Transparent);
            //DrawEnemy(xwingPosition, xwingRotation, xwingColor,shipModel);
            GraphicsDevice.SetRenderTarget(null);


            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.DarkSlateBlue, 1.0f, 0);
            
            tg.viewMatrix = viewMatrix;
            tg.projectionMatrix = projectionMatrix;
            tg.cameraPosition = cameraPosition;
            tg.Draw(gameTime,cameraPosition, xwingPosition,cameraRotation);
            
            if (shipModel == 0)
                DrawModel();
            else 
                DrawModelMaterial();
            lock (Enemies.SyncRoot)
            {
                for (int a = 0; a < Enemies.Count; a++)
                {
                    XwingPosition xp = (XwingPosition)Enemies[a];
                    if (xp.shipModel == 0)
                        DrawEnemy(xp.position, xp.rotation, xp.color, xp.shipModel);
                    else
                        DrawEnemyMaterial(xp.position, xp.rotation, xp.color, xp.shipModel);
                    if (drawBoundingSpheres)
                    {
                        BoundingSphere bs = new BoundingSphere(xp.position, 4 * shipRadius);
                        //BoundingSphereRenderer.Render(bs, device, viewMatrix, projectionMatrix, new Color(xp.color.X, xp.color.Y, xp.color.Z, alpha));
                        BoundingSphereRenderer.Render(bs, device, viewMatrix, projectionMatrix, new Color(xp.color.X, xp.color.Y, xp.color.Z));
                    }
                    xp.lastUpdate++;
                    if (xp.lastUpdate > droppingTime)
                    {
                        Enemies.Remove(xp);
                        a--;
                    }
                }
            }
            //PAR
            explosionParticles.SetCamera(viewMatrix, projectionMatrix);
            explosionSmokeParticles.SetCamera(viewMatrix, projectionMatrix);
            projectileTrailParticles.SetCamera(viewMatrix, projectionMatrix);


            
            //DrawTargets();
            base.Draw(gameTime);
            DrawBullets();
            DepthStencilState dss = GraphicsDevice.DepthStencilState;
            BlendState oldBS = GraphicsDevice.BlendState;
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            //spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
            spriteBatch.Begin();
            //spriteBatch.DrawString(font, "Fps: " + fps, new Vector2(10, 10), Color.Red);
            spriteBatch.DrawString(font, "Sting Energy: " + actualBullets + "/" + maxBullets, new Vector2(10, 10), Color.Red);
            
            Color color = new Color(xwingColor);
            spriteBatch.DrawString(font, "Frag Count: " + frags, new Vector2(10, 65), Color.Orange);
            lock (Enemies.SyncRoot)
            {
                for (int a = 0; a < Enemies.Count; a++)
                {
                    color = new Color(((XwingPosition)Enemies[a]).color);
                    spriteBatch.DrawString(font, "Frag Count: " + ((XwingPosition)Enemies[a]).frags, new Vector2(10, 40 + a * 15), color);
                }
            }
            //spriteBatch.Draw(background, new Vector2(640-background.Width-10, 480-background.Height-10), Color.White);
            spriteBatch.End();

            //DrawEnemy(xwingPosition, xwingRotation, xwingColor, shipModel);
            
            
            GraphicsDevice.DepthStencilState = dss;
            GraphicsDevice.BlendState = oldBS;
            /*
            Quaternion oldC = cameraRotation;
            cameraRotation = Quaternion.Identity;
            Vector3 np = new Vector3(xwingPosition.X+1, xwingPosition.Y,xwingPosition.Z);
            DrawEnemy(np, xwingRotation, xwingColor, shipModel);
            cameraRotation = oldC;
            GraphicsDevice.SetRenderTarget(renderTarget);
            //GraphicsDevice.Clear(Color.Transparent);
            //DrawEnemy(xwingPosition, xwingRotation, xwingColor,shipModel);
            GraphicsDevice.SetRenderTarget(null);
            */
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
           
            //spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            spriteBatch.Begin();
            spriteBatch.Draw((Texture2D)renderTarget, new Vector2(260, 140), Color.White);
            spriteBatch.End();
            GraphicsDevice.BlendState = oldBS;
            GraphicsDevice.DepthStencilState = dss;
            //DrawEnemy(xp.position, xp.rotation, xp.color, xp.shipModel);
        }
        
        private void DrawModel()
        {
            Matrix worldMatrix = Matrix.CreateScale(Models[shipModel].Scale, Models[shipModel].Scale, Models[shipModel].Scale) * Matrix.CreateRotationX(Models[shipModel].RotationX) * Matrix.CreateRotationY(Models[shipModel].RotationY) * Matrix.CreateRotationZ(Models[shipModel].RotationZ) * Matrix.CreateFromQuaternion(xwingRotation) * Matrix.CreateTranslation(xwingPosition);

            Matrix[] xwingTransforms = new Matrix[Models[shipModel].ShipModel.Bones.Count];
            Models[shipModel].ShipModel.CopyAbsoluteBoneTransformsTo(xwingTransforms);
            //VertexDeclaration vd = xwingModel.Meshes[0].MeshParts[0].VertexBuffer.VertexDeclaration;
            foreach (ModelMesh mesh in Models[shipModel].ShipModel.Meshes)
            {
                foreach (Effect currentEffect in mesh.Effects)
                {
                    //VertexDeclaration vd = mesh.MeshParts[0].VertexBuffer.VertexDeclaration;

                    currentEffect.CurrentTechnique = currentEffect.Techniques["ColoredShips"];
                    currentEffect.Parameters["xWorld"].SetValue(xwingTransforms[mesh.ParentBone.Index] * worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(viewMatrix);
                    currentEffect.Parameters["xProjection"].SetValue(projectionMatrix);
                    currentEffect.Parameters["xEnableLighting"].SetValue(true);
                    currentEffect.Parameters["xLightDirection"].SetValue(lightDirection);
                    currentEffect.Parameters["xAmbient"].SetValue(0.5f);
                    currentEffect.Parameters["shipColor"].SetValue(xwingColor);
                }
                mesh.Draw();
            }
            //BoundingSphere bs = new BoundingSphere(xwingPosition, shipRadius);
            //BoundingSphereRenderer.Render(bs, device, viewMatrix, projectionMatrix, new Color(xwingColor.X, xwingColor.Y,xwingColor.Z));
        }

        private void DrawModelMaterial()
        {
            Matrix worldMatrix = Matrix.CreateScale(Models[shipModel].Scale, Models[shipModel].Scale, Models[shipModel].Scale) * Matrix.CreateRotationX(Models[shipModel].RotationX) * Matrix.CreateRotationY(Models[shipModel].RotationY) * Matrix.CreateRotationZ(Models[shipModel].RotationZ) * Matrix.CreateFromQuaternion(xwingRotation) * Matrix.CreateTranslation(xwingPosition);

            Matrix[] xwingTransforms = new Matrix[Models[shipModel].ShipModel.Bones.Count];
            Models[shipModel].ShipModel.CopyAbsoluteBoneTransformsTo(xwingTransforms);
            //VertexDeclaration vd = xwingModel.Meshes[0].MeshParts[0].VertexBuffer.VertexDeclaration;
            
            int a = 0;
            foreach (ModelMesh mesh in Models[shipModel].ShipModel.Meshes)
            {
                foreach (Effect currentEffect in mesh.Effects)
                {
                    VertexDeclaration vd = mesh.MeshParts[0].VertexBuffer.VertexDeclaration;

                    currentEffect.CurrentTechnique = currentEffect.Techniques["ColoredShipsMaterial"];
                    currentEffect.Parameters["xWorld"].SetValue(xwingTransforms[mesh.ParentBone.Index] * worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(viewMatrix);
                    currentEffect.Parameters["xProjection"].SetValue(projectionMatrix);
                    currentEffect.Parameters["xEnableLighting"].SetValue(true);
                    currentEffect.Parameters["xLightDirection"].SetValue(lightDirection);
                    currentEffect.Parameters["xAmbient"].SetValue(0.5f);
                    currentEffect.Parameters["shipColor"].SetValue(xwingColor);
                    Vector3 dc = (Vector3)Models[shipModel].ModelColors[a];
                    a++;
                    Vector4 colorHelp = new Vector4(dc.X, dc.Y, dc.Z, 1);
                    currentEffect.Parameters["materialColor"].SetValue(colorHelp);
                }
                
                mesh.Draw();
            }
            //BoundingSphere bs = new BoundingSphere(xwingPosition, shipRadius);
            //BoundingSphereRenderer.Render(bs, device, viewMatrix, projectionMatrix, new Color(xwingColor.X, xwingColor.Y,xwingColor.Z));
        }

        private void DrawHelpShip(Quaternion rotation, Vector4 color, byte shipModel)
        {

            Matrix worldMatrix = Matrix.CreateScale(Models[shipModel].Scale, Models[shipModel].Scale, Models[shipModel].Scale) * Matrix.CreateRotationX(Models[shipModel].RotationX) * Matrix.CreateRotationY(Models[shipModel].RotationY) * Matrix.CreateRotationZ(Models[shipModel].RotationZ) * Matrix.CreateFromQuaternion(rotation);
            Vector3 camup = new Vector3(0, 1, 0);
            Matrix viewMatrixHelp = Matrix.CreateLookAt(new Vector3(0,0,4), new Vector3(0,0,0), camup);
            Vector3 lightDirection = new Vector3(1, 0, 0);

            Matrix[] xwingTransforms = new Matrix[Models[shipModel].ShipModel.Bones.Count];
            Models[shipModel].ShipModel.CopyAbsoluteBoneTransformsTo(xwingTransforms);
            Matrix projectionMatrixHelp = Matrix.CreatePerspectiveFieldOfView((float)(Math.PI / 3.0), device.Viewport.AspectRatio, 0.2f, 500.0f);
            if (shipModel == 0)
            {
                foreach (ModelMesh mesh in Models[shipModel].ShipModel.Meshes)
                {
                    foreach (Effect currentEffect in mesh.Effects)
                    {
                        currentEffect.CurrentTechnique = currentEffect.Techniques["ColoredShips"];
                        currentEffect.Parameters["xWorld"].SetValue(xwingTransforms[mesh.ParentBone.Index] * worldMatrix);
                        currentEffect.Parameters["xView"].SetValue(viewMatrixHelp);
                        currentEffect.Parameters["xProjection"].SetValue(projectionMatrixHelp);
                        currentEffect.Parameters["xEnableLighting"].SetValue(true);
                        currentEffect.Parameters["xLightDirection"].SetValue(lightDirection);
                        currentEffect.Parameters["xAmbient"].SetValue(0.5f);
                        currentEffect.Parameters["shipColor"].SetValue(color);
                    }
                    mesh.Draw();
                }
            }
            else
            {
                int a = 0;
                foreach (ModelMesh mesh in Models[shipModel].ShipModel.Meshes)
                {
                    foreach (Effect currentEffect in mesh.Effects)
                    {
                        VertexDeclaration vd = mesh.MeshParts[0].VertexBuffer.VertexDeclaration;

                        currentEffect.CurrentTechnique = currentEffect.Techniques["ColoredShipsMaterial"];
                        currentEffect.Parameters["xWorld"].SetValue(xwingTransforms[mesh.ParentBone.Index] * worldMatrix);
                        currentEffect.Parameters["xView"].SetValue(viewMatrixHelp);
                        currentEffect.Parameters["xProjection"].SetValue(projectionMatrixHelp);
                        currentEffect.Parameters["xEnableLighting"].SetValue(true);
                        currentEffect.Parameters["xLightDirection"].SetValue(lightDirection);
                        currentEffect.Parameters["xAmbient"].SetValue(0.5f);
                        currentEffect.Parameters["shipColor"].SetValue(color);
                        Vector3 dc = (Vector3)Models[shipModel].ModelColors[a];
                        a++;
                        Vector4 colorHelp = new Vector4(dc.X, dc.Y, dc.Z, 1);
                        currentEffect.Parameters["materialColor"].SetValue(colorHelp);
                    }
                    mesh.Draw();
                }
            }

        }

        private void DrawEnemy(Vector3 translation, Quaternion rotation, Vector4 color, byte shipModel)
        {
            Matrix worldMatrix = Matrix.CreateScale(Models[shipModel].Scale, Models[shipModel].Scale, Models[shipModel].Scale) * Matrix.CreateRotationX(Models[shipModel].RotationX) * Matrix.CreateRotationY(Models[shipModel].RotationY) * Matrix.CreateRotationZ(Models[shipModel].RotationZ) * Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(translation);

            Matrix[] xwingTransforms = new Matrix[Models[shipModel].ShipModel.Bones.Count];
            Models[shipModel].ShipModel.CopyAbsoluteBoneTransformsTo(xwingTransforms);
            foreach (ModelMesh mesh in Models[shipModel].ShipModel.Meshes)
            {
                foreach (Effect currentEffect in mesh.Effects)
                {
                    currentEffect.CurrentTechnique = currentEffect.Techniques["ColoredShips"];
                    currentEffect.Parameters["xWorld"].SetValue(xwingTransforms[mesh.ParentBone.Index] * worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(viewMatrix);
                    currentEffect.Parameters["xProjection"].SetValue(projectionMatrix);
                    currentEffect.Parameters["xEnableLighting"].SetValue(true);
                    currentEffect.Parameters["xLightDirection"].SetValue(lightDirection);
                    currentEffect.Parameters["xAmbient"].SetValue(0.5f);
                    currentEffect.Parameters["shipColor"].SetValue(color);
                }
                mesh.Draw();
            }
            /*if (drawBoundingSpheres)
            {
                BoundingSphere bs = new BoundingSphere(xwingPosition, shipRadius);
                BoundingSphereRenderer.Render(bs, device, viewMatrix, projectionMatrix, new Color(xwingColor.X, xwingColor.Y, xwingColor.Z));
            }*/
        }

        private void DrawEnemyMaterial(Vector3 translation, Quaternion rotation, Vector4 color, byte shipModel)
        {
            Matrix worldMatrix = Matrix.CreateScale(Models[shipModel].Scale, Models[shipModel].Scale, Models[shipModel].Scale) * Matrix.CreateRotationX(Models[shipModel].RotationX) * Matrix.CreateRotationY(Models[shipModel].RotationY) * Matrix.CreateRotationZ(Models[shipModel].RotationZ) * Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(translation);

            Matrix[] xwingTransforms = new Matrix[Models[shipModel].ShipModel.Bones.Count];
            Models[shipModel].ShipModel.CopyAbsoluteBoneTransformsTo(xwingTransforms);
            //VertexDeclaration vd = xwingModel.Meshes[0].MeshParts[0].VertexBuffer.VertexDeclaration;
            
            int a = 0;
            foreach (ModelMesh mesh in Models[shipModel].ShipModel.Meshes)
            {
                foreach (Effect currentEffect in mesh.Effects)
                {
                    VertexDeclaration vd = mesh.MeshParts[0].VertexBuffer.VertexDeclaration;

                    currentEffect.CurrentTechnique = currentEffect.Techniques["ColoredShipsMaterial"];
                    currentEffect.Parameters["xWorld"].SetValue(xwingTransforms[mesh.ParentBone.Index] * worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(viewMatrix);
                    currentEffect.Parameters["xProjection"].SetValue(projectionMatrix);
                    currentEffect.Parameters["xEnableLighting"].SetValue(true);
                    currentEffect.Parameters["xLightDirection"].SetValue(lightDirection);
                    currentEffect.Parameters["xAmbient"].SetValue(0.5f);
                    currentEffect.Parameters["shipColor"].SetValue(color);
                    Vector3 dc = (Vector3)Models[shipModel].ModelColors[a];
                    a++;
                    Vector4 colorHelp = new Vector4(dc.X, dc.Y, dc.Z, 1);
                    currentEffect.Parameters["materialColor"].SetValue(colorHelp);
                }
                mesh.Draw();
            }
            /*if (drawBoundingSpheres)
            {
                BoundingSphere bs = new BoundingSphere(xwingPosition, shipRadius);
                BoundingSphereRenderer.Render(bs, device, viewMatrix, projectionMatrix, new Color(xwingColor.X, xwingColor.Y, xwingColor.Z));
            }*/
        }
        
        
        private void DrawBullets()
        {
            lock (bulletList.SyncRoot)
            {
                if (bulletList.Count > 0)
                {
                    VertexPositionTexture[] bulletVertices = new VertexPositionTexture[bulletList.Count * 6];
                    int i = 0;
                    foreach (Bullet currentBullet in bulletList)
                    {
                        Vector3 center = currentBullet.position;

                        bulletVertices[i++] = new VertexPositionTexture(center, new Vector2(1, 1));
                        bulletVertices[i++] = new VertexPositionTexture(center, new Vector2(0, 0));
                        bulletVertices[i++] = new VertexPositionTexture(center, new Vector2(1, 0));

                        bulletVertices[i++] = new VertexPositionTexture(center, new Vector2(1, 1));
                        bulletVertices[i++] = new VertexPositionTexture(center, new Vector2(0, 1));
                        bulletVertices[i++] = new VertexPositionTexture(center, new Vector2(0, 0));
                    }

                    effect.CurrentTechnique = effect.Techniques["PointSprites"];
                    effect.Parameters["xWorld"].SetValue(Matrix.Identity);
                    effect.Parameters["xProjection"].SetValue(projectionMatrix);
                    effect.Parameters["xView"].SetValue(viewMatrix);
                    effect.Parameters["xCamPos"].SetValue(cameraPosition);
                    effect.Parameters["xTexture"].SetValue(bulletTexture);
                    effect.Parameters["xCamUp"].SetValue(cameraUpDirection);
                    effect.Parameters["xPointSpriteSize"].SetValue(bulletSpriteSize);
                    BlendState oldBs = device.BlendState;
                    device.BlendState = BlendState.Additive;

                    foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        device.DrawUserPrimitives(PrimitiveType.TriangleList, bulletVertices, 0, bulletList.Count * 2);
                    }
                    /*foreach (Bullet currentBullet in bulletList)
                    {
                        BoundingSphere bs = new BoundingSphere(currentBullet.position, bulletRadius);
                        BoundingSphereRenderer.Render(bs, device, viewMatrix, projectionMatrix, new Color(xwingColor.X, xwingColor.Y,xwingColor.Z));
                    }*/
                    //device.BlendState = BlendState.Opaque;
                    device.BlendState = oldBs;
                }
            }
        }
        
    }
}