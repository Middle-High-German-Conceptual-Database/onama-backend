using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Ontology;
using System.Text.Json.Serialization;

namespace OnamaFrontendApi.Lib
{
  public abstract class OwlResourceHierarchy : OwlResource, IHierarchical
  {
    public int Depth { get; set; } = -1;
    public void SetFromOwlResource(OntologyResource owlResource, INode descriptionNode, string language)
    {
      this.Uri = ((UriNode)(owlResource.Resource)).Uri;
      this.SetLabel(owlResource);
      this.SetDescription(owlResource, descriptionNode);
      this.SetLabelWithLanguage(language);
      this.SetDescriptionWithLanguage(language);
    }
    public abstract Task<object> GetHierarchy();
    public async Task<int> GetDepth()
    {
      return Depth;
    }
    public async Task SetDepth(int depth) 
    {
      this.Depth = depth;
    }
    public async Task<Uri> GetUri()
    {
      return this.Uri;
    }
    public async Task ReadHierarchyDepths(Dictionary<string, int> DepthDict)
    {
      if(!DepthDict.Any(d => d.Key == this.Uri.ToString()))
      {
        DepthDict.Add(this.Uri.ToString(), this.Depth);
      }
      IEnumerable<object> subHierarchy = ((IEnumerable<object>)await this.GetHierarchy());
      foreach(var h in subHierarchy)
      {
        if(h is OwlResourceHierarchy)
        {
          await ((OwlResourceHierarchy)h).ReadHierarchyDepths(DepthDict);
        }
      }
    }
  }
}