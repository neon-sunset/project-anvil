using Anvil.Core;
using Anvil.Core.Collections;
using BenchmarkDotNet.Running;

BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args);
