using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atys.PowerMES.Device;
using Atys.PowerMES.Extensibility;
using Atys.PowerMES.Foundation;
using Atys.PowerMES.Services;
using Atys.PowerMES.Support;

namespace TeamSystem.Customizations
{
    [ExtensionData("MyPowerDeviceInteractExtension", "Put here an extension description.", "1.0",
        Author = "TeamSystem", EditorCompany = "TeamSystem")]
    public class MyPowerDeviceInteractExtension : IMesExtension
    {
        private const string LOGSOURCE = @"MYPOWERDEVICEINTERACTEXTENSION";
        private const string TEST_RESOURCE_NAME = "052_DVC";

        private IMesManager _MesManager = null;
        private IMesAppLogger _Logger = null;

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
            this._MesManager = mesManager;
            this._Logger = this._MesManager.ApplicationMainLogger;

        }

        /// <summary>Esegue/avvia l'estensione</summary>
        public void Run()
        {
            this._Logger.WriteMessage(MessageLevel.Diagnostics, true, LOGSOURCE,
                "Estensione creata!");

            this.PublishExternalCommands();

            this._MesManager.ProcessExternalCommand += this._MesManager_ProcessExternalCommand;
            this._MesManager.ResourcesHandler.ResourceGlobalStateChanged += this.ResourcesHandler_ResourceGlobalStateChanged;
        }

        /// <summary>
        /// Deve contenere il codice di cleanup da eseguire prima della disattivazione
        /// dell'estensione o comunque alla chiusura di PowerMES
        /// </summary>
        public void Shutdown()
        {
            this._MesManager.ProcessExternalCommand -= this._MesManager_ProcessExternalCommand;
            this._MesManager.ResourcesHandler.ResourceGlobalStateChanged -= this.ResourcesHandler_ResourceGlobalStateChanged;

            this.RevokeExternalCommands();
        }

        #endregion

        #region external commands

        private const string COMMAND_CODE = "052_INVIO_ORDINE_DVC";

        /// <summary>
        /// Gestione evento per elaborazione comandi esterni da CLIENTS
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _MesManager_ProcessExternalCommand(object sender, ExternalCommandsExecutionEventArgs e)
        {
            var paramList = e.Parameters != null
                                ? e.Parameters.ToList()
                                : new List<ExternalCommandValue>();

            if (e.CommandCode == COMMAND_CODE
                && paramList.Count == 3)
            {
                var resourceName = paramList[0].GetConvertedValueToType<string>();
                var orderValue = paramList[1].GetConvertedValueToType<string>();
                var qtyValue = paramList[2].GetConvertedValueToType<int>();

                var resource = this._MesManager.ResourcesHandler.GetResource(resourceName);
                if (resource == null)
                {
                    this._MesManager.AppendMessageToLog(MessageLevel.Diagnostics, "WorkPlanBuilderExtension",
                                                        "Risorsa non trovata " + COMMAND_CODE);
                    return;
                }

                this._MesManager.AppendMessageToLog(MessageLevel.Diagnostics, "WorkPlanBuilderExtension",
                                                    "EXTERNAL COMMAND " + COMMAND_CODE);


                this.WriteValuesOnPowerDevice(resource, orderValue, qtyValue);
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
                                                        COMMAND_CODE,
                                                        new List<ExternalCommandValue>()
                                                            {
                                                                new ExternalCommandValue("RESOURCE", TEST_RESOURCE_NAME, ExternalCommandValueType.String),
                                                                new ExternalCommandValue("ORDINE", "WO1234", ExternalCommandValueType.String),
                                                                new ExternalCommandValue("QUANTITA", "2000", ExternalCommandValueType.Integer),
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

        /*
         * SCENARIO
         * Ipotizziamo di dover inviare ad una macchina tramite PowerDevice
         * l'ordine di lavoro e la quantità prevista nel momento in cui
         * viene messa in setup, ma solo se la macchina NON è in marcia
         * (solo come esempio di lettura indirizzi)
         */

        /// <summary>
        /// Gestione evento di cambio stato risorsa
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResourcesHandler_ResourceGlobalStateChanged(object sender, ResourceGlobalStateEventArgs e)
        {
            if (e.Resource.Name == TEST_RESOURCE_NAME && e.NewState == ResourceGlobalState.SetupMode)
            {
                /*
                 * TODO: carichiamo i dati da scrivere
                 */

                var orderValue = $"WO{DateTime.Now.Minute}";
                var qtyValue = DateTime.Now.Millisecond;

                this.WriteValuesOnPowerDevice(e.Resource, orderValue, qtyValue);
            }
        }

        /// <summary>
        /// Invio alla macchina specificata un ordine di produzione, se non in marcia
        /// </summary>
        private void WriteValuesOnPowerDevice(IMesResource resource, string orderValue, int qtyValue)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));
            if (string.IsNullOrWhiteSpace(orderValue))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(orderValue));
            if (qtyValue <= 0)
                throw new ArgumentOutOfRangeException(nameof(qtyValue));

