using AuthApi.Models;
using AuthApi.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AuthApi.Services
{
    public class ProductService
    {
        private readonly IMongoCollection<Product> _products;

        public ProductService(IOptions<MongoDbSettings> mongoDbSettings)
        {
            var client = new MongoClient(mongoDbSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoDbSettings.Value.DatabaseName);
            _products = database.GetCollection<Product>("Products");
        }

        public async Task<List<Product>> GetAllAsync()
        {
            return await _products.Find(product => true).ToListAsync();
        }

        public async Task<Product> GetByIdAsync(string id)
        {
            return await _products.Find(product => product.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<Product>> GetByUserIdAsync(Guid userId)
        {
            return await _products.Find(product => product.UserId == userId).ToListAsync();
        }

        public async Task<Product> CreateAsync(Product product)
        {
            await _products.InsertOneAsync(product);
            return product;
        }

        public async Task<bool> UpdateAsync(string id, Product productIn)
        {
            var updateResult = await _products.ReplaceOneAsync(product => product.Id == id, productIn);
            return updateResult.IsAcknowledged && updateResult.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var deleteResult = await _products.DeleteOneAsync(product => product.Id == id);
            return deleteResult.IsAcknowledged && deleteResult.DeletedCount > 0;
        }
    }
}