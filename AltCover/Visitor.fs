﻿// Based upon C# code by Sergiy Sakharov (sakharov@gmail.com)
// http://code.google.com/p/dot-net-coverage/source/browse/trunk/Coverage.Counter/Coverage.Counter.csproj

namespace AltCover

// Functional Visitor pattern

open System
open System.Collections.Generic
open System.Diagnostics.CodeAnalysis
open System.IO
open System.Linq
open System.Reflection
open System.Text.RegularExpressions

open AltCover.Augment
open AltCover.Base
open Mono.Cecil
open Mono.Cecil.Cil
open Mono.Cecil.Rocks

[<Flags>]
type Inspect = Ignore = 0 | Instrument = 1 | Track = 2 | TrackOnly = 4

[<ExcludeFromCodeCoverage>]
type SeqPnt = {
         StartLine : int
         StartColumn : int
         EndLine : int
         EndColumn : int
         Document : string
         Offset : int
         }
    with static member Build(codeSegment:Cil.SequencePoint) = {
                                                                 StartLine = codeSegment.StartLine
                                                                 StartColumn = codeSegment.StartColumn
                                                                 EndLine = if codeSegment.EndLine < 0
                                                                              then codeSegment.StartLine
                                                                              else codeSegment.EndLine
                                                                 EndColumn = if codeSegment.EndLine < 0
                                                                              then codeSegment.StartColumn + 1
                                                                              else codeSegment.EndColumn
                                                                 Document = codeSegment.Document.Url
                                                                 Offset = codeSegment.Offset
    }

[<ExcludeFromCodeCoverage>]
type GoTo = {
    Start : Instruction
    Indexes : int list
    Uid : int
    Path : int
    StartLine : int
    Offset : int
    Target : int list
    Document : string
}

[<ExcludeFromCodeCoverage>]
type internal Node =
     | Start of seq<string>
     | Assembly of AssemblyDefinition * Inspect
     | Module of ModuleDefinition * Inspect
     | Type of TypeDefinition * Inspect
     | Method of MethodDefinition * Inspect * (int * string) option
     | MethodPoint of Instruction * SeqPnt option * int * bool
     | BranchPoint of GoTo
     | AfterMethod of MethodDefinition * Inspect * (int * string) option
     | AfterType
     | AfterModule
     | AfterAssembly of AssemblyDefinition
     | Finish
with member this.After () = (match this with
                             | Start _ -> [Finish]
                             | Assembly (a, _) -> [AfterAssembly a]
                             | Module _ -> [AfterModule]
                             | Type _ -> [AfterType]
                             | Method (m,included, track) -> [AfterMethod (m,included, track)]
                             | _ -> []) |> List.toSeq

[<ExcludeFromCodeCoverage>]
type KeyRecord = {
         Pair : StrongNameKeyPair;
         Token : byte list }

module KeyStore =
    let private hash = new System.Security.Cryptography.SHA1CryptoServiceProvider()

    let private publicKeyOfKey (key:StrongNameKeyPair) =
#if NETCOREAPP2_0
      [||]
#else
      key.PublicKey
#endif

    let internal TokenOfArray (key:byte array) =
        hash.ComputeHash(key)
            |> Array.rev
            |> Array.take 8

    let internal TokenOfKey (key:StrongNameKeyPair) =
        key |> publicKeyOfKey |> TokenOfArray |> Array.toList

    let internal TokenAsULong (token:byte array) =
      BitConverter.ToUInt64(token, 0)

    let internal KeyToIndex (key:StrongNameKeyPair) =
      key
      |> TokenOfKey
      |> List.toArray
      |> TokenAsULong

    let internal ArrayToIndex (key:byte array) =
      key
      |> TokenOfArray
      |> TokenAsULong

    let internal KeyToRecord (key:StrongNameKeyPair) =
      { Pair = key
        Token = TokenOfKey key }

    let internal HashFile sPath =
      use stream = File.OpenRead sPath
      stream |>hash.ComputeHash |> BitConverter.ToString

