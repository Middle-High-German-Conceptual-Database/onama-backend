using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnamaFrontendApi.Lib
{
    public interface IHierarchical
    {
        Task<object> GetHierarchy();
        Task<int> GetDepth();
        Task SetDepth(int depth);
        Task<Uri> GetUri();
    }
}