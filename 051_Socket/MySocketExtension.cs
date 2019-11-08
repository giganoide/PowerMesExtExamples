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

        private readonly Guid _ComponentId = Guid.NewGuid(); //id specifico del componente (per scheduler)

        private Guid
            _DieCastingProductionActivityId = Guid.Empty; //id task per job schedulatore costruzione piani di lavoro

        /*
         * ManualResetEventSlim è una tipologia di lock qui usato per evitare che due
         * esecuzioni del task ricorrente vadano in sovrapposizione
         */
        private readonly ManualResetEventSlim _DieCastingProdActivityRunningMres =
            new System.Threading.ManualResetEventSlim(false); //per sincronizzazione attività ricorrente

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

            this._MesManager.ProcessExternalCommand += Manager_ProcessExternalCommand;

            //this.PublishExternalCommands();

            //this.SetupDieCastingProductionActivity();
        }

        /// <summary>
        /// Deve contenere il codice di cleanup da eseguire prima della disattivazione
        /// dell'estensione o comunque alla chiusura di PowerMES
        /// </summary>
        public void Shutdown()
        {
            this.RevokeExternalCommands();
            this._MesManager.ProcessExternalCommand -= Manager_ProcessExternalCommand;
        }

        #endregion

        #region external commands

        /// <summary>
        /// Gestione evento per elaborazione comandi esterni da CLIENTS
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Manager_ProcessExternalCommand(object sender, ExternalCommandsExecutionEventArgs e)
        {
            var paramList = e.Parameters != null
                ? e.Parameters.ToList()
                : new List<ExternalCommandValue>();

            if (e.CommandCode == EXTCMD_CHECKPROD
                && paramList.Count == 1)
            {
                var resourceName = paramList[0].GetConvertedValueToType<string>();

                var resource = this._MesManager.ResourcesHandler.GetResource(resourceName);
                if (resource == null)
                {
                    this._MesManager.AppendMessageToLog(MessageLevel.Diagnostics, "MySocket",
                        "Risorsa non trovata (" + EXTCMD_CHECKPROD + ")");
                    return;
                }

                this._MesManager.AppendMessageToLog(MessageLevel.Diagnostics, "MySocket",
                    "EXTERNAL COMMAND (" + EXTCMD_CHECKPROD + ")");

                this.SendArticle(resource.Name);
            }
        }

        private void SendArticle(string article) { }

        /// <summary>
        /// Pubblica i comandi esterni specifici per la classe
        /// </summary>
        private void PublishExternalCommands()
        {
            var publisher = this.GetType().Name.ToUpper();

            var templates = new List<ExternalCommandDescriptor>
            {
                new ExternalCommandDescriptor(publisher,
                    EXTCMD_CHECKPROD,
                    new List<ExternalCommandValue>()
                    {
                        new ExternalCommandValue("RESOURCE", string.Empty, ExternalCommandValueType.String),
                    })
            };

            this._MesManager.PublishExternalCommandsTemplates(templates);
        }

        /// <summary>
        /// Annulla la pubblicazione di comandi esterni specifici
        /// </summary>
        private void RevokeExternalCommands()
        {
            var publisher = this.GetType().Name.ToUpper();

            this._MesManager.RevokeExternalCommandsTemplates(publisher);
        }

        #endregion

        

    }
}