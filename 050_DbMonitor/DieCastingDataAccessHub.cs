using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Atys.PowerMES.Foundation;
using Atys.PowerMES.Support;
using Atys.PowerMES.Support.Data;
using Atys.PowerMES.Support.Data.Settings;

namespace TeamSystem.Customizations
{
    /// <summary>
    /// Funzioni per l'accesso alle tabelle dei database delle macchine
    /// </summary>
    internal sealed class DieCastingDataAccessHub
    {
        #region const

        private const string DATABASE_PRODUCTION = @"DIECASTING";
        private const string LOGGERSOURCE = @"DIECASTING_DH_";

        private const string SELECT_PRODUCTION = @"SELECT [Nr],[IDCQ],[DateACQ],[TimeACQ],"
                                                 + @"[Order_],[Name_],[Pz_prod_],[Nr_piece_],"
                                                 + @"[Cycle_],[Mode_],[Quality],[TC_] "
                                                 + @"FROM [dbo].[PRODUZ] WITH (NOLOCK) WHERE [Cycle_] > {0}";

        private const string SELECT_SERVERDATE = @"SELECT GETDATE() AS DT";

        #endregion

        #region fields

        private readonly IMesAppLogger _Logger;
        private readonly string _SqlInstanceName;
        private readonly string _UserName;
        private readonly string _Password;
        private readonly string _LoggerSource;

        #endregion

        /// <summary>
        /// Inizializza una nuova istanza della classe
        /// </summary>
        /// <param name="sqlInstanceName">Nome dell'istanza SQl su cui si trova il database da esaminare</param>
        /// <param name="userName">nome utente per la connessione, se non fornito verranno
        /// usate quelle di default</param>
        /// <param name="password">password dell'utente</param>
        /// <param name="logger">Riferimento a sistema di log applicazione</param>
        public DieCastingDataAccessHub(string sqlInstanceName, string userName, string password, IMesAppLogger logger)
        {
            if (string.IsNullOrWhiteSpace(sqlInstanceName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sqlInstanceName));

            this._Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this._SqlInstanceName = sqlInstanceName;
            if (string.IsNullOrWhiteSpace(userName))
            {
                this._UserName = "sa"; //TODO utente per accesso sql
                this._Password = "SEGNAPOSTO_DA_CAMBIARE"; //TODO password per accesso sql
            }
            else
            {
                this._UserName = userName;
                this._Password = password;
            }

            this._LoggerSource = LOGGERSOURCE + sqlInstanceName;
        }

        /// <summary>
        /// Verifica la raggiungibilità dell'istanza di MS SQL
        /// </summary>
        /// <returns></returns>
        public bool CheckConnection()
        {
            var dbEngine = this.CreateDbEngine(DATABASE_PRODUCTION);

            var opResult = dbEngine.CheckConnection();

            if (opResult != null && opResult.IsFault)
            {
                this._Logger.WriteMessage(MessageLevel.Warning, true, _LoggerSource,
                                          "CheckConnection(): CANNOT CONNECT TO DB");

                this._Logger.WriteException(opResult.Error, _LoggerSource, opResult.Message);
            }

            return opResult != null && opResult.Success;
        }

        public DateTime GetServerTime()
        {
            var dbEngine = this.CreateDbEngine(DATABASE_PRODUCTION);

            var selectResult = dbEngine.ExecuteScalarCommand(new CommandInfo(SELECT_SERVERDATE));
            if (selectResult == null)
                return DateTime.MinValue;

            var result = (DateTime)selectResult;

            return result;
        }

        /// <summary>
        /// Restituisce i dati delle stampate con identificativo
        /// maggiore di quello specificato
        /// </summary>
        /// <param name="fromRecordNumber">Id della stampata da usare come filtro
        /// (escluso l'id specificato)</param>
        /// <returns>elenco dati stampate</returns>
        public ICollection<DieCastingProductionStroke> GetProduction(long fromRecordNumber)
        {
            this._Logger.WriteMessage(MessageLevel.Diagnostics, false, _LoggerSource,
                                      "GetProduction(): start");

            var dbEngine = this.CreateDbEngine(DATABASE_PRODUCTION);

            var selectCommandText = string.Format(SELECT_PRODUCTION, fromRecordNumber);

            var commandInfo = new CommandInfo(selectCommandText, CommandType.Text, null);

            var data = dbEngine.GetTableData("PRODUCTION", commandInfo);
            if (data == null || data.Rows.Count == 0)
            {
                this._Logger.WriteMessage(MessageLevel.Diagnostics, false, _LoggerSource,
                                          "GetProduction(): no strokes found");
                return new List<DieCastingProductionStroke>();
            }

            var result = data.AsEnumerable().Select(r => new DieCastingProductionStroke(r)).OrderBy(x => x.MachineCycleNumber).ToList();

            this._Logger.WriteMessage(MessageLevel.Diagnostics, false, _LoggerSource,
                                      "GetProduction(): {0} new strokes found", result.Count);

            return result;
        }

        #region helpers

        /// <summary>
        /// Crea un connettore per accesso ai dati su db
        /// </summary>
        /// <param name="database">Nome database a cui ci si deve connettere</param>
        /// <returns>Gestore comandi su db creato</returns>
        private IDatabaseActions CreateDbEngine(string database)
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(database));

            var sqlSettings = new SqlConnectionSettings()
                              {
                                  ServerOrInstanceName = this._SqlInstanceName,
                                  Database = database,
                                  UserName = this._UserName,
                                  Password = this._Password,
                                  UseIntegratedSecurity = false,

                              };

            /*
             * SqlDatabaseActions è una classe utility fornita nella DLL Atys.PowerMES.Support.dll
             * che quindi deve essere referenziata.
             * Fornisce metodi standard per query e comandi di vario tipo
             *
             */

            var result = new SqlDatabaseActions(sqlSettings, this._Logger);

            return result;
        }

        #endregion

    }
}