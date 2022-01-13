using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Ontology;
using System.Text.Json.Serialization;


namespace OnamaFrontendApi.Lib
{
  public class OwlPropertyHierarchy : OwlResourceHierarchy
  {
    [JsonPropertyName("Hierarchy")]
    public List<OwlPropertyHierarchy> OwlHierarchy { get; set; } = new List<OwlPropertyHierarchy>();

    public override async Task<object> GetHierarchy()
    {
      return this.OwlHierarchy;
    }
  }
}