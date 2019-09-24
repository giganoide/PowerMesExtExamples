using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atys.PowerMES.Extensibility;
using Atys.PowerMES.Foundation;
using Atys.PowerMES.Support;

/*
 * Ogni libreria che contiene una estensione deve avere il nome che
 * termina con '.MesExt.dll'
 * e deve avere tra le reference le due librerie:
 * > Atys.PowerMES.Contracts
 * > Atys.PowerMES.Foundation
 * che si trovano nella directory di installazione dell'applicativo PowerMES.
 * Queste referenze NON devono essere copiate nella directory di destinazione-utilizzo.
 * 
 * Ogni estensione deve essere contenuta in una sottocartella del percorso:
 * > [PowerMES_Path]\Extensions
 * Quindi, ad esempio: [PowerMES_Path]\Extensions\MyPowerMesExtension\MyPowerMesExtension.MesExt.dll
 */

namespace TeamSystem.Customizations
{
    [ExtensionData("MyEmptyExtension", "Put here an extension description.", "1.0",
        Author = "TeamSystem", EditorCompany = "TeamSystem")]
    public class MyEmptyExtension : IMesExtension
    {
        private const string LOGSOURCE = @"MYEMPTYEXTENSION";

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

            /*
             * Your custom implementation here...
             * --
             * Fornire di seguito l'implementazione del metodo...
             */
        }

        /// <summary>Esegue/avvia l'estensione</summary>
        public void Run()
        {
            this._Logger.WriteMessage(MessageLevel.Diagnostics, true, LOGSOURCE,
                "Estensione creata!");
            /*
             * Your custom implementation here...
             * (Attach to application events, if needed)
             * --
             * Fornire di seguito l'implementazione del metodo...
             * (Se necessario creare qui i gestori eventi applicazione)
             */
        }

        /// <summary>
        /// Deve contenere il codice di cleanup da eseguire prima della disattivazione
        /// dell'estensione o comunque alla chiusura di PowerMES
        /// </summary>
        public void Shutdown()
        {
            /*
             * Your custom implementation here...
             * (Detach from application events)
             * --
             * Fornire di seguito l'implementazione del metodo...
             * (Rilasciare eventuale gestione eventi applicazione)
             */
        }

        #endregion
    }
}