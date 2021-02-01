using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NLog;
using Tyranny.Networking;

namespace CommonLib
{
    public static class PacketHandlerLoader<T>  where T : Enum
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        
        public static Dictionary<T, THandler> Load<THandler>(Type attrType, Type clazz) where THandler : Delegate
        {
            return DoLoad<THandler>(attrType, new Type[] { clazz });
        }

        public static Dictionary<T, THandler> Load<THandler>(Type attrType, object obj) where THandler : Delegate
        {
            return DoLoad<THandler>(attrType, new Type[] {obj.GetType()});
        }

        public static Dictionary<T, THandler> Load<THandler>(Type attrType) where THandler : Delegate
        {
            var type = typeof(IPacketHandler<T>);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p));

            return DoLoad<THandler>(attrType, types);
        }
        
        private static Dictionary<T, THandler> DoLoad<THandler>(Type attrType, IEnumerable<Type> types) where THandler : Delegate
        {
            Dictionary<T, THandler> dict = new Dictionary<T, THandler>();
            foreach (Type t in types)
            {
                foreach (MethodInfo method in t.GetMethods())
                {
                    Attribute attr = method.GetCustomAttribute(attrType);
                    if (attr != null)
                    {

                        IPacketHandler<T> handler = (IPacketHandler<T>)attr;
                        var op = handler.Opcode;
                        try
                        {
                            var func = (THandler)Delegate.CreateDelegate(typeof(THandler), method, true);
                            logger.Debug($"Binding packet handler for opcode {op}: {t.FullName}:{method.Name}");
                            if (dict.ContainsKey(op))
                            {
                                Delegate existing = dict[op] as Delegate;
                                dict[op] = (THandler)Delegate.Combine(existing, func);
                            }
                            else
                            {
                                dict[op] = func;
                            }
                        }
                        catch (ArgumentException)
                        {
                            logger.Warn($"Unable to bind packet handler for opcode {op}: {t.FullName}.{method.Name}");
                        }
                    }
                }
            }
            return dict;
        }
    }
}