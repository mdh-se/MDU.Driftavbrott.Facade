using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using NLog.Targets;
using SE.MDU.Driftavbrott.Facade.Interfaces;

namespace SE.MDU.Driftavbrott.Facade.IntegrationTest;

public class DriftavbrottFacadIntegrationTest
{
    public IHost GetTestHost()
    {
        //setup
        var logFilePath = "MDU.Driftavbrott.Facade.IntegrationTest.log";
        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.integrationtest.json");
            })
            .ConfigureLogging((hostContext, logging) =>
            {
                LogManager.Configuration = new NLogLoggingConfiguration(hostContext.Configuration.GetSection("Logging:NLog"));
                var fileTarget = LogManager.Configuration.FindTargetByName("file") as FileTarget;
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
                services.AddTransient<IDriftavbrottFacade, DriftavbrottFacade>();
            }).Build();

        return host;
    }
    
    [Fact]
    public async Task TestGetPagaendeDriftavbrott()
    {   
        // setup
        var testHost = GetTestHost();
        await testHost.StartAsync(TestContext.Current.CancellationToken);
        var testFacade = testHost.Services.GetRequiredService<IDriftavbrottFacade>();
        // act
        var driftavbrott = await testFacade.GetPagaendeDriftavbrottAsync();

        //testFacade.Dispose();
        await testHost.StopAsync(TestContext.Current.CancellationToken);
        
        
        // assert
        foreach (DriftavbrottType driftavbrottType in driftavbrott)
        {
            Assert.IsType<DriftavbrottType>(driftavbrottType);
        }
    }

    [Fact]
    public async Task TestDriftavbrottMonitor()
    {
        // setup
        List<DriftavbrottEventArgs> eventsList = new List<DriftavbrottEventArgs>();
        var testHost = GetTestHost();
        await testHost.StartAsync(TestContext.Current.CancellationToken);
        var driftavbrottFacade = testHost.Services.GetRequiredService<IDriftavbrottFacade>();

        // act
        driftavbrottFacade.DriftavbrottChanged += (sender, dae) =>
        {
            eventsList.Add(dae);
        };
        
        driftavbrottFacade.StartDriftavbrottMonitor();

        // Vänta 17 sekunder. Vi borde få 1 event under tiden
        SpinWait.SpinUntil(() => eventsList.Count == 2, TimeSpan.FromSeconds(7));
        
        //driftavbrottFacade.Dispose();
        await testHost.StopAsync(TestContext.Current.CancellationToken);
        
        Thread.Sleep(TimeSpan.FromSeconds(3));
        
        // verify
        // att vi har fått 1 event med rätt typ
        Assert.Collection(eventsList, 
            @event => Assert.Equal(DriftavbrottEventArgs.AvbrottStatus.Aktivt, @event.Status));
    }
}