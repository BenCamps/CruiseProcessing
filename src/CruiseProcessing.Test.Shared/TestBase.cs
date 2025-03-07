using Bogus;
using CruiseDAL;
using CruiseProcessing.Data;
using CruiseProcessing.Interop;
using CruiseProcessing.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Collections;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using Xunit.Abstractions;

namespace CruiseProcessing.Test;

public class TestBase : IDisposable
{
    protected ITestOutputHelper Output { get; }
    protected Randomizer Rand { get; }
    protected Stopwatch _stopwatch;
    private string _testTempPath;
    private IHost? _implicitHost;
    private bool disposedValue;

    protected LogLevel LogLevel { get; set; } = LogLevel.Information;

    private List<string> FilesToBeDeleted { get; } = new List<string>();

    private List<IDisposable>? ObjectsToBeDisposed { get; set; } = new List<IDisposable>();

    public TestBase(ITestOutputHelper output)
    {
        Output = output;
        Rand = new Randomizer(this.GetType().Name.GetHashCode()); // make the randomizer fixed based on the test class

        var testType = this.GetType();
        TestName = testType.Name;
        TestNamespace = testType.Namespace;
        TestAssemblyName = testType.Assembly.GetName().Name;

        Output.WriteLine($"Test {TestName}");
        Output.WriteLine($"Test Assembly {TestAssemblyName}");
        //LoggerFactory = new TestLoggerFactory
        //{
        //    Output = output,
        //    MinLogLevel = minLogLevel
        //};
        //CruiseProcessing.Services.Logging.LoggerProvider.DefaultLoggerFactory = LoggerFactory;
    }

    public IHost ImplicitHost => _implicitHost ??= CreateTestHost();

    public string TestExecutionDirectory
    {
        get
        {
            var codeBase = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            return Path.GetDirectoryName(codeBase);
        }
    }

    public string TestTempPath => _testTempPath ??= Path.Combine(Path.GetTempPath(), "TestTemp",
        TestAssemblyName, TestNamespace, TestName);

    protected IHost CreateTestHost(Action<IServiceCollection> configureServices = null)
    {
        var builder = new HostBuilder()
            .ConfigureServices(ConfigureServices)
            .ConfigureLogging(ConfigureLogging);

        if (configureServices != null)
        {
            builder.ConfigureServices(configureServices);
        }

        var host = builder.Build();
        CruiseProcessing.Services.Logging.LoggerProvider.Initialize(host.Services);

        return host;
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IDialogService>((x) => Substitute.For<IDialogService>());
        services.AddTransient<IVolumeLibrary, VolumeLibrary>();
    }

    protected virtual void ConfigureLogging(ILoggingBuilder loggingBuilder)
    {
        var testLoggerProvider = new TestLoggingProvider(Output, LogLevel);
        loggingBuilder.AddProvider(testLoggerProvider);
    }

    public string TestFilesDirectory => Path.Combine(TestExecutionDirectory, "TestFiles");

    public string TestName { get; }
    public string TestNamespace { get; }
    public string TestAssemblyName { get; }

    public void StartTimer()
    {
        _stopwatch = new Stopwatch();
        Output.WriteLine("Stopwatch Started");
        _stopwatch.Start();
    }

    public void EndTimer()
    {
        _stopwatch.Stop();
        Output.WriteLine("Stopwatch Ended:" + _stopwatch.ElapsedMilliseconds.ToString() + "ms");
    }

    public string GetTempFilePath(string extention, string fileName = null)
    {
        var testTempPath = TestTempPath;
        if (Directory.Exists(testTempPath) is false)
        {
            Directory.CreateDirectory(testTempPath);
        }

        // note since Rand is using a fixed see the guid generated will
        var tempFilePath = Path.Combine(testTempPath, (fileName ?? Rand.Guid().ToString()) + extention);
        Output.WriteLine($"Temp File Path Generated: {tempFilePath}");
        return tempFilePath;
    }

    public string GetTestFile(string fileName)
    {
        var sourcePath = Path.Combine(TestFilesDirectory, fileName);
        return GetTestFileFromFullPath(sourcePath);
    }

    protected string GetTestFileFromFullPath(string sourcePath)
    {
        var fileName = Path.GetFileName(sourcePath);
        if (File.Exists(sourcePath) == false) { throw new FileNotFoundException(sourcePath); }

        var targetPath = Path.Combine(TestTempPath, fileName);
        var targetDir = Path.GetDirectoryName(targetPath);
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        RegesterFileForCleanUp(targetPath);
        File.Copy(sourcePath, targetPath, true);
        return targetPath;
    }

    protected CpDataLayer GetCpDataLayer(string filePath)
    {
        var fileExtention = System.IO.Path.GetExtension(filePath);
        if (fileExtention == ".crz3")
        {
            var migrator = new DownMigrator();
            var v3Db = new CruiseDatastore_V3(filePath);
            RegisterObjectForDisposal(v3Db);

            var processPath = filePath + ".process";
            var v2Db = new DAL(processPath, true);
            RegisterObjectForDisposal(v2Db);
            RegesterFileForCleanUp(processPath);

            var cruiseID = v3Db.QueryScalar<string>("SELECT CruiseID FROM Cruise").First();
            migrator.MigrateFromV3ToV2(cruiseID, v3Db, v2Db);
            Output.WriteLine("Migrated V3 file to: " + processPath);
            return new CpDataLayer(v2Db, v3Db, cruiseID, CreateLogger<CpDataLayer>(), biomassOptions: null);
        }
        else
        {
            var db = new DAL(filePath);
            RegisterObjectForDisposal(db);
            return new CpDataLayer(db, CreateLogger<CpDataLayer>(), biomassOptions: null);
        }
    }

    public void RegesterFileForCleanUp(string path)
    {
        FilesToBeDeleted.Add(path);
    }

    protected void RegisterObjectForDisposal(IDisposable obj)
    {
        ObjectsToBeDisposed.Add(obj);
    }

    /// <summary>
    /// creates a logger that uses the log level set on the test base
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public ILogger<T> CreateLogger<T>()
    {
        return ImplicitHost.Services.GetRequiredService<ILogger<T>>();
    }

    ~TestBase()
    {
        Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            foreach (var file in FilesToBeDeleted)
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // do nothing
                }
            }
            FilesToBeDeleted.Clear();

            foreach (var obj in ObjectsToBeDisposed)
            {
                try
                {
                    obj.Dispose();
                }
                catch
                {
                    // do nothing
                }
            }
            ObjectsToBeDisposed = null;

            _implicitHost?.Dispose();
            _implicitHost = null;

            if (disposing)
            {
                
            }
            
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~TestBase()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}