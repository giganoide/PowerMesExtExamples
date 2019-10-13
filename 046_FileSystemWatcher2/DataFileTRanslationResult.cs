using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeamSystem.Customizations
{
    /// <summary>
    /// Risultato di una operazione di traduzione file
    /// </summary>
    internal sealed class DataFileTRanslationResult
    {
        /// <summary>
        /// Restituisce se la trasformazione è stata completata con esito positivo
        /// </summary>
        public bool IsSuccess { get; private set; }
        /// <summary>
        /// Restituisce il testo prodotto dalla trasformazione
        /// </summary>
        public string OutputContent { get; private set; }
        /// <summary>
        /// Restituisce eventuali messaggi e segnalazioni generati
        /// durante la trasformazione
        /// </summary>
        public string Messages { get; private set; }

        /// <summary>
        /// Inizializza una nuova istanza della classe
        /// </summary>
        /// <param name="isSuccess">se la trasformazione è stata completata con esito positivo</param>
        /// <param name="outputContent">testo prodotto dalla trasformazione</param>
        /// <param name="messages">messaggi e segnalazioni generati
        /// durante la trasformazione</param>
        public DataFileTRanslationResult(bool isSuccess, string outputContent, string messages)
        {
            this.IsSuccess = isSuccess;
            this.OutputContent = outputContent;
            this.Messages = messages;
        }

    }
}
