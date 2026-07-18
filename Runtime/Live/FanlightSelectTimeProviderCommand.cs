using System;

namespace PrismFanlight.Live
{
    internal readonly struct FanlightSelectTimeProviderCommand
    {
        internal string ProviderId { get; }


        internal FanlightSelectTimeProviderCommand(string providerId)
        {
            if (string.IsNullOrWhiteSpace(providerId))
            {
                throw new ArgumentException("Provider ID is required.", nameof(providerId));
            }

            ProviderId = providerId;
        }
    }
}