            var dvcService = this._MesManager.ServiceManager.GetService<IDvcIntegrationService>();
            if (dvcService == null || !dvcService.Enabled)
            {
                this._MesManager.ApplicationMainLogger.WriteMessage(MessageLevel.Info, false, LOGSOURCE,
                                                                    "WriteValuesOnPowerDevice(): PowerDevice integration not available");
                return;
            }

            const string dvcInstance = "localhost";

            const string qtyAddress = @"{{PLC}}{{A73}}{{DB501.DBW{0}}}"; //quantità prevista
            const string orderAddress = @"{{PLC}}{{A73}}{{DB501.DBB{0}[10]}}"; //ordine di lavoro
            const string runningMachineAddress = @"{PLC}{A74}{DB101.DBX0.0}"; //bool - macchina in marcia

            /*
             * prima di tutto leggo il valore da PowerDevice di macchina in marcia
             * se non lo trovo a false\zero interrompo l'operazione
             */
            var isRunningReadResult = dvcService.ReadAddressValue(runningMachineAddress,
                                                                  dvcInstance) as DvcReadOperationSuccess;
            if (isRunningReadResult?.AddressValue == null
                || !isRunningReadResult.AddressValue.IsValid
                || string.IsNullOrWhiteSpace(isRunningReadResult.AddressValue.ValueAsString))
            {
                this._MesManager.ApplicationMainLogger.WriteMessage(MessageLevel.Warning, false, LOGSOURCE,
                                                                    "WriteValuesOnPowerDevice(): cannot read if machine is running");
                return;
            }

            if (isRunningReadResult.AddressValue.ValueAsString.ToLowerInvariant() != Boolean.FalseString.ToLowerInvariant()
                && isRunningReadResult.AddressValue.ValueAsString.ToLowerInvariant() != "0")
            {
                this._MesManager.ApplicationMainLogger.WriteMessage(MessageLevel.Warning, false, LOGSOURCE,
                                                                    "WriteValuesOnPowerDevice(): cannot write because machine is running");
                return;
            }

            /*
             * NB: i valori numerici devono essere convertiti in stringa,
             * se decimali il separatore è sempre il punto
             */
            var nfi = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
            nfi.NumberDecimalSeparator = ".";
            nfi.NumberGroupSeparator = string.Empty;

            var qtyValueAsString = qtyValue.ToString(nfi);

            /*
             * procediamo alla scrittura
             */
            var orderWriteResponse = dvcService.SetAddressValue(orderAddress, dvcInstance, orderValue);
            var qtyWriteResponse = dvcService.SetAddressValue(qtyAddress, dvcInstance, qtyValueAsString);
            
            if (orderWriteResponse == null || qtyWriteResponse == null)
            {
                this._MesManager.ApplicationMainLogger.WriteMessage(MessageLevel.Warning, false, LOGSOURCE,
                                                                    "WriteValuesOnPowerDevice(): null response from PowerDevice integration");
                return;
            }

            if (orderWriteResponse.Success && qtyWriteResponse.Success)
            {
                this._MesManager.ApplicationMainLogger.WriteMessage(MessageLevel.Info, false, LOGSOURCE,
                                                                    "WriteValuesOnPowerDevice(): write completed");

                return;
            }

            this._MesManager.ApplicationMainLogger.WriteMessage(MessageLevel.Warning, false, LOGSOURCE,
                                                                "WriteValuesOnPowerDevice(): bad response from PowerDevice integration! "
                                                                + "ORDER = {0} \\ {1}",
                                                                orderWriteResponse.Success,
                                                                qtyWriteResponse.Success);
        }






    }
}