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
using OnamaFrontendApi.Lib;
using Microsoft.Extensions.Configuration;

namespace OnamaFrontendApi.Controllers
{
  [Route("/api/v1/structure")]
  [ApiController]
  public class StructureController : ControllerBase
  {

    protected StructureBuilder Structure { get; set; }

    public StructureController(IConfiguration configuration, ILanguageService languageService, IOwlService owlService)
    {
      Structure = new StructureBuilder(configuration, languageService, owlService);
    }
    
    [Route("classentries")]
    public async Task<IActionResult> OnGetClassEntriesAsync(string className, string language = null)
    {
      var entries = await Structure.ClassEntriesGeneric(className, language);
      return Ok(entries);
    }

    [Route("full")]
    public async Task<IActionResult> OnGetFullAsync(string language = null)
    {
      var hierarchies = await Structure.StructureFull(language);
      string message = $"Graph Data for OwlFile";
      return Ok(new { message, classes = hierarchies });
    }

    [Route("subclasses")]
    public async Task<IActionResult> OnGetSubclassesAsync(string className, string language = null)
    {
      var hierarchies = await Structure.StructureSubclasses(className, language);
      string message = $"Subclass Data for OwlFile";
      return Ok(new { message, classes = hierarchies });
    }

    [Route("depth")]
    public async Task<IActionResult> OnGetDepthAsync()
    {
      return Ok(await Structure.Depths());
    }

    [Route("properties")]
    public async Task<IActionResult> OnGetProperties(string language = null)
    {
      var hierarchies = await Structure.StructureProperties(language);
      string message = $"Property Graph Data for OwlFile";
      return Ok(new { message, properties = hierarchies });
    }
  }
}
