using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace MapGame.Core.Utils.JSON
{
    public interface IJsonReader
    {
        T? Read<T>(string relativePath);
    }

    public class JsonReader : IJsonReader
    {
        private readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public T? Read<T>(string relativePath)
        {
            Uri fileUri = new(relativePath, UriKind.Relative);
            string jsonPath = fileUri.ToString();

            if (!File.Exists(jsonPath))
            {
                System.Diagnostics.Debug.WriteLine($"BŁĄD: Nie znaleziono pliku konfiguracyjnego w {jsonPath}");
                return default;
            }

            try
            {
                string json = File.ReadAllText(jsonPath);
                return JsonSerializer.Deserialize<T>(json, _options);
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"BŁĄD PARSOWANIA: {ex.Message}");
                return default;
            }
        }
    }
}
