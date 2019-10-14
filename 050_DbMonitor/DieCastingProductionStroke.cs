using System;
using System.Data;
using Atys.PowerMES.Foundation;

namespace TeamSystem.Customizations
{
    /// <summary>
    /// Descrizione dei dati prodotti da una stampata della pressa
    /// </summary>
    internal sealed class DieCastingProductionStroke
    {
        /// <summary>
        /// Restituisce il numero identificativo del record
        /// float su db: Nr
        /// </summary>
        public int RecordNumber { get; private set; }
        /// <summary>
        /// Restituisce il numero univoco relativo all’iniezione
        /// IDCQ
        /// </summary>
        public string StrokeId { get; private set; }
        /// <summary>
        /// Restituisce il timestamp della stampata
        /// DateACQ+TimeACQ
        /// </summary>
        public DateTime Timestamp { get; private set; }
        /// <summary>
        /// Restituisce il codice ordine
        /// Order_
        /// </summary>
        public string Order { get; private set; }
        /// <summary>
        /// Restituisce il nome dello stampo
        /// Name_
        /// </summary>
        public string MouldName { get; private set; }

        /// <summary>
        /// Restituisce il numero totale di pezzi completati
        /// Pz_prod_: int
        /// </summary>
        public int TotalCompletedQty { get; private set; }

        /// <summary>
        /// Restituisce il numero di impronte dello stampo
        /// Nr_piece_: int
        /// </summary>
        public int MouldTracksNumber { get; private set; }

        /// <summary>
        /// Restituisce il numero ciclo macchina
        /// Cycle_: decimal(18,0)
        /// </summary>
        public long MachineCycleNumber { get; private set; }

        /// <summary>
        /// Restituisce se la macchina è running
        /// Mode_ = 3 int
        /// </summary>
        public bool MachineIsRunning { get; private set; }

        /// <summary>
        /// Restituisce se la battuta ha prodotto pezzi buoni
        /// Quality: decimal(18,0) = 1 pezzo buono
        /// </summary>
        public bool IsGood { get; private set; }

        /// <summary>
        /// Restituisce il tempo ciclo calcolato dalla macchina
        /// </summary>
        public TimeSpan CycleTime { get; private set; }

        /// <summary>
        /// Restituisce un articolo per la lavorazione utilizzando
        /// il codice stampo
        /// </summary>
        public ArticleItem Article
        {
            get
            {
                var art = !string.IsNullOrWhiteSpace(this.MouldName)
                              ? this.MouldName
                              : "ATYS";
                return new ArticleItem(art, "10");
            }
        }

        public DieCastingProductionStroke(int recordNumber, string strokeId,
                                      DateTime timestamp, string mouldName, string order,
                                      int totalCompletedQty, int mouldTracksNumber,
                                      long machineCycleNumber, bool machineIsRunning, bool isGood)
        {
            this.RecordNumber = recordNumber;
            this.StrokeId = strokeId;
            this.Timestamp = timestamp;
            this.MouldName = mouldName;
            this.Order = order;
            this.TotalCompletedQty = totalCompletedQty;
            this.MouldTracksNumber = mouldTracksNumber;
            this.MachineCycleNumber = machineCycleNumber;
            this.MachineIsRunning = machineIsRunning;
            this.IsGood = isGood;
        }

        public DieCastingProductionStroke(DataRow row)
        {
            const string dateTimeFormat = @"dd/MM/yyyy HH:mm:ss"; //TODO

            if (row == null) throw new ArgumentNullException(nameof(row));

            var nr = !row.IsNull("Nr") ? row.Field<double>("Nr") : 0;
            var dateString = row.Field<string>("DateACQ") ?? string.Empty;
            var timeString = row.Field<string>("TimeACQ") ?? string.Empty;
            var fullDateString = dateString + " " + timeString;
            var mode = !row.IsNull("Mode_") ? row.Field<int>("Mode_") : 0;
            var quality = !row.IsNull("Quality") ? row.Field<decimal>("Quality") : 0;
            var cycleTime = (double)(!row.IsNull("TC_") ? row.Field<decimal>("TC_") : 0);


            this.RecordNumber = Convert.ToInt32(Math.Truncate(nr));
            this.StrokeId = row.Field<string>("IDCQ") ?? string.Empty;
            this.Timestamp = DateTime.ParseExact(fullDateString, dateTimeFormat, System.Globalization.CultureInfo.InvariantCulture);
            this.MouldName = row.Field<string>("Name_") ?? string.Empty;
            this.Order = row.Field<string>("Order_") ?? string.Empty;
            this.TotalCompletedQty = !row.IsNull("Pz_prod_") ? row.Field<int>("Pz_prod_") : 0;
            this.MouldTracksNumber = !row.IsNull("Nr_piece_") ? row.Field<int>("Nr_piece_") : 0;
            this.MachineCycleNumber = Convert.ToInt64(!row.IsNull("Cycle_") ? row.Field<decimal>("Cycle_") : 0);
            this.MachineIsRunning = mode == 3;
            this.IsGood = quality == 1;
            this.CycleTime = TimeSpan.FromSeconds(cycleTime);

        }

    }
}