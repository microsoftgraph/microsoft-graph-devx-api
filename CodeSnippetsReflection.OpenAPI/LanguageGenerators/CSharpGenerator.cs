using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using CodeSnippetsReflection.OpenAPI.ModelGraph;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators {
	public class CSharpGenerator : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
	{
		private const string ClientVarName = "graphClient";
		private const string ClientVarType = "GraphServiceClient";
		private const string HttpCoreVarName = "requestAdapter";
        private const string RequestHeadersPropertyName = "Headers";
        private const string RequestConfigurationVarName = "requestConfiguration";
        private const string RequestBodyVarName = "requestBody";
        private const string RequestParametersPropertyName = "QueryParameters";
        
		public string GenerateCodeSnippet(SnippetModel snippetModel)
		{
			var indentManager = new IndentManager();
            var codeGraph = new SnippetCodeGraph(snippetModel);
			var snippetBuilder = new StringBuilder(
									"//THIS SNIPPET IS A PREVIEW FOR THE KIOTA BASED SDK. NON-PRODUCTION USE ONLY" + Environment.NewLine +
									$"var {ClientVarName} = new {ClientVarType}({HttpCoreVarName});{Environment.NewLine}{Environment.NewLine}");
			
            WriteRequestPayloadAndVariableName(codeGraph, snippetBuilder ,indentManager);
            WriteRequestExecutionPath(codeGraph, snippetBuilder, indentManager);
            
            return snippetBuilder.ToString();
		}
        
        private static void WriteRequestExecutionPath(SnippetCodeGraph codeGraph, StringBuilder payloadSb, IndentManager indentManager)
        {
            var responseAssignment = codeGraph.HasReturnedBody() ? "var result = " : string.Empty;
            var methodName = codeGraph.HttpMethod.Method.ToLower().ToFirstCharacterUpperCase() + "Async";
            var requestPayloadParameterName = codeGraph.HasBody() ? RequestBodyVarName : default;
            var requestConfigurationPayload = GetRequestConfiguration(codeGraph, indentManager);
            var parametersList = GetActionParametersList(requestPayloadParameterName , requestConfigurationPayload);
            payloadSb.AppendLine($"{responseAssignment}await {ClientVarName}.{GetFluentApiPath(codeGraph.Nodes)}.{methodName}({parametersList});");
        }
        
        
        private static string GetRequestConfiguration(SnippetCodeGraph snippetCodeGraph, IndentManager indentManager)
        {
            if (!snippetCodeGraph.HasHeaders()
                && !snippetCodeGraph.HasParameters())
                return default;
                
            var requestConfigurationBuilder = new StringBuilder();
            
            requestConfigurationBuilder.AppendLine($"({RequestConfigurationVarName}) =>");
            requestConfigurationBuilder.AppendLine($"{indentManager.GetIndent()}{{");
            WriteRequestQueryParameters(snippetCodeGraph, indentManager, requestConfigurationBuilder);
            WriteRequestHeaders(snippetCodeGraph, indentManager, requestConfigurationBuilder);
            requestConfigurationBuilder.Append($"{indentManager.GetIndent()}}}");
            
            return requestConfigurationBuilder.ToString();
        }

        private static void WriteRequestHeaders(SnippetCodeGraph snippetCodeGraph, IndentManager indentManager, StringBuilder stringBuilder) {
            if (!snippetCodeGraph.HasHeaders()) 
                return ;
            
            indentManager.Indent();
            foreach (var header in snippetCodeGraph.Headers)
            {
                stringBuilder.AppendLine(
                    $"{indentManager.GetIndent()}{RequestConfigurationVarName}.{RequestHeadersPropertyName}.Add(\"{header.Name}\", \"{header.Value.EscapeQuotes()}\");");
            }
			indentManager.Unindent();

		}
		private static string GetActionParametersList(params string[] parameters) {
			var nonEmptyParameters = parameters.Where(p => !string.IsNullOrEmpty(p));
			if(nonEmptyParameters.Any())
				return string.Join(", ", nonEmptyParameters.Aggregate((a, b) => $"{a}, {b}"));
            
            return string.Empty;
        }
		
		private static void WriteRequestQueryParameters(SnippetCodeGraph snippetCodeGraph, IndentManager indentManager, StringBuilder stringBuilder)
        {
            if (!snippetCodeGraph.HasParameters())
                return;
                
            indentManager.Indent();
            foreach(var queryParam in snippetCodeGraph.Parameters) {
                stringBuilder.AppendLine($"{indentManager.GetIndent()}{RequestConfigurationVarName}.{RequestParametersPropertyName}.{queryParam.Name.ToFirstCharacterUpperCase()} = {GetQueryParameterValue(queryParam)};");
            }
            indentManager.Unindent();
		}
        
		private static string GetQueryParameterValue(CodeProperty queryParam)
        {
            var queryParamValue = queryParam.Value;
            // boolean - true or false
            switch (queryParam.PropertyType)
            {
                case PropertyType.Boolean:
                    return queryParamValue.ToLowerInvariant(); // Boolean types

                case PropertyType.Int32:
                case PropertyType.Int64:
                case PropertyType.Double:
                case PropertyType.Float32:
                case PropertyType.Float64:
                    return queryParam.Value; // Numbers stay as is 

                case PropertyType.Array:
                    return $"new string []{{ {string.Join(",", queryParam.Children.Select(x =>  $"\"{x.Value}\"" ).ToList())} }}"; // deconstruct arrays

                default:
                    return $"\"{queryParam.Value.EscapeQuotes()}\"";
            }
        }
		
		private static void WriteRequestPayloadAndVariableName(SnippetCodeGraph snippetCodeGraph, StringBuilder snippetBuilder, IndentManager indentManager)
        {
            if (!snippetCodeGraph.HasBody())
                return;// No body
            
			if(indentManager == null) 
                throw new ArgumentNullException(nameof(indentManager));

			switch (snippetCodeGraph.Body.PropertyType) {
				case PropertyType.Object:
                    snippetBuilder.AppendLine($"var {RequestBodyVarName} = new {snippetCodeGraph.Body.Name.ToFirstCharacterUpperCase()}");
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}{{");
                    WriteCodePropertyObject(snippetCodeGraph.Body, snippetBuilder, indentManager);
                    snippetBuilder.AppendLine("};");
				    break;
                case PropertyType.Binary:
                    snippetBuilder.AppendLine($"using var {RequestBodyVarName} = new MemoryStream(); //stream to upload");
                    break;
				default:
					throw new InvalidOperationException($"Unsupported property type: {snippetCodeGraph.Body.PropertyType}");
			}
		}
        
		private static void WriteCodePropertyObject(CodeProperty codeProperty,StringBuilder snippetBuilder, IndentManager indentManager) {
            indentManager.Indent();
			indentManager.Unindent();
		}

        private static string GetNumberLiteral(OpenApiSchema schema, JsonElement value) {
			if(schema == default) return default;
			return schema.Type switch {
				"integer" when schema.Format.Equals("int64") => $"{value.GetInt64()}L",
				_ when schema.Format.Equals("float") => $"{value.GetDecimal()}f",
				_ when schema.Format.Equals("double") => $"{value.GetDouble()}d", //in MS Graph float & double are any of number, string and enum
				_ => value.GetInt32().ToString(),
			};
		}
		private static string GetFluentApiPath(IEnumerable<OpenApiUrlTreeNode> nodes) {
			if(!(nodes?.Any() ?? false)) return string.Empty;
			return nodes.Select(x => {
										if(x.Segment.IsCollectionIndex())
											return x.Segment.Replace("{", "[\"").Replace("}", "\"]");
										else if (x.Segment.IsFunction())
											return x.Segment.Split('.').Last().ToFirstCharacterUpperCase();
										return x.Segment.ToFirstCharacterUpperCase();
									})
						.Aggregate((x, y) => {
							var dot = y.StartsWith("[") ?
											string.Empty :
											".";
							return $"{x}{dot}{y}";
						});
		}
	}
}
