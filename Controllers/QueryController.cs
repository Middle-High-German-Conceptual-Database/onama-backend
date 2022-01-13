using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;
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

namespace OnamaFrontendApi.Controllers
{
  [ApiController]
  public class QueryController : ControllerBase
  {

    protected QueryBuilder Query { get; set; }

    public QueryController(IConfiguration configuration, ILanguageService languageService, ICacheService cacheService, IOwlService owlService)
    {
      Query = new QueryBuilder(configuration, languageService, cacheService, owlService);
    }

    [HttpPost]
    [Route("/api/v1/register/narrative")]
    public async Task<IActionResult> OnPostRegisterNarrativeAsync([FromBody] JsonQueryDataNarrative queryData) {
      var response = await Query.RegisterNarrative(queryData);
      return Ok(new { response = response });
    }

    [HttpPost]
    [Route("/api/v1/search")]
    public async Task<IActionResult> OnPostSearchAsync([FromBody] JsonQueryData search) {
      return await OnGetSearchAsync(search.searchString, search.language);
    }

    [HttpGet]
    [Route("/api/v1/search")]
    public async Task<IActionResult> OnGetSearchAsync(string searchstring, string language = null) {
      QueryResponse response = await Query.Search(searchstring, language);
      return Ok(new { response = response });
    }

    [HttpPost]
    [Route("/api/v1/query")]
    public async Task<IActionResult> OnPostAsync([FromBody] JsonQueryData queryData) {
      QueryResponse response = await Query.Query(queryData);
      return Ok(new { response = response });
    }

    [Route("/api/v1/describe")]
    public async Task<IActionResult> OnGetDescribeAsync(string id, string language = null) {
      string jsonResponse = await Query.Describe(id, language);
      return Content(jsonResponse, "application/json", Encoding.UTF8);
    }
    [Route("/api/v1/rdfdescription")]
    public async Task<IActionResult> OnGetDescribeRawAsync(string id, string language = null) {
      string rdf = await Query.DescribeRaw(id, language);
      return Content(rdf, "text/n3", Encoding.UTF8);
    }
  }
}
