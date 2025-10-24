using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Scv.Models.Civil.AppearanceDetail
{
    /// <summary>
    /// Extends CivilAppearanceMethod. 
    /// </summary>
    public class CivilAppearanceMethod : JCCommon.Clients.FileServices.CivilAppearanceMethod
    {
        public string RoleTypeDesc { get; set; }
        public string AppearanceMethodDesc { get; set; }
    }
}
