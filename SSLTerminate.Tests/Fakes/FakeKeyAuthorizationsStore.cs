using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SSLTerminate.Stores.KeyAuthorizations;

namespace SSLTerminate.Tests.Fakes
{
    public class FakeKeyAuthorizationsStore : IKeyAuthorizationsStore
    {
        private readonly Dictionary<string, string> _keyAuthorizations = new Dictionary<string, string>();

        public Task<string> GetKeyAuthorization(string token)
        {
            var result = _keyAuthorizations.TryGetValue(token, out var keyAuth)
                ? keyAuth
                : null;

            return Task.FromResult(result);
        }

        public Task Store(string token, string keyAuthorization)
        {
            _keyAuthorizations[token] = keyAuthorization;
            return Task.CompletedTask;
        }

        public Task Remove(string token)
        {
            _keyAuthorizations.Remove(token);
            return Task.CompletedTask;
        }
    }
}