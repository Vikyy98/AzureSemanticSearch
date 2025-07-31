using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using System.Text.Json;

public class Product
{
    [SimpleField(IsKey = true)]
    public string NodeID { get; set; }
    [SearchableField]
    public string Name { get; set; }
    [SearchableField]
    public string Description { get; set; }
}

class Program
{
    static async Task Main()
    {
        string endpoint = "";
        string apiKey = "";
        string indexName = "";
        string filePath = @"D:\SearchJson\ProductSearchTerms.json";

        var credential = new AzureKeyCredential(apiKey);
        var indexClient = new SearchIndexClient(new Uri(endpoint), credential);
        var searchClient = new SearchClient(new Uri(endpoint), indexName, credential);

        //🔥 Delete existing index
        try
        {
            await indexClient.DeleteIndexAsync(indexName);
            Console.WriteLine($"🗑️ Deleted index: {indexName}");
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            Console.WriteLine("⚠️ Index does not exist. Proceeding to create a new one.");
        }

        // 🏗️ Recreate index with updated schema
        var fieldBuilder = new FieldBuilder();
        var searchFields = fieldBuilder.Build(typeof(Product));
        var definition = new SearchIndex(indexName, searchFields);
        await indexClient.CreateOrUpdateIndexAsync(definition);
        Console.WriteLine($"✅ Created index: {indexName}");

        // 📥 Load JSON data
        if (!File.Exists(filePath))
        {
            Console.WriteLine("❌ JSON file not found.");
            return;
        }

        var json = await File.ReadAllTextAsync(filePath);
        var products = JsonSerializer.Deserialize<List<Product>>(json);
        if (products == null || products.Count == 0)
        {
            Console.WriteLine("⚠️ No products found in JSON.");
            return;
        }

        // 🚀 Upload documents
        var batch = IndexDocumentsBatch.Create<Product>(products?.Select(p => IndexDocumentsAction.MergeOrUpload(p)).ToArray()); 
        await searchClient.IndexDocumentsAsync(batch);
        Console.WriteLine("📦 Products uploaded successfully.");

        // 🔍 Perform sample search
        string searchText = "formulation";
        var options = new SearchOptions { Size = 5, IncludeTotalCount = true };
        var results = await searchClient.SearchAsync<Product>(searchText, options);

        Console.WriteLine($"\n🔎 Found {results.Value.TotalCount} matching products:\n");
        foreach (SearchResult<Product> result in results.Value.GetResults())
        {
            var product = result.Document;
            Console.WriteLine($"👉 {product.Name}");
            Console.WriteLine($"   {product.Description}");
            Console.WriteLine($"   Score: {result.Score}");
            Console.WriteLine("-----");
        }
    }
}
