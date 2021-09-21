using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atys.PowerMES.Extensibility;
using Atys.PowerMES.Foundation;
using Atys.PowerMES.Support;

namespace TeamSystem.Customizations
{
    [ExtensionData("MyFileSystemWatcherExtension", "Put here an extension description.", "1.0",
        Author = "TeamSystem", EditorCompany = "TeamSystem")]
    public class MyFileSystemWatcher : IMesExtension
    {
        private const string LOGSOURCE = @"MYFILESYSTEMWATCHER";

        private IMesManager _MesManager = null;
        private IMesAppLogger _Logger = null;
        private readonly object _SyncObject = new object(); //per sincronizzazione multithread

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

            this._MesManager.FileWatcherConnectors.DataSource.BeforeProcessingFiles += DataSource_BeforeProcessingFiles;
            this._MesManager.FileWatcherConnectors.DataSource.QueryForExternalDataFileHandling += DataSource_QueryForExternalDataFileHandling;
            this._MesManager.FileWatcherConnectors.DataSource.ProcessingAllData += DataSource_ProcessingAllData;
            this._MesManager.FileWatcherConnectors.DataSource.ProcessingDataLine += DataSource_ProcessingDataLine;
            this._MesManager.FileWatcherConnectors.DataSource.ProcessingParsingError += DataSource_ProcessingParsingError;
            this._MesManager.FileWatcherConnectors.DataSource.ProcessingPatternError += DataSource_ProcessingPatternError;
        }

        /// <summary>
        /// Deve contenere il codice di cleanup da eseguire prima della disattivazione
        /// dell'estensione o comunque alla chiusura di PowerMES
        /// </summary>
        public void Shutdown()
        {
            this._MesManager.FileWatcherConnectors.DataSource.ProcessingAllData -= DataSource_ProcessingAllData;
            this._MesManager.FileWatcherConnectors.DataSource.BeforeProcessingFiles -= DataSource_BeforeProcessingFiles;
            this._MesManager.FileWatcherConnectors.DataSource.QueryForExternalDataFileHandling -= DataSource_QueryForExternalDataFileHandling;
            this._MesManager.FileWatcherConnectors.DataSource.ProcessingDataLine -= DataSource_ProcessingDataLine;
            this._MesManager.FileWatcherConnectors.DataSource.ProcessingParsingError -= DataSource_ProcessingParsingError;
            this._MesManager.FileWatcherConnectors.DataSource.ProcessingPatternError -= DataSource_ProcessingPatternError;
        }

        #endregion

        #region FileSystemWatcher data source events

        /*
         * E' possibile filtrare in modo personalizzato i files
         * che devono essere elaborati tramite gli eventi
         * DataSource_BeforeProcessingFiles e DataSource_QueryForExternalDataFileHandling
         * da considerare mutualmente esclusivi.
         */

        /// <summary>
        /// Gestione evento per verifica files da analizzare prima di procedura
        /// NB: VIENE GENERATO SOLO QUANDO VENGONO TROVATI PIU' FILES,
        ///     SE TROVATO SOLO UNO NON VIENE GENERATO (POSSIBILI BREAKING CHANGES IN FUTURO)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DataSource_BeforeProcessingFiles(object sender, FileWatcherConnectorFilesEventArgs e)
        {
            if (e.Filenames == null || e.Filenames.Count == 0)
                return;

            List<string> filesToProcess = new List<string>();
            List<string> filesToDelete = new List<string>();

            /*
             * Esempio: si stabilisce se un file deve essere elaborato in base al nome
             * In questo caso immaginiamo che il nome contenga la data (giorno)
             * e si voglia analizzare solo quello del giorno corrente.
             * Immaginiamo che il file del giorno non si possa eliminare
             *
             */

            foreach (var fileFullPath in e.Filenames)
            {
                var canProcess = this.CheckIfCanProcessByFileName(fileFullPath);
                if (canProcess)
                {
                    filesToProcess.Add(fileFullPath);

                    this._MesManager.AppendMessageToLog(MessageLevel.Diagnostics, LOGSOURCE,
                                                        "DataSource_BeforeProcessingFiles(): file da processare = " + fileFullPath);
                }
                else
                {
                    filesToDelete.Add(fileFullPath);

                    this._MesManager.AppendMessageToLog(MessageLevel.Diagnostics, LOGSOURCE,
                                    "DataSource_BeforeProcessingFiles(): file da ELIMINARE = " + fileFullPath);
                }
            }

            e.UpdatedFilenames = filesToProcess;

            /*
             * In questo caso, se necessario, mi devo occupare direttamente
             * dell'eliminazione dei files dei giorni precedenti
             */
            if (filesToDelete.Count > 0)
                this.DeleteFiles(filesToDelete);
        }

