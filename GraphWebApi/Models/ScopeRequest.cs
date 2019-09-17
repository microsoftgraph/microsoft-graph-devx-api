using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace GraphWebApi.Models
{
    /// <summary>
    /// Defines the model for a permission scope request.
    /// </summary>
    public class ScopeRequest
    {
        [Required(ErrorMessage = "The RequestUrl parameter is required in the query string.")]
        public string RequestUrl { get; set; }
        public string HttpVerb { get; set; } = "GET";
        public string ScopeType { get; set; } = "Application";     
    }
}
