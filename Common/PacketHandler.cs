﻿using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Tyranny.Networking
{
    public delegate void Handler(PacketReader packetIn, AsyncTcpClient tcpClient);

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
    public class PacketHandler : System.Attribute
    {
        private static Logger logger = NLog.LogManager.GetCurrentClassLogger();

        TyrannyOpcode opcode;

        public PacketHandler(TyrannyOpcode opcode)
        {
            this.opcode = opcode;
        }

        public TyrannyOpcode GetOpcode()
        {
            return opcode;
        }

        public static Dictionary<TyrannyOpcode, Handler> Load()
        {
            var type = typeof(IPacketHandler);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p));

            var attrType = typeof(PacketHandler);

            Dictionary<TyrannyOpcode, Handler> dict = new Dictionary<TyrannyOpcode, Handler>();
            foreach (Type t in types)
            {
                foreach(MethodInfo method in t.GetMethods())
                {
                    Attribute attr = method.GetCustomAttribute(attrType);
                    if (attr != null)
                    {

                        PacketHandler handler = (PacketHandler)attr;
                        TyrannyOpcode opcode = handler.GetOpcode();
                        try
                        {
                            var func = (Handler)Delegate.CreateDelegate(typeof(Handler), method, true);
                            logger.Debug($"Binding packet handler for opcode {opcode}: {t.FullName}:{method.Name}");
                            if (dict.ContainsKey(opcode))
                            {
                                dict[opcode] += func;
                            }
                            else
                            {
                                dict[opcode] = func;
                            }
                        }
                        catch (ArgumentException)
                        {
                            logger.Warn($"Unable to bind packet handler for opcode {opcode}: {t.FullName}.{method.Name}");
                        }
                    }
                }
            }
            return dict;
        }
    }

    public interface IPacketHandler
    {

    }
}
