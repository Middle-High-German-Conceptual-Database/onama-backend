using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace OnamaFrontendApi.Lib
{
  public class JsonQueryData 
  {
    [JsonPropertyName("jsonquery")]
    public IEnumerable<QueryTerm> queryTerms { get; set; } 
    public List<OwlSearchHierarchy> tree { get; set; }
    public string searchString { get; set; } 
    public string language { get; set; }
  }

  public class JsonQueryDataNarrative : JsonQueryData
  {
    // list of types
    [JsonPropertyName("subclasses")]
    public List<Uri> types { get; set; }
    //public Object types { get; set; }
    //public Dictionary<Uri, Boolean> types { get; set; }

    // list of action / collection data
    [JsonPropertyName("attributes")]
    public List<JsonQueryDataUriList> classTerms { get; set; }

    // List of entities
    public List<JsonQueryDataNarrativeEntity> entities { get; set; }

    public Boolean  IsEmpty()
    {
      return string.IsNullOrWhiteSpace(searchString) && 
        (types == null || types.Count() == 0) && 
        (classTerms == null || classTerms.Count() == 0 || classTerms.All(ct => ct.terms == null || ct.terms.Count == 0)) && 
        (entities == null || entities.Count() == 0);
    }
  }

  public class JsonQueryDataUriList
  {
    public string classUri { get; set; }
    public List<Uri> terms { get; set; }
  }

  public class JsonQueryDataNarrativeEntity
  {
    public string searchString { get; set; } 
    [JsonPropertyName("narrativerole")]
    public Uri entityFunction { get; set; } 
    [JsonPropertyName("persistentitem")]
    public Uri persistentItem { get; set; } 
    public List<OwlClassHierarchyEntity> owlHierarchy { get; set; }
  }
  public class OwlClassHierarchyEntity 
  {
    public Uri Uri { get; set; }
    public List<OwlClassHierarchyEntity> owlHierarchy { get; set; } = new List<OwlClassHierarchyEntity>();
    [JsonPropertyName("checked")]
    public bool isChecked { get; set; }
  }

}