using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atys.PowerMES.Extensibility;
using Atys.PowerMES.Foundation;
using Atys.PowerMES.Foundation.DTO;
using Atys.PowerMES.Repeaters;
using Atys.PowerMES.Repeaters.GammaMes;
using Atys.PowerMES.Settings.GammaMes;
using Atys.PowerMES.Support;

namespace TeamSystem.Customizations
{
    [ExtensionData("MyMesInteractExtension", "Put here an extension description.", "1.0",
        Author = "TeamSystem", EditorCompany = "TeamSystem")]
    public class MyMesInteract : IMesExtension
    {
        private const string LOGSOURCE = @"MYMESINTERACT";

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


            this._MesManager.InitializationCompleted += _MesManager_InitializationCompleted;
        }

        /// <summary>
        /// Deve contenere il codice di cleanup da eseguire prima della disattivazione
        /// dell'estensione o comunque alla chiusura di PowerMES
        /// </summary>
        public void Shutdown()
        {
            this._MesManager.InitializationCompleted -= _MesManager_InitializationCompleted;
        }

        #endregion

        /// <summary>
        /// Gestione evento generato al termine dell'inizializzazione di tutti
        /// i sotto-sistemi di PowerMES
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _MesManager_InitializationCompleted(object sender, EventArgs e)
        {
            var tsMesRepeater = this._MesManager.RepeatersManager.GetRepeater<IGammaMesRepeater>();
            if (tsMesRepeater == null)
            {
                this._Logger.WriteMessage(MessageLevel.Warning, true, LOGSOURCE,
                                          "Data Repeater TS MES non disponibile");
                return;
            }

            /*
             * al termine dell'inizializzazione ho la certezza che anche tutti
             * i data repeaters sono disponibili e posso agganciarmi
             * ai loro eventi specifici
             */

            tsMesRepeater.QueryForAlternateCommandTargetResource += this.GammaMesRepeater_QueryForAlternateCommandTargetResource;
            tsMesRepeater.ValidatingCommandProcessVariableValues += this.GammaMesRepeater_ValidatingCommandProcessVariableValues;
        }

        /// <summary>
        /// Gestione dell'evento che permette di validare o modificare il valore di un parametro
        /// di processo associato ad una dichiarazione
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GammaMesRepeater_ValidatingCommandProcessVariableValues(object sender, GammaMesProcessVariableValuesEventArgs e)
        {
            var newValues = new List<GMProcessVariableValue>();
            foreach (GMProcessVariableValue variableValue in e.Values)
            {
                //if...
                var newValue = new GMProcessVariableValue()
                {
                    CurrentValue = "158",
                    Mapping = variableValue.Mapping
                };
                //se non aggiungo alla lista nuovi valori,
                //questo non verrà inviato o verranno usati i default da parametri
                newValues.Add(newValue);
            }

            e.OverriddenValues = newValues;
            e.Handled = true;
        }

        /// <summary>
        /// Gestione evento per la modifica dinamica della mappatura di una risorsa PowerMES
        /// verso una risorsa MES
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GammaMesRepeater_QueryForAlternateCommandTargetResource(object sender, GammaMesResourceProcessingEventArgs e)
        {
            /*
             * la mappatura tra risorse PowerMES e MES è statica e stabilita
             * in fase di configurazione.
             * E' però possibile modificare la mappatura dinamicamente per ogni chiamata
             * al web service di MES.
             * e.Command permette di sapere qual è il comando per cui è stato invocato l'evento
             *
             */

            if (e.Resource.Name == "NOME_RISORSA_PWMES")
            {
                //NB: idDepartment, idCenter, idMachine da prendere su TS MES
                var result = new GMResource(e.GammaMachine.CompanyCode,
                                            12, string.Empty, string.Empty,
                                            35, string.Empty, string.Empty,
                                            77, string.Empty, string.Empty);

                e.UpdatedGammaResource = result;
            }

        }

        /// <summary>
        /// Esempio di funzione per la lettura diretta di una tabella o una vista
        /// presente sulla base dati di MES
        /// </summary>
        /// <param name="resourceName">Nome della risorsa per cui si devono caricare i dati</param>
        /// <returns>Dati caricati da db MES</returns>
        private DataTable LeggiDaMesDatabase(string resourceName)
        {
            /*
             * in questo snippet si ipotizza l'esistenza di una vista personalizzata X_V_BOLLE_INIZIATE
             * sulla base dati di MES, con campi NUM_BOLLA, ARTICOLO
             * NON è possibile effettuare modifiche, ma solo leggere i dati.
             */

            var tsMesRepeater = this._MesManager.RepeatersManager.GetRepeater<IGammaMesRepeater>();

            if (!tsMesRepeater.DirectAccessEnabled)
            {
                //nelle opzioni del data repeater l'accesso diretto al db MES deve essere abilitato
                //e devono essere inserite le credenziali di accesso e gli altri parametri
                //per la costruzione della stringa di connessione
                return null;
            }

            IGammaMesDirectDataManager dataReader = tsMesRepeater.GetDirectDataManager();

            var query = $@"SELECT [NUM_BOLLA],[ARTICOLO] FROM [dbo].[X_V_BOLLE_INIZIATE] WHERE [MACCHINA] = '{resourceName}'";
            var result = dataReader.GetTableData("PROVA", query);

            return result;
        }

        /// <summary>
        /// Legge l'elenco delle bolle associate ad una risorsa
        /// </summary>
        /// <param name="resourceName"></param>
        private void BolleSuRisorsa(string resourceName)
        {
            var resource = this._MesManager.ResourcesHandler.GetResource(resourceName);
            var tsMesRepeater = this._MesManager.RepeatersManager.GetRepeater<IGammaMesRepeater>();

            var jobTickets = tsMesRepeater.GetResourceAvailableJobTickets(resource);

            if (jobTickets == null)
                return;

            foreach (GMJobTicketDescriptorDTO jobTicket in jobTickets)
            {
                Debug.WriteLine($"BOLLA: {jobTicket.WorkOrder} QTA: {jobTicket.TotalQuantity:#0.00}");
            }
        }

        /// <summary>
        /// Legge l'elenco delle bolle associate ad una risorsa, con il relativo stato
        /// </summary>
        /// <param name="resourceName"></param>
        private void BolleConStatoSuRisorsa(string resourceName)
        {
            var resource = this._MesManager.ResourcesHandler.GetResource(resourceName);
            var tsMesRepeater = this._MesManager.RepeatersManager.GetRepeater<IGammaMesRepeater>();

            var productions = tsMesRepeater.GetResourceGammaProductions(resource);

            if (productions == null)
                return;

            foreach (GMProductionStateDTO production in productions)
            {
                Debug.WriteLine($"BOLLA: {production.WorkOrder} QTA: {production.State}");
            }

            /*
             * l'enum RepeaterProductionItemState rappresenta lo stato
             * della lavorazione su MES
             * Unknown
               NotDefined
               Setup
               IndirectActivity
               Maintenance
               Started
               Completed
               WorkSuspended
             *
             */
        }
    }
}