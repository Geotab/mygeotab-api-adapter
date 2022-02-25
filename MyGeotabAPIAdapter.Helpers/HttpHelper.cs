using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Helpers
{
    /// <summary>
    /// A helper class to assist in working with <see cref="HttpClient"/>s.
    /// </summary>
    public class HttpHelper : IHttpHelper
    {
        readonly HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpHelper"/> class.
        /// </summary>
        public HttpHelper(HttpClient httpClient)
        { 
            this.httpClient = httpClient;
        }

        /// <inheritdoc/>
        public async Task DownloadFileAsync(string uri, string outputPath)
        {

            // Validate URI.
            if (!Uri.TryCreate(uri, UriKind.Absolute, out _))
            {
                throw new ArgumentException($"URI '{uri}' is invalid.");
            }

            // Download file.
            byte[] fileBytes = await httpClient.GetByteArrayAsync(uri);
            File.WriteAllBytes(outputPath, fileBytes);
        }
    }
}
