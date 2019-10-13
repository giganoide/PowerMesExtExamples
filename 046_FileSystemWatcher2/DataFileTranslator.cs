using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Atys.PowerMES.Foundation;
using Atys.PowerMES.Support;

namespace TeamSystem.Customizations
{
    /// <summary>
    /// Classe per la trasformazione di un file DATA in comandi PowerMES
    /// </summary>
    internal sealed class DataFileTranslator
    {
        private readonly FileWatcherConnector _Connector;
        private readonly IMesAppLogger _Logger;
        private readonly NumberFormatInfo _NumberFormat;
        private readonly string _CommandsDateFormatter;

        /// <summary>
        /// Inizializza una nuova istanza della classe
        /// </summary>
        /// <param name="connector">Riferimento al watcher di una risorsa</param>
        /// <param name="logger">Sistema di log dell'applicazione</param>
        public DataFileTranslator(FileWatcherConnector connector, IMesAppLogger logger)
        {
            this._Connector = connector ?? throw new ArgumentNullException(nameof(connector));
            this._Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            //il file ha numeri decimali con il punto come separatore
            var nfi = (NumberFormatInfo)System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.Clone();
            nfi.NumberDecimalSeparator = ".";
            nfi.NumberGroupSeparator = string.Empty;

            this._NumberFormat = nfi;

            //per formattazione data prendo come riferimento
            //il formato presente nei parametri oppure un default
            this._CommandsDateFormatter = !string.IsNullOrWhiteSpace(this._Connector.Commands.DateArgumentFormatter)
                                              ? this._Connector.Commands.DateArgumentFormatter
                                              : @"dd/MM/yyyy HH:mm:ss";
        }

        /// <summary>
        /// Trasforma un intero file
        /// </summary>
        /// <param name="inputContent">Contenuto del file da tradurre</param>
        /// <returns>Risultato operazione</returns>
        public DataFileTRanslationResult Translate(string inputContent)
        {
            if (string.IsNullOrWhiteSpace(inputContent))
                return new DataFileTRanslationResult(true, string.Empty, "Testo da analizzare vuoto.");

            var translationResult = true;
            var outputStringBuilder = new StringBuilder();
            //var messagesStringBuilder = new StringBuilder();

            var lineSeparator = Environment.NewLine; //eventualmente estrarre da this._Connector.LineSeparatorCharCodes
            //mi aspetto un comando per linea, quindi divido
            var lines = inputContent.Split(new string[] { lineSeparator }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= 1)
            {
                //prima riga ha intestazioni campi
                return new DataFileTRanslationResult(false, string.Empty, "Testo da analizzare ha meno di due righe.");
            }

            for (int i = 1; i < lines.Length; i++)
            {
                var currentLine = lines[i];
                if (string.IsNullOrWhiteSpace(currentLine) || currentLine == "Automatic")
                {
                    //mi interrompo alla prima riga vuota o quando trovo inizio seconda sezione
                    break;
                }

                string currentLineOutput;

                if (!this.TranslateLine(currentLine, out currentLineOutput))
                {
                    translationResult = false;
                    break;
                }

                outputStringBuilder.Append(currentLineOutput);
            }

            var outputContent = translationResult
                                    ? outputStringBuilder.ToString()
                                    : string.Empty;

            return new DataFileTRanslationResult(translationResult,
                                                 outputContent,
                                                 string.Empty); //messagesStringBuilder.ToString()
        }

        /// <summary>
        /// Trasforma una singola riga del file in una serie di comandi
        /// PowerMES leggibili
        /// </summary>
        /// <param name="inputLine">Linea del file da trasformare</param>
        /// <param name="outputContent">Elenco comandi MES prodotti</param>
        /// <returns>Esito operazione</returns>
        private bool TranslateLine(string inputLine, out string outputContent)
        {
            outputContent = string.Empty;

            if (string.IsNullOrWhiteSpace(inputLine))
                return false;

            var workParams = this.ExtractParamsFromLine(inputLine);

            if (workParams == null)
                return false;

            outputContent = this.BuildMesCommands(workParams);

            return !string.IsNullOrWhiteSpace(outputContent);
        }

