using System;
using System.Collections.Generic;
using System.Linq;
using VDS.RDF;
using VDS.RDF.Ontology;

namespace OnamaFrontendApi.Lib
{
  public abstract class OwlResource : RdfResource
  {
    public string Description { get; set; }
    // this dictionary holds data in the form of language -> label
    public Dictionary<string, string> Descriptions { get; set; } = new Dictionary<string, string>();

    public void SetLabel(OntologyResource resource)
    {
      string labelString = resource.ToString() ?? "";
      if(labelString.Substring(labelString.Length - 3, 1) == "@") {
        labelString = labelString.Substring(0, labelString.Length - 3);
      }

      foreach(var language in resource.Label.Select(l => l.Language))
      {
        if(!Labels.ContainsKey(language) && resource.Label.Where(l => l.Language == language).Count() > 0)
        {
          Labels.Add(language, resource.Label.Where(l => l.Language == language).First().Value);
        }        
      }
      if(Labels.Count > 0)
      {
        labelString = Labels.First().Value;
      }
      this.Label = labelString;
    }

    public void SetLabelWithLanguage(string language)
    {
      if(Labels.ContainsKey(language))
      {
        Label = Labels[language];
      }
    }
    public void SetDescription(OntologyResource owlClass, INode description)
    {
      var descString = "";
      var descs = owlClass.Triples.WithPredicate(description)
        .Where(d => d.Object != null && d.Object is ILiteralNode).Select(d => (ILiteralNode)d.Object);

      if(descs.Count() > 0) 
      {
        descString = descs.First().Value;
      }

      foreach(var desc in descs)
      {
        if(desc.Language != null && !Descriptions.ContainsKey(desc.Language) && desc.Value != null)
        {
          Descriptions.Add(desc.Language, desc.Value);
        }
      }
      if(Descriptions.Count > 0)
      {
        descString = Descriptions.First().Value;
      }
      this.Description = descString;
    }
    public void SetDescriptionWithLanguage(string language)
    {
      if(Descriptions.ContainsKey(language))
      {
        Description = Descriptions[language];
      }
    }

  }    
}