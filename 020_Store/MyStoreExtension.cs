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
using Atys.PowerMES.Services;
using Atys.PowerMES.Support;
using Atys.PowerMES.Support.Serialization;

namespace TeamSystem.Customizations
{
    [ExtensionData("MyStoreExtension", "Put here an extension description.", "1.0",
        Author = "TeamSystem", EditorCompany = "TeamSystem")]
    public class MyStoreExtension : IMesExtension
    {
        private const string LOGGERSOURCE = @"MYSTOREEXTENSION";

        private IMesManager _MesManager = null;
        private IMesAppLogger _Logger = null;


        public void Initialize(IMesManager mesManager)
        {
#if DEBUG
            Debugger.Launch();
#endif
            this._MesManager = mesManager;
            this._Logger = this._MesManager.ApplicationMainLogger;
        }

        public void Run()
        {
            this._MesManager.Controller.BeforeProcessingEvent += this.Controller_BeforeProcessingEvent;
        }

        public void Shutdown()
        {
            this._MesManager.Controller.BeforeProcessingEvent -= this.Controller_BeforeProcessingEvent;
        }

        #region Events
        
        private void Controller_BeforeProcessingEvent(object sender, ResourceDataUnitEventArgs e)
        {
            if (e.Unit is ProductDoneEvent doneEvent)
            {
                var memento = this.LoadResourceMemento(e.Resource.Name);

                // posso azzerare il contatore quando cambia l'articolo ad esempio
                memento.Articolo = doneEvent.Article.Article;
                memento.Contatore += doneEvent.Quantity;

                this.SaveResourceMemento(e.Resource.Name, memento);
            }
        }
        #endregion
        
        #region Store

        /// <summary>
        /// Carica i dati accessori di elaborazione dalla tabella SH97_RepositoryValues
        /// o crea un default
        /// </summary>
        /// <param name="resourceName">Nome della risorsa di cui si vogliono i dati</param>
        /// <returns>Dati caricati</returns>
        private TestMemento LoadResourceMemento(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
                throw new ArgumentException("Value cannot be null or whitespace.", "resourceName");

            var storageValueService = _MesManager.ServiceManager.GetService<IStorageValuesService>();

            var processingMementoContainer = storageValueService.GetOrCreateValue(
                MesGlobalConstants.PowerMesApplicationName,
                LOGGERSOURCE, "PROVA_" + resourceName);
            var processingMemento = string.IsNullOrWhiteSpace(processingMementoContainer.Value)
                ? new TestMemento()
                : JsonSerializer.DeserializeObject<TestMemento>(processingMementoContainer.Value);
            return processingMemento;
        }

        /// <summary>
        /// Salva i dati accessori di elaborazione nella tabella SH97_RepositoryValues
        /// </summary>
        /// <param name="resourceName">Nome della risorsa a cui si riferiscono i dati</param>
        /// <param name="memento">Dati da salvare</param>
        private void SaveResourceMemento(string resourceName, TestMemento memento)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
                throw new ArgumentException("Value cannot be null or whitespace.", "resourceName");

            if (memento == null)
            {
                this._Logger.WriteMessage(MessageLevel.Warning, true, LOGGERSOURCE,
                    "SaveResourceMemento(): dati da salvare non validi.");
                return;
            }

            var storageValueService = this._MesManager.ServiceManager.GetService<IStorageValuesService>();
            var processingMementoContainer = storageValueService.GetOrCreateValue(
                MesGlobalConstants.PowerMesApplicationName,
                LOGGERSOURCE,
                "PROVA_" + resourceName);

            var mementoJson = JsonSerializer.SerializeObject(memento);  // è quello in support

            //aggiornamento valori memorizzati su db locale PowerMES con parametri vari
            processingMementoContainer.ApplyValue(mementoJson, typeof(TestMemento), true);
            var saveResult = storageValueService.SaveValue(processingMementoContainer);

            this._Logger.WriteMessage(MessageLevel.Diagnostics, true, LOGGERSOURCE,
                "SaveResourceMemento(): salvataggio dati completato = {0}",
                saveResult);
        }

        #endregion
    }


    public class TestMemento
    {
        public int Contatore { get; set; }
        public string Articolo { get; set; }
    }
}