﻿using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using Hacknet.Effects;
using Hacknet.PlatformAPI.Storage;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Point = System.Drawing.Point;

namespace Hacknet
{
    public class Game1 : Game
    {
        private static Game1 singleton;
        public static bool threadsExiting;
        public static CultureInfo culture;
        private bool CanLoadContent;
        public GraphicsDeviceManager graphics;
        public GraphicsDeviceInformation graphicsInfo;
        private readonly EventHandler<PreparingDeviceSettingsEventArgs> graphicsPreparedHandler;
        private bool HasLoadedContent;
        private bool resolutionSet = true;
        private ScreenManager sman;
        private SpriteBatch spriteBatch;

        public Game1()
        {
            culture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            threadsExiting = false;
            graphics = new GraphicsDeviceManager(this)
            {
                GraphicsProfile = GraphicsProfile.HiDef,
                PreferMultiSampling = true
            };
            graphics.DeviceDisposing += Graphics_DeviceDisposing;
            graphics.DeviceResetting += Graphics_DeviceResetting;
            graphics.DeviceReset += Graphics_DeviceReset;
            SettingsLoader.checkStatus();
            if (SettingsLoader.didLoad)
            {
                CanLoadContent = true;
                graphics.PreferredBackBufferWidth = Math.Min(SettingsLoader.resWidth, 4096);
                graphics.PreferredBackBufferHeight = Math.Min(SettingsLoader.resHeight, 4096);
                graphics.IsFullScreen = SettingsLoader.isFullscreen;
            }
            else if (Settings.windowed)
            {
                graphics.PreferredBackBufferWidth = 1280;
                graphics.PreferredBackBufferHeight = 800;
                CanLoadContent = true;
            }
            else
            {
                graphicsPreparedHandler = Graphics_PreparingDeviceSettings;
                graphics.PreparingDeviceSettings += graphicsPreparedHandler;
                graphics.PreferredBackBufferWidth = 1280;
                graphics.PreferredBackBufferHeight = 800;
                graphics.IsFullScreen = true;
            }
            IsMouseVisible = true;
            IsFixedTimeStep = false;
            Content.RootDirectory = "Content";
            singleton = this;
            Exiting += HandleExit;
            PlatformAPISettings.InitPlatformAPI();
            StatsManager.InitStats();
        }

        private void Graphics_DeviceReset(object sender, EventArgs e)
        {
            Program.GraphicsDeviceResetLog = Program.GraphicsDeviceResetLog + "Reset at " +
                                             DateTime.Now.ToShortTimeString();
            Console.WriteLine("Graphics Device Reset Complete");
            Utils.white.Dispose();
            Utils.white = null;
            LoadRegenSafeContent();
            HasLoadedContent = true;
        }

        private void Graphics_DeviceResetting(object sender, EventArgs e)
        {
            HasLoadedContent = false;
        }

        private void Graphics_DeviceDisposing(object sender, EventArgs e)
        {
            HasLoadedContent = false;
        }

        private void Graphics_PreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            var currentDisplayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            var width = currentDisplayMode.Width;
            var height = currentDisplayMode.Height;
            graphics.PreferredBackBufferWidth = Math.Min(width, 4096);
            graphics.PreferredBackBufferHeight = Math.Min(height, 4096);
            graphics.PreferMultiSampling = true;
            resolutionSet = false;
            graphics.PreparingDeviceSettings -= graphicsPreparedHandler;
            CanLoadContent = true;
        }

        public void SetNewGraphics()
        {
            graphics.ApplyChanges();
            var screens = sman.GetScreens();
            var flag = false;
            string str1 = null;
            string str2 = null;
            for (var index = 0; index < screens.Length; ++index)
            {
                if (screens[index] is OS os)
                {
                    os.threadedSaveExecute();
                    flag = true;
                    str1 = os.SaveGameUserName;
                    str2 = os.SaveUserAccountName;
                    break;
                }
            }
            Components.Remove(sman);
            sman = new ScreenManager(this);
            Components.Add(sman);
            LoadGraphicsContent();
            if (flag)
            {
                OS.WillLoadSave = true;
                var os = new OS
                {
                    SaveGameUserName = str1,
                    SaveUserAccountName = str2
                };
                MainMenu.resetOS();
                sman.AddScreen(os, sman.controllingPlayer);
            }
            GuiData.spriteBatch = sman.SpriteBatch;
            if (sman.GetScreens().Length != 0)
                return;
            LoadInitialScreens();
        }

