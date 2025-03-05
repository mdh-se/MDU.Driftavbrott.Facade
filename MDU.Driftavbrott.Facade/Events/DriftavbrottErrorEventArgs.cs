using System;

namespace SE.MDU.Driftavbrott.Facade.Events
{
    /// <summary>
    /// Envent klass som hanterar error
    /// </summary>
    public class DriftavbrottErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Skapar instansen.
        /// </summary>
        /// <param name="meddelande">Meddelande</param>
        /// <param name="nivå">Nivå</param>
        /// <param name="exception">Stacktrace</param>
        public DriftavbrottErrorEventArgs(string meddelande, Exception exception = null)
        {
            Meddelande = meddelande;
            ErrorException = exception;
        }

       /// <summary>
        /// Meddelande.
        /// </summary>
        public string Meddelande { get; private set; }

        /// <summary>
        /// Stacktrace
        /// </summary>
        public Exception ErrorException { get; private set; }
    }
}