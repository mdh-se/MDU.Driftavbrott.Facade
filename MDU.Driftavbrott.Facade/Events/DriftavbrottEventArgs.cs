using System;

namespace SE.MDU.Driftavbrott.Facade.Events
{
    /// <summary>
    /// Händelsekalss för driftavbrott
    /// </summary>
    public class DriftavbrottEventArgs : EventArgs
    {
        /// <summary>
        /// Status för att detektera Change
        /// </summary>
        public AvbrottStatus Status { get; }

        /// <summary>
        /// Svenskt driftavbrottsmeddelande.
        /// </summary>
        public string MeddelandeSv { get; }

        /// <summary>
        /// Engelskt driftavbrottsmeddelande.
        /// </summary>
        public string MeddelandeEng { get; }

        /// <summary>
        /// Skapar instansen
        /// </summary>
        /// <param name="status"></param>
        /// <param name="kanal">Kanal</param>
        /// <param name="meddelandeSv">Meddelandet på svenska</param>
        /// <param name="meddelandeEng">Meddelandet på engelska</param>
        public DriftavbrottEventArgs( AvbrottStatus status, string meddelandeSv,
            string meddelandeEng)
        {
            Status = status;
            MeddelandeSv = meddelandeSv;
            MeddelandeEng = meddelandeEng;
        }

        public enum AvbrottStatus
        {
            Aktivt,
            Avslutat
        }
    }
}