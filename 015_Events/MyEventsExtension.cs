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

    [ExtensionData("MyEventsExtension", "Put here an extension description.", "1.0",
        Author = "TeamSystem", EditorCompany = "TeamSystem")]
    public class MyEventsExtension : IMesExtension
    {
        private const string LOGGERSOURCE = @"MYEVENTSEXTENSION";

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

        private void Controller_BeforeProcessingEvent(object sender, ResourceDataUnitEventArgs e)
        {
            if (e.Unit is ProductDoneEvent doneEvent)
                this._Logger.WriteMessage(MessageLevel.Diagnostics,false, LOGGERSOURCE, $"{doneEvent}");
        }
    }
}
