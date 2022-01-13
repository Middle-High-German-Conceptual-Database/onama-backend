using System;
using System.Collections.Generic;
using System.Linq;
using VDS.RDF;
using VDS.RDF.Ontology;
using VDS.RDF.Nodes;

namespace OnamaFrontendApi.Lib
{
  static class OntologyExtensions {
    public static bool IsDeprecated(this OntologyResource resource, INode deprecatedNode) 
    {
          var p = resource.Triples.WithPredicate(deprecatedNode).FirstOrDefault();
          return p != null && p.Object.AsValuedNode().AsSafeBoolean();
    }
  }

}