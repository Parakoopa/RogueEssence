﻿using System;
using System.Collections.Generic;
using System.Linq;
using RogueEssence.Data;
using RogueEssence.Ground;
using RogueEssence.Dungeon;
using Microsoft.Xna.Framework;
using NLua;
/*
* LuaEngine.cs
* 2017/06/24
* psycommando@gmail.com
* Description: Object managing the lua state, loading of scripts, and initializing the script interface.
*/

namespace RogueEssence.Script
{



    /// <summary>
    /// Class each components of the lua engine should implement
    /// </summary>
    public abstract class ILuaEngineComponent
    {
        /// <summary>
        /// Setups any extra functionalities for this object written on the lua side.
        /// </summary>
        public abstract void SetupLuaFunctions(LuaEngine state);
    }

    /**************************************************************************************
     * LuaEngine
     **************************************************************************************/
    /// <summary>
    /// Manager for the program-wide lua state. Init and de-init block!
    /// </summary>
    public partial class LuaEngine
    {
#region MAP_EVENTS
        /// <summary>
        /// The available callbacks a map's lua script may receive from the engine.
        /// </summary>
        public enum EMapCallbacks
        {
            Init = 0,    //When the map is not yet displayed and being setup
            Enter,          //When the map is just being displayed an the game fades-in
            Update         //When the game script engine ticks
        }

        /// <summary>
        /// Utility function for the EMapCallbacks enum. Allows iterating all the enum's values.
        /// Meant to be used with a foreach loop
        /// </summary>
        /// <returns>One of the enum value.</returns>
        public static IEnumerable<EMapCallbacks> EnumerateCallbackTypes()
        {
            yield return EMapCallbacks.Enter;
            yield return EMapCallbacks.Update;
            yield return EMapCallbacks.Init;
            yield break;
        }

        //Name for common map callback functions
        public static readonly string MapCurrentScriptSym = "CURMAPSCR";
        public static readonly string MapScriptEnterFun = "{0}.Enter";
        public static readonly string MapScriptUpdateFun = "{0}.Update";
        public static readonly string MapScriptInitFun = "{0}.Init";
        //The last one is optional, and is called before the map script is unloaded, so the script may do any needed cleanup
        public static readonly string MapCleanupFun = "{0}.Cleanup";

        /// <summary>
        /// Create the name of a map's expected callback function in its script.
        /// Each specifc callbacks has its own name and format.
        /// </summary>
        /// <param name="callbackformat"></param>
        /// <param name="mapname"></param>
        /// <returns></returns>
        public static string MakeMapScriptCallbackName(string mapname, EMapCallbacks callback)
        {
            switch (callback)
            {
                case EMapCallbacks.Enter:
                    return String.Format(MapScriptEnterFun, mapname);
                case EMapCallbacks.Update:
                    return String.Format(MapScriptUpdateFun, mapname);
                case EMapCallbacks.Init:
                    return String.Format(MapScriptInitFun, mapname);
                default:
                    {
                        throw new Exception("LuaEngine.MakeMapScriptCallbackName(): Unknown callback!");
                    }
            }
        }
        #endregion


        #region ZONE_EVENTS
        /// <summary>
        /// The available callbacks a zone's lua script may receive from the engine.
        /// </summary>
        public enum EZoneCallbacks
        {
            Init = 0,    //When the zone is not yet active and being setup
            //Enter,          //When the zone is just being entered
            ExitSegment,  //When a segment is exited by escape, defeat, completion, etc.
            AllyInteract,
            Rescued
        }

        public static readonly string ZoneCurrentScriptSym = "CURZONESCR";
        public static readonly string ZoneScriptInitFun = "{0}.Init";
        public static readonly string ZoneScriptExitSegmentFun = "{0}.ExitSegment";
        public static readonly string ZoneScriptAllyInteractFun = "{0}.AllyInteract";
        public static readonly string ZoneScriptRescuedFun = "{0}.Rescued";
        //The last one is optional, and is called before the map script is unloaded, so the script may do any needed cleanup
        public static readonly string ZoneCleanupFun = "{0}.Cleanup";

        /// <summary>
        /// Create the name of a map's expected callback function in its script.
        /// Each specifc callbacks has its own name and format.
        /// </summary>
        /// <param name="callbackformat"></param>
        /// <param name="mapname"></param>
        /// <returns></returns>
        public static string MakeZoneScriptCallbackName(string zonename, EZoneCallbacks callback)
        {
            switch (callback)
            {
                //case EZoneCallbacks.Enter:
                //    return String.Format(ZoneScriptEnterFun, zonename);
                case EZoneCallbacks.Init:
                    return String.Format(ZoneScriptInitFun, zonename);
                case EZoneCallbacks.ExitSegment:
                    return String.Format(ZoneScriptExitSegmentFun, zonename);
                case EZoneCallbacks.AllyInteract:
                    return String.Format(ZoneScriptAllyInteractFun, zonename);
                case EZoneCallbacks.Rescued:
                    return String.Format(ZoneScriptRescuedFun, zonename);
                default:
                    {
                        throw new Exception("LuaEngine.MakeZoneScriptCallbackName(): Unknown callback!");
                    }
            }
        }

        #endregion


        #region ENTITY_EVENT
        /// <summary>
        /// Types of events that an entity may have.
        /// </summary>
        public enum EEntLuaEventTypes
        {
            Action = 0,
            Touch = 1,
            Think = 2,
            Update = 3,
            EntSpawned = 4, //When a spawner spawned an entity
            Invalid,
        }
        
        public static readonly string ActionFun = "Action";
        public static readonly string TouchFun = "Touch";
        public static readonly string ThinkFun = "Think";
        public static readonly string UpdateFun = "Update";
        public static readonly string EntSpawnedFun = "EntSpawned";

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public static IEnumerator<EEntLuaEventTypes> IterateLuaEntityEvents()
        {
            yield return EEntLuaEventTypes.Action;
            yield return EEntLuaEventTypes.Touch;
            yield return EEntLuaEventTypes.Think;
            yield return EEntLuaEventTypes.Update;
            yield return EEntLuaEventTypes.EntSpawned;
        }

        /// <summary>
        ///
        /// </summary>
        public static readonly List<string> EntLuaEventTypeNames = new List<string>
        {
            ActionFun,
            TouchFun,
            ThinkFun,
            UpdateFun,
            EntSpawnedFun,
        };

        public static string MakeLuaEntityCallbackName(string entname, EEntLuaEventTypes type)
        {
            if (type < EEntLuaEventTypes.Invalid)
                return String.Format("{2}.{0}_{1}", entname, EntLuaEventTypeNames[(int)type], MapCurrentScriptSym);
            else
                throw new Exception("LuaEngine.MakeLuaEntityCallbackName(): Invalid Lua entity event type!");
        }
#endregion


#region SERVICES_EVENTS
        private static readonly string NameLuaServiceEventNames = "EngineServiceEvents";

