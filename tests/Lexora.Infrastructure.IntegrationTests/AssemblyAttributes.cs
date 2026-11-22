using Lexora.Infrastructure.IntegrationTests.Persistence;
using Xunit.Sdk;
using Xunit.v3;

[assembly: AssemblyFixture(typeof(DatabaseFixture))]
[assembly: Parallelization(Mode = ParallelMode.None)]
