using System;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace QuickStart.Tests
{
    [CollectionDefinition("Integration Tests", DisableParallelization = true)]
    public class TestCollection : ICollectionFixture<WebApplicationFactory<QuickStart.Startup>>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
