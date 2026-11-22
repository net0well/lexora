using Lexora.Api.IntegrationTests.Hosting;
using Xunit.Sdk;
using Xunit.v3;

[assembly: AssemblyFixture(typeof(ApiFactory))]
[assembly: Parallelization(Mode = ParallelMode.None)]