        private enum EServiceEvents
        {
            Init = 0,
            Deinit,
            GraphicsLoad,
            GraphicsUnload,
            Restart,
            Update,

            GroundEntityInteract,

            DungeonModeBegin,
            DungeonModeEnd,
            DungeonFloorPrepare,
            DungeonFloorBegin,
            DungeonFloorEnd,

            ZoneInit,
            DungeonSegmentEnd,

            GroundModeBegin,
            GroundModeEnd,
            GroundMapEnter,
            GroundMapExit,

            //Keep last
            _NBEvents,
        };

        private IEnumerator<EServiceEvents> IterateServiceEvents()
        {
            for (int cntev = 0; cntev < (int)EServiceEvents._NBEvents; ++cntev)
                yield return (EServiceEvents)cntev;
        }
#endregion

#region MAIN_SCRIPTS
        /// <summary>
        /// Keyval to access the pre-defined script files
        /// </summary>
        enum EMainScripts
        {
            MAIN,
            COMMON,
            SCRIPTVARS,
        }

        /// <summary>
        /// List of predefined script files
        /// </summary>
        private static readonly IDictionary<EMainScripts, string> MainScripts = new Dictionary<EMainScripts, string>
        {
            {EMainScripts.MAIN,         "main.lua" },
            {EMainScripts.COMMON,       "common.lua"},
            {EMainScripts.SCRIPTVARS,   "scriptvars.lua"},              //this is the lua script that contains the default values of the ScriptVariables (Aka the variables that gets saved )
        };

        /// <summary>
        /// Assemble the path to the specified script
        /// </summary>
        /// <param name="script">Script to make the path for</param>
        /// <returns>The path to the script file.</returns>
        private string PathToScript(EMainScripts script)
        {
            string sciptp = MainScripts[script];
            return String.Format("{0}{1}", m_path, sciptp);
        }
        #endregion

        //Paths
        public static readonly string MapScriptPath = "ground.{0}";
        public static readonly string MapScriptDirectory = DataManager.SCRIPT_PATH + "ground/";
        public static readonly string MapPackageEntryPointName = "init"; //filename of the map's main script file that the engine will run when the map is loaded (by default lua package entrypoints are init.lua)

        public static readonly string ZoneScriptPath = "zone.{0}";
        public static readonly string ZoneScriptDirectory = DataManager.SCRIPT_PATH + "zone/";
        public static readonly string ZonePackageEntryPointName = "init"; //filename of the zone's main script file that the engine will run when the zone is loaded (by default lua package entrypoints are init.lua)

        //Global lua symbol names
        private static readonly string ScriptVariablesTableName = "SV"; //Name of the table of script variables that gets loaded and saved with the game

        //Lua State
        private Lua m_state;                            //Lua state/interpreter
        private string m_path = DataManager.SCRIPT_PATH;  //Base script engine scripts path
        private string m_cpath = DataManager.SCRIPT_CPATH; //Base script engine libraries, for so and dlls
        private ScriptServices m_scrsvc;
        private ScriptSound m_scriptsound;
        private ScriptGround m_scriptground;
        private ScriptGame m_scriptgame;
        private ScriptUI m_scriptUI;
        private ScriptDungeon m_scriptdungeon;
        private ScriptStrings m_scriptstr;
        private ScriptTask m_scripttask;
        private ScriptAI m_scriptai;

        //Engine time
        private TimeSpan m_nextUpdate;
        private GameTime m_curtime = new GameTime();

        //Properties
        public string CurZonePackagePath { get; internal set; } //Path to the currently loaded zone script, aka the last zone script to have been run
        public string   CurMapPackagePath { get; internal set; } //Path to the currently loaded map script, aka the last map script to have been run
        public Lua      LuaState { get { return m_state; } set { m_state = value; } }
        public GameTime Curtime { get { return m_curtime; } set { m_curtime = value; } }

        //Pre-compiled internal lua functions
        private LuaFunction m_MkCoIter;  //Instantiate a lua coroutine iterator function/state, for the ScriptEvent class mainly.
        private LuaFunction m_UnpackParamsAndRun;


        //==============================================================================
        // LuaEngine Initialization code
        //==============================================================================

        private static LuaEngine instance;
        public static void InitInstance()
        {
            instance = new LuaEngine();
        }
        public static LuaEngine Instance { get { return instance; } }

        /// <summary>
        /// Constructor private, since we don't want to instantiate this more than once! Otherwise bad things will happen.
        /// </summary>
        private LuaEngine()
        {
            Reset();
        }

        /// <summary>
        /// Handles importing the various .NET namespaces used in the project to lua.
        /// It uses reflection to do so.
        /// </summary>
        private void ImportDotNet()
        {
            DiagManager.Instance.LogInfo("[SE]:Importing .NET packages..");
            LuaState.LoadCLRPackage();

            m_state.DoString(String.Format("{0} = import '{0}'", "RogueEssence"));
            m_state.DoString(String.Format("{0} = luanet.namespace('{0}')", "RogueEssence.Content"));
            m_state.DoString(String.Format("{0} = luanet.namespace('{0}')", "RogueEssence.Data"));
            m_state.DoString(String.Format("{0} = luanet.namespace('{0}')", "RogueEssence.Dungeon"));
            m_state.DoString(String.Format("{0} = luanet.namespace('{0}')", "RogueEssence.Ground"));
            m_state.DoString(String.Format("{0} = luanet.namespace('{0}')", "RogueEssence.Script"));
            m_state.DoString(String.Format("{0} = luanet.namespace('{0}')", "RogueEssence.Menu"));
            m_state.DoString(String.Format("{0} = luanet.namespace('{0}')", "RogueEssence.LevelGen"));
            m_state.DoString(String.Format("{0} = luanet.namespace('{0}')", "RogueEssence.Resources"));
            m_state.DoString(String.Format("{0} = luanet.namespace('{0}')", "RogueEssence.Network"));
            m_state.DoString(String.Format("{0} = import '{0}'", "FNA"));
            m_state.DoString(String.Format("{0} = luanet.namespace('{0}')", "Microsoft"));
            m_state.DoString(String.Format("{0} = luanet.namespace('{0}')", "Microsoft.Xna"));
            m_state.DoString(String.Format("{0} = luanet.namespace('{0}')", "Microsoft.Xna.Framework"));
            m_state.DoString(String.Format("{0} = import '{0}'", "RogueElements"));
        }

