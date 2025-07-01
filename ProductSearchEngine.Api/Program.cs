using ProductSearchEngine.Api.Configuration;
using ProductSearchEngine.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add OpenSearch
builder.Services.AddOpenSearch(builder.Configuration);

// Add application services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ISearchService, SearchService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Ensure OpenSearch index exists
using (var scope = app.Services.CreateScope())
{
    var client = scope.ServiceProvider.GetRequiredService<OpenSearch.Client.IOpenSearchClient>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    await OpenSearchConfiguration.EnsureIndexExists(client, logger);
}

app.Run();