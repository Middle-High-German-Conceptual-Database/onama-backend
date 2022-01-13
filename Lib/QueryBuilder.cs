using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Storage;
using VDS.RDF.Writing;
using VDS.RDF.Nodes;
using OnamaFrontendApi.Lib;
using Microsoft.Extensions.Configuration;

namespace OnamaFrontendApi.Lib
{
  public class QueryBuilder : QueryBase
  {
    protected const string SEARCHTERM_TYPE_CLASS = "class";
    protected const string SEARCHTERM_TYPE_PROPERTY = "property";
    protected const string CACHE_ID_PERSISTENTITEMS_ENTRIES = "PERSISTENTITEMS_ENTRIES";
    protected const int QUERY_LUCKY_COUNT = 5;
    protected ICacheService Cache { get; set; }
    protected IOwlService OwlService { get; set; }
    protected QueryResponse response { get; set; }
    protected Dictionary<Uri, DetailNode> nodes { get; set; }

    public QueryBuilder(
      IConfiguration configuration, 
      ILanguageService languageService,
      ICacheService cacheService,
      IOwlService owlService) : base(configuration, languageService)
    {
      this.OwlService = owlService;
      this.Cache = cacheService;
    }

    public async Task<QueryResponse> RegisterNarrative(JsonQueryDataNarrative queryData)
    {
      ConnectGraphDb();
      if(queryData == null || queryData.IsEmpty()) 
      {
        queryData =  new JsonQueryDataNarrative() {
          types = new List<Uri>() {
            new Uri(UriConstants.NS_ONAMA_ONTOLOGY + "Concept"), 
            new Uri(UriConstants.NS_ONAMA_ONTOLOGY + "Realisation"), 
            new Uri(UriConstants.NS_ONAMA_ONTOLOGY + "TextualRealisation"), 
            new Uri(UriConstants.NS_ONAMA_ONTOLOGY + "VisualRealisation")
          }, 
          classTerms = new List<JsonQueryDataUriList>(), 
          entities = new List<JsonQueryDataNarrativeEntity>(), 
          language = queryData != null ? queryData.language : null
        };
        
      }
      response = new QueryResponse();
      this.LanguageService.SetLanguage(queryData.language);

      // class / subclasses and label
      var classQueryStrings = new List<string>();
      var typeQueryTerms = new List<QueryTerm> {new QueryTerm {
        Uri = UriConstants.NS_ONAMA_ONTOLOGY + "Narrative",
        SearchTerm = queryData.searchString
      }};

      if(queryData.types != null && queryData.types.Count() > 0)
      {
        typeQueryTerms = new List<QueryTerm> ();
        foreach(var t in queryData.types)
        {
          typeQueryTerms.Add(new QueryTerm {
            Uri = t.ToString(),
            SearchTerm = queryData.searchString
          });
        }
      }
      foreach(var qt in typeQueryTerms)
      {
        if(!string.IsNullOrWhiteSpace(queryData.searchString))
        {
          classQueryStrings.Add(CreateClassSubQuery(qt));
        } 
        else
        {
          classQueryStrings.Add(CreateSubQueryClassTriples(qt.Uri));
        }
      }
      // action and collection
      var classTermsQueryTerms = new Dictionary<string, List<string>>();
      foreach(var att in queryData.classTerms)
      {
        if(!classTermsQueryTerms.ContainsKey(att.classUri))
        {
          classTermsQueryTerms[att.classUri] = new List<string>();
        }
        switch(att.classUri)
        {
          case UriConstants.NS_ONAMA_ONTOLOGY + "Action":
            var templ = @"
?node onama:hasAction ?action .
?action onama:hasVerb <{0}> .
";
            foreach(var term in att.terms)
            {
              classTermsQueryTerms[att.classUri].Add(string.Format(templ, term.ToString()));
            }
            break;
          case UriConstants.NS_ONAMA_ONTOLOGY + "Collection":
            foreach(var term in att.terms)
            {
              classTermsQueryTerms[att.classUri].Add($"  ?node onama:partOfCollection <{term.ToString()}> .");
            }
            break;
        }
      }
      var entityQueryTerms = new List<string>();
      foreach(var entity in queryData.entities)
      {
        var entityString = " ?node onama:hasSemanticRole ?semanticRole . ";
        if(entity.owlHierarchy != null)
        {
          var entityClassParts = await GetCheckedEntityClasses(entity.owlHierarchy, new List<Uri>());
          entityString += " { " + string.Join(" UNION ", entityClassParts.Select(s => string.Format(" {{ ?semanticRole a <{0}> . }} ", s.ToString()) + Environment.NewLine)) + " } ";
        }

        if(!string.IsNullOrWhiteSpace(entity.searchString)) 
        {
          var lt = @"
  ?semanticRole ?x ?semanticEntity .
  FILTER(STRSTARTS(STR(?x), '{0}')) .
  ?semanticEntity rdfs:label ?semanticLabel . 
  FILTER(regex(?semanticLabel, '{1}', 'i')) . 
  FILTER(lang(?semanticLabel) = '{2}') .
  ";
          entityString += string.Format(lt, UriConstants.NS_ONAMA_ONTOLOGY, entity.searchString, this.LanguageService.GetLanguage());
        }
        
        if(entity.entityFunction != null && !string.IsNullOrEmpty(entity.entityFunction.ToString()))
        {
          var ef = @"
  {{ 
    {{
      ?semanticRole onama:hasEntityFunction <{1}> . 
    }}
    UNION 
    {{ 
      ?semanticRole ?ef ?semanticEntityFunction .
      FILTER(STRSTARTS(STR(?ef), '{0}')) .
      ?semanticEntityFunction onama:hasEntityFunction <{1}> . 
    }}
  }}
  ";
          entityString += string.Format(ef, UriConstants.NS_ONAMA_ONTOLOGY, entity.entityFunction.ToString());
        }
        if(entity.persistentItem != null && !string.IsNullOrEmpty(entity.persistentItem.ToString()))
        {
          var pies = @" 
  ?semanticRole ?srpi ?pi. 
  FILTER(STRSTARTS(STR(?srpi), '{0}')) .
  ?pi a <{1}> . 
  ";
          entityString += string.Format(pies, UriConstants.NS_ONAMA_ONTOLOGY, entity.persistentItem.ToString());
        }
        entityQueryTerms.Add(entityString);
      }

      response.query = "select * where { ";
      response.query += string.Join(" UNION ", classQueryStrings.Select(s => "{" + s + "} ")) + Environment.NewLine;
      foreach(var at in classTermsQueryTerms.Where(t => t.Value.Count > 0))
      {
        var classTermsQueryStrings = new List<string> ();
        response.query += " { " + string.Join(" UNION ", at.Value.Select(s => "{" + s + "} ")) + " } " + Environment.NewLine;
      }
      response.query += string.Join(" UNION ", entityQueryTerms.Select(s => "{" + s + "} ")) + Environment.NewLine;

      response.query += await GetOptionalNodesQuery();
      response.query += " } ORDER BY ?label ";

      await AddQueryPreamble();
      await SelectQuery();
      return response;
    }

