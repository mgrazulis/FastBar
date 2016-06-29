using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using FastBar.Models;
using Microsoft.AspNet.Identity.EntityFramework;
using Stripe;

namespace FastBar.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public ManageController()
        {
        }

        public ManageController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set 
            { 
                _signInManager = value; 
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        public ActionResult EditProfile()
        {
            var currentUserId = User.Identity.GetUserId();

            var manager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext()));

            var currentUser = manager.FindById(User.Identity.GetUserId());

            EditProfileViewModel editProfileViewModel = new EditProfileViewModel();
            editProfileViewModel.FirstName = currentUser.FirstName;
            editProfileViewModel.LastName = currentUser.LastName;

            return View(editProfileViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProfile(EditProfileViewModel editProfileViewModel)
        {


            var currentUserId = User.Identity.GetUserId();

            var manager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext()));

            var currentUser = manager.FindById(User.Identity.GetUserId());

            currentUser.FirstName = editProfileViewModel.FirstName;
            currentUser.LastName = editProfileViewModel.LastName;
            

            var customerService = new StripeCustomerService();

            StripeCustomer currentStripeCustomer;

            try
            {
                currentStripeCustomer = customerService.Get(currentUser.StripeId);
            }
            catch
            {
                currentStripeCustomer = null;
            }

            try
            {

                StripeCustomer stripeCustomer;

                if (currentStripeCustomer == null)
                {
                    var stripeCustomerCreate = new StripeCustomerCreateOptions();
                    stripeCustomerCreate.Email = currentUser.Email;
                    stripeCustomerCreate.Description = currentUser.FirstName + " " + currentUser.LastName + " (" +
                                                       currentUser.Email + ")";

                    // setting up the card
                    stripeCustomerCreate.SourceCard = new SourceCard()
                    {
                        Number = editProfileViewModel.CCNumber,
                        ExpirationYear = editProfileViewModel.ExpirationYear,
                        ExpirationMonth = editProfileViewModel.ExpirationMonth,
                        Cvc = editProfileViewModel.CVC
                        
                    };
                    stripeCustomer = customerService.Create(stripeCustomerCreate);
                    currentUser.StripeId = stripeCustomer.Id;
                }
                else
                {
                    var stripeCustomerUpdate = new StripeCustomerUpdateOptions();
                    stripeCustomerUpdate.Email = currentUser.Email;
                    stripeCustomerUpdate.Description = currentUser.FirstName + " " + currentUser.LastName + " (" +
                                                       currentUser.Email + ")";

                    // setting up the card
                    stripeCustomerUpdate.SourceCard = new SourceCard()
                    {
                        Number = editProfileViewModel.CCNumber,
                        ExpirationYear = editProfileViewModel.ExpirationYear,
                        ExpirationMonth = editProfileViewModel.ExpirationMonth
                    };

                    stripeCustomer = customerService.Update(currentUser.StripeId, stripeCustomerUpdate);
                }
                manager.Update(currentUser);

                editProfileViewModel.Success = true;
            }
            catch (StripeException ex)
            {
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
                //editProfileViewModel.Error = errorMessage;
                ModelState.AddModelError(string.Empty, errorMessage);
                editProfileViewModel.Success = false;
            }

            ModelState.Clear();

            editProfileViewModel.CCNumber = "";
            editProfileViewModel.CVC = null;
            editProfileViewModel.ExpirationMonth = null;
            editProfileViewModel.ExpirationYear = null;

            return View(editProfileViewModel);
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"), User.Identity.GetUserId());
        }

        //
        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            }
            var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            return result.Succeeded ? RedirectToAction("ManageLogins") : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

#region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private bool HasPhoneNumber()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PhoneNumber != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

#endregion
    }
}