        /// <summary>
        /// Gestione evento per gestione singolo file da esterno
        /// NB: verifico qui se posso processare il file quando ne viene trovato solo uno
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DataSource_QueryForExternalDataFileHandling(object sender, FileWatcherConnectorDataHandlingEventArgs e)
        {
            var canProcess = this.CheckIfCanProcessByFileName(e.SourceFileInfo.FullPath);
            if (!canProcess)
            {
                e.Handled = true; //da considerare come già elaborato
                //non posso eliminare il file da qui
            }
        }

        /// <summary>
        /// Gestione evento FileSystemWatcher per modificare tutto il contenuto del file
        /// in un solo passaggio
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DataSource_ProcessingAllData(object sender, FileWatcherConnectorDataEventArgs e)
        {
            lock (this._SyncObject)
            {
                //TODO: trasformazione del testo completo contenuto nel file
                //e.SourceFileInfo: contiene i dati del file
                //e.UpdatedData = outputData; //inserisco il frutto dell'elaborazione in UpdatedData
            }
        }

        /// <summary>
        /// Gestione evento FileSystemWatcher per modificare il contenuto
        /// di ogni singola riga
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DataSource_ProcessingDataLine(object sender, FileWatcherConnectorDataEventArgs e)
        {
            //PRIMA VERSIONE PROCEDURA CON ANALISI SINGOLA RIGA
            //ED ELIMINAZIONE FILE

            lock (this._SyncObject)
            {
                //TODO: trasformazione della singola riga
                
                //e.SourceFileInfo: contiene i dati del file
                //e.UpdatedData = outputDataLine; //inserisco il frutto dell'elaborazione in UpdatedData
            }
        }

        /// <summary>
        /// Gestione evento FileSystemWatcher per notifica errore
        /// di decodifica degli argomenti dei comandi rispetto
        /// alla sequanza-pattern previsto
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DataSource_ProcessingPatternError(object sender, FileWatcherConnectorFullDataEventArgs e)
        {
            //TODO log
        }

        /// <summary>
        /// Gestione evento FileSystemWatcher per notifica errore
        /// nell'utilizzo dei parametri decodificati
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DataSource_ProcessingParsingError(object sender, FileWatcherConnectorFullDataEventArgs e)
        {
            //TODO log
        }

        #endregion

        /// <summary>
        /// Verifica se un file può essere elaborato
        /// NB: in questo esempio gestiamo solo files del giorno, non precedenti
        /// </summary>
        /// <param name="fileFullPath">percorso completo file</param>
        /// <returns>Se il file è del giorno e può essere elaborato</returns>
        private bool CheckIfCanProcessByFileName(string fileFullPath)
        {
            const string datePattern = @"yyyyMMdd";

            var filename = Path.GetFileNameWithoutExtension(fileFullPath);
            if (filename == null)
                return false;

            var today = DateTime.Now.Date.ToString(datePattern);

            var result = filename.StartsWith(today);

            return result;
        }

        /// <summary>
        /// Elimina una serie di files
        /// </summary>
        /// <param name="filesToDelete">Elenco percorsi completi files da eliminare</param>
        private void DeleteFiles(IList<string> filesToDelete)
        {
            if (filesToDelete == null || filesToDelete.Count == 0)
                return;

            foreach (var filename in filesToDelete)
            {
                try
                {
                    File.Delete(filename);

                    this._MesManager.AppendMessageToLog(MessageLevel.Diagnostics, LOGSOURCE,
                                                        "File eliminato:" + filename);
                }
                catch (Exception ex)
                {
                    //do nothing
                    this._MesManager.AppendExceptionToLog(LOGSOURCE, "Errore durante eliminazione file: " + filename, ex);
                    this._MesManager.SendMessageToUI(MessageLevel.Warning, LOGSOURCE, "Errore durante eliminazione file: " + filename);
                }
            }
        }
    }
}