using System;
using System.Diagnostics;
using Atys.PowerMES.Extensibility;
using Atys.PowerMES.Foundation;
using Atys.PowerMES.Services;
using Atys.PowerMES.Support;

namespace TeamSystem.Customizations
{
    [ExtensionData("MyScheduledActivity", "Put here an extension description.", "1.0",
        Author = "TeamSystem", EditorCompany = "TeamSystem")]
    public class MyScheduledActivity : IMesExtension
    {
        private const string LOGGERSOURCE = @"MYSCHEDULEDACTIVITY";

        #region fields

        private readonly Guid _ComponentId = Guid.NewGuid(); //id specifico del componente (per scheduler)
        private Guid _ActivityId = Guid.Empty; //id task per job schedulatore
        private IMesManager _MesManager = null; //riferimento a PowerMES
        private IMesAppLogger _MesLogger = null;

        #endregion

        #region IMesExtension members

        /// <inheritdoc />
        public void Initialize(IMesManager mesManager)
        {
#if DEBUG
            Debugger.Launch();
#endif
            //memorizzo il riferimento all'oggetto principale di PowerMES
            _MesManager = mesManager;

            //strumenti per messaggi di DIAGNOSTICA:
            _MesLogger = _MesManager.ApplicationMainLogger;
            _MesLogger.WriteMessage(MessageLevel.Info, true, LOGGERSOURCE, "Estensione creata!");
        }

        /// <inheritdoc />
        public void Run()
        {
            SetupActivity();
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            ClearActivity();
        }

        #endregion

        #region export measurement sets recurring activity

        private void SetupActivity()
        {
            _MesManager.ApplicationMainLogger.WriteMessage(MessageLevel.Diagnostics, false, LOGGERSOURCE,
                "SetupActivity(): called");

            //prendo un riferimento al servizio di schedulazione
            var scheduler = _MesManager.ServiceManager.GetService<IJobSchedulerService>();

            //una volta al giorno, di notte
            var interval = new TimeSpan(1, 0, 0, 0);

            
            var offsetNow = DateTimeOffset.Now;
            var firstTrigger = offsetNow.AddMinutes(5);

            //creo l'oggetto per lo schedulatore
            var recurringActivity = new RecurringActivity(Guid.NewGuid(),
                _ComponentId,
                firstTrigger,
                TaskTriggerAction,
                TaskErrorAction,
                interval);
            //creo il job nello schedulatore
            if (scheduler.SubmitRecurringActivity(recurringActivity))
            {
                _ActivityId = recurringActivity.ActivityId;
                _MesManager.ApplicationMainLogger.WriteMessage(MessageLevel.Diagnostics, false, LOGGERSOURCE,
                    "SetupActivity(): activity submitted -> "
                    + "'{0}' starting from: {1} interval: {2}",
                    recurringActivity.ActivityId.ToString(),
                    firstTrigger.ToString(), interval.ToString());
            }
            else
            {
                _MesManager.ApplicationMainLogger.WriteMessage(MessageLevel.Diagnostics, false, LOGGERSOURCE,
                    "SetupActivity(): failed to submit activity.");
            }
        }

        private void ClearActivity()
        {
            _MesManager.ApplicationMainLogger.WriteMessage(MessageLevel.Diagnostics, false, LOGGERSOURCE,
                "ClearActivity(): called");

            if (_ActivityId == Guid.Empty)
                return;

            //prendo un riferimento al servizio di schedulazione
            var scheduler = _MesManager.ServiceManager.GetService<IJobSchedulerService>();
            Debug.Assert(scheduler != null);

            if (scheduler.HasActivityById(_ActivityId))
                scheduler.CancelActivity(_ActivityId);

            //tolgo il riferimento a task
            _ActivityId = Guid.Empty;
        }

        private void TaskTriggerAction(Guid activityId)
        {
            _MesLogger.WriteMessage(MessageLevel.Diagnostics, false, LOGGERSOURCE,
                "MeasSetsExportTaskTriggerAction(): called");

            /*
             *
             * DO THE JOB
             *
             */
        }

        private void TaskErrorAction(Guid activityId, Exception ex)
        {
            _MesLogger.WriteException(ex, LOGGERSOURCE, "Recurring activity error");
        }

        #endregion
    }
}