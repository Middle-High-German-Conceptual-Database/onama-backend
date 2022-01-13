/*
An OwlSearchProperty may be part of a structured search tree
*/

namespace OnamaFrontendApi.Lib
{
  public class OwlSearchProperty : OwlResource
  {
    public OwlSearchProperty()
    {      
    }
    public string PropertySearchTerm { get; set; }
    public bool PropertySearchTermVisible { get; set; }
  }

}