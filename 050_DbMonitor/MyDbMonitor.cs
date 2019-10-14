using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    [ExtensionData("MyDbMonitorExtension", "Put here an extension description.", "1.0",
        Author = "TeamSystem", EditorCompany = "TeamSystem")]
    public class MyDbMonitor : IMesExtension
    {
        /*
         * SCENARIO
         * Estratto di una extension realmente utilizzata per il caricamento dei dati di produzione
         * da presse (pressofusione). I dati sono presi dalla tabella PRODUZ che contiene solo
         * le informazioni sulle battute della pressa, l'extension genera eventi di produzione di inizio-fine-sospensione.
         * Il monitoraggio del DB viene fatto tramite polling (attività schedulate) e ci sono
         * attributi (CASTINGPROD_ENABLED) per attivare la funzionalità sulle risorse.
         * Necessario anche un attributo che contiene il nome dell'istanza MS SQL (CASTINGPROD_SQL)
         *
         * NB: non sono state inserite le credenziali SQL per l'accesso al database.
         * In allegato il backup del DB, lo script per la creazione della tabella PRODUZ e un xls
         * con alcuni dati di produzione reali
         */

        private const string LOGRSOURCE = @"MYDBMONITOR";

        private IMesManager _MesManager = null;
        private IMesAppLogger _MesLogger = null;

        private readonly Guid _ComponentId = Guid.NewGuid(); //id specifico del componente (per scheduler)
        private Guid _DieCastingProductionActivityId = Guid.Empty; //id task per job schedulatore costruzione piani di lavoro
        /*
         * ManualResetEventSlim è una tipologia di lock qui usato per evitare che due
         * esecuzioni del task ricorrente vadano in sovrapposizione
         */
        private readonly ManualResetEventSlim _DieCastingProdActivityRunningMres = new System.Threading.ManualResetEventSlim(false); //per sincronizzazione attività ricorrente

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

            this.PublishExternalCommands();

            this.SetupDieCastingProductionActivity();
        }

        /// <summary>
        /// Deve contenere il codice di cleanup da eseguire prima della disattivazione
        /// dell'estensione o comunque alla chiusura di PowerMES
        /// </summary>
        public void Shutdown()
        {
            this.ClearDieCastingProductionActivity();
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

            if (e.CommandCode == LocalConstants.EXTCMD_CHECKPROD
                && paramList.Count == 1)
            {
                var resourceName = paramList[0].GetConvertedValueToType<string>();

                var resource = this._MesManager.ResourcesHandler.GetResource(resourceName);
                if (resource == null)
                {
                    this._MesManager.AppendMessageToLog(MessageLevel.Diagnostics, "MyDbMonitor",
                                                        "Risorsa non trovata (" + LocalConstants.EXTCMD_CHECKPROD + ")");
                    return;
                }

                this._MesManager.AppendMessageToLog(MessageLevel.Diagnostics, "MyDbMonitor",
                                                    "EXTERNAL COMMAND (" + LocalConstants.EXTCMD_CHECKPROD + ")");

                this.CheckMachineProduction(resource);
            }
        }
        /// <summary>
        /// Pubblica i comandi esterni specifici per la classe
        /// </summary>
        private void PublishExternalCommands()
        {
            var publisher = this.GetType().Name.ToUpper();

            var templates = new List<ExternalCommandDescriptor>();
            templates.Add(new ExternalCommandDescriptor(publisher,
                                                        LocalConstants.EXTCMD_CHECKPROD,
                                                        new List<ExternalCommandValue>()
                                                            {
                                                                new ExternalCommandValue("RESOURCE", string.Empty, ExternalCommandValueType.String),
                                                            }));

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

        #region recurring activity

        private void SetupDieCastingProductionActivity()
        {
            this._MesManager.ApplicationMainLogger.WriteMessage(MessageLevel.Diagnostics, false, LOGRSOURCE,
                                                                "SetupDieCastingProductionActivity(): called");

            this._DieCastingProdActivityRunningMres.Reset();

            //prendo un riferimento al servizio di schedulazione
            var scheduler = this._MesManager.ServiceManager.GetService<IJobSchedulerService>();

            var interval = TimeSpan.FromSeconds(10);
            var firstStart = DateTimeOffset.Now.AddMinutes(1);

            //creo l'oggetto per lo schedulatore
            var recurringActivity = new RecurringActivity(Guid.NewGuid(),
                                                          this._ComponentId,
                                                          firstStart,
                                                          this.DieCastingProductionTaskTriggerAction,
                                                          this.DieCastingProductionTaskErrorAction,
                                                          interval);
            //creo il job nello schedulatore
            if (scheduler.SubmitRecurringActivity(recurringActivity))
            {
                this._DieCastingProductionActivityId = recurringActivity.ActivityId;
                this._MesManager.ApplicationMainLogger.WriteMessage(MessageLevel.Diagnostics, false, LOGRSOURCE,
                                                                    "SetupDieCastingProductionActivity(): activity submitted -> "
                                                                    + "'{0}' starting from: {1} interval: {2}",
                                                                    recurringActivity.ActivityId.ToString(),
                                                                    firstStart.ToString(), interval.ToString());
            }
            else
            {
                this._MesManager.ApplicationMainLogger.WriteMessage(MessageLevel.Diagnostics, false, LOGRSOURCE,
                                                                    "SetupDieCastingProductionActivity(): failed to submit activity.");
            }
        }

        private void ClearDieCastingProductionActivity()
        {
            this._MesManager.ApplicationMainLogger.WriteMessage(MessageLevel.Diagnostics, false, LOGRSOURCE,
                                                                "ClearDieCastingProductionActivity(): called");

            this._DieCastingProdActivityRunningMres.Reset();

            if (this._DieCastingProductionActivityId == Guid.Empty)
                return;

            //prendo un riferimento al servizio di schedulazione
            var scheduler = this._MesManager.ServiceManager.GetService<IJobSchedulerService>();
            Debug.Assert(scheduler != null);

            if (scheduler.HasActivityById(this._DieCastingProductionActivityId))
                scheduler.CancelActivity(this._DieCastingProductionActivityId);

            //tolgo il riferimento a task
            this._DieCastingProductionActivityId = Guid.Empty;
        }

        private void DieCastingProductionTaskTriggerAction(Guid activityId)
        {
            Debug.Assert(activityId != Guid.Empty);

            this._MesManager.ApplicationMainLogger.WriteMessage(MessageLevel.Info, false, LOGRSOURCE,
                                                                "DieCastingProductionTaskTriggerAction(): called");

            if (this._DieCastingProdActivityRunningMres.IsSet)
            {
                this._MesLogger.WriteMessage(MessageLevel.Diagnostics, false, LOGRSOURCE,
                                             "DieCastingProductionTaskTriggerAction(): overlapping operation!");
                return;
            }

            this._DieCastingProdActivityRunningMres.Set();

            var resources = this._MesManager.ResourcesHandler.GetResources().ToList();
            try
            {

                foreach (var resource in resources)
                    this.CheckMachineProduction(resource);
            }
            catch (Exception ex)
            {
                this._MesLogger.WriteException(ex, LOGRSOURCE, "DieCastingProductionTaskTriggerAction(): error during processing.");
            }
            finally
            {
                this._DieCastingProdActivityRunningMres.Reset();
            }
        }

        private void DieCastingProductionTaskErrorAction(Guid activityId, Exception ex)
        {
            Debug.Assert(activityId != Guid.Empty);
            Debug.Assert(ex != null);

            this._MesManager.ApplicationMainLogger.WriteException(ex, LOGRSOURCE, "Error building resource work plan.");
        }

        #endregion

        /// <summary>
        /// Carica i dati di produzione per una pressofusione
        /// </summary>
        /// <param name="resource">Risorsa associata alla pressa</param>
        private void CheckMachineProduction(IMesResource resource)
        {
            /*
             * La tabella PRODUZ ha un Id e timestamp che usiamo per caricare solo
             * i nuovi record rispetto all'ultimo che abbiamo processato.
             * Le informazioni da passare da un ciclo all'altro vengono memorizzate in un
             * oggetto MachineProductionMemento sulla tabella SH97_RepositoryValues
             *
             */

            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            //abilitazione tramite attributo
            var enabled = resource.Settings
                                  .ResourceAttributes
                                  .GetActiveAttributeValueOrDefault<bool>(LocalConstants.RES_ATTR_DIECASTINGPROD_ENABLED, ValueContainerType.Boolean);

            if (!enabled)
            {
                this._MesLogger.WriteMessage(MessageLevel.Diagnostics, false, LOGRSOURCE,
                                             "CheckMachineProduction(): resource {0} not enabled", resource.Name);
                return;
            }
            //mi serve anche nome istanza SQL, anche in questo caso gestito tramite attributo.
            //Il nome del DB invece è cablato, mi aspetto che possa essere spostato, non rinominato
            var sqlInstanceName = resource.Settings
                                          .ResourceAttributes
                                          .GetActiveAttributeValueOrDefault<string>(LocalConstants.RES_ATTR_DIECASTINGPROD_SQL, ValueContainerType.String);

            if (string.IsNullOrWhiteSpace(sqlInstanceName))
            {
                this._MesLogger.WriteMessage(MessageLevel.Error, true, LOGRSOURCE,
                                             "CheckMachineProduction(): Bad SQL instance name resource {0}", resource.Name);
                return;
            }

            this._MesLogger.WriteMessage(MessageLevel.Diagnostics, false, LOGRSOURCE,
                                         "CheckMachineProduction(): start for resource {0}", resource.Name);


            var dbUserName = string.Empty; //TODO: inserire qui user e psw per accesso sql. in DieCastingDataAccessHub sono rpevisti dei default
            var dbPassword =string.Empty;

            var dataAccess = new DieCastingDataAccessHub(sqlInstanceName, dbUserName, dbPassword, this._MesLogger);
            if (!dataAccess.CheckConnection())
            {
                //db non raggiungibile
                this._MesManager.ApplicationMainLogger.WriteMessage(MessageLevel.Warning, true, LOGRSOURCE,
                                                                    "CheckMachineProduction(): DATABASE NOT AVAILABLE for {1} [{0}]",
                                                                    sqlInstanceName, resource.Name);
                return;
            }

            var localTime = DateTime.Now;
            //recupero la data del server SQL per gestire la differenza,
            //utile se l'istanza MS SQL è direttamente sul PC della macchina
            var machineTime = dataAccess.GetServerTime();
            if (machineTime == DateTime.MinValue)
            {
                machineTime = localTime;
            }

            var timeDifference = localTime - machineTime;
            
            var productionMemento = this.LoadResourceMemento(resource.Name);

            var productionStrokes = dataAccess.GetProduction(productionMemento.LastReadRecordNumber);

            var funnelEvents = new List<DataUnitEvent>();

            var notRunningAtLast = false;
            var lastTimestamp = DateTime.Now;

            if (productionStrokes.Count > 0)
            {
                /*
                 * Cicliamo sui record acquisiti, ognuno dei quali rappresenta una battuta della pressa.
                 * Per ogni stampata creaiamo una Fine\Versamento, e subito dopo un inizio.
                 * Se l'ultimo record processato segnala che la macchina non era in automatico,
                 * creo una sospensione.
                 */
                foreach (var productionStroke in productionStrokes)
                {
                    var normalizedStrokeTime = productionStroke.Timestamp.Add(timeDifference);
                    if (normalizedStrokeTime < productionMemento.LastReadStrokeTimestamp)
                    {
                        this._MesLogger.WriteMessage(MessageLevel.Diagnostics, false, LOGRSOURCE,
                                                     "CheckMachineProduction(): stroke in the past for resource {0}",
                                                     resource.Name);
                        continue;
                    }

                    if (!productionStroke.MachineIsRunning)
                    {
                        notRunningAtLast = true;
                        lastTimestamp = normalizedStrokeTime;

                        this._MesLogger.WriteMessage(MessageLevel.Warning, false, LOGRSOURCE,
                                                     "CheckMachineProduction(): stroke with resource {0} not running",
                                                     resource.Name);
                        continue;
                    }

                    notRunningAtLast = false;

                    this._MesLogger.WriteMessage(MessageLevel.Info, false, LOGRSOURCE,
                                                 "CheckMachineProduction(): NEW STROKE for resource {0} - {2} - {1}",
                                                 resource.Name, normalizedStrokeTime.ToString("G"),
                                                 productionStroke.MachineCycleNumber);

                    var bareQty = 1; //un record per ogni stampata, contiamo le battute
                    var goodQty = productionStroke.IsGood ? bareQty : 0;
                    var rejectedQty = !productionStroke.IsGood ? bareQty : 0;

                    //creo un done e poi uno start
                    var doneEvent = new ProductDoneEvent(resource.Name, normalizedStrokeTime.ToUniversalTime(),
                                                         productionStroke.Article,
                                                         goodQty, rejectedQty, 0,
                                                         productionStroke.MachineCycleNumber.ToString(), 0);
                    funnelEvents.Add(doneEvent);

                    var startEvent = new ArticleStartedEvent(resource.Name, normalizedStrokeTime.ToUniversalTime(),
                                                             productionStroke.Article, 0,
                                                             string.Empty,
                                                             0, string.Empty, 100,
                                                             productionStroke.Order ?? string.Empty);
                    funnelEvents.Add(startEvent);

                    //per definizione numero ciclo e timestamp sono maggiori del precedente
                    productionMemento.LastReadStrokeTimestamp = normalizedStrokeTime;
                    productionMemento.LastReadRecordNumber = productionStroke.MachineCycleNumber;
                    productionMemento.LastReadStrokeId = productionStroke.StrokeId;
                }
            }
            else
            {
                this._MesLogger.WriteMessage(MessageLevel.Diagnostics, false, LOGRSOURCE,
                                             "CheckMachineProduction(): no strokes for resource {0}", resource.Name);
            }

            if (notRunningAtLast && resource.Status.IsWorkingState())
            {
                //se l'ultimo record rilevato non ha macchina in automatico
                var suspension = new GenericSuspensionEvent(resource.Name, lastTimestamp.ToUniversalTime(), string.Empty);

                funnelEvents.Add(suspension);

                this._MesLogger.WriteMessage(MessageLevel.Info, true, LOGRSOURCE,
                                             "CheckMachineProduction(): setting suspension for resource NOT AUTO MODE {0} - {1}",
                                             resource.Name, lastTimestamp.ToString("G"));
            }

            //gli eventi di produzione creati vengono inseriti in un "imbuto"
            //che li mette su una coda di elaborazione asincrona
            if (funnelEvents.Count > 0)
                this._MesManager.DataInputFunnel.EnqueueEvents(funnelEvents);

            productionMemento.LastProcessingTime = DateTime.Now;

            //memorizza i dati di elaborazione
            this.SaveResourceMemento(resource.Name, productionMemento);
        }

        #region value containers

        /// <summary>
        /// Carica i dati accessori di elaborazione dalla tabella SH97_RepositoryValues
        /// o crea un default
        /// </summary>
        /// <param name="resourceName">Nome della risorsa di cui si vogliono i dati</param>
        /// <returns>Dati caricati</returns>
        private MachineProductionMemento LoadResourceMemento(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(resourceName));

            var storageValueService = this._MesManager.ServiceManager.GetService<IStorageValuesService>();

            var processingMementoContainer = storageValueService.GetOrCreateValue(MesGlobalConstants.PowerMesApplicationName,
                                                                                  LOGRSOURCE,
                                                                                  LocalConstants.ProdMementoItemRoot + resourceName);
            var processingMemento = string.IsNullOrWhiteSpace(processingMementoContainer.Value)
                                        ? new MachineProductionMemento()
                                        : JsonSerializer.DeserializeObject<MachineProductionMemento>(processingMementoContainer.Value);

            return processingMemento;
        }
        /// <summary>
        /// Salva i dati accessori di elaborazione nella tabella SH97_RepositoryValues
        /// </summary>
        /// <param name="resourceName">Nome della risorsa a cui si riferiscono i dati</param>
        /// <param name="memento">Dati da salvare</param>
        private void SaveResourceMemento(string resourceName, MachineProductionMemento memento)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(resourceName));

            if (memento == null)
            {
                this._MesLogger.WriteMessage(MessageLevel.Warning, true, LOGRSOURCE,
                                          "SaveResourceMemento(): dati da salvare non validi.");
                return;
            }

            var storageValueService = this._MesManager.ServiceManager.GetService<IStorageValuesService>();
            var processingMementoContainer = storageValueService.GetOrCreateValue(MesGlobalConstants.PowerMesApplicationName,
                                                                                  LOGRSOURCE,
                                                                                  LocalConstants.ProdMementoItemRoot + resourceName);

            //aggiornamento valori memorizzati su db locale PowerMES con parametri vari
            /*
             * NB: l'utilizzo di JsonSerializer implica una referenza a Atys.PowerMES.Support.dll
             */
            var mementoJson = JsonSerializer.SerializeObject(memento);
            processingMementoContainer.ApplyValue(mementoJson, typeof(MachineProductionMemento), true);
            var saveResult = storageValueService.SaveValue(processingMementoContainer);

            this._MesLogger.WriteMessage(MessageLevel.Diagnostics, true, LOGRSOURCE,
                                      "SaveResourceMemento(): salvataggio dati completato = {0}",
                                      saveResult);
        }

        #endregion


    }
}