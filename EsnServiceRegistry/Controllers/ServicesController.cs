﻿using EsnCore.Registry;
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
            using (var registryRepo = new RegistryRepository(new RegistryDatabaseFactory()))
            {
                return registryRepo.AllServices(ServiceState.Running);
            }
        }

        [Route("{guid}")]
        [HttpGet]
        public ServiceInfo Get(string guid)
        {
            using (var registryRepo = new RegistryRepository(new RegistryDatabaseFactory()))
            {
                return registryRepo.GetService(guid);
            }
        }

        [Route("{guid}/edit")]
        [HttpPost]
        public void Edit(string guid, ServiceEditModel model)
        {
            var tagList = new List<string>();
            if (!string.IsNullOrEmpty(model.Tags))
            {
                tagList = model.Tags.Split(',').Select(t => t.Trim()).ToList();
            }

            using (var registryRepo = new RegistryRepository(new RegistryDatabaseFactory()))
            {
                registryRepo.UpdateServiceTags(guid, tagList);
            }
        }

        [Route("instances/{guid}")]
        [HttpGet]
        public List<ServiceInfo> GetInstances(string guid)
        {
            using (var registryRepo = new RegistryRepository(new RegistryDatabaseFactory()))
            {
                return registryRepo.GetServiceInstances(guid);
            }
        }

        [Route("cluster/{guid}/stats")]
        [HttpGet]
        public ServiceCluster GetClusterStats(string guid)
        {
            using (var registryRepo = new RegistryRepository(new RegistryDatabaseFactory()))
            {
                return registryRepo.GetServiceClusterInfo(guid);
            }
        }

        [Route("cluster/{guid}")]
        [HttpGet]
        public List<ServiceInfo> GetCluster(string guid)
        {
            using (var registryRepo = new RegistryRepository(new RegistryDatabaseFactory()))
            {
                return registryRepo.GetServiceCluster(guid);
            }
        }

        [Route("host/{guid}")]
        [HttpGet]
        public List<ServiceInfo> GetHostServices(string guid)
        {
            using (var registryRepo = new RegistryRepository(new RegistryDatabaseFactory()))
            {
                return registryRepo.AllHostServices(guid);
            }
        }

        [Route("issues")]
        [HttpGet]
        public List<ServiceInfo> GetServiceIssues()
        {
            using (var registryRepo = new RegistryRepository(new RegistryDatabaseFactory()))
            {
                return registryRepo.AllDisconnectServices();
            }
        }

        [Route("decommission/{guid}")]
        [HttpGet]
        public void DecommissionService(string guid)
        {
            using (var registryRepo = new RegistryRepository(new RegistryDatabaseFactory()))
            {
                registryRepo.UpdateServiceStatus(guid, ServiceState.Decommissioned);
            }
        }

    }
}