[<ExcludeFromCodeCoverage>]
type Fix<'T> = delegate of 'T -> Fix<'T>

module Visitor =
  let internal TrackingNames = new List<String>()

  let internal NameFilters = new List<FilterClass>()
  let private specialCaseFilters = [ @"^CompareTo\$cont\@\d+\-?\d$" |> Regex |> FilterClass.Method ]

  let mutable internal inputDirectory : Option<string> = None
  let private defaultInputDirectory = "."
  let InputDirectory () = Path.GetFullPath (Option.getOrElse defaultInputDirectory inputDirectory)

  let mutable internal outputDirectory : Option<string> = None
  let private defaultOutputDirectory = "__Instrumented"
  let OutputDirectory () = Path.GetFullPath (Option.getOrElse defaultOutputDirectory outputDirectory)

  let mutable internal reportPath : Option<string> = None
  let defaultReportPath = "coverage.xml"
  let ReportPath () = Path.GetFullPath (Option.getOrElse defaultReportPath reportPath)

  let mutable internal interval : Option<int> = None
  let defaultInterval = 0
  let Interval () = (Option.getOrElse defaultInterval interval)

  let mutable internal reportFormat : Option<ReportFormat> = None
  let defaultReportFormat = ReportFormat.NCover
  let ReportKind () = (Option.getOrElse defaultReportFormat reportFormat)
  let ReportFormat () = let fmt = ReportKind()
                        if fmt = ReportFormat.OpenCover &&
                                 (TrackingNames.Any() || Interval() > 0) then
                                 ReportFormat.OpenCoverWithTracking
                        else fmt

  let mutable internal defaultStrongNameKey : option<StrongNameKeyPair> = None
  let internal keys = new Dictionary<UInt64, KeyRecord>()

  let internal Add (key:StrongNameKeyPair) =
    let index = KeyStore.KeyToIndex key
    keys.[index] <- KeyStore.KeyToRecord key

  let IsIncluded (nameProvider:Object) =
    if (NameFilters |> Seq.exists (Filter.Match nameProvider))
    then Inspect.Ignore
    else Inspect.Instrument

  let Mask = ~~~Inspect.Instrument

  let UpdateInspection before x =
    (before &&& Mask) |||
    (before &&& Inspect.Instrument &&& IsIncluded x)

  let IsInstrumented x = (x &&& Inspect.Instrument) = Inspect.Instrument

  let ToSeq node =
    List.toSeq [ node ]

  let mutable private PointNumber : int = 0
  let mutable private BranchNumber : int = 0
  let mutable private MethodNumber : int = 0

  let significant (m : MethodDefinition) =
    [Filter.IsFSharpInternal
     Filter.IsCSharpAutoProperty
     (fun m -> specialCaseFilters
               |> Seq.exists (Filter.Match m))
     ]
    |> Seq.exists (fun f -> f m)
    |> not

  let private StartVisit (paths:seq<string>) buildSequence =
        paths
        |> Seq.collect (AssemblyDefinition.ReadAssembly >>
                        (fun x -> // Reject completely if filtered here
                                  let inspection = IsIncluded x
                                  let included = inspection |||
                                                 if inspection = Inspect.Instrument &&
                                                    ReportFormat() = Base.ReportFormat.OpenCoverWithTracking
                                                 then Inspect.Track
                                                 else Inspect.Ignore
                                  ProgramDatabase.ReadSymbols(x)
                                  Assembly(x, included)) >> buildSequence)

  let private VisitAssembly (a:AssemblyDefinition) included buildSequence =
        a.Modules
        |> Seq.cast
        |> Seq.collect ((fun x -> let interim = UpdateInspection included x
                                  Module (x, if interim = Inspect.Track
                                             then Inspect.TrackOnly
                                             else interim )) >> buildSequence)

  let private ZeroPoints () =
        PointNumber <- 0
        BranchNumber <- 0

  let private VisitModule (x:ModuleDefinition) included buildSequence =
        ZeroPoints()
        [x]
        |> Seq.takeWhile (fun _ -> included <> Inspect.Ignore)
        |> Seq.collect(fun x -> x.GetAllTypes() |> Seq.cast)
        |> Seq.collect ((fun t -> Type (t, UpdateInspection included t)) >> buildSequence)

  let internal Track (m : MethodDefinition) =
    let name = m.Name
    let fullname = m.DeclaringType.FullName.Replace('/','.') + "." + name
    TrackingNames
    |> Seq.map(fun n -> if n.Chars(0) = '[' then
                            let stripped = n.Trim([| '['; ']' |])
                            let full = if stripped.EndsWith("Attribute", StringComparison.Ordinal)
                                          then stripped else stripped + "Attribute"
                            if m.HasCustomAttributes &&
                                m.CustomAttributes
                                |> Seq.map(fun a -> a.AttributeType)
                                |> Seq.tryFind(fun a -> full = a.Name || full = a.FullName)
                                |> Option.isSome
                            then Some n
                            else None
                        else
                            if n = name || n = fullname
                               then Some n
                            else None)
    |> Seq.choose id
    |> Seq.tryFind (fun _ -> true)
    |> Option.map (fun n -> let id = MethodNumber + 1
                            MethodNumber <- id
                            (id, n))

  let private VisitType (t:TypeDefinition) included buildSequence =
        t.Methods
        |> Seq.cast
        |> Seq.filter (fun (m : MethodDefinition) -> not m.IsAbstract
                                                    && not m.IsRuntime
                                                    && not m.IsPInvokeImpl
                                                    && significant m)
        |> Seq.collect ((fun m -> Method (m, UpdateInspection included m, Track m)) >> buildSequence)

  let findSequencePoint (dbg:MethodDebugInformation) (instructions:Instruction seq) =
    instructions
    |> Seq.map dbg.GetSequencePoint
    |> Seq.tryFind (fun s -> (s  |> isNull |> not) && s.StartLine <> 0xfeefee)

  let indexList l =
    l |> List.mapi (fun i x -> (i,x))

  let getJumpChain (i:Instruction) =
    Seq.unfold (fun (state:Cil.Instruction) -> if isNull state  
                                               then None 
                                               else Some (state, if state.OpCode = OpCodes.Br ||
                                                                    state.OpCode = OpCodes.Br_S
                                                                 then state.Operand :?> Instruction
                                                                 else null)) i
    |> Seq.toList
    |> List.rev

  let getJumps (i:Instruction) =
    let next = i.Next
    if i.OpCode = OpCodes.Switch then
      (i, getJumpChain next, next.Offset, -1) :: (i.Operand :?> Instruction[]
      |> Seq.mapi (fun k d -> i,getJumpChain d,d.Offset,k)
      |> Seq.toList)
    else
    let jump = i.Operand :?> Instruction
    //match Seq.unfold (fun (state:Cil.Instruction) -> if isNull state || state.Offset > jump.Offset then None else Some (state, state.Next)) i
    //        |> findSequencePoint with // TODO -- more filtering
    //| Some x ->
    [
            (i, getJumpChain next, next.Offset, -1)
            (i, getJumpChain jump, jump.Offset, 0)
        ]
    //| _ -> []

  let private VisitMethod (m:MethodDefinition) (included:Inspect) =
            let rawInstructions = m.Body.Instructions
            let dbg = m.DebugInformation
            let instructions = [rawInstructions |> Seq.cast]
                               |> Seq.filter (fun _ -> dbg |> isNull |> not)
                               |> Seq.concat
                               |> Seq.filter (fun (x:Instruction) -> if dbg.HasSequencePoints then
                                                                        let s = dbg.GetSequencePoint x
                                                                        (not << isNull) s && s.StartLine <> 0xfeefee
                                                                     else false)
                               |> Seq.toList

            let number = instructions.Length
            let point = PointNumber
            PointNumber <- point + number

            let interesting = IsInstrumented included

            let sp = if  interesting && instructions |> Seq.isEmpty && rawInstructions |> Seq.isEmpty |> not then
                        rawInstructions
                        |> Seq.take 1
                        |> Seq.map (fun i -> MethodPoint (i, None, m.MetadataToken.ToInt32(), interesting))
                     else
                        instructions.OrderByDescending(fun (x:Instruction) -> x.Offset)
                        |> Seq.mapi (fun i x -> let s = dbg.GetSequencePoint(x)
                                                MethodPoint (x, s |> SeqPnt.Build |> Some,
                                                        i+point, interesting && (s.Document.Url |>
                                                                 IsIncluded |>
                                                                 IsInstrumented)))

            let bp = if instructions.Any() && ReportKind() = Base.ReportFormat.OpenCover then
                        [rawInstructions |> Seq.cast]
                               |> Seq.filter (fun _ -> dbg |> isNull |> not)
                               |> Seq.concat
                               |> Seq.filter (fun (i:Instruction) -> i.OpCode.FlowControl = FlowControl.Cond_Branch)
                               |> Seq.map (fun (i:Instruction) ->     getJumps i
                                                                      |> List.groupBy (fun (_,_,o,_) -> o)
                                                                      |> List.map (fun (_,records) ->
                                                                                     let (from, target, _, _) = Seq.head records
                                                                                     (from, target, records
                                                                                                    |> List.map (fun (_,_,_,n) -> n)
                                                                                                    |> List.sort))
                                                                      |> List.sortBy (fun (_, _, l) -> l.Head)
                                                                      |> indexList)
                               |> Seq.filter (fun l -> l.Length > 1)
                               |> Seq.collect id
                               |> Seq.mapi (fun i (path, (from, target, indexes)) ->
                                                             Seq.unfold (fun (state:Cil.Instruction) -> if isNull state then None else Some (state, state.Previous)) from
                                                             |> (findSequencePoint dbg)
                                                             |> Option.map (fun context ->
                                                                                    BranchPoint { Path = path
                                                                                                  Indexes = indexes
                                                                                                  Uid = i + BranchNumber
                                                                                                  Start = from
                                                                                                  StartLine = context.StartLine
                                                                                                  Offset = from.Offset
                                                                                                  Target = target |> List.map (fun i -> i.Offset)
                                                                                                  Document = context.Document.Url
                                                                                                  } ))
                               |> Seq.choose id |> Seq.toList
                     else []
            BranchNumber <- BranchNumber + List.length bp
            Seq.append sp bp

  let rec internal Deeper node =
    // The pattern here is map x |> map y |> map x |> concat => collect (x >> y >> z)
    match node with
    | Start paths -> StartVisit paths  BuildSequence
    | Assembly (a, included) ->  VisitAssembly a included BuildSequence
    | Module (x, included) ->  VisitModule x included BuildSequence
    | Type (t, included) -> VisitType t included BuildSequence
    | Method (m, included, _) -> VisitMethod m included
    | _ -> Seq.empty<Node>

  and internal BuildSequence node =
    Seq.concat [ ToSeq node ; Deeper node ; node.After() ]

  let internal invoke (node : Node) (visitor:Fix<Node>)  =
    visitor.Invoke(node)

  let internal apply (visitors : list<Fix<Node>>) (node : Node) =
    visitors |>
    List.map (invoke node)

  let internal Visit (visitors : list<Fix<Node>>) (assemblies : seq<string>) =
    ZeroPoints()
    MethodNumber <- 0
    Start assemblies
    |> BuildSequence
    |> Seq.fold apply visitors
    |> ignore

  let EncloseState (visitor : 'State -> 'T -> 'State) (current : 'State) =
    let rec stateful l = new Fix<'T> (
                           fun (node:'T) ->
                           let next = visitor l node
                           stateful next)
    stateful current