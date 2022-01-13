using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Ontology;
using VDS.RDF.Nodes;
using VDS.RDF.Query;
using VDS.RDF.Storage;
using VDS.RDF.Writing;
using Microsoft.Extensions.Configuration;


namespace OnamaFrontendApi.Lib
{
  // A Singleton Service that generates/stores/caches/provides relevant structural information
  public class OwlService : IOwlService
  {
    private const string OWL_FILE = "data/onama.owl";
    private const string DC_DESCRIPTION_URI = "http://purl.org/dc/elements/1.1/description";
    private const string OWL_DEPRECATED_URI ="http://www.w3.org/2002/07/owl#deprecated";
    private const string OWL_THING_URI ="http://www.w3.org/2002/07/owl#Thing";
    private OntologyGraph owl { get; set; }
    private INode descriptionNode { get; set; }
    private INode deprecatedNode { get; set; }
    private string OwlFile { get; set; } 

    private Dictionary<string, List<OwlClassHierarchy>> ClassHierarchyWithLanguage { get; set; }
    private Dictionary<string, List<OwlPropertyHierarchy>> PropertyHierarchyWithLanguage { get; set; }
    // we cannot use Uris as "The Fragment property is not considered in any Equals comparison." 
    // https://docs.microsoft.com/en-us/dotnet/api/system.uri.fragment?view=netcore-3.1
    private Dictionary<string, int> ClassDepth { get; set; }
    private Dictionary<string, int> PropertyDepth { get; set; }

    public OwlService(IConfiguration configuration) 
    {
      OwlFile = OWL_FILE;
      if(configuration["Onama:OwlFile"] != null) 
      {
        this.OwlFile = configuration["Onama:OwlFile"];
      }
      ClassHierarchyWithLanguage = new Dictionary<string, List<OwlClassHierarchy>>();
      PropertyHierarchyWithLanguage = new Dictionary<string, List<OwlPropertyHierarchy>>();
      ClassDepth = new Dictionary<string, int>();
      PropertyDepth = new Dictionary<string, int>();
    }    

    private async Task LoadOntologyData()
    {
      owl = new OntologyGraph();
      FileLoader.Load(owl, OwlFile);
      descriptionNode = owl.GetUriNode(new Uri(DC_DESCRIPTION_URI));
      deprecatedNode = owl.GetUriNode(new Uri(OWL_DEPRECATED_URI));
    }

    private async Task LoadLanguageData(string language)
    {
      if(!ClassHierarchyWithLanguage.ContainsKey(language))
      {
        List<OwlClassHierarchy> langHierarchies = await ReadStructure(language);
        ClassHierarchyWithLanguage.Add(language, langHierarchies);
        await this.ReadClassDepths();
      }
      if(!PropertyHierarchyWithLanguage.ContainsKey(language))
      {
        List<OwlPropertyHierarchy> langHierarchies = await ReadProperties(language);
        PropertyHierarchyWithLanguage.Add(language, langHierarchies);
        await this.ReadPropertyDepths();
      }
    }

    //
    // Get the structural info of the OWL file with the requested language in the "real" properties.
    // The result is created if necessary and stored for further use.
    public async Task<List<OwlClassHierarchy>> GetStructureInfo(string language)
    {
      await LoadLanguageData(language);
      return ClassHierarchyWithLanguage[language];
    }
    public async Task<List<OwlPropertyHierarchy>> GetStructurePropertiesInfo(string language) 
    {
      await LoadLanguageData(language);
      return PropertyHierarchyWithLanguage[language];
    }

    public async Task<List<OwlClassHierarchy>> GetSubclasses(string className, string language)
    {
      if(owl == null)
      {
        await LoadOntologyData();
      }
      List<OwlClassHierarchy> hierarchies = new List<OwlClassHierarchy>();
            
      int depth = 0;

      var searchClassName = UriConstants.NS_ONAMA_ONTOLOGY + className;
      var owlClass = owl.OwlClasses.Where(o => ((UriNode)o.Resource).Uri.ToString() == searchClassName).FirstOrDefault();
      if(owlClass != null && !OntologyExtensions.IsDeprecated(owlClass, deprecatedNode))
      {
        var ch = new OwlClassHierarchy();
        ch.SetFromOwlClass(owlClass, descriptionNode, deprecatedNode, language);
        await ch.SetDepth(depth);

        getSubClasses(owlClass, ch, language, depth);

        hierarchies.Add(ch);
      }
      return hierarchies;
    }
    public async Task<Dictionary<string, int>> GetClassDepths()
    {
      return ClassDepth;
    }
    public async Task<Dictionary<string, int>> GetPropertyDepths()
    {
      return PropertyDepth;
    }
    /*************** Helper Function **************************/

