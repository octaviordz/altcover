namespace AltCover.Commands

open System
open System.Diagnostics.CodeAnalysis
open System.IO
open System.Management.Automation
open AltCover

/// <summary>
/// <para type="description">Values for the Invoke-AltCover -Summary option</para>
/// </summary>
type Summary =
  /// <summary>
  /// <para type="description">OpenCover format</para>
  /// </summary>
  | Default = 0
  /// <summary>
  /// <para type="description">TeamCity with R for bRanch</para>
  /// </summary>
  | [<SuppressMessage("Microsoft.Naming", "CA1704", Justification = "R is what is expected")>]
    R = 1
  /// <summary>
  /// <para type="description">TeamCity with B for Block representing branch coverage</para>
  /// </summary>
  | [<SuppressMessage("Microsoft.Naming", "CA1704", Justification = "B is what is expected")>]
    B = 2
  /// <summary>
  /// <para type="description">OpenCover plus TeamCity with R for bRanch</para>
  /// </summary>
  | RPlus = 3
  /// <summary>
  /// <para type="description">OpenCover plus TeamCity with B for Block representing branch coverage</para>
  /// </summary>
  | BPlus = 4

/// <summary>
/// <para type="description">Values for the Invoke-AltCover -ShowStatic option</para>
/// </summary>
type ShowHidden =
  /// <summary>
  /// <para type="description">default, don't show code ignored in static analysis</para>
  /// </summary>
  | KeepHidden = 0
  /// <summary>
  /// <para type="description">show code ignored in static analysis with a special negative visit count if not visited</para>
  /// </summary>
  | Mark = 1
  /// <summary>
  /// <para type="description">treat the code as normal</para>
  /// </summary>
  | Reveal = 2

/// <summary>
/// <para type="description">Values for the Invoke-AltCover -ReportFormat option</para>
/// </summary>
type ReportFormat =
  /// <summary>
  /// <para type="description">Generate an NCover format report</para>
  /// </summary>
  | NCover = 0
  /// <summary>
  /// <para type="description">Generate an OpenCover format report</para>
  /// </summary>
  | OpenCover = 1

/// <summary>
/// <para type="synopsis">The equivalent of the `AltCover` command or `altcover` global tool.</para>
/// <para type="description">The arguments parallel the command-line executable; the main difference is that `-Runner` is a switch parameter that selects the runner-mode parameter set if present.</para>
/// <para type="description">The "working directory" used where mentioned in the parameter descriptions is the current set location in PowerShell, **_not_** the underlying current directory.</para>
/// <para type="description">**Note**: As Powershell informational output is suppressed by default in PowerShell 5+, the `-InformationAction Continue` option is needed to show the progress and summary information for the process if this is desired.</para>
/// <para type="description">Summary information is also written to the object pipeline.</para>
/// <para type="description">**Note**: `-WhatIf` includes validation for the command line arguments.  It is ignored for the purely read-only `-Version` option </para>
/// <example>
///   <code>        Invoke-AltCover -XmlReport $x -OutputDirectory  $o -InputDirectory $i -AssemblyFilter "Adapter" -ReportFormat NCover -InformationAction Continue</code>
/// </example>
/// </summary>
[<Cmdlet(VerbsLifecycle.Invoke, "AltCover", SupportsShouldProcess = true,
         ConfirmImpact = ConfirmImpact.Medium)>]
[<OutputType([| "System.Void"; "System.String" |]); AutoSerializable(false)>]
[<SuppressMessage("Gendarme.Rules.Smells",
    "AvoidLargeClassesRule",
    Justification="Has lots of parameters to pass")>]
[<SuppressMessage("Microsoft.PowerShell",
                  "PS1101:AllCmdletsShouldAcceptPipelineInput",
                  Justification = "No valid input")>]
