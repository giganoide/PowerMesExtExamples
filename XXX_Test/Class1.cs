using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atys.PowerMES.Events;
using Atys.PowerMES.Foundation;
using Atys.PowerMES.Support;
using Moq;
using NUnit.Framework;
using TeamSystem.Customizations;

namespace XXX_Test
{
    [TestFixture]
    public class Class1
    {
        [Test]
        public void Events()
        {
            var ext = new MyEventsExtension();
            var controller = new Mock<IResourcesController>();
            var logger = new Mock<IMesAppLogger>();
            var mesManager = new Mock<IMesManager>();
            mesManager.Setup(x => x.Controller).Returns(controller.Object);
            mesManager.Setup(x => x.ApplicationMainLogger).Returns(logger.Object);
            ext.Initialize(mesManager.Object);
            ext.Run();
            var productDone = Creator.CreateProductDoneEvent();
            mesManager.Raise(x => x.Controller.BeforeProcessingEvent += null, new ResourceDataUnitEventArgs(new Mock<IMesResource>().Object, productDone));
            logger.Verify(x => x.WriteMessage(MessageLevel.Diagnostics, false, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            ext.Shutdown();
        }

        public static class Creator
        {
            public static ProductDoneEvent CreateProductDoneEvent()
            {
                return new ProductDoneEvent("Resource", DateTime.UtcNow, new ArticleItem("Article", "Phase"), 1, 0, 0,
                    null, 1);
            }
        }
    }
}
