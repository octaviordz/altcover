# ConvertToCoverageJsonCommand class

Creates a JSON format report from other report formats.

Takes either OpenCover or classic NCover format input as an `XDocument`, as an argument or from the object pipeline. Writes the JSON report to a string.

```csharp
ConvertTo-CoverageJson -InputFile "./Tests/HandRolledMonoCoverage.xml"
```

```csharp
public class ConvertToCoverageJsonCommand : PSCmdlet
```

## Public Members

| name | description |
| --- | --- |
| [ConvertToCoverageJsonCommand](ConvertToCoverageJsonCommand/ConvertToCoverageJsonCommand-apidoc)() | The default constructor. |
| [InputFile](ConvertToCoverageJsonCommand/InputFile-apidoc) { get; set; } | Input as file path |
| [XDocument](ConvertToCoverageJsonCommand/XDocument-apidoc) { get; set; } | Input as `XDocument` value |
| override [ProcessRecord](ConvertToCoverageJsonCommand/ProcessRecord-apidoc)() | Create transformed document |

## See Also

* namespace [AltCover.Commands](../AltCover.PowerShell-apidoc)

<!-- DO NOT EDIT: generated by xmldocmd for AltCover.PowerShell.dll -->