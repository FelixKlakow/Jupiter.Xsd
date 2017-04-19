using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XsdToClass
{
    /// <summary>
    /// Represents a utility class for formatting a class in a simple way.
    /// </summary>
    public sealed class SimpleClassFormatter : IDisposable
    {
        #region #### VARIABLES ##########################################################
        readonly HashSet<String> _IncludedNamespaces = new HashSet<String>();
        String _Namespace;
        Stream _Output;
        StreamWriter _Writer;

        WriterLevel _Level = WriterLevel.Using;
        #endregion
        #region #### CTOR ###############################################################
        /// <summary>
        /// Creates a new instance of the <see cref="SimpleClassFormatter"/> class.
        /// </summary>
        /// <param name="outputFile">The file to output</param>
        public SimpleClassFormatter(String outputFile)
        {
            _Output = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
            _Writer = new StreamWriter(_Output);
        }
        #endregion
        #region #### PUBLIC #############################################################
        /// <summary>
        /// Creates a using statement for the specified type.
        /// </summary>
        /// <param name="namespaceOf">The type which namespace should be included.</param>
        public void UseNamespace(Type namespaceOf) => UseNamespace(namespaceOf.Namespace);
        /// <summary>
        /// Creates a using statement for the specified namespace.
        /// </summary>
        /// <param name="namespace">The namespace which should be included.</param>
        public void UseNamespace(String @namespace)
        {
            EnsureLevel(WriterLevel.Using);

            if (_IncludedNamespaces.Add(@namespace))
                _Writer.WriteLine($"using {@namespace};");
        }
        /// <summary>
        /// Sets the namespace of the current file.
        /// </summary>
        /// <param name="namespace">The namespace to set.</param>
        public void BeginNamespace(String @namespace)
        {
            EnsureLevel(WriterLevel.Using);

            // Writes the namespace
            _Writer.WriteLine();
            _Writer.WriteLine($"namespace {@namespace}");
            _Writer.WriteLine("{");
            // Set the namespace of the file
            _Namespace = @namespace;

            // Set writer level
            _Level = WriterLevel.Namespace;
        }
        /// <summary>
        /// Begins a new class definition.
        /// </summary>
        /// <param name="className">The name of the class.</param>
        /// <param name="baseClassName">The name of the base class.</param>
        /// <param name="isAbstract">Determines if the type is abstract.</param>
        public void BeginClass(String className, String baseClassName = null, Boolean isAbstract = false)
        {
            EnsureLevel(WriterLevel.Namespace);

            if (baseClassName != null)
            {
                baseClassName = $" : {baseClassName}";
            }

            _Writer.WriteLine($"\tpublic{(isAbstract ? " abstract" : String.Empty)} class {className}{baseClassName}");
            _Writer.WriteLine("\t{");

            _Level = WriterLevel.Type;
        }
        /// <summary>
        /// Writes a attribute for the next type.
        /// </summary>
        /// <param name="attributeType">The type of the attribute to submit.</param>
        /// <param name="parameters">The parameters of the attribute.</param>
        public void WriteAttribute(Type attributeType, params KeyValuePair<String, String>[] parameters)
        {
            if (_Level == WriterLevel.Using || _Level == WriterLevel.Member) throw new InvalidOperationException($"Writer level is '{_Level}' and is not valid for attributes.");

            const String AttributePostfix = "Attribute";
            String name = attributeType.Name;
            // Remove attribute postfix if existing
            if (name.EndsWith(AttributePostfix)) name = name.Substring(0, name.Length - AttributePostfix.Length);
            // Add namespace if not included in file
            if (!_IncludedNamespaces.Contains(attributeType.Namespace)) name = $"{attributeType.Namespace}.{name}";

            String parameterString = $"({String.Join(", ", parameters.Select(p => $"{p.Key} = {p.Value}"))})";
            _Writer.WriteLine($"{GetIntent()}[{name}{(parameters?.Length > 0 ? parameterString : String.Empty)}]");
        }
        /// <summary>
        /// Writes a public auto property.
        /// </summary>
        /// <param name="typeName">The type name of the property.</param>
        /// <param name="propertyName">The name of the property.</param>
        public void WriteAutoProperty(String typeName, String propertyName)
        {
            EnsureLevel(WriterLevel.Type);

            _Writer.WriteLine($"\t\tpublic {typeName} {propertyName} {{ get; set; }}");
        }
        /// <summary>
        /// Closes the current open bracket.
        /// </summary>
        public void CloseBracket()
        {
            _Level = _Level - 1;
            _Writer.WriteLine(GetIntent() + "}");
        }
        /// <summary>
        /// Releases unmanaged resources of the file.
        /// </summary>
        public void Dispose()
        {
            // Close all open brackets
            while (_Level >= WriterLevel.Namespace) CloseBracket();

            // Release the file
            _Writer.Flush();
            ((IDisposable)_Output).Dispose();
        }
        #endregion
        #region #### PRIVATE ############################################################
        String GetIntent() => new String('\t', (Int32)_Level);
        /// <summary>
        /// Ensures that the writer state matches the required level.
        /// </summary>
        /// <param name="requiredLevel">The level required.</param>
        void EnsureLevel(WriterLevel requiredLevel)
        {
            if (requiredLevel != _Level) throw new InvalidOperationException($"Writer level is '{_Level}' and does not match required level {requiredLevel}");
        }
        #endregion
        #region #### NESTED TYPES #######################################################
        enum WriterLevel
        {
            Using = 0,
            Namespace = 1,
            Type = 2,
            Member = 3
        }
        #endregion
    }

    /// <summary>
    /// Provides helpers for formatting a class file.
    /// </summary>
    static class FormattingHelpers
    {
        #region #### CONSTANTS ##########################################################
        static readonly Char[] SplitChars = new Char[] { '_', '-', '.' };
        #endregion
        #region #### VARIABLES ##########################################################
        #endregion
        #region #### PROPERTIES #########################################################
        #endregion
        #region #### PUBLIC #############################################################
        /// <summary>
        /// Cleans a string into a .NET comform one.
        /// </summary>
        /// <param name="value">The value to clean.</param>
        /// <param name="upperCase">Specifies if all characters should be upper case.</param>
        /// <returns>The value of the string.</returns>
        public static String CleanName(this String value, Boolean upperCase)
        {
            StringBuilder builder = new StringBuilder();
            // Remove unneccessary and forbidden chars
            foreach (String item in value.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries))
            {
                // Ensure first character is always upper-case
                if (upperCase)
                {
                    // Append only the first character
                    builder.Append(Char.ToUpper(item[0]));
                    // Append rest of the string
                    builder.Append(item.Substring(1));
                }
                else builder.Append(item);
            }
            return builder.ToString();
        }
        #endregion
        #region #### PRIVATE ############################################################
        #endregion
    }
}