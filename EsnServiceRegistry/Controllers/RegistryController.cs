using EsnCore.Registry;
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
    [RoutePrefix("registry")]
    public class RegistryController : ApiController
    {
        [Route("hosts")]
        [HttpGet]
        public List<HostInfo> GetHosts()
        {
            var registryRepo = new RegistryRepository(new RegistryDatabaseFactory());
            return registryRepo.AllHosts();
        }

        [Route("running")]
        [HttpGet]
        public List<ServiceInfo> GetServices()
        {
            var registryRepo = new RegistryRepository(new RegistryDatabaseFactory());
            return registryRepo.AllServices(ServiceState.Running);
        }

        [Route("active")]
        [HttpGet]
        public List<ServiceInfo> GetActiveServices()
        {
            var registryRepo = new RegistryRepository(new RegistryDatabaseFactory());
            return registryRepo.AllActiveServices(1);
        }

        [Route("ping")]
        [HttpGet]
        public HttpResponseMessage Ping()
        {
            var stringResponse = DateTime.UtcNow.ToString();

            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            resp.Content = new StringContent(stringResponse, Encoding.UTF8, "text/plain");
            return resp;
        }

        [Route("status")]
        [HttpGet]
        public ServiceInfo Status()
        {
            return ServiceInfoFactory.CreateServiceDefinition(new ServiceInfo { Port = Convert.ToInt32(ServiceConfig.Reader.Port) });
        }
    }
}
