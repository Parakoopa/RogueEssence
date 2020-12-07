﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using RogueEssence;
using RogueEssence.Dev;
using Microsoft.Xna.Framework;
using Avalonia.Threading;
using System.Threading;
using RogueEssence.Data;
using RogueEssence.Content;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace RogueEssence.Dev.Views
{
    public class DevForm : Window, IRootEditor
    {
        public bool LoadComplete { get; private set; }

        //private MapEditor mapEditor;
        public GroundEditForm GroundEditForm;

        public IMapEditor MapEditor => null;
        public IGroundEditor GroundEditor { get { return GroundEditForm; } }
        public bool AteMouse { get { return false; } }
        public bool AteKeyboard { get { return false; } }

        private static Dictionary<string, string> devConfig;
        private static bool canSave;



        public DevForm()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        void IRootEditor.Load(GameBase game)
        {
            ExecuteOrInvoke(load);
        }

        private void load()
        {
            lock (GameBase.lockObj)
            {
                DevTileManager.Init();

                loadDevConfig();


                ViewModels.DevFormViewModel devViewModel = (ViewModels.DevFormViewModel)this.DataContext;

                devViewModel.Game.HideSprites = DataManager.Instance.HideChars;
                devViewModel.Game.HideObjects = DataManager.Instance.HideObjects;

                string[] skill_names = DataManager.Instance.DataIndices[DataManager.DataType.Skill].GetLocalStringArray();
                for (int ii = 0; ii < skill_names.Length; ii++)
                    devViewModel.Game.Skills.Add(ii.ToString("D3") + ": " + skill_names[ii]);
                devViewModel.Game.ChosenSkill = -1;
                devViewModel.Game.ChosenSkill = Math.Min(Math.Max(GetConfig("SkillChoice", 0), 0), devViewModel.Game.Skills.Count - 1);

                string[] intrinsic_names = DataManager.Instance.DataIndices[DataManager.DataType.Intrinsic].GetLocalStringArray();
                for (int ii = 0; ii < intrinsic_names.Length; ii++)
                    devViewModel.Game.Intrinsics.Add(ii.ToString("D3") + ": " + intrinsic_names[ii]);
                devViewModel.Game.ChosenIntrinsic = -1;
                devViewModel.Game.ChosenIntrinsic = Math.Min(Math.Max(GetConfig("IntrinsicChoice", 0), 0), devViewModel.Game.Intrinsics.Count - 1);

                string[] status_names = DataManager.Instance.DataIndices[DataManager.DataType.Status].GetLocalStringArray();
                for (int ii = 0; ii < status_names.Length; ii++)
                    devViewModel.Game.Statuses.Add(ii.ToString("D3") + ": " + status_names[ii]);
                devViewModel.Game.ChosenStatus = -1;
                devViewModel.Game.ChosenStatus = Math.Min(Math.Max(GetConfig("StatusChoice", 0), 0), devViewModel.Game.Statuses.Count - 1);

                string[] item_names = DataManager.Instance.DataIndices[DataManager.DataType.Item].GetLocalStringArray();
                for (int ii = 0; ii < item_names.Length; ii++)
                    devViewModel.Game.Items.Add(ii.ToString("D4") + ": " + item_names[ii]);
                devViewModel.Game.ChosenItem = -1;
                devViewModel.Game.ChosenItem = Math.Min(Math.Max(GetConfig("ItemChoice", 0), 0), devViewModel.Game.Items.Count - 1);

                string[] monster_names = DataManager.Instance.DataIndices[DataManager.DataType.Monster].GetLocalStringArray();
                for (int ii = 0; ii < monster_names.Length; ii++)
                    devViewModel.Player.Monsters.Add(ii.ToString("D3") + ": " + monster_names[ii]);
                devViewModel.Player.ChosenMonster = 0;


                string[] skin_names = DataManager.Instance.DataIndices[DataManager.DataType.Skin].GetLocalStringArray();
                for (int ii = 0; ii < DataManager.Instance.DataIndices[DataManager.DataType.Skin].Count; ii++)
                    devViewModel.Player.Skins.Add(skin_names[ii]);
                devViewModel.Player.ChosenSkin = 0;

                for (int ii = 0; ii < 3; ii++)
                    devViewModel.Player.Genders.Add(((Gender)ii).ToString());
                devViewModel.Player.ChosenGender = 0;

                for (int ii = 0; ii < GraphicsManager.Actions.Count; ii++)
                    devViewModel.Player.Anims.Add(GraphicsManager.Actions[ii].Name);
                devViewModel.Player.ChosenAnim = GraphicsManager.GlobalIdle;

                ZoneData zone = DataManager.Instance.GetZone(1);
                for (int ii = 0; ii < zone.GroundMaps.Count; ii++)
                    devViewModel.Travel.Grounds.Add(zone.GroundMaps[ii]);
                devViewModel.Travel.ChosenGround = Math.Min(Math.Max(GetConfig("MapChoice", 0), 0), devViewModel.Travel.Grounds.Count - 1);

                string[] dungeon_names = DataManager.Instance.DataIndices[DataManager.DataType.Zone].GetLocalStringArray();
                for (int ii = 0; ii < dungeon_names.Length; ii++)
                    devViewModel.Travel.Zones.Add(ii.ToString("D2") + ": " + dungeon_names[ii]);
                devViewModel.Travel.ChosenZone = Math.Min(Math.Max(GetConfig("ZoneChoice", 0), 0), devViewModel.Travel.Zones.Count - 1);

                devViewModel.Travel.ChosenStructure = Math.Min(Math.Max(GetConfig("StructChoice", 0), 0), devViewModel.Travel.Structures.Count - 1);

                devViewModel.Travel.ChosenFloor = Math.Min(Math.Max(GetConfig("FloorChoice", 0), 0), devViewModel.Travel.Floors.Count - 1);

                LoadComplete = true;
            }
        }


        public void Update(GameTime gameTime)
        {
            ExecuteOrInvoke(update);
        }

        private void update()
        {
            lock (GameBase.lockObj)
            {
                ViewModels.DevFormViewModel devViewModel = (ViewModels.DevFormViewModel)this.DataContext;
                devViewModel.Player.ChosenAnim = GraphicsManager.GlobalIdle;
                devViewModel.Player.UpdateLevel();
                if (GameManager.Instance.IsInGame())
                    devViewModel.Player.UpdateSpecies(Dungeon.DungeonScene.Instance.FocusedCharacter.BaseForm);
                if (GroundEditForm != null)
                {
                    ViewModels.GroundEditViewModel vm = (ViewModels.GroundEditViewModel)GroundEditForm.DataContext;
                    vm.Textures.TileBrowser.UpdateFrame();
                }

                if (canSave)
                    saveConfig();
            }
        }
        public void Draw() { }

        public void OpenGround()
        {
            GroundEditForm = new GroundEditForm();
            ViewModels.GroundEditViewModel vm = new ViewModels.GroundEditViewModel();
            GroundEditForm.DataContext = vm;
            vm.LoadFromCurrentGround();
            GroundEditForm.Show();
        }

        public void groundEditorClosed(object sender, EventArgs e)
        {
            GameManager.Instance.SceneOutcome = resetEditors();
        }


        private IEnumerator<YieldInstruction> resetEditors()
        {
            GroundEditForm = null;
            //mapEditor = null;
            yield return CoroutineManager.Instance.StartCoroutine(GameManager.Instance.RestartToTitle());
        }


        public void CloseGround()
        {
            if (GroundEditForm != null)
                GroundEditForm.Close();
        }



        void LoadGame()
        {
            // Windows - CAN run game in new thread, CAN run game in same thread via dispatch.
            // Mac - CANNOT run game in new thread, CAN run game in same thread via dispatch.
            // Linux - CAN run the game in new thread, CANNOT run game in same thread via dispatch.

            // When the game is started, it should run a continuous loop, blocking the UI
            // However, this is only happening on linux.  Why not windows and mac?
            // With Mac, cocoa can ONLY start the game window if it's on the main thread. Weird...

            if (CoreDllMap.OS == "windows" || CoreDllMap.OS == "osx")
                LoadGameDelegate();
            else
            {
                Thread thread = new Thread(LoadGameDelegate);
                thread.IsBackground = true;
                thread.Start();
            }
        }

        public static void ExecuteOrInvoke(Action action)
        {
            if (CoreDllMap.OS == "windows" || CoreDllMap.OS == "osx")
                action();
            else
                Dispatcher.UIThread.InvokeAsync(action, DispatcherPriority.Background);
        }

        void LoadGameDelegate()
        {
            DiagManager.Instance.DevEditor = this;
            using (GameBase game = new GameBase())
                game.Run();

            ExecuteOrInvoke(Close);
        }

        public void Window_Loaded(object sender, EventArgs e)
        {
            if (Design.IsDesignMode)
                return;
            //Thread thread = new Thread(LoadGame);
            //thread.IsBackground = true;
            //thread.Start();
            Dispatcher.UIThread.InvokeAsync(LoadGame, DispatcherPriority.Background);
            //LoadGame();
        }

        public void Window_Closed(object sender, EventArgs e)
        {
            DiagManager.Instance.LoadMsg = "Closing...";
            EnterLoadPhase(GameBase.LoadPhase.Unload);
        }

        public static void EnterLoadPhase(GameBase.LoadPhase loadState)
        {
            GameBase.CurrentPhase = loadState;
        }



        private static void loadDevConfig()
        {
            string configPath = GetConfigPath();
            string folderPath = Path.GetDirectoryName(configPath);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            devConfig = new Dictionary<string, string>();
            try
            {
                if (File.Exists(configPath))
                {
                    using (FileStream stream = File.OpenRead(configPath))
                    {
                        using (BinaryReader reader = new BinaryReader(stream))
                        {
                            while (reader.BaseStream.Position < reader.BaseStream.Length)
                            {
                                string key = reader.ReadString();
                                string val = reader.ReadString();
                                devConfig[key] = val;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DiagManager.Instance.LogError(ex);
            }
        }


        private static void saveConfig()
        {
            //save whole file
            try
            {
                using (var writer = new BinaryWriter(new FileStream(GetConfigPath(), FileMode.Create, FileAccess.Write, FileShare.None)))
                {
                    foreach (string curKey in devConfig.Keys)
                    {
                        writer.Write(curKey);
                        writer.Write(devConfig[curKey]);
                    }
                }
            }
            catch (Exception ex)
            {
                DiagManager.Instance.LogError(ex);
            }
        }

        public static string GetConfig(string key, string def)
        {
            string val;
            if (devConfig.TryGetValue(key, out val))
                return val;
            return def;
        }

        public static int GetConfig(string key, int def)
        {
            string val;
            if (devConfig.TryGetValue(key, out val))
            {
                int result;
                if (Int32.TryParse(val, out result))
                    return result;
            }
            return def;
        }

        public static void SetConfig(string key, int val)
        {
            SetConfig(key, val.ToString());
        }

        public static void SetConfig(string key, string val)
        {
            if (val == null && devConfig.ContainsKey(key))
                devConfig.Remove(key);
            else
                devConfig[key] = val;

            canSave = true;
        }

        public static string GetConfigPath()
        {
            switch (CoreDllMap.OS)
            {
                case "osx":
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "/Library/Application Support/RogueEssence/config");
                default:
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RogueEssence/config");
            }
        }
    }
}