    //
    // returns data for a simple "google" search
    // Depending on conifigured settings, this does either 
    // a "simple search in all things ONAMA" (SimpleSearchAll) or 
    // a search in predefined classes (SimpleSearchClasses)
    //
    // If no searchstring is given, several terms from PersistentItems will be chosen
    public async Task<QueryResponse> Search(string searchString, string language = null) 
    {
      ConnectGraphDb();

      response = new QueryResponse();
      this.LanguageService.SetLanguage(language);
      var searchStrings = new List<string>();
      if(string.IsNullOrEmpty(searchString))
      {
        searchStrings = await GetTermIFeelLucky();
        response.IsRandom = true;
      }
      else 
      {
        searchStrings.Add(searchString);
      }
      response.query = "select * where { ";
      if(!SimpleSearchAll)
      {
        var classParts = new List<string>();
        foreach(var searchClass in SimpleSearchClasses) {
          var qp = @" 
  {{ 
    ?node a onama:{0} . 
    ?node a ?classUri . 
    ?node rdfs:label ?label . 
  }} 
  ";
          var qs = string.Format(qp, searchClass);
          classParts.Add(qs);
        }
        response.query += string.Join(" UNION ", classParts) + Environment.NewLine;
        response.query += $"  FILTER(STRSTARTS(STR(?classUri), '{UriConstants.NS_ONAMA_ONTOLOGY}')) . " + Environment.NewLine;

      }
      else 
      {
        // Oder alles, was im onama-Namespace ist und einen Label hat
        // https://stackoverflow.com/questions/9597981/sparql-restricting-result-resource-to-certain-namespaces
        // TODO: Needs a class service to restrict to the most basic class, as the same Object will be found with several classes
        // according the the class hierarchy and we only want the most specific one.
        string pattern = @"
  ?node a ?classUri .
  FILTER(STRSTARTS(STR(?classUri), '{0}')) .
  ?node rdfs:label ?label . 
  ";
        response.query += string.Format(pattern, UriConstants.NS_ONAMA_ONTOLOGY);
      }

      // that's the same for all
      response.query += $"  FILTER(lang(?label) = '{this.LanguageService.GetLanguage()}') . " + Environment.NewLine;
      if(searchStrings.Count > 0) {
        var regexsrepl = string.Join(" || " + Environment.NewLine, searchStrings.Select(s => s.Replace("'", "\\'")).Select(s => $"regex(?label, '{s}', 'i')"));
        response.query += $"  FILTER({regexsrepl}) . ";
      }
      response.query += await GetOptionalNodesQuery();
      response.query += "} ORDER BY ?node ?label ";
      await AddQueryPreamble();
      // TODO we may actually need to aggregate the results by node and list the REO- and MHDBDB-URIs...
      await SelectQuery();
      return response;
    }

