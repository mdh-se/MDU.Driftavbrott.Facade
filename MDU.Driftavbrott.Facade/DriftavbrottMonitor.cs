using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MDU.Logging.Extensions;
using Microsoft.Extensions.Logging;
using SE.MDU.Driftavbrott.Facade.Events;
using SE.MDU.Driftavbrott.Facade.Interfaces;
using SE.MDU.Driftavbrott.Modell;

namespace SE.MDU.Driftavbrott.Facade;
/// <summary>
/// Monitorerar driftavbrott enligt angiven konfiguration
/// </summary>
internal class DriftavbrottMonitor
{
    private IDriftavbrottFacade _driftavbrottFacade;
    private readonly ILogger<DriftavbrottFacade> _logger;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="driftavbrottFacade"><see cref="IDriftavbrottFacade"/></param>
    /// <param name="logger"><see cref="IDriftavbrottFacade"/></param>
    internal DriftavbrottMonitor(IDriftavbrottFacade driftavbrottFacade, ILogger<DriftavbrottFacade> logger)
    {
        _driftavbrottFacade = driftavbrottFacade;
        _logger = logger;
        _logger.Info("DriftavbrottMonitor instansierad.");
    }
    
    internal event EventHandler<DriftavbrottEventArgs> DriftavbrottChanged;
    
    internal async Task MonitorAsync(CancellationToken cancellationToken, int intervalInSeconds = 60)
    {
        DriftavbrottType senasteDriftavbrott = null;
        _logger.Info("DriftavbrottMonitor startad.");
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    IEnumerable<DriftavbrottType> driftavbrott = await _driftavbrottFacade.GetPagaendeDriftavbrottAsync();
                
                    if (driftavbrott.Any())
                    {
                        if (senasteDriftavbrott == null)
                        {
                            foreach (DriftavbrottType da in driftavbrott)
                            {
                                senasteDriftavbrott = da;
                                _logger.Info($"Signalerar aktivt driftavbrott för kanal: '{da.Kanal}' med meddelande '{da.MeddelandeSv}'");
                                var dae = new DriftavbrottEventArgs(DriftavbrottEventArgs.AvbrottStatus.Aktivt, da.MeddelandeSv, da.MeddelandeEn);
                                DriftavbrottChanged?.Invoke(this, dae);
                            }
                        }
                    }
                    else
                    {
                        if (senasteDriftavbrott != null)
                        {
                            _logger.Info($"Signalerar avslutat driftavbrott för kanal: '{senasteDriftavbrott?.Kanal}' med meddelande '{senasteDriftavbrott?.MeddelandeSv}'");
                            var dae = new DriftavbrottEventArgs(DriftavbrottEventArgs.AvbrottStatus.Avslutat, senasteDriftavbrott?.MeddelandeSv ?? "avslutat", senasteDriftavbrott?.MeddelandeEn ?? "avslutat");
                            senasteDriftavbrott = null;
                            DriftavbrottChanged?.Invoke(_driftavbrottFacade, dae);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error($"Fel vid kontroll av driftavbrott.",e);
                }
                await Task.Delay(TimeSpan.FromSeconds(intervalInSeconds), cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.Info("DriftavbrottMonitor stoppad.");
            throw;
        }
    }
}