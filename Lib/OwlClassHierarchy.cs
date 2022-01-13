using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Ontology;

namespace OnamaFrontendApi.Lib
{
  public class OwlClassHierarchy : OwlResourceHierarchy
  {
    public List<OwlClassHierarchy> OwlHierarchy { get; set; } = new List<OwlClassHierarchy>();
    public List<OwlProperty> OwlProperties { get; set; } = new List<OwlProperty>();

    public void SetFromOwlClass(OntologyClass owlClass, INode descriptionNode, INode deprecatedNode, string language)
    {
      this.SetFromOwlResource(owlClass, descriptionNode, language);
      this.SetDomainProperties(owlClass, descriptionNode, deprecatedNode, language);
    }

    public void SetDomainProperties(OntologyClass owlClass, INode descriptionNode, INode deprecatedNode, string language) 
    {
      // Properties
      var domainProps = owlClass.IsDomainOf;
      foreach(var prop in domainProps) 
      {
        if(!OntologyExtensions.IsDeprecated(prop, deprecatedNode)) {
            var p = new OwlProperty();
            p.SetFromOwlProperty(prop, descriptionNode, language);
            this.OwlProperties.Add(p);
        }
      }
    }
    public override async Task<object> GetHierarchy()
    {
      return this.OwlHierarchy;
    }
  }
}