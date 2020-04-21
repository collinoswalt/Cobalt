using System.Security.Claims;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Cobalt.Models.Auth;

namespace Cobalt.Controllers.Auth
{
   // [Authorize(Roles="Admin,Developer")]
    [Route("User")]
    public class UserController : AbstractModelController<User>
    {
        [HttpPost]
        public JsonResult CreateUser([FromBody] User NewUser) {
            NewUser = new User(NewUser.Username, NewUser.Password){
                UserRoles = NewUser.UserRoles
            };
            NewUser.Save();

            return Json(NewUser);
        }

        //[Authorize(Roles="*")]
        [HttpPost("Login")]
        public JsonResult LogUserIn([FromBody] User NewUser) {
            if(NewUser.Password == null || NewUser.Password == "")
                return Json(false);
            var Password = NewUser.Password;
            var Users = NewUser.Get<User>(1);
            if(Users.Count == 0) {
                return Json(false);
            }
            var LogMeIn = Users[0];
            if(LogMeIn.IsUser(Password)){
                HttpContext.Session.SetString("Role", "Yes");
                User.Claims.Append(new System.Security.Claims.Claim(ClaimTypes.Role, "User"));
                return Json(true);
            }
            return Json(false);
        }
    }
}