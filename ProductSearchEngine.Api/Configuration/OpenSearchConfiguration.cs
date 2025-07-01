using OpenSearch.Client;
using ProductSearchEngine.Api.Models;

namespace ProductSearchEngine.Api.Configuration
{
    public static class OpenSearchConfiguration
    {
        public static IServiceCollection AddOpenSearch(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("OpenSearch") ?? "http://localhost:9200";
            
            var settings = new ConnectionSettings(new Uri(connectionString))
                .DefaultIndex("products")
                .DisableDirectStreaming()
                .EnableDebugMode()
                .PrettyJson()
                .RequestTimeout(TimeSpan.FromMinutes(2));
            
            var client = new OpenSearchClient(settings);
            
            services.AddSingleton<IOpenSearchClient>(client);
            
            return services;
        }
        
        public static async Task<bool> EnsureIndexExists(IOpenSearchClient client, ILogger logger)
        {
            const string indexName = "products";
            
            var indexExistsResponse = await client.Indices.ExistsAsync(indexName);
            
            if (!indexExistsResponse.Exists)
            {
                logger.LogInformation("Creating products index...");
                
                var createIndexResponse = await client.Indices.CreateAsync(indexName, c => c
                    .Settings(s => s
                        .NumberOfShards(1)
                        .NumberOfReplicas(0)
                        .Analysis(a => a
                            .Analyzers(an => an
                                .Standard("standard_analyzer", sa => sa
                                    .StopWords("_english_")
                                )
                            )
                        )
                    )
                    .Map<Product>(m => m
                        .AutoMap()
                        .Properties(p => p
                            .Text(t => t
                                .Name(n => n.Title)
                                .Analyzer("standard_analyzer")
                                .Fields(f => f
                                    .Keyword(k => k.Name("keyword"))
                                )
                            )
                            .Text(t => t
                                .Name(n => n.Description)
                                .Analyzer("standard_analyzer")
                            )
                            .Completion(c => c
                                .Name(n => n.Suggest)
                                .Contexts(ctx => ctx
                                    .Category(cat => cat
                                        .Name("category")
                                        .Path(p => p.Category)
                                    )
                                )
                            )
                        )
                    )
                );
                
                if (!createIndexResponse.IsValid)
                {
                    logger.LogError("Failed to create index: {Error}", createIndexResponse.DebugInformation);
                    return false;
                }
                
                logger.LogInformation("Products index created successfully");
            }
            
            return true;
        }
    }
}