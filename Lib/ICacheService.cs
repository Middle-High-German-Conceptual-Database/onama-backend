using System;

namespace OnamaFrontendApi.Lib
{
  public interface ICacheService
  {
    string Get(string key);
    void Put(string key, string payload);
  }
}