        /// <summary>
        /// Estrae i parametri di un ciclo di lavoro presenti 
        /// in una singola linea del file in ingresso
        /// </summary>
        /// <param name="inputLine">Stringa da cui estrarre i parametri</param>
        /// <returns>Parametri estratti, oppure <c>false</c> in caso di errori</returns>
        private SourceLineParams ExtractParamsFromLine(string inputLine)
        {

            if (string.IsNullOrWhiteSpace(inputLine))
                return null;

            const string fieldsSeparator = @";";

            /*
             * 0. Article
             * 1. ArticleNo
             * 2. Info
             * 3. StartDate
             * 4. StartTime
             * 5. EndDate
             * 6. EndTime
             * 7. CyclusTime
             * 8. BendTime
             */
            var fields = inputLine.Split(new string[] { fieldsSeparator }, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length != 9)
            {
                return null;
            }

            if (!TryJoinDateTime(fields[3], fields[4], out var startDateTime))
                return null;
            if (!TryJoinDateTime(fields[5], fields[6], out var endDateTime))
                return null;

            Debug.Assert(endDateTime >= startDateTime);

            var article = fields[0];
            var articleNo = fields[1];

            var cycleTime = double.Parse(fields[7], this._NumberFormat);
            var bendTime = double.Parse(fields[8], this._NumberFormat);

            return new SourceLineParams(article, articleNo,
                                        startDateTime, endDateTime,
                                        TimeSpan.FromSeconds(cycleTime),
                                        TimeSpan.FromSeconds(bendTime));
        }

        /// <summary>
        /// In base ai parametri di un ciclo di lavoro, produce una sequenza
        /// di comandi PowerMES inizio-fine con eventuale sospensione
        /// </summary>
        /// <param name="workParams">Parametri di un ciclo di lavoro</param>
        /// <returns>Comandi MES creati nel formato del watcher</returns>
        private string BuildMesCommands(SourceLineParams workParams)
        {
            if (workParams == null)
                throw new ArgumentNullException(nameof(workParams));

            var article = workParams.Article;
            var phase = "10";

            var suspensionTime = this.CalculateSuspensionTime(workParams.DeclaredDuration, workParams.CycleTime, workParams.BendTime);

            var result = this.BuildMesStartCommand(workParams.StartDateTime, article, phase);

            if (suspensionTime > TimeSpan.Zero)
            {
                var suspendTimeStamp = workParams.EndDateTime - suspensionTime;

                result += this.BuildMesSuspendCommand(suspendTimeStamp, article, phase);
                //il restart viene dato da PowerMES quando arriva il done
            }

            result += this.BuildMesDoneCommand(workParams.EndDateTime, article, phase, 1);

            return result;
        }

        private TimeSpan CalculateSuspensionTime(TimeSpan declaredDuration, TimeSpan cycleTime, TimeSpan bendTime)
        {
            Debug.WriteLine("> DEC = {0:#0.0} CT = {1:#0.0} BT: {2:#0.0}",
                            declaredDuration.TotalSeconds,
                            cycleTime.TotalSeconds,
                            bendTime.TotalSeconds);

            //differenza tra bendtime e cycletime è il tempo di fermo
            if (bendTime >= cycleTime)
                return TimeSpan.Zero;

            var difference = cycleTime - bendTime;

            //PowerMES ha risoluzione minima un secondo
            if (difference.Seconds == 0)
                return TimeSpan.Zero;

            return difference;
        }

