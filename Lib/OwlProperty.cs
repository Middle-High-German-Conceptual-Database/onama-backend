using System;
using System.Collections.Generic;
using System.Linq;
using VDS.RDF;
using VDS.RDF.Ontology;

namespace OnamaFrontendApi.Lib
{
  public class OwlProperty : OwlResource
  {
    public void SetFromOwlProperty(OntologyProperty owlProperty, INode description, string language)
    {
      this.Uri = ((UriNode)(owlProperty.Resource)).Uri;
      this.SetLabel(owlProperty);
      this.SetDescription(owlProperty, description);
      this.SetLabelWithLanguage(language);
      this.SetDescriptionWithLanguage(language);
    }
  }
}