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
    [RoutePrefix("service")]
    public class ServiceController : ApiController
    {
        [Route("{guid}")]
        [HttpGet]
        public ServiceInfo Get(string guid)
        {
            var registryRepo = new RegistryRepository(new RegistryDatabaseFactory());
            return registryRepo.GetService(guid);
        }


    }
}
