using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atys.PowerMES;
using Atys.PowerMES.Events;
using Atys.PowerMES.Extensibility;
using Atys.PowerMES.Foundation;
using Atys.PowerMES.Repeaters.Nicim;
using Atys.PowerMES.Support;

namespace TeamSystem.Customizations
{
    [ExtensionData("MyNicimIteractExtension", "Put here an extension description.", "1.0",
        Author = "TeamSystem", EditorCompany = "TeamSystem")]
    public class MyNicimIteract : IMesExtension
    {
        private const string LOGSOURCE = @"MYNICIMITERACT";

        private IMesManager _MesManager = null;
        private IMesAppLogger _Logger = null;

        private readonly object SyncObj = new object();

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
            this._Logger = this._MesManager.ApplicationMainLogger;
        }

        /// <summary>Esegue/avvia l'estensione</summary>
        public void Run()
        {
            this._Logger.WriteMessage(MessageLevel.Diagnostics, true, LOGSOURCE,
                "Estensione creata!");

            this._MesManager.Controller.BeforeProcessingEvent += this.Controller_BeforeProcessingEvent;
            this._MesManager.ResourcesHandler.ResourceGlobalStateChanged += this.ResourcesHandler_ResourceGlobalStateChanged;
        }

        /// <summary>
        /// Deve contenere il codice di cleanup da eseguire prima della disattivazione
        /// dell'estensione o comunque alla chiusura di PowerMES
        /// </summary>
        public void Shutdown()
        {
            this._MesManager.Controller.BeforeProcessingEvent -= this.Controller_BeforeProcessingEvent;
            this._MesManager.ResourcesHandler.ResourceGlobalStateChanged -= this.ResourcesHandler_ResourceGlobalStateChanged;
        }

        #endregion

        /*
         * SCENARIO 1:
         * devo memorizzare anche in PowerMES per analisi statistica l'ordine di lavoro
         * (a Nicim non inviamo mai l'ordine, fa lui associazioni articolo\fase -> ordine)
         * quindi recuperiamo l'ordine della lavorazione attiva da Nicim e la assegnamo
         * alla lavorazione PowerMES.
         *
         */

        /// <summary>
        /// Gestione evento di inizio elaborazione
        /// evento di produzione
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Controller_BeforeProcessingEvent(object sender, ResourceDataUnitEventArgs e)
        {
            if (e.Resource.Name != "RISORSA_LAVORAZIONI_IN_CORSO")
                return;

            lock (this.SyncObj)
            {
                var startEvent = (e.Unit as ArticleStartedEvent);
                if (startEvent == null)
                    return;

                var runningOperations = this.GetResourceNicimRunningOperations(e.Resource);

                /*
                 * carico le lavorazioni aperte tenendo conto solo di ordini singoli (non cuciti)
                 * e delle bolle madri se cuciti: in genere non è necessario gestire tutte le bolle figlie.
                 * Se me ne serve solo una ordino ad esempio per data ultima dichiarazione,
                 * oppure si può valutare lo stato
                 */

                var runningOperation = runningOperations.Where(o => o.StitchingRelation != StitchingRelationType.Child)
                    .OrderBy(o => o.LastProgressDate)
                    .FirstOrDefault();
                if (runningOperation == null)
                {
                    //nessuna lavorazione attiva su NICIM
                    //TODO: inviare una notifica al responsabile di produzione?
                    return;
                }

                //assegno ordine a evento PowerMES
                startEvent.WorkOrder = runningOperation.Order;


                Debug.WriteLine(
                    $"ORDINE: {runningOperation.Order} FASE: {runningOperation.Phase} ARTICOLO: {runningOperation.PartNumber} "
                    + $"ATTIVITA: {runningOperation.ActivityType} STATO: {runningOperation.LastProgressState.ToString()}");

                /*
                 * NicimActivityType (possibili attività associate ad una lavorazione)
                 * Unknown: Non disponibile\sconosciuta
                 * Setup: Preparazione
                 * StartUp: Avviamento (non gestito da PowerMES)
                 * Work: Lavoro
                 * Maintenance: Manutenzione
                 */

                /*
                 * NicimProgressState
                 * Unknown: Non disponibile\sconosciuta
                 * Start: Iniziata
                 * Restart: Ripresa
                 * Suspended: Sospesa
                 */
            }
        }
        
