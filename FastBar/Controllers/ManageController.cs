using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using FastBar.Domain;
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

            //Get Users FullName from UserManager
            EditProfileViewModel editProfileViewModel = new EditProfileViewModel();
            editProfileViewModel.FirstName = currentUser.FirstName;
            editProfileViewModel.LastName = currentUser.LastName;

            return View(editProfileViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProfile(EditProfileViewModel editProfileViewModel)
        {
            var manager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext()));

            //Get Users FullName and StripeId from UserManager
            var currentUser = manager.FindById(User.Identity.GetUserId());
            currentUser.FirstName = editProfileViewModel.FirstName;
            currentUser.LastName = editProfileViewModel.LastName;


            StripeCCAccount stripeAccount = new StripeCCAccount()
            {
                FirstName = editProfileViewModel.FirstName,
                LastName = editProfileViewModel.LastName,
                Email = currentUser.Email,
                StripeId = currentUser.StripeId,
                CCNumber = editProfileViewModel.CCNumber,
                ExpirationMonth = editProfileViewModel.ExpirationMonth,
                ExpirationYear = editProfileViewModel.ExpirationYear,
                CVV = editProfileViewModel.CVV
            };

            var customerService = new StripeCustomerService();

            StripeCCAccount responseStripeAccount = CCAccount.SaveStripeCustomer(customerService, stripeAccount);

            currentUser.StripeId = responseStripeAccount.StripeId;

            manager.Update(currentUser);

            //Clearing the ModelState to remove all the customer sencitive info ASAP.
            ModelState.Clear();

            editProfileViewModel.CCNumber = null;
            editProfileViewModel.CVV = null;
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