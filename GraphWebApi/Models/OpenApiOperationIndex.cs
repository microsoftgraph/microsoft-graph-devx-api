using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using System.Collections.Generic;

namespace GraphWebApi.Models
{
    /// <summary>
    /// Class to generate HTML page including index of all operations
    /// </summary>
    public class OpenApiOperationIndex : OpenApiVisitorBase
    {
        public Dictionary<OpenApiTag, List<OpenApiOperation>> Index = new Dictionary<OpenApiTag, List<OpenApiOperation>>();
        public override void Visit(OpenApiOperation operation)
        {
            foreach (var tag in operation.Tags)
            {
                AddToIndex(tag, operation);
            }
        }

        private void AddToIndex(OpenApiTag tag, OpenApiOperation operation)
        {
            List<OpenApiOperation> operations;
            if (!Index.TryGetValue(tag, out operations))
            {
                operations = new List<OpenApiOperation>();
                Index[tag] = operations;
            }

            operations.Add(operation);

        }
    }

}
