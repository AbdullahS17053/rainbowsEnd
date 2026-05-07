using System;
using System.Collections.Generic;

namespace RainbowsEnd.Core.Bootstrap
{
    public sealed class ServiceRegistry : IReadOnlyServiceRegistry
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void Register<TService>(TService service, bool replaceExisting = false) where TService : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            Type serviceType = typeof(TService);
            if (!replaceExisting && _services.ContainsKey(serviceType))
            {
                throw new InvalidOperationException($"A service of type {serviceType.Name} is already registered.");
            }

            _services[serviceType] = service;
        }

        public bool TryGet<TService>(out TService service) where TService : class
        {
            if (_services.TryGetValue(typeof(TService), out object value))
            {
                service = value as TService;
                return service != null;
            }

            service = null;
            return false;
        }

        public TService Get<TService>() where TService : class
        {
            if (TryGet(out TService service))
            {
                return service;
            }

            throw new KeyNotFoundException($"No service of type {typeof(TService).Name} is registered.");
        }
    }
}