    //
    // returns data for a structured query
    public async Task<QueryResponse> Query(JsonQueryData queryData) 
    {
      ConnectGraphDb();
      response = new QueryResponse();
      var language = queryData != null ? queryData.language : null;
      this.LanguageService.SetLanguage(language);

      
      // if queryData is null/empty, we may want to return some "favorite" result sets.
      if(queryData == null || queryData.queryTerms == null || queryData.queryTerms.Count() == 0 ) 
      {
        var searchStrings = await GetTermIFeelLucky();
        queryData = new JsonQueryData() {
          language = this.LanguageService.GetLanguage(),
          queryTerms = searchStrings.Select(t => new QueryTerm() {
            SearchTerm = t, 
            SearchTermType = "class", 
            Uri = UriConstants.NS_ONAMA_ONTOLOGY + "PersistentItem"
          }).ToList()
        };
        response.IsRandom = true;
      } 

      response.query = ParseQueryTerms(queryData.queryTerms);
      await AddQueryPreamble();
      await SelectQuery();
      return response;
    }

    private async Task<List<string>> GetTermIFeelLucky()
    {
      var cacheId = $"{CACHE_ID_PERSISTENTITEMS_ENTRIES}_{this.LanguageService.GetLanguage()}";
      var persistentItemEntriesJson = Cache.Get(cacheId);
      if(persistentItemEntriesJson == null)
      {
        var structure = new StructureBuilder(Configuration, LanguageService,OwlService);
        var persistentItemEntriesList = await structure.GetClassEntries("PersistentItem");
        persistentItemEntriesJson = JsonSerializer.Serialize<List<string>>(persistentItemEntriesList.Select(p => p.Value.ToString()).ToList());
        Cache.Put(cacheId, persistentItemEntriesJson);
      }
      var persistentItemEntries = JsonSerializer.Deserialize<List<string>>(persistentItemEntriesJson);
      // remove trailing ", n.", ", m." and stuff
      var searchStrings = new List<string>();
      var entries = persistentItemEntries.OrderBy(i => Guid.NewGuid()).Take(QUERY_LUCKY_COUNT);
      foreach(var entry in entries)
      {
        var searchString = entry;
        if(searchString.Contains(',')) 
        {
          searchString = "";
          var splits = entry.Split(',').ToList();
          foreach(var s in splits.Take(splits.Count -1))
          {
            searchString += s;
          }
        }
        searchStrings.Add(searchString);
      }
        
      return searchStrings;
    }

