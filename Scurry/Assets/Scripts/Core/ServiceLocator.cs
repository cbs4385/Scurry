using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scurry.Core
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            services[type] = service;
            Debug.Log($"[ServiceLocator] Register: registered {type.Name}");
        }

        public static T Get<T>() where T : class
        {
            var type = typeof(T);
            if (services.TryGetValue(type, out var service))
            {
                return service as T;
            }
            Debug.LogWarning($"[ServiceLocator] Get: no service registered for {type.Name}");
            return null;
        }

        public static void Unregister<T>() where T : class
        {
            var type = typeof(T);
            if (services.Remove(type))
            {
                Debug.Log($"[ServiceLocator] Unregister: removed {type.Name}");
            }
        }

        public static void Clear()
        {
            Debug.Log($"[ServiceLocator] Clear: removing all {services.Count} services");
            services.Clear();
        }
    }
}
