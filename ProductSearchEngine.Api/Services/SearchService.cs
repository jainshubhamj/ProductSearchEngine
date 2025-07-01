using OpenSearch.Client;
using ProductSearchEngine.Api.Models;

namespace ProductSearchEngine.Api.Services
{
    public class SearchService : ISearchService
    {
        private readonly IOpenSearchClient _client;
        private readonly ILogger<SearchService> _logger;
        private const string IndexName = "products";

        public SearchService(IOpenSearchClient client, ILogger<SearchService> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<SearchResponse> SearchProductsAsync(Models.SearchRequest request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var searchResponse = await _client.SearchAsync<Product>(s => s
                .Index(IndexName)
                .Query(q => BuildQuery(q, request))
                //.Sort(BuildSort(request.SortBy))
                .From((request.Page - 1) * request.PageSize)
                .Size(request.PageSize)
                //.Aggregations(BuildAggregations(request.IncludeFacets))
            );

            stopwatch.Stop();

            if (!searchResponse.IsValid)
            {
                _logger.LogError("Search failed: {Error}", searchResponse.DebugInformation);
                return new SearchResponse();
            }

            var response = new SearchResponse
            {
                Products = searchResponse.Documents.ToList(),
                TotalCount = searchResponse.Total,
                Page = request.Page,
                PageSize = request.PageSize,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };

            // Build facets
            if (request.IncludeFacets && searchResponse.Aggregations != null)
            {
                response.Facets = BuildFacetsFromAggregations(searchResponse.Aggregations);
            }

            return response;
        }

        public async Task<SuggestionResponse> GetSuggestionsAsync(Models.SuggestionRequest request)
        {
            var suggestResponse = await _client.SearchAsync<Product>(s => s
                .Index(IndexName)
                .Size(0)
                .Suggest(sg => sg
                    .Completion("product_suggest", c => c
                        .Field(f => f.Suggest)
                        .Prefix(request.Prefix)
                        .Size(request.Size)
                    )
                )
            );

            if (!suggestResponse.IsValid)
            {
                _logger.LogError("Suggestion failed: {Error}", suggestResponse.DebugInformation);
                return new SuggestionResponse();
            }

            var suggestions = suggestResponse.Suggest["product_suggest"]
                .SelectMany(s => s.Options)
                .Select(o => o.Text)
                .Distinct()
                .ToList();

            return new SuggestionResponse { Suggestions = suggestions };
        }

        private QueryContainer BuildQuery(QueryContainerDescriptor<Product> q, Models.SearchRequest request)
        {
            var queries = new List<QueryContainer>();

            // Main search query
            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                queries.Add(q.MultiMatch(m => m
                    .Fields(f => f
                        .Field(p => p.Title, 2.0)
                        .Field(p => p.Description, 1.0)
                        .Field(p => p.Brand, 1.5)
                        .Field(p => p.Category, 1.2)
                    )
                    .Query(request.Query)
                    .Type(TextQueryType.BestFields)
                    .Fuzziness(Fuzziness.Auto)
                ));
            }
            else
            {
                queries.Add(q.MatchAll());
            }

            // Filters
            var filters = new List<QueryContainer>();

            if (request.Categories?.Any() == true)
            {
                filters.Add(q.Terms(t => t.Field(f => f.Category).Terms(request.Categories)));
            }

            if (request.Brands?.Any() == true)
            {
                filters.Add(q.Terms(t => t.Field(f => f.Brand).Terms(request.Brands)));
            }

            if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
            {
                filters.Add(q.Range(r => r
                    .Field(f => f.Price)
                    .GreaterThanOrEquals((double?)request.MinPrice)
                    .LessThanOrEquals((double?)request.MaxPrice)
                ));
            }

            if (filters.Any())
            {
                return q.Bool(b => b
                    .Must(queries.ToArray())
                    .Filter(filters.ToArray())
                );
            }

            return q.Bool(b => b.Must(queries.ToArray()));
        }

        private IList<ISort> BuildSort(string sortBy)
        {
            return sortBy.ToLower() switch
            {
                "price_asc" => new List<ISort> { new FieldSort { Field = "price", Order = SortOrder.Ascending } },
                "price_desc" => new List<ISort> { new FieldSort { Field = "price", Order = SortOrder.Descending } },
                "title" => new List<ISort> { new FieldSort { Field = "title.keyword", Order = SortOrder.Ascending } },
                _ => new List<ISort> { new FieldSort { Field = "_score", Order = SortOrder.Descending } }
            };
        }

        private AggregationContainerDescriptor<Product> BuildAggregations(bool includeFacets)
        {
            if (!includeFacets)
                return new AggregationContainerDescriptor<Product>();

            return new AggregationContainerDescriptor<Product>()
                .Terms("categories", t => t.Field(f => f.Category).Size(50))
                .Terms("brands", t => t.Field(f => f.Brand).Size(50))
                .Range("price_ranges", r => r
                    .Field(f => f.Price)
                    .Ranges(
                        rng => rng.To(25),
                        rng => rng.From(25).To(50),
                        rng => rng.From(50).To(100),
                        rng => rng.From(100).To(200),
                        rng => rng.From(200)
                    )
                );
        }

        private Dictionary<string, List<FacetItem>> BuildFacetsFromAggregations(IReadOnlyDictionary<string, IAggregate> aggregations)
        {
            var facets = new Dictionary<string, List<FacetItem>>();

            if (aggregations.TryGetValue("categories", out var categoriesAgg) && categoriesAgg is BucketAggregate categoryBuckets)
            {
                facets["categories"] = categoryBuckets.Items
                    .Cast<KeyedBucket<object>>()
                    .Select(b => new FacetItem { Value = b.Key.ToString() ?? "", Count = b.DocCount ?? 0 })
                    .ToList();
            }

            if (aggregations.TryGetValue("brands", out var brandsAgg) && brandsAgg is BucketAggregate brandBuckets)
            {
                facets["brands"] = brandBuckets.Items
                    .Cast<KeyedBucket<object>>()
                    .Select(b => new FacetItem { Value = b.Key.ToString() ?? "", Count = b.DocCount ?? 0 })
                    .ToList();
            }

            if (aggregations.TryGetValue("price_ranges", out var priceAgg) && priceAgg is BucketAggregate priceBuckets)
            {
                facets["price_ranges"] = priceBuckets.Items
                    .Cast<RangeBucket>()
                    .Select(b => new FacetItem 
                    { 
                        Value = $"{b.From ?? 0}-{b.To ?? null}", 
                        Count = b.DocCount
                    })
                    .ToList();
            }

            return facets;
        }
    }
}