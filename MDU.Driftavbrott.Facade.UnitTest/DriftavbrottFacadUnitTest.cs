using Microsoft.Extensions.Logging.Abstractions;

namespace SE.MDU.Driftavbrott.Facade.UnitTest;

public class DriftavbrottFacadUnitTest
{
    [Fact]
    public void TestDriftavbrottMonitor()
    {
        
        CancellationTokenSource testSource = new CancellationTokenSource();
        CancellationToken testToken = testSource.Token;
        // setup
        var mockFacad = new Mock<IDriftavbrottFacade>();
        mockFacad.SetupSequence(facade => facade.GetPagaendeDriftavbrottAsync())
            .ReturnsAsync(new List<DriftavbrottType>()
                { new() { Kanal = "alltid", MeddelandeSv = "Testavbrott", MeddelandeEn = "Testinterruption" } })
            .ReturnsAsync(Enumerable.Empty<DriftavbrottType>())
            .ThrowsAsync(new ApplicationException("Driftavbrottstjänsten svarar inte."));

        DriftavbrottMonitor monitorUnderTest = new DriftavbrottMonitor(mockFacad.Object, NullLogger<DriftavbrottFacade>.Instance);

        List<DriftavbrottEventArgs> eventsList = new List<DriftavbrottEventArgs>();
        List<DriftavbrottErrorEventArgs> errorList = new List<DriftavbrottErrorEventArgs>();

       monitorUnderTest.DriftavbrottChanged += MonitorUnderTestOnDriftavbrottChanged;

        void MonitorUnderTestOnDriftavbrottChanged(object? sender, DriftavbrottEventArgs e)
        {
            eventsList.Add(e);
        }

        monitorUnderTest.DriftavbrottError += MonitorUnderTestOnDriftavbrottError;

        void MonitorUnderTestOnDriftavbrottError(object? sender, DriftavbrottErrorEventArgs e)
        {
            errorList.Add(e);
        }
        
        // act
        //Disablar varning här: vi behöver inte await här, vi vill att exekveringen ska fortsätta till SpinWait, annars fortsätter testet in perpetuum.
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        monitorUnderTest.MonitorAsync(testToken, 2);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        
        // Vänta 17 sekunder. Vi borde få 1 event under tiden
        SpinWait.SpinUntil(() => eventsList.Count == 2 && errorList.Count == 1, TimeSpan.FromSeconds(7));
        
        // verify
        // att vi har fått 2 event med rätt AvbrottStatus
        Assert.Collection(eventsList,
            @event => Assert.Equal(DriftavbrottEventArgs.AvbrottStatus.Aktivt, @event.Status),
            @event => Assert.Equal(DriftavbrottEventArgs.AvbrottStatus.Avslutat, @event.Status));
        
        Assert.Single(errorList);
        
    }
    [Fact]
    public void TestFacadeConfiguration()
    {
        //setup
        var logFilePath = "MDU.Driftavbrott.Facade.UnitTest.log";
        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.test.json");
            })
            .ConfigureLogging((hostContext, logging) =>
            {
                LogManager.Configuration = new NLogLoggingConfiguration(hostContext.Configuration.GetSection("Logging:NLog"));
                var fileTarget = (FileTarget)LogManager.Configuration.FindTargetByName("file");
                if (fileTarget != null)
                {
                    fileTarget.FileName = logFilePath;
                }
                LogManager.ReconfigExistingLoggers();
                logging.ClearProviders();
                logging.AddNLog();
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.Configure<DriftavbrottFacadeSettings>(hostContext.Configuration.GetSection("DriftavbrottFacadeSettings"));
                services.AddSingleton<IDriftavbrottFacade, DriftavbrottFacade>();
            }).Build();

        // act
        var facade = host.Services.GetRequiredService<IDriftavbrottFacade>();
        facade.StartDriftavbrottMonitor();

        var currentConfig = facade.CurrentConfig;
        
        facade.Dispose();

        //facade.StopDriftavbrottMonitor();
        
        // verify
        Assert.Equal("http://integration-linux-dev.ita.mdh.se:3301/mdh-driftavbrott/v1", currentConfig.Url);
        Assert.Equal(new []{"alltid"}, currentConfig.Kanaler);
        Assert.Equal("MDU.Driftavbrott.Facade.UnitTest", currentConfig.System);
        Assert.Equal(60, currentConfig.MonitorIntervalInSeconds);
    }
}