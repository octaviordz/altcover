﻿namespace AltCover

open System
open System.IO
open System.Xml.Linq
open System.Globalization
open System.Text

[<System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704",
    Justification="'Json' is jargon")>]
module internal Json =
  let internal path : Option<string> ref = ref None

  // --- Generic XML to JSON helpers ---

  [<Sealed>]
  type BuildWriter() =
    inherit TextWriter(CultureInfo.InvariantCulture)
    member val Builder:StringBuilder = null with get, set
    member self.Clear() = let temp = self.Builder
                          self.Builder <- null
                          temp
    override self.Encoding = Encoding.Unicode // pointless but required
    [<System.Diagnostics.CodeAnalysis.SuppressMessage(
      "Gendarme.Rules.Exceptions", "UseObjectDisposedExceptionRule",
      Justification="Would be meaningless")>]
    override self.Write(value:Char) =
        value
        |> self.Builder.Append
        |> ignore
    override self.Write(value:String) =
        value
        |> self.Builder.Append
        |> ignore

  let buildWriter = new BuildWriter()

  let internal escapeString (s:String) =
    System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(buildWriter, s)

  let internal appendSimpleValue (builder:StringBuilder) (value:string) =
    let b,v = Double.TryParse value
    if b then builder.AppendFormat(CultureInfo.InvariantCulture, "{0}", v) |> ignore
    else
      let b2, v2 = Boolean.TryParse value
      if b2 then buildWriter.Write(if v2 then "true" else "false") |> ignore
      else
        buildWriter.Write '"'
        escapeString value
        buildWriter.Write '"'

  let internal appendSequence builder topComma (sequence:(StringBuilder -> unit)seq)  =
    sequence
    |> Seq.fold (fun comma item ->
       if comma
       then buildWriter.Write ','
       item builder
       true
      ) topComma

  let internal appendSingleTime (builder:StringBuilder) (value:string) =
    buildWriter.Write '"'
    value
    |> Int64.TryParse
    |> snd
    |> BitConverter.GetBytes
    |> Convert.ToBase64String
    |> escapeString
    buildWriter.Write '"'

  let internal formatTimeList (builder:StringBuilder) (value:string) =
    buildWriter.Write '['
    value.Split([|';'|], StringSplitOptions.RemoveEmptyEntries)
    |> Seq.map (fun v -> fun b -> appendSingleTime b v)
    |> appendSequence builder false
    |> ignore
    buildWriter.Write ']'

  let specialValues = [
                       ("time", appendSingleTime)
                       ("offsetchain", (fun builder value ->
                          buildWriter.Write '['
                          value.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
                          |> Seq.filter (Int32.TryParse >> fst)
                          |> Seq.map (fun v -> fun b -> v |> (builder.Append >> ignore))
                          |> appendSequence builder false
                          |> ignore
                          buildWriter.Write ']'))
                       ("entry", formatTimeList)
                       ("exit", formatTimeList)
                      ]
                      |> Map.ofSeq

  let internal appendMappedElement
    (builder:StringBuilder)
    (topComma:bool)
    (xElement : XElement) =

    xElement.Attributes()
    |> Seq.filter (fun a -> a.Name.ToString().StartsWith("{", StringComparison.Ordinal) |> not)
    |> Seq.map (fun a -> (fun (b:StringBuilder) ->
          let local = a.Name.LocalName
          b.Append("\"")
           .Append(local) // known safe
           .Append("\":") |> ignore
          let mapped = specialValues
                       |> Map.tryFind local
                       |> Option.defaultValue appendSimpleValue

          mapped builder a.Value))
    |> (appendSequence builder topComma)

  let internal appendSimpleElement builder (xElement : XElement) =
    appendMappedElement builder false xElement

  let internal applyHeadline report (tag:string) =
    let builder = StringBuilder()
    buildWriter.Builder <- builder
    buildWriter.Write tag
    (builder, appendSimpleElement builder report)

  let close() =
    buildWriter.Write("}}")
    buildWriter.Clear()// return value

  // --- OpenCover ---

  [<System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Gendarme.Rules.Smells", "AvoidLongParameterListsRule",
    Justification = "Should the need ever arise...")>]
  let internal appendInternalElements next group key
    (builder:StringBuilder) (comma:bool) (x:XContainer) =
    let mutable first = true

    x.Elements(XName.Get group)
    |> Seq.collect(fun xx -> xx.Elements(XName.Get key))
    |> Seq.map (fun xx -> (fun (b:StringBuilder) ->
      if first
      then
        if comma
        then buildWriter.Write ','
        b.Append("\"")
         .Append(key)
         .Append("\":[")
         |> ignore
      first <- false
      buildWriter.Write '{'
      let c2 = appendSimpleElement b xx
      next b c2 xx |> ignore
      buildWriter.Write '}'
    ))

    |> appendSequence builder false // (sequence:(StringBuilder -> unit)seq)
    |> ignore

    let result = first |> not
    if result
    then builder.Append ("]")|> ignore
    result

  let internal appendItemSummary (builder:StringBuilder) comma (x:XContainer) =
    let mutable topComma = comma
    x.Elements(XName.Get "Summary")
    |> Seq.tryHead
    |> Option.iter (fun s -> if topComma then buildWriter.Write ',' |> ignore
                             appendSimpleElement (builder.Append("\"Summary\":{")) s |> ignore
                             buildWriter.Write '}'
                             topComma <- true)
    topComma

  let openCoverSink _ _ _ = false

  let internal appendPointTracking (builder:StringBuilder) comma (x:XContainer) =
    let mutable topComma = appendInternalElements openCoverSink "Times" "Time" builder comma x
    appendInternalElements openCoverSink "TrackedMethodRefs" "TrackedMethodRef" builder topComma x

  let internal appendMethodPoints (builder:StringBuilder) comma (x:XContainer) =
    let mutable topComma = comma
    [
      "Summary"
      "FileRef"
      "MethodPoint"
    ]
    |> Seq.iter (fun name ->
      x.Elements(XName.Get name)
      |> Seq.tryHead
      |> Option.iter (fun s -> if topComma then buildWriter.Write ',' |> ignore
                               appendSimpleElement (
                                 builder.Append('"')
                                        .Append(name)
                                        .Append( "\":{")) s |> ignore
                               buildWriter.Write '}'
                               topComma <- true))

    [
      "MetadataToken"
      "Name"
    ]
    |> Seq.iter  (fun tag ->
        x.Elements(XName.Get tag)
        |> Seq.tryHead
        |> Option.iter (fun s -> if topComma then buildWriter.Write ',' |> ignore
                                 appendSimpleValue
                                   (builder.Append('"')
                                           .Append(tag)
                                           .Append("\":"))
                                   s.Value
                                 topComma <- true))
    topComma <- appendInternalElements appendPointTracking "SequencePoints" "SequencePoint" builder topComma x
    appendInternalElements appendPointTracking "BranchPoints" "BranchPoint" builder topComma x

  let internal appendClassMethods (builder:StringBuilder) comma (x:XContainer) =
    let mutable topComma = appendItemSummary builder comma x
    [
        "FullName"
    ]
    |> Seq.iter  (fun tag ->
        x.Elements(XName.Get tag)
        |> Seq.tryHead
        |> Option.iter (fun s -> if topComma then buildWriter.Write ',' |> ignore
                                 appendSimpleValue
                                   (builder.Append('"')
                                           .Append(tag)
                                           .Append("\":"))
                                   s.Value
                                 topComma <- true))
    topComma <- appendInternalElements appendMethodPoints "Methods" "Method" builder topComma x

  let internal appendModuleInternals (builder:StringBuilder) comma (x:XContainer) =
    let mutable topComma = appendItemSummary builder comma x
    [
        "FullName"
        "ModulePath"
        "ModuleTime"
        "ModuleName"
    ]
    |> Seq.iter  (fun tag ->
        x.Elements(XName.Get tag)
        |> Seq.tryHead
        |> Option.iter (fun s -> if topComma then buildWriter.Write ',' |> ignore
                                 appendSimpleValue
                                   (builder.Append('"')
                                           .Append(tag)
                                           .Append("\":"))
                                   s.Value
                                 topComma <- true))

    topComma <- appendInternalElements openCoverSink "Files" "File" builder comma x
    topComma <- appendInternalElements appendClassMethods "Classes" "Class" builder topComma x
    appendInternalElements openCoverSink "TrackedMethods" "TrackedMethod" builder topComma x

  let internal appendSessionModules (builder:StringBuilder) comma (x:XContainer) =
    appendInternalElements appendModuleInternals "Modules" "Module" builder comma x

  let opencoverToJson report =
    let (builder,comma) = applyHeadline report "{\"CoverageSession\":{"
    let topComma = appendItemSummary builder comma report
    appendSessionModules builder topComma report |> ignore
    close()

  // --- NCover ---

  let internal appendNCoverElements next key
    (builder:StringBuilder) (comma:bool) (x:XContainer) =
    let mutable first = true

    x.Elements(XName.Get key)
    |> Seq.map (fun xx -> (fun (b:StringBuilder) ->
      if first
      then
        if comma
        then buildWriter.Write ','
        b.Append("\"")
         .Append(key)
         .Append("\":[")
         |> ignore
      first <- false
      buildWriter.Write '{'
      let c2 = appendSimpleElement b xx
      next b c2 xx |> ignore
      buildWriter.Write '}'
    ))

    |> appendSequence builder false // (sequence:(StringBuilder -> unit)seq)
    |> ignore

    let result = first |> not
    if result
    then builder.Append ("]")|> ignore
    result

  let internal appendMethodSeqpnts (builder:StringBuilder) comma (x:XContainer) =
    appendNCoverElements (fun _ _ _ -> false) "seqpnt" builder comma x

  let internal appendModuleMethods (builder:StringBuilder) comma (x:XContainer) =
    appendNCoverElements appendMethodSeqpnts "method" builder comma x

  let internal appendCoverageModules (builder:StringBuilder) comma (x:XContainer) =
    appendNCoverElements appendModuleMethods "module" builder comma x

  let ncoverToJson report =
    let (builder,comma) = applyHeadline report "{\"coverage\":{"
    appendCoverageModules builder comma report |> ignore
    close()

  let internal convertReport (report : XDocument) (format:ReportFormat) (stream : Stream) =
    doWithStream (fun () -> new StreamWriter(stream)) (fun writer ->
        (report.Root
        |> (match format with
            | ReportFormat.NCover -> ncoverToJson
            | _ -> opencoverToJson)).ToString() // ready minified
        |> writer.Write)

  let internal summary (report : XDocument) (format : ReportFormat) result =
    doWithStream(fun () -> File.OpenWrite(!path |> Option.get))
      (convertReport report format)
    (result, 0uy, String.Empty)