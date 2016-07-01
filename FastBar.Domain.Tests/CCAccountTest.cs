using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Stripe;

namespace FastBar.Domain.Tests
{
    [TestClass]
    public class CCAccountTest
    {
        [TestMethod]
        public void TestSaveStripeCustomer_UpdateStripeAccount()
        {
            //Arrange
            StripeCCAccount stripeAccount = new StripeCCAccount()
            {
                FirstName = "FirstName",
                LastName = "LastName",
                Email = "email1234@mailinator.com",
                StripeId = "strp_asdf!@#$",
                CCNumber = "4242424242424242",
                ExpirationMonth = "11",
                ExpirationYear = "2020",
                CVV = "123"
            };


            var customerService = new Moq.Mock<StripeCustomerService>("test");

            customerService.Setup(m => m.Get(It.IsAny<string>(), It.IsAny<StripeRequestOptions>())).Returns(new StripeCustomer()
            {
                Id = "UpdateStripeAccountId"
            });

            customerService.Setup(m => m.Update(It.IsAny<string>(), It.IsAny<StripeCustomerUpdateOptions>(), It.IsAny<StripeRequestOptions>())).Returns(new StripeCustomer()
            {
                //Email = "stripeUpdateCustomer@mailinator.com",
                Id = "StripeUpdateId"
            });

            //Act
            StripeCCAccount responseStripeAccount = CCAccount.SaveStripeCustomer(customerService.Object, stripeAccount);


            //Assert
            Assert.AreEqual("StripeUpdateId", responseStripeAccount.StripeId, "Account was not created correctly.");
        }

        [TestMethod]
        public void TestSaveStripeCustomer_UpdateStripeAccount_FailedToLocate()
        {
            //Arrange
            StripeCCAccount stripeAccount = new StripeCCAccount()
            {
                FirstName = "FirstName",
                LastName = "LastName",
                Email = "email1234@mailinator.com",
                StripeId = "strp_asdf!@#$",
                CCNumber = "4242424242424242",
                ExpirationMonth = "11",
                ExpirationYear = "2020",
                CVV = "123"
            };


            var customerService = new Moq.Mock<StripeCustomerService>("test");

            customerService.Setup(m => m.Get(It.IsAny<string>(), It.IsAny<StripeRequestOptions>()))
                .Throws<StripeException>();

            customerService.Setup(m => m.Update(It.IsAny<string>(), It.IsAny<StripeCustomerUpdateOptions>(), It.IsAny<StripeRequestOptions>())).Returns(new StripeCustomer()
            {
                //Email = "stripeUpdateCustomer@mailinator.com",
                Id = "StripeUpdateId"
            });

            //Act
            StripeCCAccount responseStripeAccount = CCAccount.SaveStripeCustomer(customerService.Object, stripeAccount);


            //Assert
            Assert.AreEqual("Stripe failed to locate the account.", responseStripeAccount.ErrorMessage, "Error message does not match.");
        }

        [TestMethod]
        public void TestSaveStripeCustomer_CreateStripeAccount()
        {
            //Arrange
            StripeCCAccount stripeAccount = new StripeCCAccount()
            {
                FirstName = "FirstName",
                LastName = "LastName",
                Email = "email1234@mailinator.com",
                StripeId = "strp_asdf!@#$",
                CCNumber = "4242424242424242",
                ExpirationMonth = "11",
                ExpirationYear = "2020",
                CVV = "123"
            };


            var customerService = new Moq.Mock<StripeCustomerService>("test");

            //customerService.Setup(m => m.Get(It.IsAny<string>(), It.IsAny<StripeRequestOptions>()))
            //    .Throws<StripeException>();



            customerService.Setup(m => m.Create(It.IsAny<StripeCustomerCreateOptions>(), It.IsAny<StripeRequestOptions>())).Returns(new StripeCustomer()
            {
                //Email = "stripeCreateCustomer@mailinator.com",
                Id = "StripeCreateId"

            });

            //Act
            StripeCCAccount responseStripeAccount = CCAccount.SaveStripeCustomer(customerService.Object, stripeAccount);

            //Assert
            Assert.AreEqual("StripeCreateId", responseStripeAccount.StripeId, "Account was not created correctly.");
        }

        [TestMethod]
        public void TestSaveStripeCustomer_CreateStripeAccount_InvalidNumber()
        {
            //Arrange
            StripeCCAccount stripeAccount = new StripeCCAccount()
            {
                FirstName = "FirstName",
                LastName = "LastName",
                Email = "email1234@mailinator.com",
                StripeId = "strp_asdf!@#$",
                CCNumber = "4242424242424242",
                ExpirationMonth = "11",
                ExpirationYear = "2020",
                CVV = "123"
            };


            var customerService = new Moq.Mock<StripeCustomerService>("test");

            customerService.Setup(m => m.Get(It.IsAny<string>(), It.IsAny<StripeRequestOptions>()))
                .Throws<StripeException>();

            customerService.Setup(
                m => m.Create(It.IsAny<StripeCustomerCreateOptions>(), It.IsAny<StripeRequestOptions>()))
                .Throws(new StripeException()
                {
                    StripeError = new StripeError()
                    {
                        Code = "invalid_number"
                    }
                });

            //Act
            StripeCCAccount responseStripeAccount = CCAccount.SaveStripeCustomer(customerService.Object, stripeAccount);

            //Assert
            Assert.AreEqual(false, responseStripeAccount.Success, "Account creation did not fail as was expected.");
            Assert.AreEqual("The Credit Card Number is invalid.", responseStripeAccount.ErrorMessage, "Error message is wrong");
        }
    }
}
