using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace OnamaFrontendApi.Lib
{
  public class QueryResponse
  {
    public List<Dictionary<string, Object>> results { get; set; } =  new List<Dictionary<string, Object>>();
    public string message { get; set; }
    public string query {get; set; }
    public bool IsRandom {get; set; } = false;
    public List<string> columns { get; set; } = new List<string>();
    public List<DetailNode> nodes { get; set; }
  }

  public class DetailNode
  {
    [JsonPropertyName("uri")]
    public Uri Uri { get; set; }
    [JsonPropertyName("isOutgoing")]
    public bool IsOutgoing { get; set; } = true;
    [JsonPropertyName("predicateObjects")]
    public List<NodePredicateObject> PredicateObjects { get; set; }
  }

  public class NodePredicateObject
  {
    [JsonPropertyName("predicate")]
    public string NodePredicate { get; set; }
    [JsonPropertyName("object")]
    public string NodeObject { get; set; }
  }
}