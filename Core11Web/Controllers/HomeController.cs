using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nest;

namespace Core11Web.Controllers
{
    // this defines the schema
    public class Content
    {
        public int ContentId { get; set; }
        public DateTime PostDate { get; set; }
        public string ContentText { get; set; }
    }

    public class HomeController : Controller
    {
        public static Uri node;
        public static ConnectionSettings settings;
        public static ElasticClient client;

        // check out this handy site
        // https://hassantariqblog.wordpress.com/category/back-end-stuff/elastic-search/

        // and the .net NEST lib is in.  https://github.com/elastic/elasticsearch-net
        private void TestElasticSearch()
        {
            node = new Uri("http://localhost:9200");
            settings = new ConnectionSettings(node);
            settings.DefaultIndex("contentidx");
            client = new ElasticClient(settings);

            // Mapping for indexes
            var indexSettings = new IndexSettings();
            indexSettings.NumberOfReplicas = 1;
            indexSettings.NumberOfShards = 1;
            // Create properties from the Post class
            // https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/fluent-mapping.html

            // after creating this, you can issue
            // GET /myindex    in the command area.
            // https://www.elastic.co/guide/en/elasticsearch/reference/current/indices-get-index.html

            if (client.IndexExists("contentidx").Exists)
            {
                TestDeleteIndex();
            }
            var createIndexResponse = client.CreateIndex("contentidx", c => c
                    .Mappings(ms => ms
                        .Map<Content>(m => m
                            .AutoMap(typeof(Content))
                        )
                    )
            );
            // todo, check createIndexResponse

            TestInsert();

            TestTermQuery();

            TestMatchPhrase();

            TestFilter();
        }

        private void TestTermQuery()
        {
            var result = client.Search<Content>(s =>               
                s.From(0).Size(10000).Type("content").Query(q => q.Term(t=>t.ContentId, 2)));
            /*
            GET contentidx/content/_search
            {
              "query": {
                "match":{
                  "contentText":"Louis"
                }
              }
            }
             */

            string[] matchTerms =
            {
                "The quick",  // will find two entries.  Two with "the" and one with "quick"(but that has "the" as well with a score of 2)
                "Football",
                "Hockey",
                "Chicago Bears",
                "St. Louis"
            };

            // Match terms would come from what the user typed in
            foreach (var term in matchTerms)
            {
                result = client.Search<Content>(s =>
                   s
                   .From(0)
                   .Size(10000)
                   .Type("content")
                   .Query(q => q.Match(mq => mq.Field(f => f.ContentText).Query(term))));
                // print out the result.
            }
        }

        private void TestMatchPhrase()
        {
            // Exact phrase matching
            string[] matchPhrases =
            {
                "The quick",
                "Louis Blues",
                "Chicago Bears"                
            };

            // Match terms would come from what the user typed in
            foreach (var phrase in matchPhrases)
            {
                var result = client.Search<Content>(s =>
                   s
                   .From(0)
                   .Size(10000)
                   .Type("content")
                   .Query(q => q.MatchPhrase(mq => mq.Field(f => f.ContentText).Query(phrase))));
                // print out the result.
            }
        }

        private void TestFilter()
        {                       
            var result = client.Search<Content>(s =>
                s
                .From(0)
                .Size(10000)
                .Type("content")
                .Query(q => q
                    .Bool(b => b
                        .Filter(filter => filter.Range(m => m.Field(fld => fld.ContentId).GreaterThanOrEquals(4)))
                        )
                    ));
            // print out the result.            
        }

        private void TestInsert()
        {
            // Insert data

            string[] contentText =
            {
                "<p>Chicago Cubs Baseball</p>",
                "<html><body><p>St. Louis Cardinals Baseball</p></body></html>",
                "St. Louis Blues Hockey",
                "The Chicago Bears Football",
                "The quick fox jumped over the lazy dog"
            };

            int idx = 1;
            foreach (var text in contentText)
            { 
                var simulatedContentFromDB = new Content()
                {
                    ContentId = idx++,
                    PostDate = DateTime.Now,
                    ContentText = text
                };
                // this will insert
                // See https://hassantariqblog.wordpress.com/2016/09/21/elastic-search-insert-documents-in-index-using-nest-in-net/
                client.Index(simulatedContentFromDB, i => i.Index("contentidx"));
            }
            
            // To confirm you added data from "Content", you can type this in
            // GET contentindex/_search
        }

        public object TestDeleteIndex()
        {
            var response = client.DeleteIndex("contentidx");
            return response;
        }

        public IActionResult Index()
        {

            TestElasticSearch();
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