        /*
         * SCENARIO 2:
         * Quando una risorsa viene messa in setup, inviamo in macchina
         * i dati del prossimo ordine da lavorare prendendolo da quelli pianificati
         * dallo schedulatore.
         *
         * NB: quando c'è un MES, il setup deve essere gestito sul MES e non su PowerMES
         *     in autonomia. Lo stato setup viene acquisito da PowerMES attraverso le funzionalità
         *     di "sincronizza stato risorse"
         *
         */

        /// <summary>
        /// Gestione evento di cambio stato risorsa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResourcesHandler_ResourceGlobalStateChanged(object sender, ResourceGlobalStateEventArgs e)
        {
            if (e.Resource.Name == "RISORSA_CON_SCHEDULATORE" && e.NewState == ResourceGlobalState.SetupMode)
            {
                var plannedWorkOrders = this.GetResourceNicimWorkPlanItems(e.Resource);
                if (plannedWorkOrders.Count == 0)
                    return;

                /*
                 * carico gli ordini pianificati tenendo conto solo di ordini singoli (non cuciti)
                 * e delle bolle madri se cuciti: in genere non è necessario gestire tutte le bolle figlie.
                 * Il tutto ordinato per la priorità di schedulazione
                 */
                var nextWorkOrder = plannedWorkOrders.Where(o => o.StitchingRelation != StitchingRelationType.Child)
                                                     .OrderByDescending(o => o.PhasePriority)
                                                     .First();


                Debug.WriteLine($"ORDINE: {nextWorkOrder.Order} FASE: {nextWorkOrder.Phase} ARTICOLO: {nextWorkOrder.PartNumber} QTA: {nextWorkOrder.PlannedQuantity}");

                //TODO: invio dati in macchina tramite PowerDevice o altro mezzo
            }
        }

        /// <summary>
        /// Restituisce il repeater Nicim
        /// </summary>
        /// <returns>Repeater Nicim o <c>null</c> se non disponibile</returns>
        private INicimRepeater GetNicimRepeater()
        {
            var result = this._MesManager.RepeatersManager.GetRepeater<INicimRepeater>();
            return result;
        }

        /// <summary>
        /// Recupera le informazioni delle lavorazioni pianificate su Nicim
        /// per la macchina specificata
        /// NB: i dati sono generati dallo SCHEDULATORE di NICIM che è un modulo
        /// opzionale e solo pochi clienti lo hanno
        /// </summary>
        /// <param name="resource">Macchina per cui si vogliono le lavorazioni</param>
        /// <returns>Elenco lavorazioni pianificate per la risorsa</returns>
        private IList<NicimWorkPlanItem> GetResourceNicimWorkPlanItems(IMesResource resource)
        {
            var nicimRepeater = this.GetNicimRepeater();
            if (nicimRepeater == null || !nicimRepeater.DirectAccessEnabled)
            {
                return new List<NicimWorkPlanItem>();
            }

            var workPlanItems = nicimRepeater.LoadWorkPlan(resource);

            return workPlanItems == null
                       ? new List<NicimWorkPlanItem>()
                       : workPlanItems.ToList();
        }

        /// <summary>
        /// Recupera le informazioni delle lavorazioni in corso su Nicim
        /// per la macchina specificata
        /// </summary>
        /// <param name="resource">Macchina per cui si vogliono le lavorazioni</param>
        /// <returns>Elenco lavorazioni in corso per la risorsa</returns>
        private IList<NicimRunningOperation> GetResourceNicimRunningOperations(IMesResource resource)
        {
            var nicimRepeater = this.GetNicimRepeater();
            if (nicimRepeater == null || !nicimRepeater.DirectAccessEnabled)
            {
                return new List<NicimRunningOperation>();
            }

            //operazioni attive sulla risorsa specificata
            var runningOperations = nicimRepeater.LoadRunningOperations(resource);
            ////operazioni attive su TUTTE le risorse
            //var allRunningOperations = nicimRepeater.LoadRunningOperations();

            //l'oggetto NicimRunningOperation ha i vari campi della vista Nicim
            //e una proprietà CustomFields che contiene l'elenco dei 10 campi
            //personalizzati

            return runningOperations == null
                       ? new List<NicimRunningOperation>()
                       : runningOperations.ToList();
        }
    }
}