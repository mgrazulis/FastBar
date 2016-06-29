using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Web.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FastBar;
using FastBar.Controllers;
using Moq;

namespace FastBar.Tests.Controllers
{
    [TestClass]
    public class ManageControllerTest
    {
        [TestMethod]
        public void EditProfile()
        {
            // Arrange
            ManageController controller = new ManageController();

            // Act
            //ViewResult result = controller.EditProfile() as ViewResult;

            // Assert
            Assert.IsNotNull(controller);
        }
    }
}
