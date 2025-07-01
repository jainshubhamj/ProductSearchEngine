# Product Search Engine with OpenSearch

A search engine implementation using OpenSearch backend and .NET 8 API with faceted search and AI-readiness.

## How to Run

1. **Start OpenSearch**:
   ```bash
   docker-compose up -d
   ```

2. **Run the application**:
   Open the terminal in "ProductSearchEngine.Api" folder
   ```bash
   dotnet run
   ```

3. **Test the API**:
   - Navigate to `https://localhost:7000/swagger` (or the port shown in console)
   - Upload sample data using the bulk endpoint
   - Test search functionality

## API Endpoints

### Product Management

#### POST /api/products
Create a single product
```json
{
  "title": "Product Name",
  "description": "Product description",
  "category": "Category",
  "price": 99.99,
  "brand": "Brand Name",
  "sku": "SKU123",
  "attributes": {
    "color": "Red",
    "size": "Large"
  },
  "tags": ["tag1", "tag2"]
}
```

#### POST /api/products/bulk
Bulk create products (accepts array of products)

#### GET /api/products/{id}
Get product by ID

#### PUT /api/products/{id}
Update product

#### DELETE /api/products/{id}
Delete product

### Search

#### POST /api/search
Advanced search with full request body:
```json
{
  "query": "nike shoes",
  "categories": ["Footwear"],
  "brands": ["Nike", "Adidas"],
  "minPrice": 50,
  "maxPrice": 200,
  "sortBy": "price_asc",
  "page": 1,
  "pageSize": 20,
  "includeFacets": true
}
```

#### GET /api/search
Simple search with query parameters:
```
GET /api/search?q=nike&categories=Footwear&sortBy=price_asc&page=1&pageSize=10
```

#### POST /api/search/suggestions
Get autocomplete suggestions:
```json
{
  "prefix": "nik",
  "size": 10
}
```

#### GET /api/search/suggestions
Get suggestions via query parameters:
```
GET /api/search/suggestions?prefix=nik&size=10
```

## Response Examples

### Search Response
```json
{
  "products": [
    {
      "id": "uuid",
      "title": "Nike Air Max 270",
      "description": "Revolutionary Air Max...",
      "category": "Footwear",
      "price": 150.00,
      "brand": "Nike",
      "sku": "AH8050-001",
      "attributes": {
        "size": "10",
        "color": "Black"
      },
      "tags": ["running", "casual"],
      "createdAt": "2024-01-01T00:00:00Z"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20,
  "facets": {
    "categories": [
      { "value": "Footwear", "count": 15 },
      { "value": "Electronics", "count": 8 }
    ],
    "brands": [
      { "value": "Nike", "count": 12 },
      { "value": "Adidas", "count": 8 }
    ],
    "price_ranges": [
      { "value": "0-25", "count": 5 },
      { "value": "25-50", "count": 10 }
    ]
  },
  "executionTimeMs": 45
}
```

### Suggestions Response
```json
{
  "suggestions": [
    "Nike Air Max",
    "Nike React",
    "Nike Dunk"
  ]
}
```

## Configuration

### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "OpenSearch.Client": "Information"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "OpenSearch": "http://localhost:9200"
  }
}
```

## Troubleshooting

### Common Issues

1. **OpenSearch connection failed**
   - Ensure OpenSearch is running: `docker ps`
   - Check logs: `docker logs opensearch`
   - Verify port 9200 is accessible: `curl http://localhost:9200`

2. **Index creation failed**
   - Check OpenSearch logs for detailed errors
   - Verify OpenSearch has sufficient memory
   - Ensure no existing index conflicts

3. **Search returns no results**
   - Verify data was indexed: Check OpenSearch Dashboards at `http://localhost:5601`
   - Check index mapping: `curl http://localhost:9200/products/_mapping`
   - Verify search syntax in logs

4. **Performance issues**
   - Monitor OpenSearch heap usage
   - Adjust JVM settings in docker-compose.yml
   - Consider adding more replicas for read-heavy workloads

### Health Checks

#### OpenSearch Dashboards
Check OpenSearch Dashboards at `http://localhost:5601`

#### OpenSearch Health
```bash
curl http://localhost:9200/_cluster/health?pretty
```

#### Index Statistics
```bash
curl http://localhost:9200/products/_stats?pretty
```

