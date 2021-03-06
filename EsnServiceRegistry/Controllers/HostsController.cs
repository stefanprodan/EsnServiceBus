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
    [RoutePrefix("hosts")]
    public class HostsController : ApiController
    {
        [Route]
        [HttpGet]
        public List<HostInfo> GetAll()
        {
            using (var registryRepo = new RegistryRepository(new RegistryDatabaseFactory()))
            {
                return registryRepo.AllHosts();
            }
        }

        [Route("{guid}")]
        [HttpGet]
        public HostInfo Get(string guid)
        {
            using (var registryRepo = new RegistryRepository(new RegistryDatabaseFactory()))
            {
                return registryRepo.GetHost(guid);
            }
        }

        [Route("{guid}/edit")]
        [HttpPost]
        public void Edit(string guid, HostEditModel model)
        {
            var tagList = new List<string>();
            if(!string.IsNullOrEmpty(model.Tags))
            {
                tagList = model.Tags.Split(',').Select(t => t.Trim()).ToList();
            }

            using (var registryRepo = new RegistryRepository(new RegistryDatabaseFactory()))
            {
                registryRepo.UpdateHostTagsLocation(guid, model.Location, tagList);
            }
        }
    }
}
