using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MDU.Logging.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SE.MDU.Driftavbrott.Facade.Configuration;
using SE.MDU.Driftavbrott.Facade.Events;
using SE.MDU.Driftavbrott.Facade.Interfaces;
using SE.MDU.Driftavbrott.Modell;
[assembly: InternalsVisibleTo("MDU.Driftavbrott.Facade.UnitTest")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace SE.MDU.Driftavbrott.Facade;

/// <summary>
/// Facade-klass som används för att fråga efter driftavbrott. Innehåller en publikmetod för att Hämta aktuella driftavbrott för konfigurerade kanaler,
/// samt en Monitor, som indikerar nya driftavbrott genom att signalera Events
/// </summary>
public class DriftavbrottFacade : IDriftavbrottFacade
{
    private readonly DriftavbrottFacadeSettings _settings;
    private ILogger<DriftavbrottFacade> _logger;
    private CancellationTokenSource _monitorCancellationTokenSource;
    private CancellationToken _cancellationToken;
    private Task _driftavbrottMonitorTask;
    private readonly DriftavbrottMonitor _driftavbrottMonitor;
    
    DriftavbrottFacadeSettings IDriftavbrottFacade.CurrentConfig => _settings;
    public event EventHandler<DriftavbrottEventArgs> DriftavbrottChanged;
    public event EventHandler<DriftavbrottErrorEventArgs> DriftavbrottError;

    /// <summary>Konstruktor som även instansierar DriftavbrottsMonitor</summary>
    /// <remarks>Observera att för att prenumerera på Driftavbrott-Events måste även <see cref="StartDriftavbrottMonitor()"/> anropas.</remarks>
    public DriftavbrottFacade(IOptions<DriftavbrottFacadeSettings> config, ILogger<DriftavbrottFacade> logger)
    {
        ConfigurationValidator.ValidateConfiguration(config.Value);
        _monitorCancellationTokenSource = new CancellationTokenSource();
        _cancellationToken = _monitorCancellationTokenSource.Token;
        _settings = config.Value;
        _logger = logger;
        _logger.Info($"Startar DriftavbrottFacade med konfiguration: Url:'{_settings.Url}', Kanaler:'{string.Join(",",_settings.Kanaler)}', System: '{_settings.System}'");
        _driftavbrottMonitor = new DriftavbrottMonitor(this, _logger);
        _driftavbrottMonitor.DriftavbrottChanged += (sender, @event) =>
        {
            DriftavbrottChanged?.Invoke(sender, @event);
        };
        _driftavbrottMonitor.DriftavbrottError += (sender, @event) =>
        {
            DriftavbrottError?.Invoke(sender, @event);
        };
    }

    /// <summary>
    /// Startar DriftavbrottMonitor som kontrollerar aktuella driftavbrott enligt angiven konfiguration
    /// </summary>
    public void StartDriftavbrottMonitor()
    {
        _driftavbrottMonitorTask = _driftavbrottMonitor.MonitorDriftavbrottAsync(_cancellationToken);
    }
    /// <summary>
    /// Hämtar pågående driftavbrott på de kanaler som anges i konfiguration. Om något annat fel inträffar kastas ett ApplicationException.
    /// </summary>
    /// <returns>Ska endast returnera noll eller ett driftavbrott i praktiken</returns>
    /// <exception cref="ApplicationException"></exception>
    public async Task<IEnumerable<DriftavbrottType>> GetPagaendeDriftavbrottAsync()
    {
        List<DriftavbrottType> driftavbrottResult = new List<DriftavbrottType>();

        NameValueCollection queryParameters = HttpUtility.ParseQueryString(string.Empty);

        foreach (var kanal in _settings.Kanaler)
        {
            queryParameters["kanal"] = kanal;
        }

        queryParameters["system"] = _settings.System;

        UriBuilder baseUri = new UriBuilder(_settings.Url.TrimEnd('/') + "/driftavbrott/pagaende");

        baseUri.Query = queryParameters.ToString() ?? string.Empty;

        HttpClient httpClient = new HttpClient();

        httpClient.BaseAddress = baseUri.Uri;

        httpClient.DefaultRequestHeaders.Add("Accept", "application/xml,application/json");

        try
        {
            // Gör anropet
            var response = await httpClient.GetAsync("", _cancellationToken);

            // Fick vi något svar alls?
            if (response != null)
            {
                // Hämta HTTP statuskoden i numerisk form (ex: 200)
                Int32 numericStatusCode = (Int32)response.StatusCode;

                // Servern returnerade 404 eller 406 (HTTP Statuskod=404)
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new ApplicationException(
                        $"Driftavbrottstjänsten returnerade 404/406. ResponseCode={numericStatusCode} {response.StatusCode}, ResponseServer={response.Headers.Server}, RequestBaseUrl={baseUri.Uri.PathAndQuery}.",
                        new HttpRequestException("File Not Found"));
                }

                // Servern returnerade inga driftavbrott alls (HTTP Statuskod=204, innehåll saknas)
                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return driftavbrottResult;
                }

                // Servern returnerade eventuella driftavbrott (HTTP Statuskod=200)
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var driftavbrott = MDU.Util.Xml.Parser.ToObject<DriftavbrottType>(responseContent);

                    if (driftavbrott != null)
                        driftavbrottResult.Add(driftavbrott);
                    return driftavbrottResult;
                }

                throw new ApplicationException(
                    $"Driftavbrottstjänsten returnerade en oväntad statuskod. ResponseCode={numericStatusCode} {response.StatusCode}, ResponseServer={response.Headers.Server}, RequestBaseUrl={baseUri.Uri.PathAndQuery}.");
            }
        }
        catch (Exception e)
        {
            throw new ApplicationException(
                $"Driftavbrottstjänsten svarar inte. RequestBaseUrl={baseUri.Uri.PathAndQuery}.", e);
        }

        return driftavbrottResult;
    }

    public void Dispose()
    {
        _monitorCancellationTokenSource.Cancel();

        try
        {
            _driftavbrottMonitorTask?.Wait();
        }
        catch (AggregateException ae)
        {
            
        }
        finally
        {
            _monitorCancellationTokenSource.Dispose();
        }
    }
}