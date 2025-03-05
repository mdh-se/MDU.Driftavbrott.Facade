using System;
using System.Linq;

namespace SE.MDU.Driftavbrott.Facade.Configuration;

public class ConfigurationValidator
{
    public static void ValidateConfiguration(DriftavbrottFacadeSettings settings)
    {
        if(string.IsNullOrEmpty(settings.Url)) throw new ArgumentException("Ingen Url angiven till driftavbrottstjänst.");

        if(!settings.Kanaler.Any()) throw new ArgumentException("Inga kanaler för driftavbrott angivna.");
        
        if(string.IsNullOrEmpty(settings.System)) throw new ArgumentException("Inget namn på anropande system angivet.");
    }
}