    //
    // read the structure of an OWL file and return a hierarchical list with all languages for labels and descriptions
    // but the requested language in the "real" properties.
    private async Task<List<OwlClassHierarchy>> ReadStructure(string language)
    {
      if(owl == null)
      {
        await LoadOntologyData();
      }
      List<OwlClassHierarchy> hierarchies = new List<OwlClassHierarchy>();
      int depth = 0;

      foreach (var owlClass in owl.OwlClasses.Where(o => o.IsTopClass || !o.SuperClasses.Any(c => ((UriNode)c.Resource).Uri.ToString().StartsWith(UriConstants.NS_ONAMA_ONTOLOGY))))
      {
        if (owlClass is OntologyClass && owlClass.Resource is UriNode)
        {
          if(!OntologyExtensions.IsDeprecated(owlClass, deprecatedNode))
          {
            var ch = new OwlClassHierarchy();
            ch.SetFromOwlClass(owlClass, descriptionNode, deprecatedNode, language);
            ch.SetDepth(depth);

            getSubClasses(owlClass, ch, language, depth);

            hierarchies.Add(ch);
          } 
        }
      }
      return hierarchies;
    }
    //
    // recursively get subclasses for a given class and add them to the hierarchy
    private void getSubClasses(OntologyClass owlClass, OwlClassHierarchy hierarchy, string language, int depth) 
    {
      depth++;
      foreach (var subClass in owlClass.DirectSubClasses)
      {
        if (subClass is OntologyClass && subClass.Resource is UriNode)
        {
          if(!OntologyExtensions.IsDeprecated(subClass, deprecatedNode))
          {
            OwlClassHierarchy ch = new OwlClassHierarchy();
            ch.SetFromOwlClass(subClass, descriptionNode, deprecatedNode, language);
            ch.SetDepth(depth);
            getSubClasses(subClass, ch, language, depth);
            hierarchy.OwlHierarchy.Add(ch);
          } 
        }
      }
      depth--;
    }
    private async Task<List<OwlPropertyHierarchy>> ReadProperties(string language)
    {
      if(owl == null)
      {
        await LoadOntologyData();
      }
      List<OwlPropertyHierarchy> hierarchies = new List<OwlPropertyHierarchy>();
      int depth = 0;

      // Data Properties
      foreach(var dataProperty in owl.OwlDatatypeProperties.Where(o => ((UriNode)o.Resource).Uri.ToString().StartsWith(UriConstants.NS_ONAMA_ONTOLOGY)))
      {
        if(!dataProperty.DirectSuperProperties.Any(p => ((UriNode)p.Resource).Uri.ToString().StartsWith(UriConstants.NS_ONAMA_ONTOLOGY)))
        {
          var topDataHierarchy = new OwlPropertyHierarchy();
          topDataHierarchy.SetFromOwlResource(dataProperty, descriptionNode, language);
          getSubProperties(dataProperty, topDataHierarchy, language, depth);
          topDataHierarchy.SetDepth(depth);
          hierarchies.Add(topDataHierarchy);
        }
      }
      // Object Properties
      foreach(var objectProperty in owl.OwlObjectProperties.Where(o => ((UriNode)o.Resource).Uri.ToString().StartsWith(UriConstants.NS_ONAMA_ONTOLOGY)))
      {
        if(!objectProperty.DirectSuperProperties.Any(p => ((UriNode)p.Resource).Uri.ToString().StartsWith(UriConstants.NS_ONAMA_ONTOLOGY)))
        {
          //Console.WriteLine($"Top Property {((UriNode)op.Resource).Uri} ({op.IsTopProperty})");
          var topObjectHierarchy = new OwlPropertyHierarchy();
          topObjectHierarchy.SetFromOwlResource(objectProperty, descriptionNode, language);
          getSubProperties(objectProperty, topObjectHierarchy, language, depth);
          topObjectHierarchy.SetDepth(depth);
          hierarchies.Add(topObjectHierarchy);
        }
      }

      return hierarchies;
    }
    private void getSubProperties(OntologyProperty owlProperty, OwlPropertyHierarchy hierarchy, string language, int depth) 
    {
      depth++;
      foreach (var subProperty in owlProperty.DirectSubProperties)
      {
        if (subProperty is OntologyProperty && subProperty.Resource is UriNode)
        {
          if(!OntologyExtensions.IsDeprecated(subProperty, deprecatedNode))
          {
            OwlPropertyHierarchy ch = new OwlPropertyHierarchy();
            ch.SetFromOwlResource(subProperty, descriptionNode, language);
            ch.SetDepth(depth);
            getSubProperties(subProperty, ch, language, depth);
            hierarchy.OwlHierarchy.Add(ch);
          } 
        }
      }
      depth--;
    }
    private async Task ReadClassDepths()
    {
      if(ClassDepth == null || ClassDepth.Count == 0) 
      {
        foreach(var hierarchy in ClassHierarchyWithLanguage.First().Value)
        {
          await hierarchy.ReadHierarchyDepths(ClassDepth);
        }
      }
    }
    private async Task ReadPropertyDepths()
    {
      if(PropertyDepth == null || PropertyDepth.Count == 0) 
      {
        foreach(var hierarchy in PropertyHierarchyWithLanguage.First().Value)
        {
          await hierarchy.ReadHierarchyDepths(PropertyDepth);
        }
      }
    }
  }
}