        /// <summary>
        /// Clean the lua state
        /// Must call init methods manually again!!
        /// </summary>
        public void Reset()
        {
            //init lua
            LuaState = new Lua();
            LuaState.State.Encoding = System.Text.Encoding.UTF8;
            m_nextUpdate = new TimeSpan(-1);
            DiagManager.Instance.LogInfo(String.Format("[SE]:Initialized {0}", LuaState["_VERSION"] as string));

            ImportDotNet();
            m_scriptstr = new ScriptStrings();

            //Disable the import command, we could also rewrite it to only allow importing some specific things!
            //LuaState.DoString("import = function() end");
            //!#FIXME: disable  import again

            //Instantiate components
            m_nextUpdate = new TimeSpan(0);
            m_scrsvc = new ScriptServices(this);
            m_scriptsound = new ScriptSound();
            m_scriptgame = new ScriptGame();
            m_scriptground = new ScriptGround();
            m_scriptUI = new ScriptUI();
            m_scriptdungeon = new ScriptDungeon();
            m_scripttask = new ScriptTask();
            m_scriptai = new ScriptAI();

            //Expose symbols
            ExposeInterface();
            SetupGlobals();
            CacheMainScripts();

            DiagManager.Instance.LogInfo("[SE]: **- Lua engine ready! -**");
        }

        /// <summary>
        /// Calling this sends the OnInit event to the script engine.
        /// Use this if you just reset the script state, and want to force it to do its initialization.
        /// </summary>
        public void ReInit()
        {
            DiagManager.Instance.LogInfo("[SE]:Re-initializing scripts!");
            DiagManager.Instance.LogInfo("[SE]:Loading last serialized script variables!");
            if (DataManager.Instance.Save != null)
                LoadSavedData(DataManager.Instance.Save);
            if (ZoneManager.Instance != null)
                ZoneManager.Instance.LuaEngineReload();

            //!#FIXME : We'll need to call the method for zone init too!
            //!#FIXME : We'll need to call the method on map entry too if a map is running!
            if (ZoneManager.Instance.CurrentGround != null)
            {
                OnGroundMapInit(ZoneManager.Instance.CurrentGround.AssetName, ZoneManager.Instance.CurrentGround);
                OnGroundModeBegin();

                //process events before the map fades in
                GameManager.Instance.SceneOutcome = ZoneManager.Instance.CurrentGround.OnInit();
            }
            else if (ZoneManager.Instance.CurrentMap != null)
            {
                //OnDungeonFloorInit();
                throw new NotImplementedException("LuaEngine.ReInit() not implemented for dungeon!");
            }
        }

