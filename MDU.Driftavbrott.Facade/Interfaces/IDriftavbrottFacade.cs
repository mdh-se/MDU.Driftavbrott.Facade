using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SE.MDU.Driftavbrott.Facade.Configuration;
using SE.MDU.Driftavbrott.Facade.Events;
using SE.MDU.Driftavbrott.Modell;

namespace SE.MDU.Driftavbrott.Facade.Interfaces;

public interface IDriftavbrottFacade : IDisposable
{
    internal DriftavbrottFacadeSettings CurrentConfig { get; }
    public event EventHandler<DriftavbrottEventArgs> DriftavbrottChanged;
    Task<IEnumerable<DriftavbrottType>> GetPagaendeDriftavbrottAsync();

    public void StartDriftavbrottMonitor();
}