    //
    // Fills Response data for a select query
    private async Task SelectQuery() 
    {
      response.message = "Query";

      // TODO: use ansynchronous method with callback
      try
      {
        var results = endpoint.QueryWithResultSet(response.query);
        if(results is SparqlResultSet)
        {
          // SparqlJsonWriter with a MemoryStream doesn't work and is not necessary
          response.columns = results.Variables.ToList();
          foreach(var result in results)
          {
            var r = new Dictionary<string, Object>();
            foreach(var c in response.columns) 
            {
              if(result.HasBoundValue(c)) {
                // they are all Literal Nodes...
                r[c] = result[c];
              }
            }
            response.results.Add(r);
          }
        }
      } 
      catch (Exception e)
      {
        Console.WriteLine(e);
      }
    }

    //
    // Returns a description of the given ID within the onama namespace 
    // by Getting all related nodes and their respective children with labels
    //
    // This returns the complete JSON response as a string
    public async Task<string> Describe(string id, string language = null) {
      this.LanguageService.SetLanguage(language);
      string jsonResponse = Cache.Get(id);
      if(jsonResponse == null)
      {
        response = new QueryResponse();
        IGraph graph = await GetDescribeGraph(id, true);

        if(graph != null && graph is Graph)
        {
          response.message = "Query";
          response.columns = new List<string> {"subject", "predicate", "object"};
          await FillDescribeResponse(graph.Triples);
        }
        // Use the Serializer from System.Text.Json to respect "JsonPropertyName" attributes
        jsonResponse = System.Text.Json.JsonSerializer.Serialize(new { response = response });
        Cache.Put(id, jsonResponse);
      }
      return jsonResponse;
    }

    public async Task<string> DescribeRaw(string id, string language = null) {
      this.LanguageService.SetLanguage(language);
      string cacheId = $"RDF-{id}";
      string rdf = Cache.Get(cacheId);
      if(rdf == null)
      {
        response = new QueryResponse();
        IGraph graph = await GetDescribeGraph(id, false);
        if(graph != null && graph is Graph)
        {
          response.message = "RDF";
          response.columns = new List<string> {"subject", "predicate", "object"};
          IRdfWriter rdfWriter = new Notation3Writer();
          var textWriter = new System.IO.StringWriter();
          graph.SaveToStream(textWriter, rdfWriter);
          rdf = textWriter.ToString();
          Cache.Put(cacheId, rdf);
        }
      }
      return rdf;
    }

    private async Task<IGraph> GetDescribeGraph(string id, bool doFilter = true)
    {
      IGraph graph = null;

      Uri uri = new Uri($"{UriConstants.NS_ONAMA_OBJECT}{id}");
      ConnectGraphDb();
      await CreateDescribeQueryChildren(id);
      string queryChildren = response.query;
      graph = await GraphQuery();
      if(doFilter)
      {
        graph = await FilterGraph((BaseGraph)graph);
      }
      await FillDetailGraph(graph, true);
      await CreateDescribeQueryParents(id);
      string queryParents = response.query;
      var graphParents = await GraphQuery();
      if(doFilter) 
      {
        graphParents = await FilterGraph((BaseGraph)graphParents);
      }
      graphParents.Retract(graph.Triples.Where(t => graph.Triples.Contains(t)));
      await FillDetailGraph(graphParents, false);
      response.query = JsonSerializer.Serialize(new[] { queryChildren, queryParents });
      graph.Merge(graphParents);
      await FillDetailNodes();
      return graph;
    }

