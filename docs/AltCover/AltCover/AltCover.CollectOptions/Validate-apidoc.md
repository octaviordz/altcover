# AltCover.CollectOptions.Validate method

Does simple checking of the arguments without causing any changes to the system

```csharp
public string[] Validate(bool afterPreparation)
```

| parameter | description |
| --- | --- |
| afterPreparation | `true` if the Prepare step has already run and there should be instrumented code the `RecorderDirectory` |

## Return Value

All the problems that the application command-line could report, so empty is success.

## See Also

* class [CollectOptions](../AltCover.CollectOptions-apidoc)
* namespace [AltCover](../../AltCover-apidoc)

<!-- DO NOT EDIT: generated by xmldocmd for AltCover.exe -->