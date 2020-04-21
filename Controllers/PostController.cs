using System;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Cobalt.Models.Posts;

namespace Cobalt.Controllers
{
    [Route("Post/")]
    //[Authorize(Roles="Editor,Admin,Developer")]
    public class PostController : AbstractModelController<Post>
    {
        [HttpPost]
        public JsonResult CreatePost([FromBody] Post P) {
            P.Id = P.Save();
            return Json(P);
        }

        [HttpGet("{Id}")]
        public JsonResult GetPost(int Id) {
            return Json(Post.Get(Id));
        }

        [HttpPut]
        public JsonResult UpdatePost([FromBody] Post P) {
            P.Update();
            return Json(P);
        }

        [HttpDelete("{Id}")]
        public JsonResult DeletePost(int Id) {
            (new Post(){ Id = Id }).Delete();
            return Json(true);
        }
    }
}