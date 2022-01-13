using System;
using System.Collections.Generic;
using System.Linq;
using OnamaFrontendApi.Lib;
using Microsoft.Extensions.Configuration;

namespace OnamaFrontendApi.Lib
{

  public class LanguageService : ILanguageService
  {
    /*
    protected string Language {get; set; } = "en";
    protected List<string> LanguageOrder { get; set; } = new string[] {"en", "de"}.ToList();
    */
    protected string Language {get; set; } = "de";
    protected List<string> LanguageOrder { get; set; } = new string[] {"de", "en"}.ToList();

    protected readonly IConfiguration Configuration;

    public LanguageService(IConfiguration configuration)
    {
        this.Configuration = configuration;
        Configure();
    }

    private void Configure()
    {
      if(Configuration["Onama:LanguageDefault"] != null)  
      {
          SetLanguage(Configuration["Onama:LanguageDefault"]);
      }
      
      if(Configuration.GetSection("Onama:LanguageOrder") != null)
      {
        var lo = Configuration.GetSection("Onama:LanguageOrder").Get<List<string>>();
        if(lo != null && lo.Count > 0) 
        {
          LanguageOrder = lo;
          SetLanguage(LanguageOrder[0]);
        }
      }
    }

    public void SetLanguage(string language)
    {
      if(!string.IsNullOrWhiteSpace(language))
      {
        this.Language = language;

        if(LanguageOrder[0] != language) 
        {
          if(LanguageOrder.Contains(language)) 
          {
            LanguageOrder.Remove(language);
          } 
          LanguageOrder.Insert(0, language);
        }
      }
    }

    public string GetLanguage()
    {
      return this.Language;
    }

    public List<string> GetLanguageOrder()
    {
      return LanguageOrder;
    }
  }
}