type InvokeAltCoverCommand() =
  inherit PSCmdlet()

  /// <summary>
  /// <para type="description">Selects `Runner` mode</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Runner", Mandatory = true, Position = 1,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  member val Runner = SwitchParameter(false) with get, set

  /// <summary>
  /// <para type="description">The folder containing the instrumented code to monitor (including the `AltCover.Recorder.g.dll` generated by previous a use of the .net core `AltCover`).</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Runner", Mandatory = true, ValueFromPipeline = false,
              ValueFromPipelineByPropertyName = false)>]
  member val RecorderDirectory = String.Empty with get, set

  /// <summary>
  /// <para type="description">The working directory for the application launch</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Runner", Mandatory = false, ValueFromPipeline = false,
              ValueFromPipelineByPropertyName = false)>]
  member val WorkingDirectory = String.Empty with get, set

  /// <summary>
  /// <para type="description">The executable to run e.g. dotnet</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Runner", Mandatory = false, ValueFromPipeline = false,
              ValueFromPipelineByPropertyName = false)>]
  member val Executable = String.Empty with get, set

  /// <summary>
  /// <para type="description">File for lcov format version of the collected data</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Runner", Mandatory = false, ValueFromPipeline = false,
              ValueFromPipelineByPropertyName = false)>]
  [<SuppressMessage("Microsoft.Naming", "CA1704", Justification = "Lcov is a name")>]
  member val LcovReport = String.Empty with get, set

  /// <summary>
  /// <para type="description">One or more of minimum acceptable statement (S), branch (B) or method (M) coverage percentage (integer, 1 to 100) or maximum acceptable CRAP score (C followed by integer, 1 to 255) e.g. M80C40B50. If the value starts with a number, a leading S is assumed. If any threshold is specified more than once, the last instance is assumed -- so 25S50 counts as S50. Zero/absent values are ignored. If a coverage result is below threshold, or the CRAP score is above threshold, the return code of the process is the largest abs(threshold - actual) rounded up to the nearest integer.</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Runner", Mandatory = false, ValueFromPipeline = false,
              ValueFromPipelineByPropertyName = false)>]
  member val Threshold = String.Empty with get, set

  /// <summary>
  /// <para type="description">File for Cobertura format version of the collected data</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Runner", Mandatory = false, ValueFromPipeline = false,
              ValueFromPipelineByPropertyName = false)>]
  [<SuppressMessage("Microsoft.Naming", "CA1704", Justification = "Cobertura is a name")>]
  member val Cobertura = String.Empty with get, set

  /// <summary>
  /// <para type="description">Write the recorded coverage to this file rather than overwriting the original report file.</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Runner", Mandatory = false, ValueFromPipeline = false,
              ValueFromPipelineByPropertyName = false)>]
  member val OutputFile = String.Empty with get, set

  /// <summary>
  /// <para type="description">Arguments for a launched process</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  [<Parameter(ParameterSetName = "Runner", Mandatory = false, ValueFromPipeline = false,
              ValueFromPipelineByPropertyName = false)>]
  [<SuppressMessage(
      "Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule",
      Justification = "Cannot convert 'System.Object[]' to the type 'System.Collections.Generic.IEnumerable`1[System.String]'")>]
  [<SuppressMessage("Microsoft.Performance", "CA1819",
                    Justification = "ditto, ditto")>]
  member val CommandLine : string array = [||] with get, set

  /// <summary>
  /// <para type="description">Folder or folders containing assemblies to instrument (default: current directory)</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  [<SuppressMessage(
      "Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule",
      Justification = "Cannot convert 'System.Object[]' to the type 'System.Collections.Generic.IEnumerable`1[System.String]'")>]
  [<SuppressMessage("Microsoft.Performance", "CA1819",
                    Justification = "ditto, ditto")>]
  member val InputDirectory : string array = [||] with get, set

  /// <summary>
  /// <para type="description">Folder or folders to receive the instrumented assemblies and their companions (default: sub-folder `__Instrumented` of the current directory; or `__Saved` if `-Inplace` is set).</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  [<SuppressMessage(
      "Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule",
      Justification = "Cannot convert 'System.Object[]' to the type 'System.Collections.Generic.IEnumerable`1[System.String]'")>]
  [<SuppressMessage("Microsoft.Performance", "CA1819",
                    Justification = "ditto, ditto")>]
  member val OutputDirectory : string array = [||] with get, set

  /// <summary>
  /// <para type="description">Additional folder or folders to search for matching symbols for the assemblies in the input directory</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  [<SuppressMessage(
      "Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule",
      Justification = "Cannot convert 'System.Object[]' to the type 'System.Collections.Generic.IEnumerable`1[System.String]'")>]
  [<SuppressMessage("Microsoft.Performance", "CA1819",
                    Justification = "ditto, ditto")>]
  member val SymbolDirectory : string array = [||] with get, set

  /// <summary>
  /// <para type="description">Assembly paths to resolve missing references.</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  [<SuppressMessage(
      "Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule",
      Justification = "Cannot convert 'System.Object[]' to the type 'System.Collections.Generic.IEnumerable`1[System.String]'")>]
  [<SuppressMessage("Microsoft.Performance", "CA1819",
                    Justification = "ditto, ditto")>]
  member val Dependency : string array = [||] with get, set

  /// <summary>
  /// <para type="description">Strong name key or keys used to sign the inputs</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  [<SuppressMessage(
      "Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule",
      Justification = "Cannot convert 'System.Object[]' to the type 'System.Collections.Generic.IEnumerable`1[System.String]'")>]
  [<SuppressMessage("Microsoft.Performance", "CA1819",
                    Justification = "ditto, ditto")>]
  member val Key : string array = [||] with get, set

  /// <summary>
  /// <para type="description">The default strong naming key to apply to instrumented assemblies</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  member val StrongNameKey = String.Empty with get, set

  /// <summary>
  /// <para type="description">The output report template file (default: coverage.xml in the current directory)</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  member val XmlReport = String.Empty with get, set

  /// <summary>
  /// <para type="description">Source file names to exclude from instrumentation</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  [<SuppressMessage(
      "Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule",
      Justification = "Cannot convert 'System.Object[]' to the type 'System.Collections.Generic.IEnumerable`1[System.String]'")>]
  [<SuppressMessage("Microsoft.Performance", "CA1819",
                    Justification = "ditto, ditto")>]
  member val FileFilter : string array = [||] with get, set

  /// <summary>
  /// <para type="description">Source file paths to exclude from instrumentation</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  [<SuppressMessage(
      "Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule",
      Justification = "Cannot convert 'System.Object[]' to the type 'System.Collections.Generic.IEnumerable`1[System.String]'")>]
  [<SuppressMessage("Microsoft.Performance", "CA1819",
                    Justification = "ditto, ditto")>]
  member val PathFilter : string array = [||] with get, set

  /// <summary>
  /// <para type="description">Assembly names to exclude from instrumentation (linked by instrumented assemblies)</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  [<SuppressMessage(
      "Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule",
      Justification = "Cannot convert 'System.Object[]' to the type 'System.Collections.Generic.IEnumerable`1[System.String]'")>]
  [<SuppressMessage("Microsoft.Performance", "CA1819",
                    Justification = "ditto, ditto")>]
  member val AssemblyFilter : string array = [||] with get, set

  /// <summary>
  /// <para type="description">Assembly names to exclude from instrumentation (linked to instrumented assemblies)</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  [<SuppressMessage(
      "Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule",
      Justification = "Cannot convert 'System.Object[]' to the type 'System.Collections.Generic.IEnumerable`1[System.String]'")>]
  [<SuppressMessage("Microsoft.Performance", "CA1819",
                    Justification = "ditto, ditto")>]
  member val AssemblyExcludeFilter : string array = [||] with get, set

  /// <summary>
  /// <para type="description">Type names to exclude from instrumentation</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  [<SuppressMessage(
      "Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule",
      Justification = "Cannot convert 'System.Object[]' to the type 'System.Collections.Generic.IEnumerable`1[System.String]'")>]
  [<SuppressMessage("Microsoft.Performance", "CA1819",
                    Justification = "ditto, ditto")>]
  member val TypeFilter : string array = [||] with get, set

  /// <summary>
  /// <para type="description">Method names to exclude from instrumentation</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  [<SuppressMessage(
      "Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule",
      Justification = "Cannot convert 'System.Object[]' to the type 'System.Collections.Generic.IEnumerable`1[System.String]'")>]
  [<SuppressMessage("Microsoft.Performance", "CA1819",
                    Justification = "ditto, ditto")>]
  member val MethodFilter : string array = [||] with get, set

  /// <summary>
  /// <para type="description">Attribute names to exclude from instrumentation</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  [<SuppressMessage(
      "Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule",
      Justification = "Cannot convert 'System.Object[]' to the type 'System.Collections.Generic.IEnumerable`1[System.String]'")>]
  [<SuppressMessage("Microsoft.Performance", "CA1819",
                    Justification = "ditto, ditto")>]
  member val AttributeFilter : string array = [||] with get, set

  /// <summary>
  /// <para type="description">Tracking either times of visits in ticks or designated method calls leading to the visits.</para>
  /// <para type="description">A single digit 0-7 gives the number of decimal places of seconds to report; everything else is at the mercy of the system clock information available through DateTime.UtcNow</para>
  /// <para type="description">A string in brackets "[]" is interpreted as an attribute type name (the trailing "Attribute" is optional), so [Test] or [TestAttribute] will match; if the name contains one or more ".", then it will be matched against the full name of the attribute type.</para>
  /// <para type="description">Other strings are interpreted as method names (fully qualified if the string contains any "." characters).</para>
  /// <para type="description">Incompatible with -Single</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  [<SuppressMessage(
      "Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule",
      Justification = "Cannot convert 'System.Object[]' to the type 'System.Collections.Generic.IEnumerable`1[System.String]'")>]
  [<SuppressMessage("Microsoft.Performance", "CA1819",
                    Justification = "ditto, ditto")>]
  member val CallContext : string array = [||] with get, set

  /// <summary>
  /// <para type="description">Generate the report in the specified format (NCover or the default OpenCover)</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  member val ReportFormat : ReportFormat = ReportFormat.OpenCover with get, set

  /// <summary>
  /// <para type="description">Instrument the inputDirectory, rather than the outputDirectory (e.g. for `dotnet test`)</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  member val InPlace : SwitchParameter = SwitchParameter(false) with get, set

  /// <summary>
  /// <para type="description">Write raw coverage data to file for later processing</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  member val Save : SwitchParameter = SwitchParameter(false) with get, set

  /// <summary>
  /// <para type="description">Emit the XML report inside a zip archive.</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  member val ZipFile : SwitchParameter = SwitchParameter(false) with get, set

  /// <summary>
  /// <para type="description">record only whether a method has been visited or not.  Overrides the `-LineCover` and `-BranchCover` options.</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  member val MethodPoint : SwitchParameter = SwitchParameter(false) with get, set

  /// <summary>
  /// <para type="description">only record the first hit at any location.  Incompatible with `-CallContext`.</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  member val Single : SwitchParameter = SwitchParameter(false) with get, set

  /// <summary>
  /// <para type="description">Do not record branch coverage.  Implies, and is compatible with, the -ReportFormat "opencover" option. Incompatible with `-BranchCover`.</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  member val LineCover : SwitchParameter = SwitchParameter(false) with get, set

  /// <summary>
  /// <para type="description">Do not record line coverage.  Implies, and is compatible with, the -ReportFormat "opencover" option. Incompatible with `-LineCover`.</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  member val BranchCover : SwitchParameter = SwitchParameter(false) with get, set

  /// <summary>
  /// <para type="description">Selects `Version` mode</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Version", Mandatory = true, Position = 1,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  member val Version : SwitchParameter = SwitchParameter(false) with get, set

  /// <summary>
  /// <para type="description">Do not report any non-zero return code from a launched process.</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  [<Parameter(ParameterSetName = "Runner", Mandatory = false, ValueFromPipeline = false,
              ValueFromPipelineByPropertyName = false)>]
  member val DropReturnCode : SwitchParameter = SwitchParameter(false) with get, set

  /// <summary>
  /// <para type="description">Display sourcelink URLs rather than file paths if present.</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  member val SourceLink : SwitchParameter = SwitchParameter(false) with get, set

  /// <summary>
  /// <para type="description">Defers writing runner-mode coverage data until process exit.</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  member val Defer : SwitchParameter = SwitchParameter(false) with get, set

  /// <summary>
  /// <para type="description">Don't instrument code for which the source file is not present.</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  member val LocalSource : SwitchParameter = SwitchParameter(false) with get, set

  /// <summary>
  /// <para type="description">Hide complex internal IL branching implementation details in switch/match constructs, and just show what the source level logic implies.</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  member val VisibleBranches : SwitchParameter = SwitchParameter(false) with get, set

  /// <summary>
  /// <para type="description">Mark generated code with a visit count of -2 (Automatic) for the Visualizer if unvisited</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  member val ShowGenerated : SwitchParameter = SwitchParameter(false) with get, set

  /// <summary>
  /// <para type="description">Instrument and show code that is by default skipped as trivial.</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Instrument", Mandatory = false,
              ValueFromPipeline = false, ValueFromPipelineByPropertyName = false)>]
  member val ShowStatic = ShowHidden.KeepHidden with get, set

  /// <summary>
  /// <para type="description">Selects summary format</para>
  /// </summary>
  [<Parameter(ParameterSetName = "Runner", Mandatory = false, ValueFromPipeline = false,
              ValueFromPipelineByPropertyName = false)>]
  member val SummaryFormat : Summary = Summary.Default with get, set

  member val private Fail : String list = [] with get, set

  member private self.Collect() =
    let formats = [| String.Empty; "R"; "B"; "+R"; "+B" |]
    FSApi.CollectParameters.Primitive
      { RecorderDirectory = self.RecorderDirectory
        WorkingDirectory = self.WorkingDirectory
        Executable = self.Executable
        LcovReport = self.LcovReport
        Threshold = self.Threshold
        Cobertura = self.Cobertura
        OutputFile = self.OutputFile
        CommandLine = self.CommandLine
        ExposeReturnCode = not self.DropReturnCode.IsPresent
        SummaryFormat = formats.[self.SummaryFormat |> int] }

  member private self.Prepare() =
    let showStatic = [| "-"; "+"; "++ " |]
    FSApi.PrepareParameters.Primitive
      { InputDirectories = self.InputDirectory
        OutputDirectories = self.OutputDirectory
        SymbolDirectories = self.SymbolDirectory
        Dependencies = self.Dependency
        Keys = self.Key
        StrongNameKey = self.StrongNameKey
        XmlReport = self.XmlReport
        FileFilter = self.FileFilter
        AssemblyFilter = self.AssemblyFilter
        AssemblyExcludeFilter = self.AssemblyExcludeFilter
        TypeFilter = self.TypeFilter
        MethodFilter = self.MethodFilter
        AttributeFilter = self.AttributeFilter
        PathFilter = self.PathFilter
        CallContext = self.CallContext
        ReportFormat = self.ReportFormat.ToString()
        InPlace = self.InPlace.IsPresent
        Save = self.Save.IsPresent
        ZipFile = self.ZipFile.IsPresent
        MethodPoint = self.MethodPoint.IsPresent
        Single = self.Single.IsPresent
        LineCover = self.LineCover.IsPresent
        BranchCover = self.BranchCover.IsPresent
        CommandLine = self.CommandLine
        ExposeReturnCode = not self.DropReturnCode.IsPresent
        SourceLink = self.SourceLink.IsPresent
        Defer = self.Defer.IsPresent
        LocalSource = self.LocalSource.IsPresent
        VisibleBranches = self.VisibleBranches.IsPresent
        ShowStatic = showStatic.[self.ShowStatic |> int]
        ShowGenerated = self.ShowGenerated.IsPresent }

  member private self.Log() =
    FSApi.Logging.Primitive
      { Primitive.Logging.Create() with
          Error = (fun s -> self.Fail <- s :: self.Fail)
          Info = (fun s -> self.WriteInformation(s, [||]))
          Warn = (fun s -> self.WriteWarning s) }

  member private self.Dispatch() =
    let log = self.Log()
    let zero _ = 0

    (match (self.Version.IsPresent, self.Runner.IsPresent) with
     | (true, _) ->
         (fun _ ->
           Api.Version() |> log.Info
           0)
     | (_, true) ->
         let task = self.Collect()
         // unset is error, but if set the recorder may not exist yet
         let recording =
           self.RecorderDirectory
           |> String.IsNullOrWhiteSpace
           || Path.Combine(self.RecorderDirectory, "AltCover.Recorder.g.dll")
              |> File.Exists
         if (self.ShouldProcess("Command Line : " + task.WhatIf(recording).ToString()))
         then Api.Collect task
         else zero
     | _ ->
         let task = self.Prepare()
         if (self.ShouldProcess("Command Line : " + task.WhatIf().ToString()))
         then Api.Prepare task
         else zero) log

  override self.ProcessRecord() =
    let here = Directory.GetCurrentDirectory()
    try
      let where = self.SessionState.Path.CurrentLocation.Path
      Directory.SetCurrentDirectory where
      let makeError s =
        ErrorRecord(InvalidOperationException(s), s, ErrorCategory.InvalidOperation, self)
        |> self.WriteError

      let status = self.Dispatch()
      if status <> 0 then status.ToString() |> self.Log().Error
      else if self.Runner.IsPresent then Api.Summary() |> self.WriteObject

      match self.Fail with
      | [] -> ()
      | things -> String.Join(Environment.NewLine, things |> List.rev) |> makeError
    finally
      Directory.SetCurrentDirectory here