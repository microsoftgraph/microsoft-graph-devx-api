using Microsoft.AspNetCore.Http.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Net;


namespace CodeSnippetsReflection
{
    /// <summary>
    /// This class will generate snippets code for c#
    /// </summary>
    public class SnippetsGeneratorCSharp
    {

        public string TestTypes()
        {
            string graphTargetAssemblyLocation = typeof(Microsoft.Graph.GraphServiceClient).Assembly.Location;

            Assembly assembly = Assembly.LoadFrom(graphTargetAssemblyLocation);
            Type[] types = assembly.GetTypes();


            StringBuilder sb = new StringBuilder();

            foreach(Type t in types)
            {
                if (t.Name == "Drive")
                {
                    var p = t.GetProperties();

                    sb.Append(t.Name + "\n");

                    foreach (PropertyInfo i in p)
                    {
                        sb.Append(i.Name + "\t");
                    }
                    break;
                }

            }

            return sb.ToString();
        }

        public string GenerateCode(string args)
        {
            string Code = ""; // this will be returned as the generated code

            // for reflection purposes we need to get the assembly location
            // this method also allows us to avoid hard coding the path
            string graphTargetAssemblyLocation = typeof(Microsoft.Graph.GraphServiceClient).Assembly.Location;

            List<InputSnippet> inputSnippetList = new List<InputSnippet>();

            try
            {
                string[] inputSnippets = args.Split(','); // get the request parameter e.g GET,/me/events
                inputSnippetList.Add(new InputSnippet(inputSnippets[0], inputSnippets[1]));

                foreach (InputSnippet s in inputSnippetList)
                {
                    string requestUrl = s.UrlToResource;
                    string httpVerb = s.HttpVerb;

                    // Breakdown the URL payload into parts. We will use this to look up types 
                    // that are used to build up the snippet.
                    requestUrl = requestUrl.Trim('/');
                    string[] requestUrlParts = requestUrl.Split('/');

                    // Where we store the snippet parts. We will use this to fill out the templates.
                    List<string> snippetParts = new List<string>();

                    // Get all of the types in the assembly.
                    Assembly assembly = Assembly.LoadFrom(graphTargetAssemblyLocation);
                    Type[] types = assembly.GetTypes();

                    // Create our list of types that are used to build up the code snippet. The typeChain list
                    // contains the request builder types used to create the input URL.
                    List<Type> typeChain = new List<Type>();
                    Type graphServiceClient = types.Where(t => t.Name == "GraphServiceClient")
                                                   .Select(t => t)
                                                   .First();

                    // Initial type in our chain.
                    typeChain.Add(graphServiceClient);

                    // Process the navigation properties to add to our type chain and snippet parts.
                    for (int i = 0; i < requestUrlParts.Length; i++)
                    {

                        //TODO
                        string propertyName = char.ToUpper(requestUrlParts[i][0]) + requestUrlParts[i].Substring(1);

                        Type propertyType = types.Where(t => t.Name == typeChain[i].Name)
                                                 .SelectMany(pl => pl.GetProperties()) // Have all the properties
                                                 .Where(p => p.Name == propertyName) // Get property based on Url parts.
                                                 .Select(v => v.PropertyType).First();

                        typeChain.Add(propertyType);

                        //TODO
                        snippetParts.Add("." + char.ToUpper(requestUrlParts[i][0]) + requestUrlParts[i].Substring(1));
                    }

                    // Once we get to the end of the property typeChain, we need to select the Request() method return type.
                    Type lastType = typeChain[typeChain.Count - 1];
                    MethodInfo[] lastTypeMethods = lastType.GetMethods();
                    MethodInfo defaultRequestMethod = lastTypeMethods.Where(m => m.Name == "Request")
                                                                     .Select(m => m)
                                                                     .First();

                    // Now we have the *Request object. Let's first add the .Request() segment to our
                    // snippetParts List.
                    snippetParts.Add(".Request()");

                    // Now we need to match the input HTTP verb with the right method.
                    Type requestObject = defaultRequestMethod.ReturnType;
                    MethodInfo[] requestMethods = requestObject.GetMethods();

                    // Collections we add to instead of using the http verb.
                    if (requestObject.Name.Contains("CollectionRequest") && httpVerb != "Get")
                    {
                        httpVerb = "ADD";
                        httpVerb = httpVerb.ToLower();
                        httpVerb = InputSnippet.UppercaseFirstLetter(httpVerb);
                        //httpVerb = char.ToUpper(httpVerb[0]) + httpVerb.Substring(1); // TODO: refactor this out into a separate method.
                    }


                    MethodInfo targetHttpMethod = requestMethods.Where(m => m.Name.Contains(httpVerb))
                                                           .Select(m => m)
                                                           .First();

                    // Now we add the Http verb method to the snippetParts list.
                    snippetParts.Add("." + targetHttpMethod.Name + "()");

                    // Set the method return type. We will need to extract it.
                    string methodReturnType = targetHttpMethod.ReturnType.FullName;
                    string entryPoint = "Microsoft.Graph.";

                    int start = methodReturnType.IndexOf(entryPoint);
                    start = start + entryPoint.Length;
                    int end = methodReturnType.IndexOf(',');
                    methodReturnType = methodReturnType.Substring(start, end - start);

                    // Construct the code snippet.
                    // Templates for filling out snippets.
                    string commonSnippet = "GraphServiceClient graphClient = new GraphServiceClient();";
                    string snippetTemplate = "{0} {1} = await graphClient{2}";
                    string snippetRequestBuilders = "";

                    foreach (string sp in snippetParts)
                    {
                        snippetRequestBuilders = snippetRequestBuilders + sp;
                    }

                    string snippet = String.Format(snippetTemplate, methodReturnType, methodReturnType.ToLower(), snippetRequestBuilders);

                    return Code = commonSnippet + Environment.NewLine + snippet;

                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return Code;
        }       
    }
}