#### Sample Queries via cURL
```bash
# Simple search
curl -X GET "http://localhost:9200/products/_search?q=nike&pretty"

# Advanced search with filters
curl -X POST "http://localhost:9200/products/_search?pretty" \
  -H "Content-Type: application/json" \
  -d '{
    "query": {
      "bool": {
        "must": [
          {"multi_match": {"query": "running", "fields": ["title", "description"]}}
        ],
        "filter": [
          {"term": {"category": "Footwear"}},
          {"range": {"price": {"gte": 50, "lte": 200}}}
        ]
      }
    },
    "aggs": {
      "brands": {"terms": {"field": "brand"}},
      "categories": {"terms": {"field": "category"}}
    }
  }'
```

## Design Decisions

### Architecture Choices

1. **Clean Architecture**: Separation of concerns with distinct layers for controllers, services, and models
2. **Dependency Injection**: All services are registered in DI container for testability
3. **OpenSearch.Client**: Official client library provides type safety and optimal performance
4. **Async/Await**: All operations are asynchronous for better scalability
5. **Configuration**: Externalized configuration supports different environments

### Index Design

1. **Single Index**: Products stored in one index with proper mapping
2. **Field Types**: Optimized field types for search, filtering, and sorting
3. **Analyzers**: Standard analyzer with English stopwords for better search
4. **Suggestions**: Completion suggester with category context for better UX
5. **Future Fields**: Vector field included for AI embeddings

### Search Features

1. **Multi-field Search**: Searches across title, description, brand, and category with different weights
2. **Fuzzy Matching**: Handles typos with automatic fuzziness
3. **Faceted Search**: Dynamic facets with counts for categories, brands, and price ranges
4. **Sorting Options**: Multiple sort options including relevance and price
5. **Pagination**: Efficient pagination with configurable page sizes

## Future-Readiness for AI & NLP

### Architecture for AI Integration

The current implementation provides several extension points for AI/NLP capabilities:

#### 1. Query Processing Pipeline
```csharp
// Future: Natural Language Query Processor
public interface INaturalLanguageProcessor
{
    Task<SearchRequest> ProcessNaturalLanguageQuery(string naturalQuery);
}

// Example: "show me black sneakers under $100"
// Would be converted to structured SearchRequest
```

#### 2. Vector Embeddings Support
```csharp
// Already included in Product model
[DenseVector(Dimensions = 768)]
public float[]? EmbeddingVector { get; set; }

// Future: Semantic Search Service
public interface ISemanticSearchService
{
    Task<float[]> GenerateEmbedding(string text);
    Task<SearchResponse> SemanticSearch(string query, SearchRequest baseRequest);
}
```

#### 3. AI-Enhanced Ranking
```csharp
// Future: Personalized Ranking
public interface IPersonalizationService
{
    Task<SearchResponse> ApplyPersonalization(SearchResponse results, string userId);
    Task<SearchRequest> EnhanceWithUserPreferences(SearchRequest request, string userId);
}
```

#### 4. Dynamic Query Enhancement
```csharp
// Future: Query Understanding
public interface IQueryEnhancementService
{
    Task<SearchRequest> ExpandQuery(SearchRequest originalRequest);
    Task<List<string>> ExtractIntents(string query);
    Task<Dictionary<string, string>> ExtractEntities(string query);
}
```

### Implementation Roadmap

#### Phase 1: Natural Language Processing
1. Integrate with OpenAI or similar service for query understanding
2. Parse natural language queries into structured search requests
3. Handle entities like colors, sizes, brands, price ranges
4. Support complex queries like "red Nike shoes under $150"

#### Phase 2: Semantic Search
1. Generate embeddings for product descriptions using models like sentence-transformers
2. Store embeddings in OpenSearch vector fields
3. Implement hybrid search combining keyword and vector similarity
4. Add semantic similarity scoring to improve relevance

#### Phase 3: Personalization
1. Track user behavior and preferences
2. Build user profile embeddings
3. Implement collaborative filtering
4. Personalize search results and recommendations

#### Phase 4: Advanced AI Features
1. Auto-categorization of products using ML
2. Dynamic pricing insights
3. Trend analysis and recommendations
4. Visual search capabilities

### Extension Points in Current Code

1. **Service Interfaces**: Easy to extend with decorators or new implementations
2. **Query Building**: Modular query construction supports enhancement
3. **Response Processing**: Response pipeline can be extended with AI post-processing
4. **Configuration**: Settings support for AI service endpoints and parameters

This foundation provides a robust, scalable search engine that can evolve with AI advancements while maintaining performance and reliability.
   