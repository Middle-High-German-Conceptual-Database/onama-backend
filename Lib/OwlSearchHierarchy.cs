using System;
using System.Collections.Generic;
using System.Linq;

namespace OnamaFrontendApi.Lib
{
  public class OwlSearchHierarchy : OwlResource
  {
    public List<OwlSearchHierarchy> OwlHierarchy { get; set; } // = new List<OwlSearchHierarchy>();
    public List<OwlSearchProperty> OwlProperties { get; set; } // = new List<OwlSearchProperty>();

    public string ClassSearchTerm { get; set; }
    public bool ClassSearchTermVisible { get; set; }
  }
}