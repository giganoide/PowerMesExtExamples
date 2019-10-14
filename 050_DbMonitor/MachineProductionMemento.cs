using System;

namespace TeamSystem.Customizations
{
    /// <summary>
    /// Dati memorizzati in SH97_RepositoryValues per l'utilizzo
    /// tra varie sessioni\elaborazioni
    /// </summary>
    public sealed class MachineProductionMemento
    {
        /// <summary>
        /// Imposta e restituisce l'ultimo numero di stampata elaborato
        /// </summary>
        public long LastReadRecordNumber { get; set; }
        /// <summary>
        /// Imposta e restituisce l'identificativo dell'ultima stampata elaborata
        /// </summary>
        public string LastReadStrokeId { get; set; }
        /// <summary>
        /// Imposta e restituisce il timestamp dell'ultima stampata elaborata
        /// </summary>
        public DateTime LastReadStrokeTimestamp { get; set; }

        /// <summary>
        /// Imposta e restituisce data e ora dell'ultima elaborazione effettuata
        /// </summary>
        public DateTime LastProcessingTime { get; set; }

    }
}