using System.Diagnostics;
using Atys.PowerMES.Extensibility;
using Atys.PowerMES.Foundation;
using Atys.PowerMES.Support;
using TeamSystem.Customizations.Support;

namespace TeamSystem.Customizations
{
    [ExtensionData("MyEmptyExtension", "Put here an extension description.", "1.0",
        Author = "TeamSystem", EditorCompany = "TeamSystem")]
    public class MyAttributes : IMesExtension
    {
        private const string LOGSOURCE = @"MYATTRIBUTES";

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

            var resource = this._MesManager.ResourcesHandler.GetResource("Sabbiatrice");
            if (resource == null)
            {
                this._MesManager.AppendMessageToLog(MessageLevel.Diagnostics, LOGSOURCE,
                    "Risorsa non trovata");
                return;
            }

            var enabled = resource.Settings
                .ResourceAttributes
                .GetActiveAttributeValueOrDefault<bool>("MyResourceAttribute", ValueContainerType.Boolean);

            if (enabled)
            {
                var descr = this._MesManager.GeneralSettings.ServiceAttributes.GetActiveAttributeValueOrDefault<string>(
                    "MyApplicationAttribute", ValueContainerType.String);

                if (!string.IsNullOrWhiteSpace(descr))
                    this._Logger.WriteMessage(MessageLevel.Diagnostics, true, LOGSOURCE,
                        descr);
            }
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