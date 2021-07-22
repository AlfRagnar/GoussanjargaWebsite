using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Goussanjarga.Services
{
    public interface IGraphService
    {
        Task<User> CurrentUserAsync();
    }
}
