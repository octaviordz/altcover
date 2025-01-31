<!-- DO NOT EDIT: generated by ./Build/prepareDocumentation.ps1 for .\AltCover.Engine\Primitive.fsi -->







# namespace `AltCoverFake.DotNet.Testing`
```
namespace AltCoverFake.DotNet.Testing
```

## module `Primitive`

```
  [<RequireQualifiedAccess>]
  module Primitive = begin
```

This holds the weakly ("stringly") typed equivalent of the command line options

### type `CollectOptions`

The members correspond to the like-named command line options for `AltCover Runner`, except
* `ExposeReturnCode` being the converse of the `dropReturnCode` option
* `CommandLine` being the material after a `-- `

```
    [<NoComparison>]
    type CollectOptions =
      {
        RecorderDirectory: System.String
        WorkingDirectory: System.String
        Executable: System.String
        LcovReport: System.String
        Threshold: System.String
        Cobertura: System.String
        OutputFile: System.String
        CommandLine: seq<System.String>
        ExposeReturnCode: bool
        SummaryFormat: System.String
        Verbosity : System.Diagnostics.TraceLevel
      }
      with
        static member Create : unit -> CollectOptions
      end
```
`Create()` returns an instance with all values empty and `ExposeReturnCode` is `true`.

Fields that are not applicable to the use case or platform are silently ignored.

### type `PrepareOptions`

The members correspond to the like-named command line options for `AltCover`, except
* `ExposeReturnCode` being the converse of the `dropReturnCode` option
* `CommandLine` being the material after a `-- `
* `SingleVisit` being the name for `--single`

```
    [<NoComparison>]
    type PrepareOptions =
      {
        InputDirectories: seq<System.String>
        OutputDirectories: seq<System.String>
        SymbolDirectories: seq<System.String>
        Dependencies: seq<System.String>
        Keys: seq<System.String>
        StrongNameKey: System.String
        Report: System.String
        FileFilter: seq<System.String>
        AssemblyFilter: seq<System.String>
        AssemblyExcludeFilter: seq<System.String>
        TypeFilter: seq<System.String>
        MethodFilter: seq<System.String>
        AttributeFilter: seq<System.String>
        PathFilter: seq<System.String>
        AttributeTopLevel: seq<System.String>
        TypeTopLevel: seq<System.String>
        MethodTopLevel: seq<System.String>
        CallContext: seq<System.String>
        ReportFormat: System.String
        InPlace: bool
        Save: bool
        ZipFile: bool
        MethodPoint: bool
        SingleVisit: bool
        LineCover: bool
        BranchCover: bool
        CommandLine: seq<System.String>
        ExposeReturnCode: bool
        SourceLink: bool
        Defer: bool
        LocalSource: bool
        VisibleBranches: bool
        ShowStatic: string
        ShowGenerated: bool
        Verbosity : System.Diagnostics.TraceLevel
        Trivia: bool
      }
    with
        static member Create : unit -> PrepareOptions
    end
```
`Create()` returns an instance that has all empty or `false` fields except `ExposeReturnCode`, `OpenCover`, `InPlace` and `Save` are `true`, and `ShowStatic` is `-`

Fields that are not applicable to the use case or platform are silently ignored.








































