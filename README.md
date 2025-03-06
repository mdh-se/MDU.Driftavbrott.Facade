# MDU.Driftavbrott.Facade
MDU.Driftavbrott.Facade är en nuget-paket byggt på dotnet 8, C# 12.0, och Microsoft.NET.Sdk SDK för användning 
i MDUs integrationskomponenter och applikationer. 

En dotnet-facade som kan kommunicera med mdh-driftavbrott-service.

## Användning, konfiguration i appsettings

Här följer ett exempel på lite C#-kod:

```C#
// Registrera som Service
    .ConfigureServices((hostContext, services) =>
            {
                services.Configure<DriftavbrottFacadeSettings>(hostContext.Configuration.GetSection("DriftavbrottFacadeSettings"));
                services.AddSingleton<IDriftavbrottFacade, DriftavbrottFacade>();
            })
     ...

// Begär instans
var driftavbrottFacade = host.Services.GetRequiredService<IDriftavbrottFacade>();

// Registrera eventlyssnare

driftavbrottFacade.DriftavbrottChanged += (sender, dae) =>
        {
            // Hantera driftavbrott här
        };
        
// Starta monitorering
driftavbrottFacade.StartDriftavbrottMonitor();


```

## Konfiguration
````json
"DriftavbrottFacadeSettings": {
        "Url": "http://integration-linux-dev.ita.mdh.se:3301/mdh-driftavbrott/v1",
        "Kanaler": [
            "ladok.backup"
        ],
        "System": "MDU.Exempel.IC",
        "MonitorIntervalInSeconds": 60
    }
```