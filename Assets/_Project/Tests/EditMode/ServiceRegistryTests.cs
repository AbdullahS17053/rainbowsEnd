using NUnit.Framework;
using RainbowsEnd.Core.Bootstrap;

namespace RainbowsEnd.Tests.EditMode
{
    public sealed class ServiceRegistryTests
    {
        [Test]
        public void Register_ThenGet_ReturnsSameInstance()
        {
            ServiceRegistry registry = new ServiceRegistry();
            object service = new object();

            registry.Register(service);

            Assert.That(registry.Get<object>(), Is.SameAs(service));
        }

        [Test]
        public void Register_DuplicateWithoutReplace_Throws()
        {
            ServiceRegistry registry = new ServiceRegistry();

            registry.Register(new object());

            Assert.That(() => registry.Register(new object()), Throws.InvalidOperationException);
        }

        [Test]
        public void TryGet_MissingService_ReturnsFalse()
        {
            ServiceRegistry registry = new ServiceRegistry();

            bool found = registry.TryGet(out object _);

            Assert.That(found, Is.False);
        }
    }
}