        private string BuildMesStartCommand(DateTime localTimeStamp, string article, string phase)
        {
            /*
             * NB: nelle opzioni del watcher il data-pattern del comando deve essere impostato come segue
             * 
             * {ART}{PHASE}{DATE} =>es: I;A1234;10;05/11/2017 12:51:38
             */

            const string realCommandPattern = @"{0}{1}{2}{1}{3}{1}{4}{5}";


            var commandIdentifier = this._Connector.Commands.StartCommandCharCodes.GetSafeTextFromAsciiCodes();
            var separator = this._Connector.DataSeparatorCharCode.GetSafeTextFromAsciiCodes();

            var result = string.Format(realCommandPattern,
                                       commandIdentifier, separator,
                                       article, phase,
                                       localTimeStamp.ToString(this._CommandsDateFormatter),
                                       Environment.NewLine);

            return result;
        }

        private string BuildMesSuspendCommand(DateTime localTimeStamp, string article, string phase)
        {
            /*
            * NB: nelle opzioni del watcher il data-pattern del comando deve essere impostato come segue
            * 
            * {ART}{PHASE}{REA}{DATE} =>es: S;A1234;10;SOSP;05/11/2017 12:51:38
            */

            const string realCommandPattern = @"{0}{1}{2}{1}{3}{1}{4}{1}{5}{6}";
            const string reason = "SOSP.GENERICA";


            var commandIdentifier = this._Connector.Commands.SuspensionCommandCharCodes.GetSafeTextFromAsciiCodes();
            var separator = this._Connector.DataSeparatorCharCode.GetSafeTextFromAsciiCodes();

            var result = string.Format(realCommandPattern,
                                       commandIdentifier, separator,
                                       article, phase,
                                       reason,
                                       localTimeStamp.ToString(this._CommandsDateFormatter),
                                       Environment.NewLine);

            return result;
        }

        private string BuildMesDoneCommand(DateTime localTimeStamp, string article, string phase, int qty)
        {
            /*
            * NB: nelle opzioni del watcher il data-pattern del comando deve essere impostato come segue
            * 
            * {ART}{PHASE}{QTY}{DATE} =>es: V;A1234;10;1;05/11/2017 12:51:38
            */

            const string realCommandPattern = @"{0}{1}{2}{1}{3}{1}{4}{1}{5}{6}";

            var commandIdentifier = this._Connector.Commands.DoneCommandCharCodes.GetSafeTextFromAsciiCodes();
            var separator = this._Connector.DataSeparatorCharCode.GetSafeTextFromAsciiCodes();

            var result = string.Format(realCommandPattern,
                                       commandIdentifier, separator,
                                       article, phase,
                                       qty,
                                       localTimeStamp.ToString(this._CommandsDateFormatter),
                                       Environment.NewLine);

            return result;
        }

        /// <summary>
        /// Decodifica una data ed un'ora da formato stringa e li unisce in un solo DateTime
        /// </summary>
        /// <param name="datePart">Sezione data da decodificare</param>
        /// <param name="timePart">Sezione ora da decodificare</param>
        /// <param name="extractedDateTime">Data prodotta</param>
        /// <returns>Esito operazione</returns>
        private static bool TryJoinDateTime(string datePart, string timePart, out DateTime extractedDateTime)
        {
            extractedDateTime = DateTime.Now;

            if (string.IsNullOrWhiteSpace(datePart))
                throw new ArgumentException("Value cannot be null or whitespace.", "datePart");
            if (string.IsNullOrWhiteSpace(timePart))
                throw new ArgumentException("Value cannot be null or whitespace.", "timePart");

            const string dateFormatPattern = @"dd.MM.yyyy";
            const string timeFormatPattern = @"hh\:mm\:ss\.fff"; //@"HH:mm:ss.ttt";

            DateTime date;
            if (!DateTime.TryParseExact(datePart, dateFormatPattern,
                                        System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat,
                                        DateTimeStyles.None, out date))
            {
                return false;
            }

            TimeSpan time;
            if (!TimeSpan.TryParseExact(timePart, timeFormatPattern,
                                        System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat,
                                        out time))
            {
                return false;
            }

            extractedDateTime = date.ToLocalTime().Date.Add(time);

            return true;
        }

    }
}