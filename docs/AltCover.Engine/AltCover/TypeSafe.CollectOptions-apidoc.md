# TypeSafe.CollectOptions class

Command line options for `AltCover Runner`

```csharp
public sealed class CollectOptions : IEquatable<CollectOptions>, IStructuralEquatable
```

## Public Members

| name | description |
| --- | --- |
| [CollectOptions](TypeSafe.CollectOptions/CollectOptions-apidoc)(…) |  |
| static [Create](TypeSafe.CollectOptions/Create-apidoc)() | Returns an instance with all fields empty save `ExposeReturnCode` being `Set` |
| [Cobertura](TypeSafe.CollectOptions/Cobertura-apidoc) { get; } | Corresponds to command line option `-c, --cobertura=VALUE` |
| [CommandLine](TypeSafe.CollectOptions/CommandLine-apidoc) { get; } | Corresponds to the command line arguments for the executable, given after a `-- ` |
| [Executable](TypeSafe.CollectOptions/Executable-apidoc) { get; } | Corresponds to command line option `-x, --executable=VALUE` |
| [ExposeReturnCode](TypeSafe.CollectOptions/ExposeReturnCode-apidoc) { get; } | Corresponds to the converse of command line option `--dropReturnCode ` |
| [LcovReport](TypeSafe.CollectOptions/LcovReport-apidoc) { get; } | Corresponds to command line option `-l, --lcovReport=VALUE` |
| [OutputFile](TypeSafe.CollectOptions/OutputFile-apidoc) { get; } | Corresponds to command line option `-o, --outputFile=VALUE` |
| [RecorderDirectory](TypeSafe.CollectOptions/RecorderDirectory-apidoc) { get; } | Corresponds to command line option `-r, --recorderDirectory=VALUE` |
| [SummaryFormat](TypeSafe.CollectOptions/SummaryFormat-apidoc) { get; } | Corresponds to command line option `--teamcity[=VALUE]` |
| [Threshold](TypeSafe.CollectOptions/Threshold-apidoc) { get; } | Corresponds to command line option `-t, --threshold=VALUE` |
| [WorkingDirectory](TypeSafe.CollectOptions/WorkingDirectory-apidoc) { get; } | Corresponds to command line option `-w, --workingDirectory=VALUE` |

## See Also

* class [TypeSafe](TypeSafe-apidoc)
* namespace [AltCover](../AltCover.Engine-apidoc)

<!-- DO NOT EDIT: generated by xmldocmd for AltCover.Engine.dll -->