    private async Task FillDetailGraph(IGraph graph, bool outgoing)
    {
      if(response.nodes == null)
      {
        response.nodes = new List<DetailNode>();
        nodes = new Dictionary<Uri, DetailNode>();
      }

      foreach(var triple in graph.Triples)
      {
        var nodeUri = ((UriNode)triple.Subject).Uri;
        if(!nodes.ContainsKey(nodeUri))
        {
          var node = new DetailNode {Uri = ((UriNode)triple.Subject).Uri};
          node.PredicateObjects = new List<NodePredicateObject>();
          node.IsOutgoing = outgoing;
          nodes.Add(nodeUri, node);
        }
        var nodePredicate = triple.Predicate.ToString();
        var nodeObject = triple.Object.ToString();
        var po = new NodePredicateObject { NodePredicate = nodePredicate, NodeObject = nodeObject};
        nodes[nodeUri].PredicateObjects.Add(po);
      }
    }

    private async Task FillDetailNodes()
    {
      if(nodes == null)
      {
        return;
      }
      foreach(var node in nodes.Values)
      {
        response.nodes.Add(node);
      }
    }

    private async Task<IGraph> FilterGraph(BaseGraph graph)
    {
      INode typeNode = graph.GetUriNode(new Uri(UriConstants.NS_RDF + "type"));
      // TODO: These should be made configurable
      var nameSpacesToKeep = new List<string> {UriConstants.NS_ONAMA_ONTOLOGY, UriConstants.NS_SKOS};
      var predicatesToKeep = new List<string> {UriConstants.NS_RDFS + "label", UriConstants.NS_RDF + "type"};
      var triples = graph.Triples.ToList();
      foreach(var ns in nameSpacesToKeep)
      {
        triples = triples.Where(t => !t.Predicate.ToString().StartsWith(ns)).ToList();
      }
      foreach(var p in predicatesToKeep)
      {
        triples = triples.Where(t => t.Predicate.ToString() != p).ToList();
      }

      graph.Retract(triples);
      
      triples = graph.Triples.Where(t => t.Object.ToString().StartsWith(UriConstants.NS_OWL)).ToList();
      graph.Retract(triples);
      // Return only the most specific class 
      // TODO: Or maybe we want to do that in the frontend to be able to show all classses?
      // TODO: Also think about moving that into the OwlService, so we can pass a list of Uris
      // and get them back in order of depth or something
      // Also, this could be solved more elegantly
      var unSpecificClassTriples = new List<Triple>();
      var typeTriples = graph.Triples.Where(t => t.HasPredicate(typeNode)).GroupBy(t => t.Subject).ToDictionary(c => c.Key, c => c.ToList());
      var classDepths = await OwlService.GetClassDepths();
      foreach(var t in typeTriples)
      {
        string mostSpecificUri = null;
        int depth = -1;
        foreach(var c in t.Value)
        {
          string u = ((UriNode)c.Object).Uri.ToString();
          int currentDepth = -1;
          if(classDepths.TryGetValue(u, out currentDepth) && currentDepth > depth ) 
          { 
            mostSpecificUri = u;
            depth = currentDepth;
          }
        }
        if(mostSpecificUri != null) 
        {
          foreach(var ut in t.Value.Where(c => ((UriNode)c.Object).Uri.ToString() != mostSpecificUri))
          {
            unSpecificClassTriples.Add(ut);
          }
        }

      }
      graph.Retract(unSpecificClassTriples);
      // keep only the most specific property for every subject (combined with object?)
      var unSpecificPropertyTriples = new List<Triple>();
      var propertyDepths = await OwlService.GetPropertyDepths();
      var subjectTriples = graph.Triples.Where(t => ((UriNode)t.Predicate).Uri.ToString().StartsWith(UriConstants.NS_ONAMA_ONTOLOGY)).GroupBy(t => t.Subject).ToDictionary(c => c.Key, c => c.ToList());
      foreach(var s in subjectTriples)
      {
        var objectTriples = s.Value.GroupBy(s => s.Object).ToDictionary(c => c.Key, c => c.ToList()).Where(c => c.Value.Count > 1);
        foreach(var o in objectTriples)
        {
          string mostSpecificUri = null;
          int depth = -1;
          foreach(var p in o.Value)
          {
            // do we need to check, whether entries are in the same tree (d.h. have the same top property)? Yes... -> TODO
            string u = ((UriNode)p.Predicate).Uri.ToString();
            int currentDepth = -1;
            if(propertyDepths.TryGetValue(u, out currentDepth) && currentDepth > depth ) 
            { 
              mostSpecificUri = u;
              depth = currentDepth;
            }
          }
          if(mostSpecificUri != null) 
          {
            foreach(var up in o.Value.Where(c => ((UriNode)c.Predicate).Uri.ToString() != mostSpecificUri))
            {
              unSpecificPropertyTriples.Add(up);
            }
          }
        }
      }
      graph.Retract(unSpecificPropertyTriples);

      return graph;

    }

