using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Storage;
using VDS.RDF.Writing;
using VDS.RDF.Nodes;
using OnamaFrontendApi.Lib;
using Microsoft.Extensions.Configuration;

namespace OnamaFrontendApi.Lib 
{
    public abstract class QueryBase 
    {
        protected const string GRAPHDB_ENDPOINT = "http://onama-frontend-graphdb:7200/repositories/ONAMA";

        protected string endpointUrl { get; private set; }
        protected SparqlRemoteEndpoint endpoint { get; private set; }
        //protected SparqlConnector sparql { get; private set; }

        protected readonly IConfiguration Configuration;

        protected ILanguageService LanguageService;

        protected bool SimpleSearchAll { get; private set; } = true;
        protected List<string> SimpleSearchClasses { get; private set; } = new string[] {"Narrative", "Place", "SemanticRole", "Verb", "Person"}.ToList();

        public QueryBase(IConfiguration configuration, ILanguageService languageService)
        {
            this.Configuration = configuration;
            this.LanguageService = languageService;

            // TODO: see also https://weblog.west-wind.com/posts/2016/may/23/strongly-typed-configuration-settings-in-aspnet-core 
            this.endpointUrl = GRAPHDB_ENDPOINT;
            if(Configuration["Onama:GraphdbEndpoint"] != null) 
            {
                this.endpointUrl = Configuration["Onama:GraphdbEndpoint"];
            }

            if(Configuration["Onama:SimpleSearchAll"] != null) 
            {
                this.SimpleSearchAll = Configuration.GetValue<bool>("Onama:SimpleSearchAll");
            }
            if(!this.SimpleSearchAll && Configuration.GetSection("Onama:SimpleSearchClasses") != null) 
            {
                var ssc = Configuration.GetSection("Onama:SimpleSearchClasses").Get<List<string>>();
                if(ssc != null && ssc.Count > 0) 
                {
                    this.SimpleSearchClasses = ssc;
                }
            }
        }
        
        protected void ConnectGraphDb()
        {
            endpoint = new SparqlRemoteEndpoint(new Uri(endpointUrl));
            //sparql = new SparqlConnector(endpoint); // TODO: OBSOLETE?
        }
    }
}
