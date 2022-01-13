using System;
using System.Collections.Generic;
using System.Linq;

namespace OnamaFrontendApi.Lib
{
  public interface ILanguageService
  {
    //void SetLanguageOrder();
    void SetLanguage(string language);
    string GetLanguage();
    List<string> GetLanguageOrder();
  }
}
