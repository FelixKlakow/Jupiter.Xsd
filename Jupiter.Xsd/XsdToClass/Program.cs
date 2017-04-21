using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using CommandLine;

namespace XsdToClass
{
    /// <summary>
    /// Provides the entry point of the application.
    /// </summary>
    static class Program
    {
        #region #### CONSTANTS ##########################################################
        #endregion
        #region #### DEPENDENCY DECLARATIONS ############################################
        #endregion
        #region #### VARIABLES ##########################################################
        static readonly XmlSchemaSet LoadedSchemas = new XmlSchemaSet();
        static readonly Dictionary<String, XmlSchema> LoadedLocations = new Dictionary<String, XmlSchema>();
        #endregion
        #region #### PROPERTIES #########################################################
        #endregion
        #region #### EVENTS #############################################################
        #endregion
        #region #### CTOR ###############################################################

        #endregion
        #region #### PUBLIC #############################################################
        /// <summary>
        /// The entry point of the application.
        /// </summary>
        /// <param name="args">The application event args.</param>
        /// <returns>A indication if the operation has been sucessful.</returns>
        static Int32 Main(String[] args) => Parser.Default.ParseArguments<CommandLineOptions>(args).MapResult(o => ConvertXsd(o), e => -1);
        #endregion
        #region #### PRIVATE ############################################################
        /// <summary>
        /// Converts the content of the xsd into a class file.
        /// </summary>
        /// <param name="options">The command line options.</param>
        /// <returns>Retrieves the code which indicates a sucessful operation.</returns>
        private static Int32 ConvertXsd(CommandLineOptions options)
        {
            // Create a list with all files accepted by the converter and leave out any included classes which are not necessary
            Queue<XmlSchema> schemas = new Queue<XmlSchema>(options.InputFiles
                .FilterValidUris()
                .Select(LoadSchema)
                .Distinct());
            // All schemas which have been processed
            HashSet<XmlSchema> processedSchemas = new HashSet<XmlSchema>(schemas);

            // Compile schemas
            LoadedSchemas.Compile();

            SimpleClassFormatter formatter = null;
            if (options.SingleElementFiles)
            {
                formatter = new SimpleClassFormatter(Path.Combine(Environment.CurrentDirectory, String.Join("_", options.InputFiles.Select(p => Path.GetFileNameWithoutExtension(p))) + ".cs"));
                formatter.WriteCSharpHeader(options.TargetNamespace, options);
            }

            // Go through all schemas which should be exported
            while (schemas.Count > 0)
            {
                XmlSchema schema = schemas.Dequeue();

                if (!options.SingleElementFiles)
                {
                    formatter = new SimpleClassFormatter(Path.Combine(Environment.CurrentDirectory, Path.GetFileNameWithoutExtension(LoadedLocations.First(p => p.Value == schema).Key) + ".cs"));
                    formatter.WriteCSharpHeader(options.TargetNamespace, options);
                }

                foreach (XmlSchemaElement element in schema.Elements.Values)
                {
                    // Elements do not have 
                    if (element.SchemaType is XmlSchemaComplexType complex) formatter.WriteComplexType(complex, element.Name, options, schemas, processedSchemas);
                    else throw new NotImplementedException();
                }
                foreach (XmlSchemaType type in schema.SchemaTypes.Values)
                {
                    switch (type)
                    {
                        case XmlSchemaComplexType complex:
                            formatter.WriteComplexType(complex, null, options, schemas, processedSchemas);
                            break;
                        case XmlSchemaSimpleType simple:
                            formatter.WriteSimpleType(simple, options, schemas, processedSchemas);
                            break;
                    }
                }

                if (!options.SingleElementFiles) formatter.Dispose();
            }

            // Release file
            if (options.SingleElementFiles) formatter.Dispose();

            return 0;
        }

