using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace OnamaFrontendApi.Lib
{
  public class QueryTerm {
    [JsonPropertyName("uri")]
    public string Uri { get; set; }
    [JsonPropertyName("searchTerm")]
    public string SearchTerm { get; set; }
    [JsonPropertyName("type")]
    public string SearchTermType { get; set; } //  SEARCHTERM_TYPE_*
    [JsonPropertyName("domain")]
    public string SearchTermDomain { get; set; } // only for domains
  }
}