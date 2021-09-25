using Microsoft.AspNetCore.Mvc.Rendering;
using PowerBIAdmin.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerBIAdmin.Models
{
    public class CreateUserModel
    {
        public List<SelectListItem> WorkspaceOptions { get; set; }
    }

    public class EditUserModel
    {
        public User User { get; set; }
        public List<SelectListItem> WorkspaceOptions { get; set; }
    }

}
