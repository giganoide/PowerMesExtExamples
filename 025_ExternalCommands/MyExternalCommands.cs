using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Atys.PowerMES.Extensibility;
using Atys.PowerMES.Foundation;
using Atys.PowerMES.Support;

namespace TeamSystem.Customizations
{

    [ExtensionData("PowerMesStore.MesExt", "Put here an extension description.", "1.0",
        Author = "TeamSystem", EditorCompany = "TeamSystem")]
    public class MyExternalCommands : IMesExtension
    {
        private const string LOGGERSOURCE = @"MYEXTERNALCOMMANDS";

        private IMesManager _MesManager = null; //riferimento a PowerMES
        private IMesAppLogger _Logger = null;


        public void Initialize(IMesManager mesManager)
        {
#if DEBUG
            Debugger.Launch();
#endif
            //memorizzo il riferimento all'oggetto principale di PowerMES
            this._MesManager = mesManager;
            this._Logger = this._MesManager.ApplicationMainLogger;
        }

        public void Run()
        {
            this._MesManager.ProcessExternalCommand += this._MesManager_ProcessExternalCommand;
            this.PublishExternalCommands();
        }

        public void Shutdown()
        {
            this.RevokeExternalCommands();
        }


        #region external commands

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

            if (e.CommandCode == "PIANO_LAVORO"
                && paramList.Count == 1)
            {
                var resourceName = paramList[0].GetConvertedValueToType<string>();

                var resource = this._MesManager.ResourcesHandler.GetResource(resourceName);
                if (resource == null)
                {
                    this._MesManager.AppendMessageToLog(MessageLevel.Diagnostics, "WorkPlanBuilderExtension",
                                                        "Risorsa non trovata (PIANO_LAVORO)");
                    return;
                }

                this._MesManager.AppendMessageToLog(MessageLevel.Diagnostics, "WorkPlanBuilderExtension",
                                                    "EXTERNAL COMMAND (PIANO_LAVORO)");


                var enabledAttribute = resource.Settings
                                               .ResourceAttributes
                                               .FirstOrDefault(a => !a.Disabled
                                                                    && a.Name == "CORSO"
                                                                    && a.Type == ValueContainerType.Boolean);

                var enabled = enabledAttribute != null && enabledAttribute.GetConvertedValueToType<bool>();

                if (!enabled)
                    return;


                //TODO esecuzione azione
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
                                                        "PIANO_LAVORO",
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
    }
}
