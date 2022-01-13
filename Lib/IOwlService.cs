using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnamaFrontendApi.Lib
{
    public interface IOwlService
    {
        Task<List<OwlClassHierarchy>> GetStructureInfo(string language);
        Task<List<OwlClassHierarchy>> GetSubclasses(string className, string language);
        Task<List<OwlPropertyHierarchy>> GetStructurePropertiesInfo(string language);
        Task<Dictionary<string, int>> GetClassDepths();
        Task<Dictionary<string, int>> GetPropertyDepths();
    }
}