        /// <summary>
        /// Define some globals for commonly used types from the project
        /// </summary>
        private void DefineDotNetTypes()
        {
            //!REMOVEME: Probably not needed anymore, beyond just being convenient!
            DiagManager.Instance.LogInfo("[SE]:Force-exposing dotnet types..");
            RunString(@"
            FrameType   = luanet.import_type('RogueEssence.Content.FrameType')
            DrawLayer   = luanet.import_type('RogueEssence.Content.DrawLayer')
            MonsterID       = luanet.import_type('RogueEssence.Dungeon.MonsterID')
            Gender      = luanet.import_type('RogueEssence.Data.Gender')
            Direction   = luanet.import_type('RogueElements.Dir8')
            GameTime    = luanet.import_type('Microsoft.Xna.Framework.GameTime')
            Color       = luanet.import_type('Microsoft.Xna.Framework.Color')
            ActivityType = luanet.import_type('RogueEssence.Network.ActivityType')
            TimeSpan    = luanet.import_type('System.TimeSpan')
            ");

        }


        /// <summary>
        /// Set lua package paths to the ones in the game's files.
        /// </summary>
        private void SetLuaPaths()
        {
            DiagManager.Instance.LogInfo("[SE]:Setting up lua paths..");
            //Add the current script directory to the lua searchpath for loading modules!
            LuaState["package.path"] = LuaState["package.path"] + ";" +
                                        System.IO.Path.GetFullPath(m_path) + "lib/?.lua;" +
                                        System.IO.Path.GetFullPath(m_path) + "?.lua;" +
                                        System.IO.Path.GetFullPath(m_path) + "?/init.lua";

            //Add lua binary path
            string cpath = LuaState["package.cpath"] + ";" + System.IO.Path.GetFullPath(m_cpath);
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                cpath += "?.dll";
            else
                cpath += "?.so";
            LuaState["package.cpath"] = cpath;
        }

        /// <summary>
        /// Expose the list of available service callbacks names to Lua. It should make it easier for script devs to get a list of them to fiddle with.
        /// </summary>
        private void FillServiceEventsTable()
        {
            LuaTable availables = RunString("return {}").First() as LuaTable;
            LuaFunction fnAddToTable = RunString("return function(tbl, name, item) tbl[name] = item end").First() as LuaFunction;
            var svcev = IterateServiceEvents();
            while (svcev.MoveNext())
                fnAddToTable.Call(availables, svcev.Current.ToString(), svcev.Current.ToString());
            LuaState[NameLuaServiceEventNames] = availables;
        }

        /// <summary>
        /// Add all the required global variables to the lua environment!
        /// </summary>
        private void SetupGlobals()
        {
            DiagManager.Instance.LogInfo("[SE]:Setting up lua globals..");

            SetLuaPaths();

            //Setup some globabl vars
            LuaState["_SCRIPT_PATH"] = System.IO.Path.GetFullPath(m_path); //Share with the script engine the path to the root of the script files

            RunString(ZoneCurrentScriptSym + " = nil");
            RunString(MapCurrentScriptSym + " = nil");

            //Make empty script variable table
            LuaState.NewTable(ScriptVariablesTableName);
            LuaState[ScriptVariablesTableName + ".__ALL_SET"] = "OK"; //This is just a debug variable, to make sure the table isn't overwritten at runtime by something else, and help track down issues linked to that.

            //Add the list of available Callbacknames
            FillServiceEventsTable();

            //Add Dotnet types
            DefineDotNetTypes();

            RunString("require 'CLRPackage'");

            RunString(@"
                __GetLevel = function()
                    local curground = RogueEssence.Dungeon.ZoneManager.Instance.CurrentGround
                    local curmap    = RogueEssence.Dungeon.ZoneManager.Instance.CurrentMap

                    if curground then
                      return curground
                    elseif curmap then
                      return curmap
                    end
                    return nil
                  end
            ", "__GetLevel function init");

            //Setup lookup functions
            //Character lookup
            RunString(@"
                function CH(charname)
                    local curlvl = __GetLevel()
                    if curlvl then
                      return curlvl:GetChar(charname)
                    end
                    return nil
                  end
            ", "CH function init");

            //Object lookup
            RunString(@"
                OBJ = function(name)
                    if _ZONE.CurrentGround then
                      return _ZONE.CurrentGround:GetObj(name)
                    elseif _ZONE.CurrentMap then
                      assert(false, 'OBJ(' .. name .. '): unimplemented on dungeon maps!!!!')
                    end
                    return nil
                  end
            ", "OBJ function init");

            //Character localized name
            RunString(@"
                CHName = function(name)
                    if _ZONE.CurrentGround then
                      local ch = _ZONE.CurrentGround:GetChar(name)
                      if ch then
                          return ch:GetDisplayName()
                      end
                    elseif _ZONE.CurrentMap then
                      assert(false, 'CHName(' .. name .. '): unimplemented on dungeon maps!!!!')
                    end
                    return nil
                  end
            ", "CHName function init");

            //This returns a MonsterID
            RunString(@"
                IDX = function(id, ...)
                        local param = {...}
                        local form  = param[0]
                        local skin = param[1]
                        local gender= param[2]

                        if not form then form = 0 end
                        if not skin then skin = 0 end
                        if not gender then gender = Gender.Male end
                        return MonsterID(id, form, skin, gender)
                    end
            ", "IDX function init");

            //Function to lookup a NPC spawner by name on the current level
            RunString(@"
                SPWN = function(spawnername)
                    local curlvl = __GetLevel()
                    if curlvl then
                      return curlvl:GetSpanwer(spawnername)
                    end
                    return nil
                end
            ", "SPWN function init");

            //Expose the lua system
            LuaState["LUA_ENGINE"] = this;

        }


        /// <summary>
        /// Call this on map changes to ensure that the script engine has access to those .NET globals!
        /// </summary>
        private void ExposeInterface()
        {
            DiagManager.Instance.LogInfo("[SE]:Exposing game engine components instances..");
            //Expose directly parts of the engine
            LuaState[ScriptServices.SInterfaceInstanceName] = m_scrsvc;

            LuaState["_GROUND"] = GroundScene.Instance;
            LuaState["_DUNGEON"] = DungeonScene.Instance;
            LuaState["_ZONE"] = ZoneManager.Instance;
            LuaState["_GAME"] = GameManager.Instance;
            LuaState["_DATA"] = DataManager.Instance;

            DiagManager.Instance.LogInfo("[SE]:Exposing script interface..");
            //Expose script interface  objects
            LuaState["GROUND"] = m_scriptground;
            LuaState["DUNGEON"] = m_scriptdungeon;
            LuaState["GAME"] = m_scriptgame;
            m_scriptgame.SetupLuaFunctions(this); //Need to do this at this very moment.
            LuaState["SOUND"] = m_scriptsound;
            LuaState["UI"] = m_scriptUI;
            LuaState["STRINGS"] = m_scriptstr;
            LuaState["TASK"] = m_scripttask;
            LuaState["AI"] = m_scriptai;

        }

        /// <summary>
        /// Since some  instance of the game's internal change on map load and various event, this function is here to update them.
        /// </summary>
        public void UpdateExposedInstances()
        {
            //LuaState["_GROUND"] = GroundScene.Instance;
            //LuaState["_DUNGEON"] = DungeonScene.Instance;
            //LuaState["_ZONE"] = ZoneManager.Instance;
            //LuaState["_GAME"] = GameManager.Instance;
            //LuaState["_DATA"] = DataManager.Instance;
        }
        public void UpdateZoneInstance()
        {
            LuaState["_ZONE"] = ZoneManager.Instance;
        }


        private void SetupLuaFunctions()
        {
            //Add the function to make iterators on coroutines
            m_MkCoIter = RunString(
                @"return function(fun,...)
                      local arguments = {...}
                      local co = coroutine.create(function() xpcall( fun, PrintStack, table.unpack(arguments)) end)
                      return function ()   -- iterator
                        local code, res = coroutine.resume(co)
                        if code == false then --This means there was an error in there
                            assert(false,'Error running coroutine ' .. tostring(fun) .. '! :\n' .. PrintStack(res))
                        end
                        return res
                      end
                    end",
            "MakeCoroutineIterator").First() as LuaFunction;

            m_UnpackParamsAndRun = RunString(
                @"return function(fun, params)
                    local size = params.Length
                    local transittbl = {}
                    print('Length == ' .. tostring(params.Length))
                    local i = 0
                    while i < size do
                        table.insert(transittbl, params[i])
                        i = i + 1
                    end
                    return fun(table.unpack(transittbl))
                end",
                "UnpackParamsAndRun").First() as LuaFunction;
        }

        /// <summary>
        /// Preload the script files we expect to be there!
        /// </summary>
        private void CacheMainScripts()
        {
            DiagManager.Instance.LogInfo("[SE]:Caching scripts..");
            m_scrsvc.SetupLuaFunctions(this);
            //Cache default script variables
            LuaState.DoFile(PathToScript(EMainScripts.SCRIPTVARS));
            //Cache common lib
            LuaState.LoadFile(PathToScript(EMainScripts.COMMON));

            //Install misc lua functions each interfaces needs
            DiagManager.Instance.LogInfo("[SE]:Installing game interface functions..");
            SetupLuaFunctions();
            //m_scrco.SetupLuaFunctions(this);
            m_scriptstr.SetupLuaFunctions(this);
            m_scriptsound.SetupLuaFunctions(this);
            m_scriptground.SetupLuaFunctions(this);
            m_scriptUI.SetupLuaFunctions(this);
            m_scriptdungeon.SetupLuaFunctions(this);
            m_scripttask.SetupLuaFunctions(this);
            m_scriptai.SetupLuaFunctions(this);

            //If script vars aren't initialized in the save, do it now!
            DiagManager.Instance.LogInfo("[SE]:Checking if we need to initialize the script variables saved state..");
            if (DataManager.Instance != null && DataManager.Instance.Save != null && String.IsNullOrEmpty(DataManager.Instance.Save.ScriptVars))
                SaveData(DataManager.Instance.Save);

            //Run main script
            DiagManager.Instance.LogInfo(String.Format("[SE]:Running {0} script..", MainScripts[EMainScripts.MAIN]));
            LuaState.DoFile(PathToScript(EMainScripts.MAIN));
        }


        /// <summary>
        /// Use this to un-serialize the script variables and load them.
        /// </summary>
        public void LoadSavedData(GameProgress loaded)
        {
            //Do stuff when resuming from a save!
            DiagManager.Instance.LogInfo("LuaEngine.LoadSavedData()..");
            if ( loaded == null || loaded.ScriptVars == null)
            {
                LuaState.DoFile(PathToScript(EMainScripts.SCRIPTVARS));
            }
            else
            {
                DiagManager.Instance.LogInfo("============ Deserialized SV : ============");
                DiagManager.Instance.LogInfo(loaded.ScriptVars);
                DiagManager.Instance.LogInfo("===========================================");

                //Deserialize the script variables and put them in the global
                RunString(String.Format(@"local ok,res = Serpent.load('{0}')
            if ok then
                {1} = res
            else
                PrintInfo('LuaEngine.LoadSavedData(): Script var loading failed! Details : ' .. res)
            end
            ",
                    loaded.ScriptVars, ScriptVariablesTableName));
            }
            //Tell the script we've just resumed a save!
            m_scrsvc.Publish("LoadSavedData");
        }

        /// <summary>
        /// Use this to serialize the script variables and place the serialized data into the current GameProgress.
        /// </summary>
        public void SaveData(GameProgress save)
        {
            //First tell the script we're gonna save
            m_scrsvc.Publish("SaveData");

            //Save script engine stuff here!
            DiagManager.Instance.LogInfo("LuaEngine.SaveData()..");
            object[] result = RunString(String.Format("return Serpent.dump({0})", ScriptVariablesTableName));
            if (result[0] != null)
            {
                save.ScriptVars = result[0] as string;
                DiagManager.Instance.LogInfo("============ Serialized SV : ============");
                DiagManager.Instance.LogInfo(save.ScriptVars);
                DiagManager.Instance.LogInfo("=========================================");
            }
            else
                DiagManager.Instance.LogInfo("LuaEngine.SaveData(): Script var saving failed!");
        }

        /// <summary>
        /// Struct used for defining an entry to be imported by the script engine.
        /// </summary>
        private struct ImportEntry
        {
            String m_Assembly;
            String m_Namespace;

            public ImportEntry(String asm, String nspace)
            {
                m_Assembly = asm;
                m_Namespace = nspace;
            }

            public bool hasAssembly()
            {
                return (m_Assembly!= null && m_Assembly.Length > 0) &&
                       (m_Namespace != null && m_Namespace.Length > 0);
            }

            public string Namespace { get { return m_Namespace;} set { m_Namespace = value;} }
            public string Assembly { get { return m_Assembly; } set { m_Assembly = value; } }
        }
    }


    /**************************************************************************************
     * LuaEngine
     **************************************************************************************/
    /// <summary>
    /// Manager for the program-wide lua state.
    /// Acces to interpreter.
    /// </summary>
    partial class LuaEngine
    {

        /// <summary>
        /// Instantiate a lua module's Class table using its metatable's "__call" definition
        /// </summary>
        /// <param name="classpath"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public LuaTable InstantiateLuaModule(string modulepath, params object[] args)
        {
            try
            {
                LuaTable luaclass = RunString(String.Format("return require '{0}'", modulepath)).First() as LuaTable;
                //LuaTable tbl = LuaState.GetTable(classpath);
                if (luaclass != null)
                {
                    //LuaFunction DoInstantiate =   RunString("return function(srcclass, ...) return srcclass(...) end").First() as LuaFunction;
                    //LuaFunction fn =  luaclass["__call"] as LuaFunction;
                    //if (fn != null)
                    //    return fn.Call(args).First() as LuaTable;
                    //if(DoInstantiate != null)
                    //{
                        return m_UnpackParamsAndRun.Call(luaclass, args).First() as LuaTable;
                    //}
                }
            }
            catch (Exception ex)
            {
                DiagManager.Instance.LogInfo(String.Format("LuaEngine.InstantiateLuaClass(): Error instantiating class \"{0}\"!\n{1}", modulepath, ex.Message));
            }
            return null;
        }

        /// <summary>
        /// Allow calling a lua function of a lua table/object with the specified parameters, and returns the result.
        /// </summary>
        /// <param name="objname">Path of the lua object instance whose method we'll call.</param>
        /// <param name="funname">Name of the method of the lua object instance to call.</param>
        /// <param name="args">Parameters to pass the method (excluding "self")</param>
        /// <returns>Returns the array of objects that the lua interpreter returns after executing the method.</returns>
        public object[] CallLuaMemberFun(string objname, string funname, params object[] args)
        {
            string fpath = objname + "." + funname;
            LuaFunction fun = LuaState.GetFunction(fpath);
            if (fun == null)
            {
                DiagManager.Instance.LogInfo("[SE]:LuaEngine.CallLuaMemberFun(): Tried to call undefined method " + fpath + "!");
                return null;
            }
            List<object> ar = (args == null) ? new List<object>() : new List<object>(args);
            ar.Insert(0, LuaState[objname]);
            try
            {
                return m_UnpackParamsAndRun.Call(fun, ar.ToArray()); //fun.Call(ar.ToArray()); //We need to pass the "self" parameter first
            }
            catch (Exception e)
            {
                DiagManager.Instance.LogInfo("[SE]:LuaEngine.CallLuaMemberFun(): Error calling member function :\n" + e.Message);
            }
            return null;
        }


        /// <summary>
        /// Calls a lua function of the given name, with the given arguments, and returns its return value(s).
        /// </summary>
        /// <param name="path">Path of the function to call.</param>
        /// <param name="args">Parameters to pass the function being called.</param>
        /// <returns>Returns the array of objects that the lua interpreter returns after executing the method.</returns>
        public object[] CallLuaFunctions(string path, params object[] args)
        {
            var scriptFunc = LuaState[path] as LuaFunction;
            if (scriptFunc == null)
            {
                DiagManager.Instance.LogInfo("[SE]:LuaEngine.CallLuaFunctions(): Tried to call undefined function " + path + "!");
                return null;
            }

            try
            {
                return m_UnpackParamsAndRun.Call(scriptFunc, args);
                //return scriptFunc.Call(args);
            }
            catch (Exception e)
            {
                DiagManager.Instance.LogInfo("[SE]:LuaEngine.CallLuaFunctions(): Error calling function :\n" + e.Message);
            }
            return null;
        }

        /// <summary>
        /// Makes the lua interpreters execute the given string as lua code.
        /// </summary>
        /// <param name="luatxt">Lua code  to execute.</param>
        /// <returns>Object array containing the return value of the string's execution.</returns>
        public object[] RunString(string luatxt, string chunkname = "chunk")
        {
            //Pretty basic so far, but ideally this should be a lot more sophisticated.
            // Might need to inject things in the lua stack too
            try
            {
                return LuaState.DoString(luatxt, chunkname);
            }
            catch (Exception e)
            {
                DiagManager.Instance.LogInfo("[SE]:LuaEngine.RunString(): Error executing string:\n" + e.Message + "\nContent:\n" + luatxt);
            }
            return null;
        }

        /// <summary>
        /// Makes the full absolute path to the directory a map's script should be in.
        /// </summary>
        /// <param name="mapname">AssetName of the map to look for.</param>
        /// <returns>Absolute path to the map's script directory.</returns>
        public string _MakeZoneScriptPath(string zonename)
        {
            return System.IO.Path.GetFullPath(m_path) +
                   string.Format(ZoneScriptPath, zonename).Replace('.', '/'); //The physical path to the map's script dir
        }

        /// <summary>
        /// Load and execute the script of a zone.
        /// </summary>
        /// <param name="zoneassetname">The AssetName of the zone for which we have to load the script of</param>
        public void RunZoneScript(string zoneassetname)
        {
            string zonepath = _MakeZoneScriptPath(zoneassetname);
            try
            {
                string abspath = System.IO.Path.GetFullPath(zonepath + "/init.lua");
                m_state.LoadFile(abspath);
                RunString(String.Format("{2} = require('{0}'); {1} = {2}", string.Format(ZoneScriptPath, zoneassetname), ZoneCurrentScriptSym, zoneassetname),
                          abspath);
                CurZonePackagePath = zonepath; //Set this only on success
            }
            catch (Exception e)
            {
                DiagManager.Instance.LogInfo("[SE]:LuaEngine.RunZoneScript(): Error running zone script!:\n" + e.Message + "\npath:\n" + zonepath);
            }
        }

        /// <summary>
        /// Use this to clean up the traces left behind by a zone package.
        /// Also collects garbages.
        /// </summary>
        /// <param name="zoneassetname"></param>
        public void CleanZoneScript(string zoneassetname)
        {
            RunString(@"
                local tbllen = 0
                for k,v in pairs(_ENV) do
                    tbllen = tbllen + 1
                end
                print('=>_ENV pre-cleanup:' .. tostring(tbllen))
            ");

            RunString(
                    String.Format(@"
                        if {2} then
                            xpcall( {2}, PrintStack)
                        end
                        package.loaded.{0} = nil
                        {1} = nil
                        {0} = nil
                        collectgarbage()", zoneassetname, ZoneCurrentScriptSym, String.Format(ZoneCleanupFun, ZoneCurrentScriptSym)),
                      "CleanZoneScript");

            RunString(@"
                local tbllen = 0
                for k,v in pairs(_ENV) do
                    tbllen = tbllen + 1
                end
                print('=>_ENV post cleanup:' .. tostring(tbllen))
            ");
        }

        /// <summary>
        /// Makes the full absolute path to the directory a map's script should be in.
        /// </summary>
        /// <param name="mapname">AssetName of the map to look for.</param>
        /// <returns>Absolute path to the map's script directory.</returns>
        public string _MakeMapScriptPath(string mapname)
        {
            return System.IO.Path.GetFullPath(m_path) +
                   string.Format(MapScriptPath, mapname).Replace('.', '/'); //The physical path to the map's script dir
        }

        /// <summary>
        /// Load and execute the script of a map.
        /// </summary>
        /// <param name="mapassetname">The AssetName of the map for which we have to load the script of</param>
        public void RunMapScript(string mapassetname)
        {
            string mappath = _MakeMapScriptPath(mapassetname);
            try
            {
                string abspath = System.IO.Path.GetFullPath(mappath + "/init.lua");
                m_state.LoadFile(abspath);
                RunString(String.Format("{2} = require('{0}'); {1} = {2}", string.Format(MapScriptPath, mapassetname), MapCurrentScriptSym, mapassetname),
                          abspath);
                CurMapPackagePath = mappath; //Set this only on success
            }
            catch (Exception e)
            {
                DiagManager.Instance.LogInfo("[SE]:LuaEngine.RunMapScript(): Error running map script!:\n" + e.Message + "\npath:\n" + mappath);
            }
        }

        /// <summary>
        /// Use this to clean up the traces left behind by a map package.
        /// Also collects garbages.
        /// </summary>
        /// <param name="mapassetname"></param>
        public void CleanMapScript(string mapassetname)
        {
            RunString(@"
                local tbllen = 0
                for k,v in pairs(_ENV) do
                    tbllen = tbllen + 1
                end
                print('=>_ENV pre-cleanup:' .. tostring(tbllen))
            ");

            RunString(
                    String.Format(@"
                        if {2} then
                            xpcall( {2}, PrintStack)
                        end
                        package.loaded.{0} = nil
                        {1} = nil
                        {0} = nil
                        collectgarbage()", mapassetname, MapCurrentScriptSym, String.Format(MapCleanupFun, MapCurrentScriptSym)),
                      "CleanMapScript");

            RunString(@"
                local tbllen = 0
                for k,v in pairs(_ENV) do
                    tbllen = tbllen + 1
                end
                print('=>_ENV post cleanup:' .. tostring(tbllen))
            ");
        }

        /// <summary>
        /// Creates the bare minimum script and map folder for a ground map.
        /// </summary>
        /// <param name="mapassetname"></param>
        public void CreateNewMapScriptDir(string mapassetname)
        {
            string mappath = _MakeMapScriptPath(mapassetname);
            try
            {
                //Check if files exists already
                if (!System.IO.Directory.Exists(mappath))
                    System.IO.Directory.CreateDirectory(mappath);

                string packageentry = String.Format("{1}/{0}.lua", MapPackageEntryPointName, mappath);
                if (!System.IO.File.Exists(packageentry))
                {
                    using (var fstream = System.IO.File.CreateText(packageentry))
                    {
                        //Insert comment header
                        fstream.WriteLine(
                        @"
                        --[[
                            {0}
                            Created: {2}
                            Description: Autogenerated script file for the map {1}.
                        ]]--
                        -- Commonly included lua functions and data
                        require 'common'

                        -- Package name
                        local {1} = {}

                        -- Local, localized strings table
                        -- Use this to display the named strings you added in the strings files for the map!
                        -- Ex:
                        --      local localizedstring = MapStrings['SomeStringName']
                        local MapStrings = {}

                        -------------------------------
                        -- Map Callbacks
                        -------------------------------
                    ", MapPackageEntryPointName + ".lua", mapassetname, DateTime.Now.ToString());

                        //Insert the default map functions and comment header
                        foreach (EMapCallbacks fn in EnumerateCallbackTypes())
                        {
                            string callbackname = MakeMapScriptCallbackName(mapassetname, fn);
                            fstream.WriteLine("---{0}\n--Engine callback function\nfunction {0}(map, time)\n", callbackname);
                            if (fn == EMapCallbacks.Init)
                            {
                                //Add the map string loader
                                fstream.WriteLine(
                                @"
                            --This will fill the localized strings table automatically based on the locale the game is 
                            -- currently in. You can use the MapStrings table after this line!
                            MapStrings = AutoLoadLocalizedStrings()
                            ");
                            }
                            fstream.WriteLine("\nend\n");
                        }

                        fstream.WriteLine(
                        @"
                        -------------------------------
                        -- Entities Callbacks
                        -------------------------------


                        return {0}
                    ", mapassetname);

                        fstream.Flush();
                        fstream.Close();
                    }
                }


            }
            catch (Exception e)
            {
                DiagManager.Instance.LogInfo("[SE]:LuaEngine.CreateNewMapScriptDir(): Error creating map package!!:\n" + e.Message + "\npath:\n" + mappath);
            }
        }

        /// <summary>
        /// Creates a wrapped coroutine that works like a lua iterator.
        /// Each call to the returned function will call resume on the wrapped coroutine, and resume from where it left off.
        /// </summary>
        /// <param name="luapath">Path to the lua function. Ex: "Mytable.ObjectInstance.luafunction"</param>
        /// <param name="arguments">Arguments to pass the function on call. Those will be wrapped into the lua iterator function.</param>
        /// <returns></returns>
        public LuaFunction CreateCoroutineIterator(string luapath, params object[] arguments)
        {
            LuaFunction luafun = LuaState.GetFunction(luapath);
            if (luafun != null)
                return CreateCoroutineIterator(luafun, arguments);
            else
                throw new Exception(String.Format("LuaEngine.CreateCoroutineIterator(): NLua returned null for the function path \"{0}\"!", luapath));
        }

        /// <summary>
        /// Creates a wrapped coroutine that works like a lua iterator.
        /// Each call to the returned function will call resume on the wrapped coroutine, and resume from where it left off.
        /// </summary>
        /// <param name="luafun">LuaFunction the iterator should run!</param>
        /// <param name="arguments">Arguments to pass the function on call. Those will be wrapped into the lua iterator function.</param>
        /// <returns></returns>
        public LuaFunction CreateCoroutineIterator(LuaFunction luafun, params object[] arguments)
        {
            object iter = null;
            try
            {
                //!NOTE: I had to make this because the arguments were being passed as a single table of userdata. Which isn't practical at all.
                //!      So I made this to try to unpack up to 10 parameters for a lua function. I doubt we'll ever need that many parameters, but, at least its all there!
                if (arguments.Count() == 0)
                    iter = m_MkCoIter.Call(luafun).First();
                else if (arguments.Count() == 1)
                    iter = m_MkCoIter.Call(luafun, arguments[0]).First();
                else if (arguments.Count() == 2)
                    iter = m_MkCoIter.Call(luafun, arguments[0], arguments[1]).First();
                else if (arguments.Count() == 3)
                    iter = m_MkCoIter.Call(luafun, arguments[0], arguments[1], arguments[2]).First();
                else if (arguments.Count() == 4)
                    iter = m_MkCoIter.Call(luafun, arguments[0], arguments[1], arguments[2], arguments[3]).First();
                else if (arguments.Count() == 5)
                    iter = m_MkCoIter.Call(luafun, arguments[0], arguments[1], arguments[2], arguments[3], arguments[4]).First();
                else if (arguments.Count() == 6)
                    iter = m_MkCoIter.Call(luafun, arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5]).First();
                else if (arguments.Count() == 7)
                    iter = m_MkCoIter.Call(luafun, arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5], arguments[6]).First();
                else if (arguments.Count() == 8)
                    iter = m_MkCoIter.Call(luafun, arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5], arguments[6], arguments[7]).First();
                else if (arguments.Count() == 9)
                    iter = m_MkCoIter.Call(luafun, arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5], arguments[6], arguments[7], arguments[8]).First();
                else if (arguments.Count() == 10)
                    iter = m_MkCoIter.Call(luafun, arguments[0], arguments[1], arguments[2], arguments[3], arguments[4], arguments[5], arguments[6], arguments[7], arguments[8], arguments[9]).First();
                else if (arguments.Count() > 10)
                {
                    throw new OverflowException("Function has too many paramters!!!");
                }
            }
            catch (Exception e)
            {
                DiagManager.Instance.LogInfo("LuaEngine.CreateCoroutineIterator(): Failed to create coroutine : " + e.Message);
            }
            return iter as LuaFunction;
        }


        /// <summary>
        /// Checks if a Lua function exists in the current state.
        /// </summary>
        /// <param name="luapath"></param>
        /// <returns></returns>
        public bool DoesFunctionExists(string luapath)
        {
            try
            {
                if (String.IsNullOrEmpty(luapath))
                {
                    DiagManager.Instance.LogInfo("[SE]:LuaEngine.DoesFunctionExists(): Empty function path!");
                    return false;
                }

                string[] splitted = luapath.Split('.');
                string curp = "";
                foreach (string s in splitted )
                {
                    curp += s;
                    if (LuaState[curp] == null)
                        return false;
                    curp += ".";
                }

                return true;
            }
            catch (Exception e)
            {
                DiagManager.Instance.LogInfo("[SE]:LuaEngine.DoesFunctionExists(): Error looking for function!: " + luapath + "\npath:\n" + e.Message);
                return false;
            }
        }

    };

    //Lua accessible functions
    partial class LuaEngine
    {


        /// <summary>
        /// Makes a .net Action to be used in lua
        /// </summary>
        /// <param name="fun"></param>
        /// <returns></returns>
        public Action MakeLuaAction( LuaFunction fun, params object[] param )
        {
            return new Action( ()=>{ fun.Call(param); } );
        }

        //public dynamic LuaCast(object val, object t)
        //{
        //    if (t.GetType().IsEquivalentTo(typeof(NLua.ProxyType)))
        //    {
        //        Type orig = val.GetType();
        //        var castedorig = Convert.ChangeType(val, orig);
        //        Type destTy = ((NLua.ProxyType)t).UnderlyingSystemType;
        //        var result = Convert.ChangeType(castedorig, destTy);
        //        return result;
        //    }
        //    else
        //    {
        //        return Convert.ChangeType((object)val, t.GetType());
        //    }
        //}

        //public Type TypeOf(object v)
        //{
        //    if (v.GetType().IsEquivalentTo(typeof(NLua.ProxyType)))
        //    {
        //        return ((ProxyType)v).UnderlyingSystemType;
        //    }
        //    else if (v.GetType() == typeof(NLua.ProxyType))
        //    {
        //        return ((ProxyType)v).UnderlyingSystemType;
        //    }
        //    else
        //        return v.GetType();
        //}

        /// <summary>
        /// Utility function for returning a dummy yield through the lua layer.
        /// </summary>
        /// <returns></returns>
        public static IEnumerator<YieldInstruction> _DummyWait()
        {
            yield break;
        }


        //
        // Common Casts
        //
        public GroundChar CastToGroundChar(GroundEntity o)
        {
            return (GroundChar)o;
        }

        public GroundAIUser CastToGroundAIUser(GroundEntity o)
        {
            return (GroundAIUser)o;
        }

        public GroundObject CastToGroundObject(GroundEntity o)
        {
            return (GroundObject)o;
        }

        public BaseTaskUser CastToBaseTaskUser(object o)
        {
            return (BaseTaskUser)o;
        }

    }

    /**************************************************************************************
     * LuaEngine
     **************************************************************************************/
    /// <summary>
    /// Manager for the program-wide lua state.
    /// Engine services!
    /// </summary>
    partial class LuaEngine
    {
        /// <summary>
        /// Call this when DataManager is being initialized
        /// </summary>
        public void OnDataLoad()
        {
            m_scrsvc.Publish(EServiceEvents.Init.ToString());
        }

        /// <summary>
        /// Call this when DataManager is being de-initialized
        /// </summary>
        public void OnDataUnload()
        {
            m_scrsvc.Publish(EServiceEvents.Deinit.ToString());
        }

        /// <summary>
        /// Call this when GraphicsManager is being loaded.
        /// </summary>
        public void OnGraphicsLoad()
        {
            m_scrsvc.Publish(EServiceEvents.GraphicsLoad.ToString());
        }

        /// <summary>
        /// Call this when GraphicsManager is being unloaded.
        /// </summary>
        public void OnGraphicsUnload()
        {
            //Do stuff..
            DiagManager.Instance.LogInfo("LuaEngine.OnGraphicsUnload()..");
            m_scrsvc.Publish(EServiceEvents.GraphicsUnload.ToString());
        }


        /// <summary>
        /// Called when the game mode switches to GroundMode!
        /// </summary>
        public void OnGroundModeBegin()
        {
            m_scrsvc.Publish(EServiceEvents.GroundModeBegin.ToString());
        }

        /// <summary>
        /// Called when the game mode switches to another mode from ground mode!
        /// </summary>
        public void OnGroundModeEnd()
        {
            m_scrsvc.Publish(EServiceEvents.GroundModeEnd.ToString());
        }


        public void OnGroundMapInit(string mapname, GroundMap map)
        {
            m_scriptUI.Reset();
            DiagManager.Instance.LogInfo("LuaEngine.OnGroundMapInit()..");
            //Update the exposed globals to the various parts of the game engine, since things change between loads
            UpdateExposedInstances();
            m_scrsvc.Publish("OnGroundMapInit", mapname, map);

        }

        /// <summary>
        /// #TODO: Call this when a ground map is entered!
        /// </summary>
        public void OnGroundMapEnter(string mapname, GroundMap mapobj)
        {
            //Do stuff..
            DiagManager.Instance.LogInfo("LuaEngine.OnGroundMapEnter()..");
            m_scrsvc.Publish(EServiceEvents.GroundMapEnter.ToString(), mapname);
        }

        /// <summary>
        /// #TODO: Call this when a ground map is exited!
        /// </summary>
        public void OnGroundMapExit(/*GroundResult result*/)
        {
            //Do stuff..
            DiagManager.Instance.LogInfo("LuaEngine.OnGroundMapExit()..");
            m_scrsvc.Publish(EServiceEvents.GroundMapExit.ToString());
        }

        /// <summary>
        /// Called when the game switches to DungeonMode
        /// </summary>
        public void OnDungeonModeBegin()
        {
            m_scrsvc.Publish(EServiceEvents.DungeonModeBegin.ToString());
        }

        /// <summary>
        /// Called when the game switches to another mode from DungeonMode
        /// </summary>
        public void OnDungeonModeEnd()
        {
            m_scrsvc.Publish(EServiceEvents.DungeonModeEnd.ToString());
        }

        /// <summary>
        /// #TODO: Call this when a dungeon map starts!
        /// </summary>
        public void OnDungeonFloorPrepare(/*DungeonInfo info*/)
        {
            //Stop lua execution, and save stack or something?
            DiagManager.Instance.LogInfo("LuaEngine.DungeonFloorPrepare()..");
            UpdateExposedInstances();
            m_scrsvc.Publish(EServiceEvents.DungeonFloorPrepare.ToString());
        }

        /// <summary>
        /// When entering a new dungeon floor this is called
        /// </summary>
        public void OnDungeonFloorBegin()
        {
            m_scrsvc.Publish(EServiceEvents.DungeonFloorBegin.ToString());
        }

        /// <summary>
        /// When leaving a dungeon floor this is called.
        /// </summary>
        /// <param name="floor">Floor on which was just exited</param>
        public void OnDungeonFloorEnd()
        {
            m_scrsvc.Publish(EServiceEvents.DungeonFloorEnd.ToString());
        }

        public void OnZoneInit()
        {
            m_scrsvc.Publish(EServiceEvents.ZoneInit.ToString());
        }

        public void OnDungeonSegmentEnd()
        {
            m_scrsvc.Publish(EServiceEvents.DungeonSegmentEnd.ToString());
        }

        /// <summary>
        /// Called when an entity activates another.
        /// </summary>
        /// <param name="activator">The entity that activates the target</param>
        /// <param name="target">The entity that is being activated</param>
        /// <param name="info">The context of the activation</param>
        public void OnActivate(GroundEntity activator, GroundEntity target )
        {
            //CallLuaMemberFun(MainScriptInstanceName, "OnActivate", activator, target, info);
            m_scrsvc.Publish(EServiceEvents.GroundEntityInteract.ToString(), activator, target);
        }

        /// <summary>
        /// Call this so the LuaEngine calls the main script's update method.
        /// </summary>
        /// <param name="gametime">Time elapsed since launch in game time.</param>
        /// <param name="frametime">Value between 0 and 1 indicating the current time fraction of a frame the call is taking place at.</param>
        public void Update(GameTime gametime)
        {
            m_curtime = gametime;
            if (m_nextUpdate.Ticks < 0)
                return;

            if (m_nextUpdate < gametime.TotalGameTime)
            {
                //The lua engine handles processing things as coroutines!
                m_scrsvc.Publish(EServiceEvents.Update.ToString(), gametime);


                m_nextUpdate = gametime.TotalGameTime + TimeSpan.FromMilliseconds(20); //Schedule next update
            }
        }
    }

}
