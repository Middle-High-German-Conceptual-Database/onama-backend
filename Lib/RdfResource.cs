using System;
using System.Collections.Generic;
using System.Linq;

namespace OnamaFrontendApi.Lib
{
  public abstract class RdfResource
  {
    public Uri Uri { get; set; }
    public string Label { get; set; }
    // this dictionary holds data in the form of language -> label
    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();

  }
}