    private async Task CreateDescribeQueryParents(string id)
    {
      // for certain connections (i.e. Verbs), the first intermediate node may not have a label
      string pattern = @"
PREFIX rdfs: <{2}> 
CONSTRUCT 
{{ 
  ?x ?xp <{1}{0}> . 
  ?x (rdfs:label) ?l .
  ?x (rdf:type) ?t .
  ?x1 ?xp1 ?x .
  ?x1 (rdfs:label) ?l1 .
  ?x1 (rdf:type) ?t1 .
}} where {{  
  ?x ?xp <{1}{0}> . 
  OPTIONAL
  {{
      ?x (rdfs:label) ?l .
      OPTIONAL {{
         ?x (rdf:type) ?t .
      }}
  }}
  OPTIONAL {{
      ?x (rdf:type) ?t .
  }}
  OPTIONAL 
  {{
      ?x1 ?xp1 ?x .   
      ?x1 (rdfs:label) ?l1 .
      OPTIONAL {{
         ?x1 (rdf:type) ?t1 .
      }}
  }}
}} ";
      response.query = string.Format(pattern, id, UriConstants.NS_ONAMA_OBJECT, UriConstants.NS_RDFS);
    }
    private async Task CreateDescribeQueryChildren(string id)
    {
      string pattern = @"
CONSTRUCT 
{{ 
  <{1}{0}> ?p ?o . 
  ?o ?op1 ?o1 .
  ?o1 ?op2 ?o2 .
}} where {{  
  <{1}{0}> ?p ?o . 
  OPTIONAL 
  {{
    ?o ?op1 ?o1 .        
    OPTIONAL 
    {{
      ?o1 ?op2 ?o2 .      
    }}
  }}
}} ";
      response.query = string.Format(pattern, id, UriConstants.NS_ONAMA_OBJECT);
    }

    private async Task FillDescribeResponse(BaseTripleCollection triples)
    {
      response.results = new List<Dictionary<string, object>>();
      foreach(var triple in triples)
      {
        var r = new Dictionary<string, Object>();
        r["subject"] = triple.Subject.ToString();
        r["predicate"] = triple.Predicate.ToString();
        r["object"] = triple.Object.ToString();
        response.results.Add(r);
      }
    }

    //
    // Fills the reponse data for the given construct query
    private async Task<IGraph> GraphQuery() {
      IGraph resultGraph = null;

      // TODO: use ansynchronous method with callback
      try
      {
        var g = endpoint.QueryWithResultGraph(response.query);
        if(g is Graph)
        {
          resultGraph = g;
        }
      } 
      catch (Exception e)
      {
        Console.WriteLine(e);
      }
      return resultGraph;
    }

    //
    // Recursively get the checked role classes as a flattened list
    // If a Role class is checked, its subclasses don't need to be checked anymore, as they are automatically part of the parent
    private async Task<List<Uri>> GetCheckedEntityClasses(List<OwlClassHierarchyEntity> owlHierarchy, List<Uri> checkedClasses)
    { 
      if(checkedClasses == null) 
      {
        checkedClasses = new List<Uri>();
      }
      foreach(var entityHierarchy in owlHierarchy)
      {
        if(entityHierarchy.isChecked)
        {
          checkedClasses.Add(entityHierarchy.Uri);
        }
        else 
        {
          checkedClasses = await GetCheckedEntityClasses(entityHierarchy.owlHierarchy, checkedClasses);
        }
      }
      return checkedClasses;
    }

