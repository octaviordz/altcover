#addin "nuget:?package=Microsoft.TestPlatform.ObjectModel&Version=16.1.1"
#addin "nuget:?package=PowerShellStandard.Library&Version=5.1.0"
#addin "nuget:file://{0}?package=altcover.cake&Version={1}"

var target = Argument("target", "Test");
var configuration = Argument("configuration", "Debug");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .WithCriteria(c => HasArgument("rebuild"))
    .Does(() =>
{
    CleanDirectory($"../_Binaries/cake_dotnettest/{configuration}+AnyCPU");
});

Task("Build")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetBuild("./_DotnetTest/cake_dotnettest.fsproj", new DotNetBuildSettings
    {
        Configuration = configuration,
    });
});

  class TestOptions : AltCover.DotNet.ICLIOptions
  {
    public bool ForceDelete => false;
    public bool FailFast => false;
    public string ShowSummary => String.Empty;
  }

  class TestPrepareOptions : AltCover.Abstract.IPrepareOptions
  {
    public IEnumerable<string> InputDirectories => Array.Empty<string>();
    public IEnumerable<string> OutputDirectories => Array.Empty<string>();
    public IEnumerable<string> SymbolDirectories => Array.Empty<string>();
    public IEnumerable<string> Dependencies => Array.Empty<string>();
    public IEnumerable<string> Keys => Array.Empty<string>();
    public string StrongNameKey => String.Empty;
    public string Report => String.Empty;
    public IEnumerable<string> FileFilter => Array.Empty<string>();
    public IEnumerable<string> AssemblyFilter => Array.Empty<string>();
    public IEnumerable<string> AssemblyExcludeFilter => Array.Empty<string>();
    public IEnumerable<string> TypeFilter => Array.Empty<string>();
    public IEnumerable<string> MethodFilter => Array.Empty<string>();
    public IEnumerable<string> AttributeFilter => Array.Empty<string>();
    public IEnumerable<string> PathFilter => Array.Empty<string>();
    public IEnumerable<string> AttributeTopLevel => Array.Empty<string>();
    public IEnumerable<string> TypeTopLevel => Array.Empty<string>();
    public IEnumerable<string> MethodTopLevel => Array.Empty<string>();
    public IEnumerable<string> CallContext => new string[] {"[Fact]", "0"};
    public string ReportFormat => String.Empty;
    public bool InPlace => false;
    public bool Save => false;
    public bool ZipFile => false;
    public bool MethodPoint => false;
    public bool SingleVisit => false;
    public bool LineCover => false;
    public bool BranchCover => false;
    public IEnumerable<string> CommandLine => Array.Empty<string>();
    public bool ExposeReturnCode => true;
    public bool SourceLink => true;
    public bool Defer => true;
    public bool LocalSource => true;
    public bool VisibleBranches => false;
    public string ShowStatic => String.Empty;
    public bool ShowGenerated => false;
    public System.Diagnostics.TraceLevel Verbosity => System.Diagnostics.TraceLevel.Verbose;
  }

  class TestCollectOptions : AltCover.Abstract.ICollectOptions
  {
    public string RecorderDirectory => String.Empty;
    public string WorkingDirectory => String.Empty;
    public string Executable => String.Empty;
    public string LcovReport => String.Empty;
    public string Threshold => String.Empty;
    public string Cobertura => String.Empty;
    public string OutputFile => String.Empty;
    public IEnumerable<string> CommandLine => Array.Empty<string>();
    public bool ExposeReturnCode => true;
    public string SummaryFormat => String.Empty;
    public System.Diagnostics.TraceLevel Verbosity => System.Diagnostics.TraceLevel.Verbose;
  }

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    using AltCover.Cake;

    var testv = Version();
    Console.WriteLine("Version = {1}", null, testv);

    var im = ImportModule();
    Console.WriteLine("Import = {1}", null, im);

    var altcoverSettings = new CoverageSettings {
        PreparationPhase = new TestPrepareOptions(),
        CollectionPhase = new TestCollectOptions(),
        Options = new TestOptions()
    };

    var testSettings = new DotNetTestSettings {
        Configuration = configuration,
        NoBuild = true,
    };

    DotNetCoverTest("./_DotnetTest", 
                    // this strips down to just the project file name
                    // losing the relative directory
                    //"./_DotnetTest/cake_dotnettest.fsproj",
                      testSettings, altcoverSettings);
});

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);