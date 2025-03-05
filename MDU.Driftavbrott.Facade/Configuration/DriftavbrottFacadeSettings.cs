using System.Collections.Generic;

namespace SE.MDU.Driftavbrott.Facade.Configuration;

/// <summary>
///  Konfigurationsobjekt för DriftavbrottFacade
/// </summary>
public class DriftavbrottFacadeSettings
{
    /// <summary>
    /// Anger Url till driftavbrottstjänst
    /// </summary>
    public string Url { get; set; }
    /// <summary>
    /// Anger vilka kanaler som är giltiga för aktuell komponent
    /// </summary>
    public IEnumerable<string> Kanaler { get; set; }
    /// <summary>
    /// Anger namn på anropande komponent
    /// </summary>
    public string System { get; set; }
    
    /// <summary>
    /// Anger med vilket interval DriftavbrottMonitor ska fråga driftavbrottstjänst.
    /// </summary>
    public int MonitorIntervalInSeconds { get; set; }
}