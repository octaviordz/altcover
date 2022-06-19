﻿namespace AltCover

open System
open System.Collections.Generic
open System.IO

open Mono.Cecil
open Mono.Cecil.Cil
open Mono.Cecil.Mdb
open Mono.Cecil.Pdb
open Mono.Cecil.Rocks

open AltCover.Shared

[<RequireQualifiedAccess>]
module internal ProgramDatabase =
  // "Public" "field"
  let internal symbolFolders = List<String>()

  // Implementation details
  module internal I =

    // We no longer have to violate Cecil encapsulation to get the PDB path
    // but we do to get the embedded PDB info
    let internal getEmbed =
      (typeof<Mono.Cecil.AssemblyDefinition>.Assembly.GetTypes ()
       |> Seq.filter (fun m -> m.FullName == "Mono.Cecil.Mixin")
       |> Seq.head)
        .GetMethod("GetEmbeddedPortablePdbEntry")

    let internal getEmbeddedPortablePdbEntry (assembly: AssemblyDefinition) =
      getEmbed.Invoke(null, [| assembly.MainModule.GetDebugHeader() :> obj |])
      :?> ImageDebugHeaderEntry

    let internal getSymbolsByFolder fileName folderName =
      let name =
        Path.Combine(folderName, fileName)

      let fallback =
        Path.ChangeExtension(name, ".pdb")

      if File.Exists(fallback) then
        Some fallback
      else
        let fallback2 = name + ".mdb"
        // Note -- the assembly path, not the mdb path, because GetSymbolReader wants the assembly path for Mono
        if File.Exists(fallback2) then
          Some name
        else
          None

    let lead (bytes : byte array) =
      (int bytes[0]) ||| ((int bytes[1]) <<< 8) ||| ((int bytes[2]) <<< 16) ||| ((int bytes[3]) <<< 24)

    let getAssemblyTokens (assembly: AssemblyDefinition) =
      let m = assembly.MainModule
      let t =
        m.GetDebugHeader().Entries
        |> Seq.filter (fun e -> e.Data.Length > 0x18 &&
                                e.Directory.Type = ImageDebugType.CodeView)
        |> Seq.map (fun x -> x.Data)
        |> Seq.filter (fun x -> lead x = 1396986706)
        |> Seq.map (fun x -> x |> Array.skip 4 |> Array.take 16 |> System.Guid)
        |> Seq.toList
      (m.Mvid, t)

    let symbolMatch tokens (path : String) =
      use s = if (Path.GetExtension path) <> ".pdb"
              then path + ".mdb"
              else path
              |> File.OpenRead
      let buffer = Array.create 16 0uy

      let (ok, guids) =
        if (Path.GetExtension path) <> ".pdb"
        then
          use b = new BinaryReader(s, System.Text.Encoding.UTF8, true)
          let magic = b.ReadInt64 ()
          let major = b.ReadInt32 ()
          let minor = b.ReadInt32 ()
          (magic = 5037318119232611860L &&
           major = 50 &&
           minor = 0, [fst tokens])
        else
          // TODO -- find the guid offset
          (true, snd tokens)

      let got = s.Read(buffer, 0, 16)// don't complicate things yet
      let g = Guid buffer
      sprintf "Symbol file %s GUID = %A" path g
      |> Output.verbose

      ok && got = 16 &&
        guids |> Seq.exists(fun t -> t = g)

  // "Public" API
  let internal getPdbFromImage (assembly: AssemblyDefinition) =
    let tokens = I.getAssemblyTokens assembly
    (tokens,
    Some assembly.MainModule
    |> Option.filter (fun x -> x.HasDebugHeader)
    |> Option.map (fun x -> x.GetDebugHeader())
    |> Option.filter (fun x -> x.HasEntries)
    |> Option.bind (fun x -> x.Entries |> Seq.tryHead)
    |> Option.map (fun x -> x.Data)
    |> Option.filter (fun x -> x.Length > 0x18)
    |> Option.map (fun x ->
      x
      |> Seq.skip 0x18 // size of the debug header
      |> Seq.takeWhile (fun x -> x <> byte 0)
      |> Seq.toArray
      |> System.Text.Encoding.UTF8.GetString)
    |> Option.filter (fun s -> s.Length > 0)
    |> Option.filter (fun s ->
      File.Exists s
      || (s == (assembly.Name.Name + ".pdb")
          && (assembly |> I.getEmbeddedPortablePdbEntry)
            .IsNotNull)))

  let internal getPdbWithFallback (assembly: AssemblyDefinition) =
    let path = assembly.MainModule.FileName

    match getPdbFromImage assembly with
    | (_, None) when path |> String.IsNullOrWhiteSpace |> not -> // i.e. assemblies read from disk only
      let foldername = Path.GetDirectoryName path
      let filename = Path.GetFileName path

      let folder =
        foldername :: (Seq.toList symbolFolders)
        |> Seq.map (I.getSymbolsByFolder filename)
        |> Seq.choose id
        |> Seq.tryFind (fun _ -> true)

      sprintf
        "Assembly %s symbols from folder '%A'"
        path
        (Option.defaultValue String.Empty folder)
      |> Output.verbose

      folder
    | (_, pdbpath) ->
      sprintf "Assembly %s symbols from image '%s'" path (Option.defaultValue String.Empty pdbpath)
      |> Output.verbose

      pdbpath

  // Ensure that we read symbols from the .pdb path we discovered.
  // Cecil currently only does the Path.ChangeExtension(path, ".pdb") fallback if left to its own devices
  // Will fail  with InvalidOperationException if there is a malformed file with the expected name
  let internal readSymbols (assembly: AssemblyDefinition) =
    getPdbWithFallback assembly
    |> Option.iter (fun pdbpath ->
      let provider: ISymbolReaderProvider =
        if pdbpath.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) then
          PdbReaderProvider() :> ISymbolReaderProvider
        else
          MdbReaderProvider() :> ISymbolReaderProvider

      let reader =
        provider.GetSymbolReader(assembly.MainModule, pdbpath)

      assembly.MainModule.ReadSymbols(reader))

  // reflective short-cuts don't work.
  // maybe move this somewhere else, now?
  let internal getModuleDocuments (``module``: ModuleDefinition) =
    ``module``.GetAllTypes()
    |> Seq.collect (fun t -> t.GetMethods())
    |> Seq.collect (fun m -> m.DebugInformation.SequencePoints)
    |> Seq.map (fun s -> s.Document)
    |> Seq.distinctBy (fun d -> d.Url)
    |> Seq.toList