using System;
using System.Threading.Tasks;
using System.Net.Http;

namespace MyGeotabAPIAdapter.Helpers
{
    /// <summary>
    /// Interface for a helper class to assist in working with <see cref="HttpClient"/>s.
    /// </summary>
    public interface IHttpHelper
    {
        /// <summary>
        /// Downloads a file with the specified <paramref name="uri"/> and <paramref name="outputPath"/>.
        /// </summary>
        /// <param name="uri">A string representation of the <see cref="Uri"/> of the file to be downloaded.</param>
        /// <param name="outputPath">The desired filename, including path, of the file to be downloaded.</param>
        /// <returns></returns>
        Task DownloadFileAsync(string uri, string outputPath);
    }
}
