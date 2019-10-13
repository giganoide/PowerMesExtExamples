using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atys.PowerMES.Extensibility;
using Atys.PowerMES.Foundation;
using Atys.PowerMES.Support;

namespace TeamSystem.Customizations
{
    /*
     * SCENARIO
     * esempio di reale di file prodotto da piegatrice automatica (vedi DataFileExample.txt)
     * L'elaborazione viene attivata tramite parametro su attributi risorsa DATA_TRANSLATE
     *
     * Il file macchina viene trasformato riga per riga in un testo riconducibile
     * ai pattern che il file system watcher riesce ad elaborare in automatico
     *
     */

    [ExtensionData("FileTranslatorExtension", "Put here an extension description.", "1.0",
        Author = "Atys", EditorCompany = "Atys")]
    public class FileTranslatorExtension : IMesExtension
    {
        #region fields

        private IMesAppLogger _Logger;
        private IMesManager _MesManager = null; //riferimento a PowerMES

        #endregion

        #region IMesExtension implementation

        /// <summary>
        /// Inizializzazione estensione e collegamento all'oggetto principale PowerMES
        /// (eseguito al caricamento in memoria dell'estensione)
        /// </summary>
        /// <param name="mesManager">Riferimento all'oggetto principale PowerMES</param>
        public void Initialize(IMesManager mesManager)
        {
            //memorizzo il riferimento all'oggetto principale di PowerMES
            this._MesManager = mesManager;

            //strumenti per messaggi di DIAGNOSTICA:
            this._Logger = this._MesManager.ApplicationMainLogger;

            //questa istruzione inserisce un messaggio nel file di log di PowerMES
            this._MesManager.AppendMessageToLog(MessageLevel.Diagnostics, "FileTranslatorExtension", "Estensione creata!");
            //mentre la successiva invia un messaggio ai Clients
            this._MesManager.SendMessageToUI(MessageLevel.Diagnostics, "FileTranslatorExtension", "Estensione creata!");
        }

        /// <summary>Esegue/avvia l'estensione</summary>
        public void Run()
        {
#if DEBUG
            System.Diagnostics.Debugger.Launch();
#endif
            this._MesManager.FileWatcherConnectors.DataSource.ProcessingAllData += DataSource_ProcessingAllData;
        }

        /// <summary>
        /// Deve contenere il codice di cleanup da eseguire prima della disattivazione
        /// dell'estensione o comunque alla chiusura di PowerMES
        /// </summary>
        public void Shutdown()
        {
            this._MesManager.FileWatcherConnectors.DataSource.ProcessingAllData -= DataSource_ProcessingAllData;
        }


        #endregion

        #region mes components events

        void DataSource_ProcessingAllData(object sender, FileWatcherConnectorDataEventArgs e)
        {
            /*
             * NB: mettere un attributo sulla risorsa di tipo BOOL
             *     con nome DATA_TRANSLATE e valore a 1 per attivare la trasformazione dei dati
             */
            var resource = this._MesManager.ResourcesHandler.GetResource(e.Connector.ResourceId);

            //discrimino se devo tradurre
            var mustProcessAttribute = resource.Settings.ResourceAttributes.FirstOrDefault(a => a.Name == "DATA_TRANSLATE");

            var mustProcess = mustProcessAttribute != null
                              && mustProcessAttribute.IsValid
                              && !mustProcessAttribute.Disabled
                              && mustProcessAttribute.Type == ValueContainerType.Boolean
                              && mustProcessAttribute.GetConvertedValueToType<bool>();

            if (!mustProcess)
                return;

            //trasformazione
            var translator = new DataFileTranslator(e.Connector, this._Logger);

            var translationResult = translator.Translate(e.Data);

            if (translationResult == null || !translationResult.IsSuccess)
                return;

            //esito e uscita
            e.UpdatedData = translationResult.OutputContent;
        }

        #endregion




    }
}