    //
    // add namespace prefixes to query
    private async Task AddQueryPreamble()
    {
      string pattern = @"
PREFIX onama: <{0}>
PREFIX rdfs: <{1}>
";
      response.query = string.Format(pattern, UriConstants.NS_ONAMA_ONTOLOGY, UriConstants.NS_RDFS) + response.query;
    }
    //
    // add query for optional MHDBDB and REO nodes
    private async Task<string> GetOptionalNodesQuery()
    {
      return @"
  OPTIONAL 
  {
      ?node onama:depictedIn ?depictedin_uri . 
  }
  OPTIONAL 
  {
      ?node onama:hasRoWorkURL ?reo_uri . 
  }
  OPTIONAL 
  {
      ?node onama:hasTextPassage ?mhdbdb_uri . 
      ?mhdbdb_uri onama:fromToken ?mhdbdb_from .
      ?mhdbdb_uri onama:toToken ?mhdbdb_to .
  }
  ";
    }
    
    private string CreateSubQueryClassTriples(string uri)
    {
      var qp = @"
  ?node a <{0}> . 
  ?node a ?classUri . 
  ?node rdfs:label ?label . 
  FILTER(lang(?label) = '{1}') . 
  FILTER(STRSTARTS(STR(?classUri), '{2}')) .
  ";
      var queryString = string.Format(qp, uri, this.LanguageService.GetLanguage(), UriConstants.NS_ONAMA_ONTOLOGY);
      return queryString;
    }
    
    private string ParseQueryTerms(IEnumerable<QueryTerm> queryTerms)
    {
      var subQueryStrings = new List<string>();
      foreach(var queryTerm in queryTerms) {
        if(queryTerm.SearchTermType == SEARCHTERM_TYPE_CLASS) {
          var subQueryString = CreateClassSubQuery(queryTerm);
          subQueryStrings.Add("{ " + subQueryString + " }");
        }
        else if(queryTerm.SearchTermType == SEARCHTERM_TYPE_PROPERTY) {
          var subQueryString = CreatePropertySubQuery(queryTerm);
          subQueryStrings.Add("{ " + subQueryString + " }");
        }
      }
      string queryString = @"select * where { ";      
      queryString += String.Join(" UNION ", subQueryStrings.Select(s => " { " + s + " } "));
      queryString += " } ORDER BY ?node ?label ";

      return queryString;
    }

    private string CreateClassSubQuery(QueryTerm term) 
    {
      var qp = @"
  ?node a <{0}> . 
  ?node a ?classUri .
  OPTIONAL 
  {{
    ?node rdfs:label ?label .
  }}
  OPTIONAL 
  {{
    ?node ?nextLink ?nextNode.
    FILTER(STRSTARTS(STR(?nextLink), '{1}')) .
    ?nextNode rdfs:label ?label .
  }}
  FILTER(lang(?label) = '{2}') .
  FILTER(regex(str(?label), '{3}', 'i')) .
  FILTER(STRSTARTS(STR(?classUri), '{1}')) .
  ";
      var queryString = string.Format(qp, term.Uri, UriConstants.NS_ONAMA_ONTOLOGY, this.LanguageService.GetLanguage(), term.SearchTerm);
      return queryString;
    }

    private string CreatePropertySubQuery(QueryTerm term) 
    {
      var qp = @"
  ?node a <{0}> . 
  ?node a ?classUri .
  OPTIONAL 
  {{
    ?node rdfs:label ?label .
    FILTER(lang(?label) = '{3}') . 
  }}
  ?node <{1}> ?property .
  OPTIONAL 
  {{
    ?property rdfs:label ?propertyLabel
  }}
  OPTIONAL 
  {{
    ?property ?p1 ?o1 . 
    FILTER(STRSTARTS(STR(?p1), '{2}')) .
    ?o1 rdfs:label ?propertyLabel
  }}
  FILTER(lang(?propertyLabel) = '{3}') .
  FILTER(regex(str(?propertyLabel), '{4}', 'i')) .
  FILTER(STRSTARTS(STR(?classUri), '{2}')) .
  ";
      var queryString = string.Format(qp, term.SearchTermDomain, term.Uri, UriConstants.NS_ONAMA_ONTOLOGY, this.LanguageService.GetLanguage(), term.SearchTerm);
      return queryString;
    }
  }
}