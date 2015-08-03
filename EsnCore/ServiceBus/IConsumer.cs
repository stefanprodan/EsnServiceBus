using System;

namespace EsnCore.ServiceBus
{
    public interface IConsumer<T>
    {
        void OnConsumerExit(ConsumerExitEventArgs args);
        void OnVersionMismatch(Version messageVer, Version runningVer);
        void ProcessMessage(T message);
    }
}