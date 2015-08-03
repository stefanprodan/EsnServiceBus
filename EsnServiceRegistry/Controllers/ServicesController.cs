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
    [RoutePrefix("services")]
    public class ServicesController : ApiController
    {
        [Route]
        [HttpGet]
        public List<ServiceInfo> GetAll()
        {
            var registryRepo = new RegistryRepository(new RegistryDatabaseFactory());
            return registryRepo.AllServices(ServiceState.Running);
        }

        [Route("{guid}")]
        [HttpGet]
        public ServiceInfo Get(string guid)
        {
            var registryRepo = new RegistryRepository(new RegistryDatabaseFactory());
            return registryRepo.GetService(guid);
        }

        [Route("instances/{guid}")]
        [HttpGet]
        public List<ServiceInfo> GetInstances(string guid)
        {
            var registryRepo = new RegistryRepository(new RegistryDatabaseFactory());
            return registryRepo.GetServiceInstances(guid);
        }

        [Route("cluster/{guid}")]
        [HttpGet]
        public List<ServiceInfo> GetCluster(string guid)
        {
            var registryRepo = new RegistryRepository(new RegistryDatabaseFactory());
            return registryRepo.GetServiceCluster(guid);
        }

        [Route("host/{guid}")]
        [HttpGet]
        public List<ServiceInfo> GetHostServices(string guid)
        {
            var registryRepo = new RegistryRepository(new RegistryDatabaseFactory());
            return registryRepo.AllHostServices(guid);
        }

        [Route("issues")]
        [HttpGet]
        public List<ServiceInfo> GetServiceIssues()
        {
            var registryRepo = new RegistryRepository(new RegistryDatabaseFactory());
            return registryRepo.AllDisconnectServices();
        }

        [Route("decommission/{guid}")]
        [HttpGet]
        public void DecommissionService(string guid)
        {
            var registryRepo = new RegistryRepository(new RegistryDatabaseFactory());
            registryRepo.UpdateServiceStatus(guid, ServiceState.Decommissioned);
        }

    }
}
