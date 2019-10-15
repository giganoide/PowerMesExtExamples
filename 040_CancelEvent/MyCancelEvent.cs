using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atys.PowerMES.Events;
using Atys.PowerMES.Extensibility;
using Atys.PowerMES.Foundation;
using Atys.PowerMES.Support;

namespace TeamSystem.Customizations
{
    [ExtensionData("MyCancelEventExtension", "Put here an extension description.", "1.0",
        Author = "TeamSystem", EditorCompany = "TeamSystem")]
    public class MyCancelEvent : IMesExtension
    {
        private const string LOGSOURCE = @"MYCANCELEVENT";

        private IMesManager _MesManager = null;
        private IMesAppLogger _Logger = null;
        private readonly object _SyncObj = new object(); //per sincronizzazione multi-threading

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

            this._MesManager.Controller.BeforeDataUnitProcessorQueueUp += this.Controller_BeforeDataUnitProcessorQueueUp;
            this._MesManager.Controller.BeforeProcessingEvent += this.Controller_BeforeProcessingEvent;
            this._MesManager.Controller.QueryCanProcessEvent += this.Controller_QueryCanProcessEvent;
            this._MesManager.Controller.ManipulatingDataUnitOnProcessorQueueUp += this.Controller_ManipulatingDataUnitOnProcessorQueueUp;
            this._MesManager.InitializationCompleted += this._MesManager_InitializationCompleted;
        }

        /// <summary>
        /// Deve contenere il codice di cleanup da eseguire prima della disattivazione
        /// dell'estensione o comunque alla chiusura di PowerMES
        /// </summary>
        public void Shutdown()
        {
            this._MesManager.Controller.BeforeDataUnitProcessorQueueUp -= this.Controller_BeforeDataUnitProcessorQueueUp;
            this._MesManager.Controller.BeforeProcessingEvent -= this.Controller_BeforeProcessingEvent;
            this._MesManager.Controller.QueryCanProcessEvent -= this.Controller_QueryCanProcessEvent;
            this._MesManager.Controller.ManipulatingDataUnitOnProcessorQueueUp -= this.Controller_ManipulatingDataUnitOnProcessorQueueUp;
            this._MesManager.InitializationCompleted -= this._MesManager_InitializationCompleted;
        }

        #endregion

        #region cancellazione e manipolazione di eventi di produzione