        public void SetWindowPosition()
        {
            if (SettingsLoader.isFullscreen)
                return;
            var form = (Form) Control.FromHandle(Window.Handle);
            form.Location = new Point(form.Location.X - 5, form.Location.Y - 5);
            Control.FromHandle(Window.Handle).FindForm().FormBorderStyle = FormBorderStyle.None;
        }

        protected override void Initialize()
        {
            sman = new ScreenManager(this);
            Components.Add(sman);
            graphics.PreferMultiSampling = true;
            NameGenerator.init();
            Helpfile.init();
            FileEntry.init(Content);
            PatternDrawer.init(Content);
            ProgramList.init();
            Cube3D.Initilize(graphics.GraphicsDevice);
            base.Initialize();
        }

        private void HandleExit(object sender, EventArgs e)
        {
            threadsExiting = true;
            MusicManager.stop();
        }

        private void LoadRegenSafeContent()
        {
            Utils.white = new Texture2D(graphics.GraphicsDevice, 1, 1);
            var data = new Color[1]
            {
                Color.White
            };
            Utils.white.SetData(data);
            var content = Content;
            Utils.gradient = content.Load<Texture2D>("Gradient");
            Utils.gradientLeftRight = content.Load<Texture2D>("GradientHorizontal");
        }

        protected override void LoadContent()
        {
            if (!CanLoadContent)
                return;
            PortExploits.populate();
            sman.controllingPlayer = PlayerIndex.One;
            if (Settings.isConventionDemo)
                SetWindowPosition();
            LoadGraphicsContent();
            LoadRegenSafeContent();
            var content = Content;
            GuiData.font = content.Load<SpriteFont>("Font23");
            GuiData.titlefont = content.Load<SpriteFont>("Kremlin");
            GuiData.smallfont = Content.Load<SpriteFont>("Font12");
            GuiData.UISmallfont = GuiData.smallfont;
            GuiData.tinyfont = Content.Load<SpriteFont>("Font10");
            GuiData.UITinyfont = GuiData.tinyfont;
            GuiData.detailfont = Content.Load<SpriteFont>("Font7");
            GuiData.spriteBatch = sman.SpriteBatch;
            GuiData.font = content.Load<SpriteFont>("Font23");
            GuiData.titlefont = content.Load<SpriteFont>("Kremlin");
            GuiData.smallfont = Content.Load<SpriteFont>("Font12");
            GuiData.UISmallfont = GuiData.smallfont;
            GuiData.tinyfont = Content.Load<SpriteFont>("Font10");
            GuiData.UITinyfont = GuiData.tinyfont;
            GuiData.detailfont = Content.Load<SpriteFont>("Font7");
            GuiData.init(Window);
            GuiData.InitFontOptions(Content);
            VehicleInfo.init();
            WorldLocationLoader.init();
            ThemeManager.init(content);
            MissionGenerationParser.init();
            MissionGenerator.init(content);
            UsernameGenerator.init();
            MusicManager.init(content);
            SFX.init(content);
            OldSystemSaveFileManifest.Load();
            SaveFileManager.Init();
            HasLoadedContent = true;
            LoadInitialScreens();
        }

        protected void LoadGraphicsContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            PostProcessor.init(graphics.GraphicsDevice, spriteBatch, Content);
            WebRenderer.init(graphics.GraphicsDevice);
        }

        protected void LoadInitialScreens()
        {
            if (Settings.MenuStartup)
                sman.AddScreen(new MainMenu(), sman.controllingPlayer);
            else
                sman.AddScreen(new OS(), sman.controllingPlayer);
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            if (!HasLoadedContent)
                LoadContent();
            if (!resolutionSet)
            {
                SetNewGraphics();
                resolutionSet = true;
            }
            PatternDrawer.update((float) gameTime.ElapsedGameTime.TotalSeconds);
            GuiData.setTimeStep((float) gameTime.ElapsedGameTime.TotalSeconds);
            MusicManager.Update((float) gameTime.ElapsedGameTime.TotalSeconds);
            ThemeManager.Update((float) gameTime.ElapsedGameTime.TotalSeconds);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            if (!HasLoadedContent)
                return;
            base.Draw(gameTime);
        }

        public static Game1 GetSingleton()
        {
            return singleton;
        }
    }
}