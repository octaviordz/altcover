﻿namespace AltCover

open System.Diagnostics.CodeAnalysis
open System.Reflection

[<AutoSerializable(false)>]
[<SuppressMessage("Gendarme.Rules.Smells",
                  "AvoidSpeculativeGeneralityRule",
                  Justification = "It's not speculative")>]
[<SuppressMessage("Gendarme.Rules.Maintainability",
                  "VariableNamesShouldNotMatchFieldNamesRule",
                  Justification = "Compiler generated")>]
type Icons<'TIcon>(toIcon: System.IO.Stream -> 'TIcon) =
  let makeIcon name =
    lazy
     try
      (toIcon (
        Assembly
          .GetExecutingAssembly()
          .GetManifestResourceStream("AltCover.UICommon." + name + ".png")
      ))
     with
     | x -> System.InvalidOperationException(name, x) |> raise

  member self.Xml = makeIcon "XMLFile_16x"
  member self.Assembly = makeIcon "Assembly_16x"
  member self.Event = makeIcon "Event_16x"
  member self.Namespace = makeIcon "Namespace_16x"
  member self.Module = makeIcon "Module_16x"
  member self.Effect = makeIcon "Effect_16x" // TODO -- why are exceptions detected as such?
  member self.Class = makeIcon "Class_16x"
  member self.Property = makeIcon "Property_16x"
  member self.Method = makeIcon "Method_16x"
  member self.Branched = makeIcon "Branch_12x_16x_grn" // maybe
  member self.Branch = makeIcon "Branch_12x_16x_ylw" // maybe + Branch_12x_16x
  member self.RedBranch = makeIcon "Branch_12x_16x_red" // maybe
  member self.Blank = makeIcon "Blank_12x_16x" // maybe
  member self.TreeExpand = makeIcon "ExpandRight_16x"
  member self.TreeCollapse = makeIcon "CollapseChevronDown_16x" // TODO
  member self.MRU = makeIcon "ExpandDown_16x"
  member self.Source = makeIcon "TextFile_16x"

  member self.MRUInactive =
    makeIcon "ExpandDown_lightGray_16x"

  member self.RefreshActive = makeIcon "Refresh_16x"
  member self.Refresh = makeIcon "Refresh_greyThin_16x"
  member self.Info = makeIcon "StatusInformation_32x" // TODO
  member self.Warn = makeIcon "StatusWarning_32x" // TODO
  member self.Error = makeIcon "StatusCriticalError_32x" // TODO
  member self.Open = makeIcon "OpenFile_16x"
  member self.Font = makeIcon "Font_16x"
  member self.ShowAbout = makeIcon "VSTAAbout_16x"
  member self.Exit = makeIcon "Exit_16x"
  member self.Logo = makeIcon "logo"

  member self.VIcon =
    let makeIco name =
      toIcon (
        Assembly
          .GetExecutingAssembly()
          .GetManifestResourceStream("AltCover.UICommon." + name + ".ico")
      )

    makeIco "VIcon"