        /// <summary>
        /// Evento per la manipolazione dei dati di produzione (articolo-fase-workorder)
        /// prima dell'accodamento in processor risorsa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Controller_ManipulatingDataUnitOnProcessorQueueUp(object sender, ResourceProcessingDataManipulationEventArgs e)
        {
            const string resourceManip = @"040_MANIP";
            if (e.Resource.Name != resourceManip)
                return;

            /*
             * NB: questo evento viene generato solo se è attiva l'opzione a livello di servizio:
             *
             * this._MesManager.GeneralSettings.Processing.EnableDataUnitManipulationOnProcessorQueueUp = true
             *
             * Con questo evento è possibile sostituire l'evento di produzione oppure
             * moltiplicare le lavorazioni associate come se fossero arrivati più eventi
             * dello stesso tipo, ma per articolo-fase diversi
             *
             * COME ESEMPIO, per avere i diversi comportamenti, inviamo eventi di produzione
             * di qualsiasi tipo con codici articolo MOLTIPLICA, CAMBIAARTICOLO, CAMBIAEVENTO
             */

            var articleBaseEvent = (e.Unit as ArticleBaseEvent); // passano tutti gli eventi con articolo
            if (articleBaseEvent == null)
                return; //in genere manipolazione ha senso solo per eventi legati ad articolo

            // col simulatore mandare uno start sulla macchina 040_MANIP con articolo MOLTIPLICA
            if (articleBaseEvent.Article.Article == "MOLTIPLICA")
            {
                /*
                 * la risorsa NON deve essere singolo articolo
                 */

                var articleDataList = new List<ProcessingData>()
                                      {
                                          new ProcessingData(new ArticleItem("ART0001", "10"), "WO0001"),
                                          new ProcessingData(new ArticleItem("ART0002", "20"), "WO0002"),
                                          new ProcessingData(new ArticleItem("ART0003", "30"), string.Empty),
                                      };

                e.ManipulationData = articleDataList;
                e.ManipulationMode = ProcessingDataManipulationMode.Multiply;
                e.HideSource = true; //se TRUE l'evento originario viene eliminato, se false le tre lavorazioni si AGGIUNGONO a quella originaria
            }

            if (articleBaseEvent.Article.Article == "CAMBIAARTICOLO")
            {
                var articleDataList = new List<ProcessingData>()
                                      {
                                          new ProcessingData(new ArticleItem("ART0004", "40"), "WO0004"),
                                      };

                e.ManipulationData = articleDataList;
                e.ManipulationMode = ProcessingDataManipulationMode.SwapArticle;
                //se swap, non è necessario impostare e.HideSource = true
            }

            if (articleBaseEvent.Article.Article == "CAMBIAEVENTO" && e.Unit is RefreshTimerEvent)
            {
                /*
                 * Ipotizziamo di trasformare un REFRESH in INIZIO
                 * se non ho alcuna lavorazione attiva sulla macchina
                 *
                 */

                // richiesta lavorazioni attive per la risorsa
                var jobs = this._MesManager.Controller.GetJobsInfos(e.Resource);
                if (jobs != null && jobs.Any())
                    return; //almeno una lavorazione attiva

                var refresh = (e.Unit as RefreshTimerEvent);

                e.ReplacementDataUnit = new ArticleStartedEvent(e.Resource.Name, refresh.UtcTimestamp, refresh.Article, 0, 
                                                                "MYEXTENSION",
                                                                refresh.ProgressiveNumber,
                                                                string.Empty, 100,
                                                                string.Empty); //nei refresh non ho l'ordine

                e.ManipulationMode = ProcessingDataManipulationMode.Replace;
            }
        }
        
        /// <summary>
        /// Evento cancellabile di notifica pre-accodamento in processor risorsa
        /// CANCELLABILE
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Controller_BeforeDataUnitProcessorQueueUp(object sender, ResourceDataUnitCancelEventArgs e)
        {
            DataUnitEvent unit = e.Unit;
            //e.Cancel = true: annulla l'operazione
        }

        /// <summary>
        /// Evento per la notifica di inizio procedura di elaborazione
        /// di un evento di produzione, con possibilità di cancellazione
        /// CANCELLABILE
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Controller_QueryCanProcessEvent(object sender, ResourceDataUnitCancelEventArgs e)
        {
            //e.Cancel = true: annulla l'operazione
        }
        /// <summary>
        /// Evento per la notifica di inizio procedura di elaborazione
        /// di un evento di produzione
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Controller_BeforeProcessingEvent(object sender, ResourceDataUnitEventArgs e)
        {
            /*
             * in questo caso abbiamo la notifica di inizio
             * elaborazione di un evento di produzione che però
             * non può essere cancellato. Devo usare gli altri eventi.
             * Qui posso fare operazioni preparatorie.
             */
        }

        #endregion

        #region cancellazione di transazioni

        private void _MesManager_InitializationCompleted(object sender, EventArgs e)
        {
            /*
             * meglio agganciarsi agli eventi dei data repeater
             * quando l'inizializzazione di tutti i componenti è completa
             */

            var defaultRepeater = this._MesManager.RepeatersManager.DefaultRepeater;
            //in un'installazione base è il repeater che scrive su database
            defaultRepeater.PushingTransaction += this.DefaultRepeater_PushingTransaction;
            defaultRepeater.TransactionStarting += this.DefaultRepeater_TransactionStarting;
        }

        /// <summary>
        /// Evento che indica che una transazione sta per essere inserita nella
        /// coda di elaborazione del data repeater
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DefaultRepeater_PushingTransaction(object sender, TransactionCancelEventArgs e)
        {
            //e.Cancel = true: annulla l'operazione
        }

        /// <summary>
        /// evento di notifica inizio elaborazione di una transazione
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DefaultRepeater_TransactionStarting(object sender, TransactionCancelEventArgs e)
        {
            //e.Cancel = true: annulla l'operazione
        }

        #endregion
    }
}
