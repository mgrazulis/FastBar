using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using Stripe;

namespace FastBar.Domain
{
    public class CCAccount
    {
        static public StripeCCAccount SaveStripeCustomer(StripeCustomerService stripeCustomerService, StripeCCAccount account)
        {
            StripeCustomer currentStripeCustomer = null;

            StripeCCAccount responseAccount = new StripeCCAccount()
            {
                FirstName = account.FirstName,
                LastName = account.LastName
            };

            try
            {

                StripeCustomer stripeCustomer = null;


                if (account.StripeId != null)
                {
                    try
                    {
                        currentStripeCustomer = stripeCustomerService.Get(account.StripeId);
                    }
                    catch
                    {
                        responseAccount.Success = false;
                        responseAccount.ErrorMessage = "Stripe failed to locate the account.";
                        return responseAccount;
                    }
                }


                //If the user was not found on Stripe then create it, if it was found then update it with the new Credit Card info.
                if (account.StripeId == null)
                {
                    var stripeCustomerCreate = new StripeCustomerCreateOptions();
                    stripeCustomerCreate.Email = account.Email;
                    stripeCustomerCreate.Description = account.FirstName + " " + account.LastName + " (" +
                                                       account.Email + ")";

                    // setting up the card
                    stripeCustomerCreate.SourceCard = new SourceCard()
                    {
                        Number = account.CCNumber,
                        ExpirationYear = account.ExpirationYear,
                        ExpirationMonth = account.ExpirationMonth,
                        Cvc = account.CVV

                    };
                    stripeCustomer = stripeCustomerService.Create(stripeCustomerCreate);
                    //account.StripeId = stripeCustomer.Id;
                }
                else if (account.StripeId != null && currentStripeCustomer != null)
                {
                    var stripeCustomerUpdate = new StripeCustomerUpdateOptions();
                    stripeCustomerUpdate.Email = account.Email;
                    stripeCustomerUpdate.Description = account.FirstName + " " + account.LastName + " (" +
                                                       account.Email + ")";

                    // setting up the card
                    stripeCustomerUpdate.SourceCard = new SourceCard()
                    {
                        Number = account.CCNumber,
                        ExpirationYear = account.ExpirationYear,
                        ExpirationMonth = account.ExpirationMonth
                    };

                    stripeCustomer = stripeCustomerService.Update(account.StripeId, stripeCustomerUpdate);
                }
                else if (account.StripeId != null && currentStripeCustomer == null)
                {
                    responseAccount.Success = false;
                    responseAccount.ErrorMessage = "Stripe failed to locate the account.";
                    return responseAccount;
                }

                if (stripeCustomer != null)
                {
                    responseAccount.Email = stripeCustomer.Email;
                    responseAccount.StripeId = stripeCustomer.Id;
                    responseAccount.Success = true;

                    return responseAccount;
                }

                responseAccount.Success = false;
                responseAccount.ErrorMessage = "Stripe failed to save the account.";

                return responseAccount;
            }
            catch (StripeException ex)
            {

                //Handle errors based on the Error Codes that Stripe supplies. The idea is not to bubble up third party error messages to our customers.
                string errorMessage;
                switch (ex.StripeError.Code)
                {
                    case "invalid_number":
                        errorMessage = "The Credit Card Number is invalid.";
                        break;
                    case "invalid_expiry_month":
                        errorMessage = "The Expiry Month is invalid.";
                        break;
                    case "invalid_expiry_year":
                        errorMessage = "The Expiry Year is invalid.";
                        break;
                    case "expired_card":
                        errorMessage = "The Credit Card is expired.";
                        break;
                    case "incorrect_number":
                        errorMessage = "The Credit Card number is incorrect.";
                        break;
                    case "incorrect_cvc":
                        errorMessage = "Incorect CVC.";
                        break;
                    case "incorrect_zip":
                        errorMessage = "Incorect Zip Code.";
                        break;
                    default:
                        errorMessage = "Error occured while saving the payment information.";
                        break;

                }

                responseAccount.Success = false;
                responseAccount.ErrorMessage = errorMessage;

                return responseAccount;
            }
        }
    }

    public class StripeCCAccount
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string StripeId { get; set; }
        public string CCNumber { get; set; }
        public string ExpirationYear { get; set; }
        public string ExpirationMonth { get; set; }
        public string CVV { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}
