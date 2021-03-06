﻿using System;
using System.Collections.Generic;
using System.Linq;
using RogueEssence.Dungeon;
using Microsoft.Xna.Framework;
using NLua;

namespace RogueEssence.Script
{


    /// <summary>
    ///
    /// </summary>
    class ScriptServices : ILuaEngineComponent
    {
        private struct ServiceEntry
        {
            public string      name;
            public LuaTable    lobj;
            public TimeSpan    updateinterval; //Rate to update this service at
            public Dictionary<string, LuaFunction> callbacks;
        }

        #region Constants
        public static readonly string SInterfaceInstanceName = "SCRIPT";
        public static readonly int ScriptSvcUpdateInt = 20;
        #endregion

        #region Variables
        private LuaEngine m_state; //reference on the main lua state!
        private Dictionary<string, ServiceEntry>    m_services;  //An internal copie of all services instances
        private LuaFunction                         m_fncallsub;
        private LuaFunction                         m_fncallunsub;
        #endregion

        public LuaEngine State { get { return m_state; } set { m_state = value; } }

        public ScriptServices(LuaEngine state )
        {
            m_services      = new Dictionary<string, ServiceEntry>();
            m_state         = state;
        }

        /// <summary>
        /// Returns the script package path for the currently loaded level
        /// </summary>
        /// <returns></returns>
        public string CurrentScriptDir()
        {
            //We can get the path 3 ways.
            if (!String.IsNullOrEmpty(m_state.CurMapPackagePath))
                return m_state.CurMapPackagePath;
            else if (ZoneManager.Instance.CurrentGround != null)
                return ZoneManager.Instance.CurrentGround.AssetName;
            else if (ZoneManager.Instance.CurrentMap != null)
                throw new NotImplementedException("ScriptServices.CurrentScriptDir(): Dungeon map script packages path handling not implemented yet!!");
            else
                throw new Exception("ScriptServices.CurrentScriptDir(): No map lua package currently loaded! And no map currently loaded either! Cannot assemble the current package path!");
        }

        /// <summary>
        /// Send a message to all the services listening for it.
        /// </summary>
        /// <param name="msgname">Name of the message</param>
        /// <param name="arguments">Value passed along the message</param>
        public void Publish( string msgname, params object[] arguments )
        {
            //DiagManager.Instance.LogInfo("[SE]: Dispatching " + msgname + " event!!");

            foreach(var svc in m_services)
            {
                if(svc.Value.callbacks.ContainsKey(msgname))
                    svc.Value.callbacks[msgname].Call(svc.Value.lobj, arguments);
            }
        }

        /// <summary>
        /// Installs some common lua functions.
        /// </summary>
        public override void SetupLuaFunctions(LuaEngine state)
        {
            m_fncallsub = State.RunString("return function(med, svc) xpcall(svc.Subscribe, PrintStack, svc, med) end").First() as LuaFunction;
            m_fncallunsub = State.RunString("return function(med, svc) xpcall(svc.UnSubscribe, svc, med) end").First() as LuaFunction;
        }

        /// <summary>
        /// Add a service to the list of managed services
        /// </summary>
        /// <param name="name">Handle for the given service instance.</param>
        /// <param name="classpath">Class to instanciate the service from.</param>
        public void AddService(string name, LuaTable instance)
        {
            ServiceEntry svc = new ServiceEntry();
            svc.name = name;
            svc.lobj = instance;
            svc.updateinterval = new TimeSpan(0, 0, 0, 0, ScriptSvcUpdateInt);
            svc.callbacks = new Dictionary<string, LuaFunction>();
            m_services.Add(name, svc);

            //Tell the service to subscribe its callbacks
            m_fncallsub.Call(this, svc.lobj);
            DiagManager.Instance.LogInfo("[SE]:Registered service " + name + "!");
        }

        /// <summary>
        /// Removes the given service from the service list
        /// </summary>
        /// <param name="name"></param>
        public void RemoveService(string name)
        {
            if (m_services.ContainsKey(name))
            {
                ServiceEntry svc = m_services[name];
                m_fncallunsub.Call(this, svc.lobj);
                m_services.Remove(name);
            }
        }

        /// <summary>
        ///
        /// </summary>
        public void Subscribe(string svc, string eventname, LuaFunction fn)
        {
            foreach( var serv in m_services )
            {
                if (serv.Key == svc)
                    serv.Value.callbacks.Add(eventname, fn);
            }
        }

        /// <summary>
        ///
        /// </summary>
        public void UnSubscribe(string svc, string eventname)
        {
            foreach (var serv in m_services)
            {
                if (serv.Key == svc && serv.Value.callbacks.ContainsKey(eventname))
                    serv.Value.callbacks.Remove(eventname);
            }
        }

        /// <summary>
        /// Sends the Update message to all services listening for it!
        /// </summary>
        /// <param name="gtime">Current game engine time.</param>
        public void UpdateServices(GameTime gtime)
        {
            //TODO: Need to come up with something to hopefully reduce script induced latency for stuff being processed often. Coroutines will probably be handy here.
        }
    }
}