        /// <summary>
        /// Writes a simple type into the file.
        /// </summary>
        /// <param name="formatter"></param>
        /// <param name="type"></param>
        /// <param name="options"></param>
        /// <param name="schemas"></param>
        /// <param name="processedSchemas"></param>
        static void WriteSimpleType(this SimpleClassFormatter formatter, XmlSchemaSimpleType type, CommandLineOptions options, Queue<XmlSchema> schemas, HashSet<XmlSchema> processedSchemas)
        {
            throw new NotImplementedException();
        }
        static void WriteComplexType(this SimpleClassFormatter formatter, XmlSchemaComplexType type, String name, CommandLineOptions options, Queue<XmlSchema> schemas, HashSet<XmlSchema> processedSchemas)
        {
            String baseType = null;
            XmlSchemaComplexContentExtension extension = type.ContentModel?.Content as XmlSchemaComplexContentExtension;
            // Check if we've a extension
            if (extension != null)
            {
                // Set base type
                baseType = extension.BaseTypeName.Name.CleanName(options.CleanNames);
                // Add schema for processing
                XmlSchema schema = LoadedSchemas.Schemas().Cast<XmlSchema>().FirstOrDefault(p => p.SchemaTypes.Contains(extension.BaseTypeName));
                if (schema != null && processedSchemas.Add(schema)) schemas.Enqueue(schema);
            }

            // Open class definition
            if (options.DataContracts)
            {
                if (name == null) formatter.WriteAttribute(typeof(DataContractAttribute), new KeyValuePair<String, String>("Namespace", $"\"{GetSchema(type).TargetNamespace}\""));
                else formatter.WriteAttribute(typeof(DataContractAttribute), new KeyValuePair<String, String>("Name", $"\"{name}\""), new KeyValuePair<String, String>("Namespace", $"\"{GetSchema(type).TargetNamespace}\""));
            }
            formatter.BeginClass((name ?? type.Name).CleanName(options.CleanNames), baseType, isAbstract: type.IsAbstract);

            if ((type.Particle ?? extension.Particle) is XmlSchemaGroupBase sequence)
            {
                formatter.WriteSequence(sequence, options, schemas, processedSchemas);
            }
            else if (type.Particle != null) throw new NotImplementedException();

            // Close class definition
            formatter.CloseBracket();
        }
        static void WriteSequence(this SimpleClassFormatter formatter, XmlSchemaGroupBase sequence, CommandLineOptions options, Queue<XmlSchema> schemas, HashSet<XmlSchema> processedSchemas)
        {
            // Parse schema elements
            foreach (XmlSchemaElement sequenceItem in sequence.Items.OfType<XmlSchemaElement>().Where(p => p.ElementSchemaType != null))
                formatter.WriteMember(sequenceItem, options, schemas, processedSchemas);
            
            foreach (XmlSchemaGroupBase item in sequence.Items.OfType<XmlSchemaGroupBase>())
                formatter.WriteSequence(item, options, schemas, processedSchemas);
        }
        /// <summary>
        /// Writes a csharp header into the file.
        /// </summary>
        /// <param name="formatter">The formatter to use.</param>
        /// <param name="targetNamespace">The namespace for the class file.</param>
        /// <param name="options">Provides settings for the generation.</param>
        static void WriteCSharpHeader(this SimpleClassFormatter formatter, String targetNamespace, CommandLineOptions options)
        {
            formatter.UseNamespace(typeof(Int32));
            // Emit data contract namespace if required
            if (options.DataContracts) formatter.UseNamespace(typeof(DataMemberAttribute));
            formatter.BeginNamespace(targetNamespace);
        }
        /// <summary>
        /// Writes a member into the file.
        /// </summary>
        /// <param name="formatter">The formatter to use.</param>
        static void WriteMember(this SimpleClassFormatter formatter, XmlSchemaElement element, CommandLineOptions options, Queue<XmlSchema> schemas, HashSet<XmlSchema> processedSchemas)
        {
            String typeName = null;
            String memberName = element.Name;

            if (element.ElementSchemaType is XmlSchemaSimpleType simple) typeName = simple.Name ?? simple.Datatype.ValueType.Name;
            else if (element.ElementSchemaType is XmlSchemaComplexType complex)
            {
                typeName = (complex.Name ?? (complex.Parent as XmlSchemaElement).Name);
                // For complex types we could have a different name than the default name of the element
                if (memberName == null) memberName = typeName;
            }
            else throw new NotImplementedException();

            String cleanMemberName = memberName.CleanMemberName(options.CleanNames, formatter, options.RenamingPattern);
            String cleanTypeName = typeName.CleanMemberName(options.CleanNames, formatter, options.RenamingPattern);
            // Write data contract if enabled
            if (options.DataContracts && memberName != cleanMemberName)
            {
                formatter.WriteAttribute(typeof(DataMemberAttribute), new KeyValuePair<String, String>("Name", $"\"{memberName}\""));
            }

            if (element.MaxOccurs > 1) cleanTypeName = $"{cleanTypeName}[]";
            formatter.WriteAutoProperty(cleanTypeName, cleanMemberName);

            XmlSchema memberSchema = GetSchema(element.ElementSchemaType);
            if (memberSchema != null && processedSchemas.Add(memberSchema)) schemas.Enqueue(memberSchema);
        }
        /// <summary>
        /// Gets the root <see cref="XmlSchema"/> for the specified element.
        /// </summary>
        /// <param name="schemaObject">The schema object to get the root for.</param>
        /// <returns>The <see cref="XmlSchema"/> which is the root of the object.</returns>
        static XmlSchema GetSchema(XmlSchemaObject schemaObject)
        {
            if (schemaObject == null) return null;
            if (schemaObject.Parent is XmlSchema schema) return schema;
            return GetSchema(schemaObject.Parent);
        }
        /// <summary>
        /// Loads xml schemas from the specified uri.
        /// </summary>
        /// <param name="source">The uri to load the xml schmea from.</param>
        /// <returns>The <see cref="XmlSchema"/> which has been loaded.</returns>
        static XmlSchema LoadSchema(Uri source)
        {
            // Prevent duplicated loadings
            if (!LoadedLocations.TryGetValue(source.AbsoluteUri, out XmlSchema schema))
            {
                if (source.IsFile)
                {
                    // Load file content from local path
                    using (FileStream stream = new FileStream(source.LocalPath, FileMode.Open, FileAccess.Read))
                    {
                        schema = XmlSchema.Read(XmlReader.Create(stream, null, Path.GetDirectoryName(source.LocalPath)), null);
                    }
                }
                // Load file from remote location
                else
                {
                    throw new NotImplementedException();
                }

                // Add schema to set
                if (schema != null)
                {
                    LoadedLocations.Add(source.AbsoluteUri, schema);
                    LoadedSchemas.Add(schema);

                    // Try to resolve includes
                    foreach (XmlSchemaImport item in schema.Includes)
                    {
                        if (item.Schema == null)
                        {
                            // Create a path from the specified schema
                            if (ValidateUri(Path.Combine(item.SourceUri, item.SchemaLocation), null, out Uri uri))
                            {
                                // Load the missing schema
                                item.Schema = LoadSchema(uri);
                            }
                        }
                    }
                }
            }
            return schema;
        }
        /// <summary>
        /// Filters valid and reachable uris from a enumeration of strings.
        /// </summary>
        /// <param name="source">The source strings to convert into uris.</param>
        /// <returns>A enumeration with uris which are valid.</returns>
        static IEnumerable<Uri> FilterValidUris(this IEnumerable<String> source)
        {
            // Create a new http client which is used to check if http or https addresses are reachable
            using (HttpClient client = new HttpClient())
            {
                foreach (String item in source)
                {
                    if (ValidateUri(item, client, out Uri uri)) yield return uri;
                }
            }
        }
        /// <summary>
        /// Validates the source uri to have content behind.
        /// </summary>
        /// <param name="sourceUri">The source uri to check.</param>
        /// <param name="client">The <see cref="HttpClient"/> to use for web requests; can be null.</param>
        /// <param name="uri">The resulting uri.</param>
        /// <returns>True if the uri is valid; otherwise false.</returns>
        static Boolean ValidateUri(this String sourceUri, HttpClient client, out Uri uri)
        {
            // Try to create a uri from the given string
            if (Uri.TryCreate(sourceUri, UriKind.RelativeOrAbsolute, out uri))
            {
                Boolean localClient = false;
                Boolean isHttp = (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
                try
                {
                    if (isHttp && client == null)
                    {
                        localClient = true;
                        client = new HttpClient();
                    }

                    // Check if the uri is stored in a remote location
                    if ((isHttp && client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri)).Result.IsSuccessStatusCode) ||
                        // If the uri represents a file, check if the file exists
                        uri.IsFile && File.Exists(uri.LocalPath))
                    {
                        return true;
                    }

                }
                finally
                {
                    if (localClient)
                        client.Dispose();
                }
            }
            return false;
        }
        #endregion
        #region #### NESTED TYPES #######################################################
        #endregion
    }
}
