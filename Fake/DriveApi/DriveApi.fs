﻿namespace ApiUse

module DriveApi =

  open System
  open Fake.IO
  open Fake.Core
  open Fake.DotNet
  open Fake.IO
  open Fake.IO.FileSystemOperators
  open Fake.IO.Globbing
  open Fake.IO.Globbing.Operators

  open AltCover.Fake.DotNet // extension methods

  let _Target s f =
    Target.description s
    Target.create s f

  let dotnetVersion = DotNet.getVersion id

  let paramsToEnvironment (o: DotNet.Options) =
    o.CustomParams
    |> Option.map (fun x ->
      let bits =
        x.Split("/p:", StringSplitOptions.RemoveEmptyEntries)

      bits
      |> Array.fold
           (fun (o2: DotNet.Options) flag ->
             let line = flag.TrimEnd([| ' '; '"' |])

             let split =
               if line.Contains "=\"" then
                 "=\""
               else
                 "="

             let parts =
               line.Split(split, StringSplitOptions.RemoveEmptyEntries)

             { o2 with Environment = o2.Environment |> Map.add parts[0] parts[1] })
           o)
    |> Option.defaultValue o

  let testWithEnvironment (o: Fake.DotNet.DotNet.TestOptions) =
    if dotnetVersion <> "7.0.100" then
      o
    else
      { o with Common = { paramsToEnvironment o.Common with CustomParams = None } }

  let DoIt =
    (fun _ ->
      let expected = "{0}"
      let acv = AltCover.Command.Version()
      printfn "AltCover.Command.Version - Returned %A expected %A" acv expected

      if acv.ToString() <> expected then
        failwith "AltCover.Command.Version mismatch"

      let acfv =
        AltCover.Command.FormattedVersion()

      printfn
        "AltCover.Command.FormattedVersion - Returned '%s' expected %A"
        acfv
        expected

      if acfv <> (sprintf "AltCover version %s" expected) then
        failwith "AltCover.Command.FormattedVersion mismatch"

      let afcv =
        AltCover.Fake.Command.Version().ToString()

      afcv |> Trace.trace
      printfn "expected %A" expected

      if afcv.ToString() <> expected then
        failwith "AltCover.Fake.Command.Version mismatch"

      let collect =
        AltCover.AltCover.CollectOptions.Primitive
          { AltCover.Primitive.CollectOptions.Create() with LcovReport = "x" }

      let prepare =
        AltCover.AltCover.PrepareOptions.Primitive
          { AltCover.Primitive.PrepareOptions.Create() with TypeFilter = [| "a"; "b" |] }

      let forceTrue =
        AltCover.DotNet.CLIOptions.Force true

      printfn
        "Test arguments : '%s'"
        (AltCover.DotNet.ToTestArguments prepare collect forceTrue)

      let t =
        DotNet.TestOptions.Create().WithAltCoverOptions prepare collect forceTrue

      printfn "WithAltCoverOptions returned '%A'" t.Common.CustomParams

      let p2 =
        { AltCover.Primitive.PrepareOptions.Create() with
            LocalSource = true
            CallContext = [| "[Fact]"; "0" |]
            AssemblyFilter = [| "xunit" |] }

      let pp2 =
        AltCover.AltCover.PrepareOptions.Primitive p2

      let c2 =
        AltCover.Primitive.CollectOptions.Create()

      let cc2 =
        AltCover.AltCover.CollectOptions.Primitive c2

      let setBaseOptions (o: DotNet.Options) =
        { o with
            WorkingDirectory = Path.getFullName "./_DotnetTest"
            Verbosity = Some DotNet.Verbosity.Minimal }

      let cliArguments =
        { MSBuild.CliArguments.Create() with
            ConsoleLogParameters = []
            DistributedLoggers = None
            Properties = []
            DisableInternalBinLog = true }

      DotNet.test
        (fun to' ->
          { to'.WithCommon(setBaseOptions).WithAltCoverOptions pp2 cc2 forceTrue with
              MSBuildParams = cliArguments }
          |> testWithEnvironment)
        "apiuse_dotnettest.fsproj"

      let im = AltCover.Command.ImportModule()
      printfn "Import module %A" im

      let importModule =
        (im.Trim().Split()
         |> Seq.take 2
         |> Seq.skip 1
         |> Seq.head)
          .Trim([| '"' |])

      let command =
        "$ImportModule = '"
        + importModule
        + "'; Import-Module $ImportModule; ConvertTo-BarChart -?"

      let corePath =
        AltCover.Fake.Command.ToolPath AltCover.Fake.Implementation.DotNetCore

      printfn "corePath = %A" corePath

      let frameworkPath =
        AltCover.Fake.Command.ToolPath AltCover.Fake.Implementation.Framework

      printfn "frameworkPath = %A" frameworkPath

      if frameworkPath |> String.IsNullOrEmpty |> not then
        let framework =
          Fake.DotNet.ToolType.CreateFullFramework()

        { AltCoverFake.DotNet.Testing.AltCoverCommand.Options.Create
            AltCoverFake.DotNet.Testing.AltCoverCommand.ArgumentType.GetVersion with
            ToolType = framework
            ToolPath = frameworkPath }
        |> AltCoverFake.DotNet.Testing.AltCoverCommand.run

      let core =
        Fake.DotNet.ToolType.CreateFrameworkDependentDeployment id

      { AltCoverFake.DotNet.Testing.AltCoverCommand.Options.Create
          AltCoverFake.DotNet.Testing.AltCoverCommand.ArgumentType.GetVersion with
          ToolType = core
          ToolPath = corePath }
      |> AltCoverFake.DotNet.Testing.AltCoverCommand.run

      let pwsh =
        if Environment.isWindows then
          Fake.Core.ProcessUtils.findLocalTool
            String.Empty
            "pwsh.exe"
            [ Environment.environVar "ProgramFiles"
              @@ "PowerShell" ]
        else
          "pwsh"

      let r =
        CreateProcess.fromRawCommand pwsh [ "-NoProfile"; "-Command"; command ]
        |> CreateProcess.withWorkingDirectory "."
        |> Proc.run

      if (r.ExitCode <> 0) then
        InvalidOperationException("Non zero return code")
        |> raise)

  let Execute argv =
    argv
    |> Array.toList
    |> Context.FakeExecutionContext.Create false "build.fsx"
    |> Context.RuntimeContext.Fake
    |> Context.setExecutionContext

    _Target "DoIt" DoIt
    Target.runOrDefault "DoIt"

#if !INTERACTIVE
  [<EntryPoint>]
  let main argv =
    Execute argv

    0 // return an integer exit code
#endif