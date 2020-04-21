using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Cobalt.Models;
using Cobalt.Models.Editor;

namespace Cobalt.Controllers
{
    public abstract class AbstractModelController<T> : Controller where T : DatabaseObject, new()
    {
        [HttpGet("Edit/{Id?}")]
        public IActionResult Edit(int? Id = null)
        {
            T Model = null;
            if(Id != null) {
                Model = new T();
                Model.Id = Id;
                var Models = Model.Get<T>(1);
                if(Models.Count() != 0)
                    Model = Models[0];
            }
            return View("Views/Edit/Edit.cshtml", new Tuple<Type, Object, List<EditField>>(
                typeof(T),
                Model,
                EditField.FieldFromModel<T>(Model)
            ));
        }
    }
}

