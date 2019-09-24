using System;
using System.Collections.Generic;
using System.Diagnostics;
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


        }

        /// <summary>
        /// Deve contenere il codice di cleanup da eseguire prima della disattivazione
        /// dell'estensione o comunque alla chiusura di PowerMES
        /// </summary>
        public void Shutdown()
        {

        }

        #endregion
    }
}