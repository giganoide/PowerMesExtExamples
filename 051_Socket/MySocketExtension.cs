using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Atys.PowerMES;
using Atys.PowerMES.Events;
using Atys.PowerMES.Extensibility;
using Atys.PowerMES.Foundation;
using Atys.PowerMES.Services;
using Atys.PowerMES.Support;
using Atys.PowerMES.Support.Serialization;

namespace TeamSystem.Customizations
{
    [ExtensionData("MySocketExtension", "Put here an extension description.", "1.0",
        Author = "TeamSystem", EditorCompany = "TeamSystem")]
    public class MySocketExtension : IMesExtension
    {
        /*
         * SCENARIO
         * Un robot ci fornisce informazioni relative alla lavorazione tramite un socket tcp
         * L'extension genera eventi di produzione di inizio-fine-sospensione.
         * Sono utilizzati attributi per attivare la funzionalità e per la parametrizzazione dellla connessione
         *
         *
         * Esempio di file dati con eventi di produzione:
         *
         * I+ABCDE+10+20150316085013
         * F+ABCDE+10+1+20150316085510
         * I+ABCDE+10+20150316095013
         * F+ABCDE+10+1+20150316095510
         * 
         * S+ABCDE+10+99+20150316095510
         * R+ABCDE+10+20150316095510
         * 
         * Il carattere '+' è il separatore tra i vari campi dato.
         * 
         * Il primo carattere identifica sempre il tipo di evento di produzione.
         * I = inizio ciclo
         * F = fine ciclo con versamento pezzi
         * S = allarme, la macchina è ferma (opzionale - se disponibile)
         * R = fine allarme, la macchina è di nuovo in lavoro (opzionale - se disponibile)
         * 
         * L'ultimo campo è sempre la data-ora in cui l'evento si è verificato, nel formato yyyyMMddHHmmss
         * Tutte le righe devono terminare con un ritorno a capo (CR LF).
         * 
         * Comando 'I' : I+ABCDE+10+20150316085013
         * I : codice comando
         * ABCDE : codice articolo
         * 10 : fase di lavorazione
         * 20150316085013 : data e ora evento
         * 
         * Comando 'F' : F+ABCDE+10+1+20150316085510
         * F : codice comando
         * ABCDE : codice articolo
         * 10 : fase di lavorazione
         * 1 : numero pezzi prodotti nel ciclo
         * 20150316085510 : data e ora evento
         * 
         * Comando 'S' : S+ABCDE+10+99+20150316095510
         * S : codice comando
         * ABCDE : codice articolo
         * 10 : fase di lavorazione
         * 99 : causale del fermo
         * 20150316095510 : data e ora evento
         * 
         * Comando 'R' : R+ABCDE+10+20150316095510
         * R : codice comando
         * ABCDE : codice articolo
         * 10 : fase di lavorazione
         * 20150316095510 : data e ora evento
         *
         */

        private const string LOGRSOURCE = @"MYSOCKET";

        private const string EXTCMD_CHECKPROD = "SEND_ART";

        private IMesManager _MesManager = null;
        private IMesAppLogger _MesLogger = null;

        private TcpServer tcpListener;

        #region IMesExtension members

        /// <summary>
        /// Inizializzazione estensione e collegamento all'oggetto principale PowerMES
        /// (eseguito al caricamento in memoria dell'estensione)
        /// </summary>
        /// <param name="mesManager">Riferimento all'oggetto principale PowerMES</param>
        public void Initialize(IMesManager mesManager)
        {
#if DEBUG
            Debugger.Launch();
#endif
            //memorizzo il riferimento all'oggetto principale di PowerMES
            this._MesManager = mesManager;
            this._MesLogger = this._MesManager.ApplicationMainLogger;
        }

        /// <summary>Esegue/avvia l'estensione</summary>
        public void Run()
        {
            this._MesLogger.WriteMessage(MessageLevel.Diagnostics, true, LOGRSOURCE,
                "Estensione creata!");

            tcpListener = new TcpServer(this._MesLogger);
            tcpListener.StartListening();
        }

        /// <summary>
        /// Deve contenere il codice di cleanup da eseguire prima della disattivazione
        /// dell'estensione o comunque alla chiusura di PowerMES
        /// </summary>
        public void Shutdown()
        {
            tcpListener.StopListening();
        }

        #endregion

    }
}