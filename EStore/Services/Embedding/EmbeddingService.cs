using EStore.Services.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace EStore.Services.Embedding;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text);
}

public class EmbeddingService(IEmbeddingGenerator<string, Embedding<float>> generator) : IEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator = generator;

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        Embedding<float> embedding = await _generator.GenerateAsync(text);
        
        // Convert the ReadOnlyMemory<float> collection straight to a float[] array
        return embedding.Vector.ToArray();
    }
}
