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
  public class StructureBuilder : QueryBase
  {
    protected IOwlService OwlService { get; set; }

    public StructureBuilder(IConfiguration configuration, ILanguageService languageService, IOwlService owlService) : base(configuration, languageService)
    {
      OwlService = owlService;
    }    
    public async Task<List<OwlClassHierarchy>> StructureFull(string language = null)
    {
      this.LanguageService.SetLanguage(language);
      var hierarchies = await OwlService.GetStructureInfo(this.LanguageService.GetLanguage());
      return hierarchies;
    }

    public async Task<List<OwlClassHierarchy>> StructureSubclasses(string className, string language = null)
    {
      this.LanguageService.SetLanguage(language);
      var hierarchies = await OwlService.GetSubclasses(className, this.LanguageService.GetLanguage());
      return hierarchies;
    }

    public async Task<List<KeyValuePair<string, int>>> Depths()
    {
      List<KeyValuePair<string, int>> ret = new List<KeyValuePair<string, int>>();
      var cd = await OwlService.GetClassDepths();
      if(cd != null) 
      {
        foreach(var c in cd)
        {
          ret.Add(new KeyValuePair<string, int> (c.Key, c.Value));
        }
      }
      return ret;
    }

    public async Task<List<object>> ClassEntriesGeneric(string className, string language = null)
    {
      this.LanguageService.SetLanguage(language);

      var entries = new List<object>();
      if(className == "Action")
      {
        entries = await ClassEntriesAction();
      } 
      else
      {
        var data = await GetClassEntries(className);
        entries = data.OrderBy(d => d.Value).Select(d => new {uri = d.Key, label = d.Value}).ToList<object>();
      }
      return entries;
    }

    public async Task<List<OwlPropertyHierarchy>> StructureProperties(string language = null)
    {
      this.LanguageService.SetLanguage(language);
      var hierarchies = await OwlService.GetStructurePropertiesInfo(this.LanguageService.GetLanguage());
      return hierarchies;
    }

    /*************** Helper Function **************************/


    // Actions must display the label of the assigned verb
    private async Task<List<object>> ClassEntriesAction()
    {
      var data = new Dictionary<string, string> ();
      var entries = new List<object>();

      string queryString = @"
      PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
      PREFIX onama: <http://onama.sbg.ac.at/ontology#> 
      select distinct ?uri ?label where { 
        ?action a onama:Action .
        ?action onama:hasVerb ?uri . 
        ?uri a onama:Verb .
        ?uri rdfs:label ?label . ";
      queryString += $" FILTER(lang(?label) = '{this.LanguageService.GetLanguage()}') . }} ";

      data = await GetSimpleResult(queryString);
      entries = data.OrderBy(d => d.Value).Select(d => new {uri = d.Key, label = d.Value}).ToList<object>();
      return entries;
    }
    //
    // Builds the query and returns the dictionary for simple class uri -> label listings
    public async Task<Dictionary<string, string>> GetClassEntries(string className)
    {
      string queryString = @"
      PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
      PREFIX onama: <http://onama.sbg.ac.at/ontology#> 
      select ?uri ?label where { ";
      queryString += $" ?uri a onama:{className} . ?uri rdfs:label ?label .  FILTER(lang(?label) = '{this.LanguageService.GetLanguage()}') . }}";

      return await GetSimpleResult(queryString);
    }


    //
    // Returns a dictionary with class URIs as keys and labels as values
    // The queryString must be built so that the results contain these nodes 
    // and the URIs are distinct
    private async Task<Dictionary<string, string>> GetSimpleResult(string queryString)
    {
      ConnectGraphDb();
      var data = new Dictionary<string, string>();
      var results = endpoint.QueryWithResultSet(queryString);
      if(results is SparqlResultSet)
      {
        foreach(var result in results)
        {
          if(result.HasBoundValue("uri") && result.HasBoundValue("label")) 
          {
            data.Add(result["uri"].ToString(), result["label"].ToString());
          }
        }
      }
      return data;
    }

  }
}