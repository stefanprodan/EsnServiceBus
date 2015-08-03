using EsnCore.Registry;
using EsnServiceRegistry.Attributes;
using EsnServiceRegistry.Models;
using EsnServiceRegistry.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace EsnServiceRegistry.Controllers
{
    [NoCache]
    [RoutePrefix("host")]
    public class HostController : ApiController
    {
        [Route("{guid}")]
        [HttpGet]
        public HostInfo Get(string guid)
        {
            var registryRepo = new RegistryRepository(new RegistryDatabaseFactory());
            return registryRepo.GetHost(guid, 60);
        }


    }
}
